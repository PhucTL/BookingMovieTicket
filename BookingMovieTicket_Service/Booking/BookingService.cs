using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Booking.Request;
using BookingMovieTicket.Contracts.DTOs.Booking.Response;
using BookingMovieTicket.Contracts.Enums;
using BookingMovieTicket_Repository.Entities;
using BookingEntity = BookingMovieTicket_Repository.Entities.Booking;
using PaymentEntity = BookingMovieTicket_Repository.Entities.Payment;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Service.Authentication.Email;
using BookingMovieTicket_Service.Payment;
using BookingMovieTicket_Service.Realtime;
using BookingMovieTicket_Service.Redis;
using BookingMovieTicket_Service.Showtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Booking;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly IShowtimeService _showtimeService;
    private readonly ISeatNotificationService _seatNotificationService;
    private readonly IPayOSService _payOSService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork unitOfWork,
        IRedisService redisService,
        IShowtimeService showtimeService,
        ISeatNotificationService seatNotificationService,
        IPayOSService payOSService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _showtimeService = showtimeService;
        _seatNotificationService = seatNotificationService;
        _payOSService = payOSService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Giữ ghế tạm thời (5 phút)
    /// </summary>
    public async Task<ApiResponse<HoldSeatsResponse>> HoldSeatsAsync(Guid userId, HoldSeatsRequest request)
    {
        var distinctSeatIds = request.SeatIds.Distinct().ToList();
        if (distinctSeatIds.Count == 0)
        {
            return ApiResponse<HoldSeatsResponse>.ErrorResult("Vui lòng chọn ít nhất 1 ghế.");
        }

        var holdMinutes = 5;
        var now = DateTime.UtcNow;
        var heldUntil = now.AddMinutes(holdMinutes);

        var sortedSeatIds = distinctSeatIds.OrderBy(id => id).ToList();

        var acquiredLockKeys = new List<string>();
        var lockValue = $"{userId}:{Guid.NewGuid()}";
        var lockTimeout = TimeSpan.FromSeconds(10); 

        try
        {
            foreach (var seatId in sortedSeatIds)
            {
                var lockKey = $"lock:seat:{request.ShowtimeId}:{seatId}";
                var acquired = await _redisService.AcquireLockAsync(lockKey, lockValue, lockTimeout);

                if (!acquired)
                {
                    return ApiResponse<HoldSeatsResponse>.ErrorResult(
                        $"Ghế có mã ID '{seatId}' đang được người khác thao tác chọn. Vui lòng thử lại sau giây lát.");
                }

                acquiredLockKeys.Add(lockKey);
            }

            var seats = await _unitOfWork.ShowtimeRepository.GetShowtimeSeatsByIdsAsync(request.ShowtimeId, sortedSeatIds);

            if (seats.Count != sortedSeatIds.Count)
            {
                return ApiResponse<HoldSeatsResponse>.ErrorResult("Một hoặc nhiều ghế bạn chọn không tồn tại trong suất chiếu này.");
            }

            foreach (var seat in seats)
            {
                var isExpired = seat.HeldUntil.HasValue && seat.HeldUntil.Value <= now;

                if (seat.Status != (short)SeatStatus.Available && !(seat.Status == (short)SeatStatus.Held && isExpired))
                {
                    var seatLabel = seat.Seat != null ? $"{seat.Seat.RowLabel}{seat.Seat.Number}" : seat.Id.ToString();
                    return ApiResponse<HoldSeatsResponse>.ErrorResult(
                        $"Ghế {seatLabel} hiện không khả dụng để giữ (đã được giữ hoặc đã bán).");
                }
            }

            foreach (var seat in seats)
            {
                seat.Status = (short)SeatStatus.Held;
                seat.HeldByUserId = userId;
                seat.HeldUntil = heldUntil;
            }

            await _unitOfWork.ShowtimeRepository.UpdateShowtimeSeatsAsync(seats);
            await _unitOfWork.SaveChangesAsync();

            await _showtimeService.InvalidateSeatMapCacheAsync(request.ShowtimeId);

            await _seatNotificationService.NotifySeatsHeldAsync(request.ShowtimeId, userId, heldUntil, seats);

            var seatCodes = seats.Select(s => s.Seat != null ? $"{s.Seat.RowLabel}{s.Seat.Number}" : string.Empty).ToList();
            var totalPrice = seats.Sum(s => s.Price);

            var response = new HoldSeatsResponse
            {
                ShowtimeId = request.ShowtimeId,
                ShowtimeSeatIds = seats.Select(s => s.Id).ToList(),
                SeatCodes = seatCodes,
                TotalPrice = totalPrice,
                HeldUntil = heldUntil,
                ExpirySeconds = holdMinutes * 60,
                Message = $"Giữ ghế thành công! Vui lòng hoàn tất thanh toán trước {heldUntil.ToLocalTime():HH:mm:ss}."
            };

            return ApiResponse<HoldSeatsResponse>.SuccessResult(response, "Giữ ghế thành công.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối hạ tầng Redis khi giữ ghế.");
            return ApiResponse<HoldSeatsResponse>.ErrorResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi giữ ghế.");
            return ApiResponse<HoldSeatsResponse>.ErrorResult("Đã xảy ra lỗi khi giữ ghế. Vui lòng thử lại sau.");
        }
        finally
        {
            foreach (var lockKey in acquiredLockKeys)
            {
                await _redisService.ReleaseLockAsync(lockKey, lockValue);
            }
        }
    }

    /// <summary>
    /// Người dùng chủ động nhả ghế đã giữ
    /// </summary>
    public async Task<ApiResponse<string>> ReleaseSeatsAsync(Guid userId, ReleaseSeatsRequest request)
    {
        var distinctSeatIds = request.SeatIds.Distinct().ToList();
        if (distinctSeatIds.Count == 0)
        {
            return ApiResponse<string>.ErrorResult("Vui lòng chọn ít nhất 1 ghế cần nhả.");
        }

        var seats = await _unitOfWork.ShowtimeRepository.GetShowtimeSeatsByIdsAsync(request.ShowtimeId, distinctSeatIds);

        var myHeldSeats = seats.Where(s => s.HeldByUserId == userId && s.Status == (short)SeatStatus.Held).ToList();

        if (myHeldSeats.Count == 0)
        {
            return ApiResponse<string>.ErrorResult("Không tìm thấy ghế nào do bạn đang giữ để giải phóng.");
        }

        foreach (var seat in myHeldSeats)
        {
            seat.Status = (short)SeatStatus.Available;
            seat.HeldByUserId = null;
            seat.HeldUntil = null;
        }

        await _unitOfWork.ShowtimeRepository.UpdateShowtimeSeatsAsync(myHeldSeats);
        await _unitOfWork.SaveChangesAsync();

        await _showtimeService.InvalidateSeatMapCacheAsync(request.ShowtimeId);

        await _seatNotificationService.NotifySeatsReleasedAsync(request.ShowtimeId, myHeldSeats);

        return ApiResponse<string>.SuccessResult($"Đã nhả thành công {myHeldSeats.Count} ghế.", "Thành công.");
    }

    /// <summary>
    /// Tạo đơn đặt vé từ các ghế đang giữ và sinh link thanh toán PayOS VietQR
    /// </summary>
    public async Task<ApiResponse<CheckoutResponse>> CheckoutAsync(Guid userId, CheckoutRequest request)
    {
        try
        {
            var distinctSeatIds = request.SeatIds.Distinct().ToList();
            if (distinctSeatIds.Count == 0)
            {
                return ApiResponse<CheckoutResponse>.ErrorResult("Vui lòng chọn ít nhất 1 ghế để thanh toán.");
            }

            var seats = await _unitOfWork.ShowtimeRepository.GetShowtimeSeatsByIdsAsync(request.ShowtimeId, distinctSeatIds);
            if (seats.Count != distinctSeatIds.Count)
            {
                return ApiResponse<CheckoutResponse>.ErrorResult("Một số ghế bạn chọn không tồn tại trong suất chiếu này.");
            }

            var now = DateTime.UtcNow;
            var invalidSeats = seats.Where(s => s.HeldByUserId != userId || s.Status != (short)SeatStatus.Held || s.HeldUntil <= now).ToList();

            if (invalidSeats.Any())
            {
                return ApiResponse<CheckoutResponse>.ErrorResult("Phiên giữ ghế của bạn đã hết hạn (5 phút) hoặc ghế đang thuộc về người khác. Vui lòng chọn và giữ lại ghế.");
            }

            var showtime = await _unitOfWork.ShowtimeRepository.GetShowtimeWithDetailsAsync(request.ShowtimeId);
            if (showtime == null)
            {
                return ApiResponse<CheckoutResponse>.ErrorResult("Không tìm thấy thông tin suất chiếu.");
            }

            decimal totalAmount = seats.Sum(s => s.Price);

            long orderCode = long.Parse($"{DateTimeOffset.UtcNow:yyMMddHHmm}{Random.Shared.Next(10, 99)}");

            var booking = new BookingEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShowtimeId = request.ShowtimeId,
                Status = (short)BookingStatus.Pending,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                QrCode = $"TICKET-{orderCode}"
            };

            foreach (var seat in seats)
            {
                booking.BookingSeats.Add(new BookingSeat
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    ShowtimeSeatId = seat.Id,
                    PriceAtBooking = seat.Price
                });
            }

            var payment = new PaymentEntity
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Amount = totalAmount,
                Status = (short)PaymentStatus.Pending,
                Method = (short)request.PaymentMethod,
                TransactionRef = orderCode.ToString(),
                CreatedAt = DateTime.UtcNow
            };
            booking.Payment = payment;

            await _unitOfWork.BookingRepository.AddBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            var items = seats.Select(s => new PaymentLinkItem
            {
                Name = $"Ghe {s.Seat?.RowLabel}{s.Seat?.Number}",
                Quantity = 1,
                Price = (long)s.Price
            }).ToList();

            var eventTitle = showtime.Event?.Title ?? "Phim";
            var description = $"Ve {eventTitle}";
            if (description.Length > 25)
            {
                description = description.Substring(0, 25);
            }

            var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "https://localhost:7214/swagger";
            var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "https://localhost:7214/swagger";

            var payOSResult = await _payOSService.CreatePaymentLinkAsync(
                orderCode: orderCode,
                amount: (int)totalAmount,
                description: description,
                items: items,
                returnUrl: returnUrl,
                cancelUrl: cancelUrl
            );

            var seatCodes = seats.Select(s => s.Seat != null ? $"{s.Seat.RowLabel}{s.Seat.Number}" : string.Empty).ToList();

            var response = new CheckoutResponse
            {
                BookingId = booking.Id,
                OrderCode = orderCode,
                TotalAmount = totalAmount,
                Status = "PENDING",
                CheckoutUrl = payOSResult.CheckoutUrl,
                QrCode = payOSResult.QrCode,
                SeatCodes = seatCodes,
                Message = "Tạo đơn đặt vé thành công! Vui lòng quét mã QR hoặc truy cập link PayOS để thanh toán."
            };

            return ApiResponse<CheckoutResponse>.SuccessResult(response, "Khởi tạo thanh toán PayOS thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi Checkout đơn hàng.");
            return ApiResponse<CheckoutResponse>.ErrorResult("Đã xảy ra lỗi khi tạo đơn hàng thanh toán.");
        }
    }

    /// <summary>
    /// Xử lý Webhook từ PayOS khi khách hàng chuyển khoản thành công
    /// </summary>
    public async Task<ApiResponse<string>> HandlePayOSWebhookAsync(Webhook webhookBody)
    {
        try
        {
            var webhookData = await _payOSService.VerifyPaymentWebhookDataAsync(webhookBody);
            _logger.LogInformation("[PayOS Webhook] Nhận webhook hợp lệ cho OrderCode={OrderCode}, Code={Code}", webhookData.OrderCode, webhookBody.Code);

            var orderCodeStr = webhookData.OrderCode.ToString();
            var booking = await _unitOfWork.BookingRepository.GetBookingByOrderCodeAsync(orderCodeStr);
            if (booking == null)
            {
                _logger.LogWarning("[PayOS Webhook] Không tìm thấy Booking cho OrderCode={OrderCode}", orderCodeStr);
                return ApiResponse<string>.ErrorResult("Không tìm thấy đơn hàng tương ứng với OrderCode này.");
            }

            if (webhookBody.Code == "00")
            {
                await ConfirmBookingPaymentAsync(booking.Id);
            }

            return ApiResponse<string>.SuccessResult("Xử lý Webhook PayOS thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOS Webhook] Lỗi khi xử lý Webhook.");
            return ApiResponse<string>.ErrorResult("Xác thực hoặc xử lý Webhook PayOS thất bại.");
        }
    }

    /// <summary>
    /// Chủ động đồng bộ trạng thái thanh toán từ PayOS
    /// </summary>
    public async Task<ApiResponse<BookingDetailResponse>> SyncPayOSPaymentAsync(Guid bookingId)
    {
        try
        {
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
            if (booking == null)
            {
                return ApiResponse<BookingDetailResponse>.ErrorResult("Không tìm thấy đơn đặt vé.");
            }

            if (booking.Payment == null || string.IsNullOrWhiteSpace(booking.Payment.TransactionRef))
            {
                return ApiResponse<BookingDetailResponse>.ErrorResult("Đơn đặt vé chưa có thông tin giao dịch thanh toán.");
            }

            if (booking.Status == (short)BookingStatus.Confirmed)
            {
                var detailAlreadyConfirmed = MapToBookingDetailResponse(booking);
                return ApiResponse<BookingDetailResponse>.SuccessResult(detailAlreadyConfirmed, "Đơn đặt vé đã thanh toán thành công trước đó.");
            }

            if (long.TryParse(booking.Payment.TransactionRef, out var orderCode))
            {
                var paymentInfo = await _payOSService.GetPaymentLinkInformationAsync(orderCode);
                _logger.LogInformation("[Sync PayOS] Trạng thái PayOS của OrderCode={OrderCode} là {Status}", orderCode, paymentInfo.Status);

                if (paymentInfo.Status == PaymentLinkStatus.Paid)
                {
                    await ConfirmBookingPaymentAsync(booking.Id);
                    booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
                }
            }

            var detail = MapToBookingDetailResponse(booking!);
            return ApiResponse<BookingDetailResponse>.SuccessResult(detail, "Đồng bộ trạng thái thanh toán từ PayOS hoàn tất.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đồng bộ trạng thái thanh toán PayOS cho BookingId={BookingId}", bookingId);
            return ApiResponse<BookingDetailResponse>.ErrorResult("Đã xảy ra lỗi khi đồng bộ thanh toán.");
        }
    }

    /// <summary>
    /// Xác nhận thanh toán thành công và xuất vé điện tử cho khách hàng
    /// </summary>
    private async Task ConfirmBookingPaymentAsync(Guid bookingId)
    {
        var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
        if (booking == null || booking.Status == (short)BookingStatus.Confirmed)
        {
            return;
        }

        booking.Status = (short)BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;
        booking.QrCode = $"TICKET-{booking.Id.ToString().Substring(0, 8).ToUpper()}-{DateTime.UtcNow.Ticks % 100000}";

        if (booking.Payment != null)
        {
            booking.Payment.Status = (short)PaymentStatus.Success;
        }

        var showtimeSeatsToUpdate = new List<ShowtimeSeat>();
        foreach (var bs in booking.BookingSeats)
        {
            if (bs.ShowtimeSeat != null)
            {
                bs.ShowtimeSeat.Status = (short)SeatStatus.Booked;
                bs.ShowtimeSeat.HeldByUserId = null;
                bs.ShowtimeSeat.HeldUntil = null;
                showtimeSeatsToUpdate.Add(bs.ShowtimeSeat);
            }
        }

        if (showtimeSeatsToUpdate.Count > 0)
        {
            await _unitOfWork.ShowtimeRepository.UpdateShowtimeSeatsAsync(showtimeSeatsToUpdate);
        }

        await _unitOfWork.BookingRepository.UpdateBookingAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        await _showtimeService.InvalidateSeatMapCacheAsync(booking.ShowtimeId);

        await _seatNotificationService.NotifySeatsBookedAsync(booking.ShowtimeId, showtimeSeatsToUpdate);

        if (booking.User != null && !string.IsNullOrWhiteSpace(booking.User.Email))
        {
            var eventTitle = booking.Showtime?.Event?.Title ?? "Phim";
            var venueName = booking.Showtime?.SeatMap?.Venue?.Name ?? "Rạp chiếu phim";
            var startTime = booking.Showtime?.StartTime.ToLocalTime().ToString("HH:mm - dd/MM/yyyy") ?? string.Empty;
            var seatNames = string.Join(", ", showtimeSeatsToUpdate.Select(s => s.Seat != null ? $"{s.Seat.RowLabel}{s.Seat.Number}" : string.Empty));

            var emailBody = $@"
            <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                <h2 style=""color: #28a745; text-align: center;"">🎉 ĐẶT VÉ VÀ THANH TOÁN THÀNH CÔNG!</h2>
                <hr style=""border: 0; border-top: 1px solid #eee;"">
                <p>Kính chào <strong>{booking.User.FullName}</strong>,</p>
                <p>Cảm ơn bạn đã lựa chọn hệ thống đặt vé. Đây là thông tin vé xem phim điện tử của bạn:</p>
                <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                    <tr><td style=""padding: 8px; color: #555;"">Phim:</td><td style=""padding: 8px; font-weight: bold;"">{eventTitle}</td></tr>
                    <tr><td style=""padding: 8px; color: #555;"">Rạp:</td><td style=""padding: 8px; font-weight: bold;"">{venueName}</td></tr>
                    <tr><td style=""padding: 8px; color: #555;"">Suất chiếu:</td><td style=""padding: 8px; font-weight: bold;"">{startTime}</td></tr>
                    <tr><td style=""padding: 8px; color: #555;"">Ghế đã chọn:</td><td style=""padding: 8px; font-weight: bold; color: #e50914;"">{seatNames}</td></tr>
                    <tr><td style=""padding: 8px; color: #555;"">Tổng tiền:</td><td style=""padding: 8px; font-weight: bold;"">{booking.TotalAmount:N0} VNĐ</td></tr>
                    <tr><td style=""padding: 8px; color: #555;"">Mã vé (QR Code):</td><td style=""padding: 8px; font-weight: bold; color: #007bff;"">{booking.QrCode}</td></tr>
                </table>
                <div style=""text-align: center; margin: 20px 0; background: #f8f9fa; padding: 15px; border-radius: 6px;"">
                    <p style=""margin: 0; font-size: 14px; color: #555;"">Vui lòng xuất trình mã vé trên khi đến rạp để nhân viên hỗ trợ vào phòng chiếu.</p>
                </div>
                <hr style=""border: 0; border-top: 1px solid #eee;"">
                <p style=""color: #aaa; font-size: 12px; text-align: center;"">© 2026 Booking Movie Ticket System. Chúc bạn có trải nghiệm xem phim vui vẻ!</p>
            </div>";

            await _emailService.SendEmailAsync(booking.User.Email, $"[BookingMovieTicket] Xác nhận vé xem phim: {eventTitle}", emailBody);
        }

        _logger.LogInformation("Đã hoàn tất thanh toán và xuất vé cho BookingId={BookingId}", bookingId);
    }

    /// <summary>
    /// Khách hàng xem danh sách lịch sử các đơn đặt vé của chính mình
    /// </summary>
    public async Task<ApiResponse<List<BookingHistoryResponse>>> GetMyBookingsAsync(Guid userId)
    {
        try
        {
            var bookings = await _unitOfWork.BookingRepository.GetBookingsByUserIdAsync(userId);

            var response = bookings.Select(b => new BookingHistoryResponse
            {
                Id = b.Id,
                ShowtimeId = b.ShowtimeId,
                EventTitle = b.Showtime?.Event?.Title ?? string.Empty,
                PosterUrl = b.Showtime?.Event?.PosterUrl,
                VenueName = b.Showtime?.Event?.Venue?.Name ?? b.Showtime?.SeatMap?.Venue?.Name ?? string.Empty,
                SeatMapName = b.Showtime?.SeatMap?.Name ?? string.Empty,
                StartTime = b.Showtime?.StartTime ?? DateTime.MinValue,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                StatusName = Enum.IsDefined(typeof(BookingStatus), b.Status) ? ((BookingStatus)b.Status).ToString() : b.Status.ToString(),
                QrCode = b.QrCode,
                CreatedAt = b.CreatedAt,
                SeatCodes = b.BookingSeats?.Select(bs => bs.ShowtimeSeat?.Seat != null ? $"{bs.ShowtimeSeat.Seat.RowLabel}{bs.ShowtimeSeat.Seat.Number}" : string.Empty).ToList() ?? new()
            }).ToList();

            return ApiResponse<List<BookingHistoryResponse>>.SuccessResult(response, "Lấy lịch sử đặt vé thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy lịch sử đặt vé của UserId={UserId}", userId);
            return ApiResponse<List<BookingHistoryResponse>>.ErrorResult("Đã xảy ra lỗi khi lấy lịch sử đặt vé.");
        }
    }

    /// <summary>
    /// Xem thông tin chi tiết một vé đã đặt
    /// </summary>
    public async Task<ApiResponse<BookingDetailResponse>> GetBookingByIdAsync(Guid userId, Guid bookingId, bool isStaff = false)
    {
        try
        {
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(bookingId);
            if (booking == null)
            {
                return ApiResponse<BookingDetailResponse>.ErrorResult("Không tìm thấy đơn đặt vé.");
            }

            if (!isStaff && booking.UserId != userId)
            {
                return ApiResponse<BookingDetailResponse>.ErrorResult("Bạn không có quyền truy cập thông tin vé này.");
            }

            var response = MapToBookingDetailResponse(booking);
            return ApiResponse<BookingDetailResponse>.SuccessResult(response, "Lấy thông tin vé thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết vé BookingId={BookingId}", bookingId);
            return ApiResponse<BookingDetailResponse>.ErrorResult("Đã xảy ra lỗi khi lấy chi tiết vé.");
        }
    }

    /// <summary>
    /// Nhân viên soát vé / check-in vé tại rạp bằng mã QR hoặc TicketCode
    /// </summary>
    public async Task<ApiResponse<BookingDetailResponse>> CheckInTicketAsync(CheckInTicketRequest request)
    {
        try
        {
            var booking = await _unitOfWork.BookingRepository.GetBookingByQrCodeAsync(request.TicketCode);
            if (booking == null)
            {
                return ApiResponse<BookingDetailResponse>.ErrorResult("Không tìm thấy thông tin vé từ mã quét hoặc mã vé không hợp lệ.");
            }

            if (booking.Status == (short)BookingStatus.CheckedIn)
            {
                return ApiResponse<BookingDetailResponse>.ErrorResult(
                    $"Vé này đã được soát vé (check-in) trước đó vào lúc {booking.CheckedInAt?.ToLocalTime():HH:mm:ss dd/MM/yyyy}.");
            }

            if (booking.Status != (short)BookingStatus.Confirmed)
            {
                return ApiResponse<BookingDetailResponse>.ErrorResult("Vé này chưa hoàn tất thanh toán hoặc đã bị hủy, không đủ điều kiện vào rạp.");
            }

            booking.Status = (short)BookingStatus.CheckedIn;
            booking.CheckedInAt = DateTime.UtcNow;

            await _unitOfWork.BookingRepository.UpdateBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            var response = MapToBookingDetailResponse(booking);
            return ApiResponse<BookingDetailResponse>.SuccessResult(response, "Soát vé (Check-in) thành công! Quý khách đủ điều kiện vào phòng chiếu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thực hiện Check-in vé TicketCode={TicketCode}", request.TicketCode);
            return ApiResponse<BookingDetailResponse>.ErrorResult("Đã xảy ra lỗi khi soát vé.");
        }
    }

    private static BookingDetailResponse MapToBookingDetailResponse(BookingEntity booking)
    {
        var seats = booking.BookingSeats?.Select(bs => new BookingSeatItemDto
        {
            SeatId = bs.ShowtimeSeat?.SeatId ?? Guid.Empty,
            RowLabel = bs.ShowtimeSeat?.Seat?.RowLabel ?? string.Empty,
            Number = bs.ShowtimeSeat?.Seat?.Number ?? 0,
            Price = bs.PriceAtBooking,
            SeatType = bs.ShowtimeSeat?.Seat?.SeatType ?? 0,
            SeatTypeName = bs.ShowtimeSeat?.Seat != null && Enum.IsDefined(typeof(SeatType), bs.ShowtimeSeat.Seat.SeatType)
                ? ((SeatType)bs.ShowtimeSeat.Seat.SeatType).ToString()
                : "Standard"
        }).ToList() ?? new();

        BookingPaymentDto? paymentDto = null;
        if (booking.Payment != null)
        {
            paymentDto = new BookingPaymentDto
            {
                Amount = booking.Payment.Amount,
                Status = booking.Payment.Status,
                StatusName = Enum.IsDefined(typeof(PaymentStatus), booking.Payment.Status)
                    ? ((PaymentStatus)booking.Payment.Status).ToString()
                    : booking.Payment.Status.ToString(),
                Method = booking.Payment.Method,
                MethodName = Enum.IsDefined(typeof(PaymentMethod), booking.Payment.Method)
                    ? ((PaymentMethod)booking.Payment.Method).ToString()
                    : "PayOS",
                TransactionRef = booking.Payment.TransactionRef,
                CreatedAt = booking.Payment.CreatedAt
            };
        }

        return new BookingDetailResponse
        {
            Id = booking.Id,
            UserId = booking.UserId,
            UserEmail = booking.User?.Email ?? string.Empty,
            UserFullName = booking.User?.FullName ?? string.Empty,
            ShowtimeId = booking.ShowtimeId,
            EventTitle = booking.Showtime?.Event?.Title ?? string.Empty,
            PosterUrl = booking.Showtime?.Event?.PosterUrl,
            VenueName = booking.Showtime?.Event?.Venue?.Name ?? booking.Showtime?.SeatMap?.Venue?.Name ?? string.Empty,
            VenueAddress = booking.Showtime?.Event?.Venue?.Address ?? booking.Showtime?.SeatMap?.Venue?.Address,
            SeatMapName = booking.Showtime?.SeatMap?.Name ?? string.Empty,
            StartTime = booking.Showtime?.StartTime ?? DateTime.MinValue,
            EndTime = booking.Showtime?.EndTime ?? DateTime.MinValue,
            TotalAmount = booking.TotalAmount,
            Status = booking.Status,
            StatusName = Enum.IsDefined(typeof(BookingStatus), booking.Status)
                ? ((BookingStatus)booking.Status).ToString()
                : booking.Status.ToString(),
            QrCode = booking.QrCode,
            CreatedAt = booking.CreatedAt,
            ConfirmedAt = booking.ConfirmedAt,
            CheckedInAt = booking.CheckedInAt,
            Seats = seats,
            Payment = paymentDto
        };
    }
}
