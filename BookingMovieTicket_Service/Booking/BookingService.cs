using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Booking.Request;
using BookingMovieTicket.Contracts.DTOs.Booking.Response;
using BookingMovieTicket.Contracts.Enums;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Service.Realtime;
using BookingMovieTicket_Service.Redis;
using BookingMovieTicket_Service.Showtime;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork unitOfWork,
        IRedisService redisService,
        IShowtimeService showtimeService,
        ISeatNotificationService seatNotificationService,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _showtimeService = showtimeService;
        _seatNotificationService = seatNotificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<HoldSeatsResponse>> HoldSeatsAsync(Guid userId, HoldSeatsRequest request)
    {
        var distinctSeatIds = request.SeatIds.Distinct().ToList();
        if (distinctSeatIds.Count == 0)
        {
            return ApiResponse<HoldSeatsResponse>.ErrorResult("Vui lòng chọn ít nhất 1 ghế.");
        }

        if (distinctSeatIds.Count > 8)
        {
            return ApiResponse<HoldSeatsResponse>.ErrorResult("Mỗi lần giữ chỗ chỉ được chọn tối đa 8 ghế.");
        }

        // 1. Chuẩn bị chiếm Redis Distributed Lock cho từng ghế trong danh sách
        var lockValue = Guid.NewGuid().ToString();
        var acquiredLockKeys = new List<string>();
        var lockExpiry = TimeSpan.FromSeconds(5); // Khóa trong 5s để bảo vệ giao dịch DB

        try
        {
            foreach (var seatId in distinctSeatIds)
            {
                var lockKey = $"lock:showtime:{request.ShowtimeId}:seat:{seatId}";
                var acquired = await _redisService.AcquireLockAsync(lockKey, lockValue, lockExpiry);

                if (!acquired)
                {
                    _logger.LogWarning("Không thể chiếm lock cho ghế {SeatId} suất chiếu {ShowtimeId}", seatId, request.ShowtimeId);
                    return ApiResponse<HoldSeatsResponse>.ErrorResult(
                        "Một số ghế bạn chọn đang có người khác thao tác. Vui lòng thử lại sau giây lát.");
                }

                acquiredLockKeys.Add(lockKey);
            }

            // 2. Đã chiếm toàn bộ Lock -> Bắt đầu kiểm tra dữ liệu trong DB
            var seats = await _unitOfWork.ShowtimeRepository.GetShowtimeSeatsByIdsAsync(request.ShowtimeId, distinctSeatIds);

            if (seats.Count != distinctSeatIds.Count)
            {
                return ApiResponse<HoldSeatsResponse>.ErrorResult("Một số ghế bạn chọn không tồn tại trong suất chiếu này.");
            }

            var now = DateTime.UtcNow;

            // 3. Kiểm tra tính khả dụng của từng ghế
            foreach (var seat in seats)
            {
                var seatCode = seat.Seat != null ? $"{seat.Seat.RowLabel}{seat.Seat.Number}" : "này";

                if (seat.Status == (short)SeatStatus.Booked)
                {
                    return ApiResponse<HoldSeatsResponse>.ErrorResult($"Ghế {seatCode} đã được bán. Vui lòng chọn ghế khác.");
                }

                if (seat.Status == (short)SeatStatus.Disabled)
                {
                    return ApiResponse<HoldSeatsResponse>.ErrorResult($"Ghế {seatCode} hiện không mở bán.");
                }

                // Nếu ghế đang bị giữ bởi người khác và chưa hết hạn 5 phút
                var isHeldByOther = seat.Status == (short)SeatStatus.Held
                                    && seat.HeldUntil.HasValue
                                    && seat.HeldUntil.Value > now
                                    && seat.HeldByUserId != userId;

                if (isHeldByOther)
                {
                    return ApiResponse<HoldSeatsResponse>.ErrorResult($"Ghế {seatCode} đang được khách hàng khác giữ chỗ.");
                }
            }

            // 4. Tất cả ghế đều hợp lệ -> Chuyển sang trạng thái Held trong 5 phút
            var holdMinutes = 5;
            var heldUntil = now.AddMinutes(holdMinutes);

            foreach (var seat in seats)
            {
                seat.Status = (short)SeatStatus.Held;
                seat.HeldByUserId = userId;
                seat.HeldUntil = heldUntil;
            }

            await _unitOfWork.ShowtimeRepository.UpdateShowtimeSeatsAsync(seats);
            await _unitOfWork.SaveChangesAsync();

            // 5. Xóa cache sơ đồ ghế của suất chiếu trên Redis để client khác thấy cập nhật
            await _showtimeService.InvalidateSeatMapCacheAsync(request.ShowtimeId);

            // 6. Bắn thông báo real-time qua SignalR cho tất cả client đang xem suất chiếu này
            await _seatNotificationService.NotifySeatsHeldAsync(request.ShowtimeId, userId, heldUntil, seats);

            var seatCodes = seats.Select(s => s.Seat != null ? $"{s.Seat.RowLabel}{s.Seat.Number}" : string.Empty)
                                 .Where(c => !string.IsNullOrEmpty(c))
                                 .ToList();

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
        finally
        {
            // 6. Luôn giải phóng toàn bộ Redis Distributed Lock sau khi hoàn tất
            foreach (var lockKey in acquiredLockKeys)
            {
                await _redisService.ReleaseLockAsync(lockKey, lockValue);
            }
        }
    }

    public async Task<ApiResponse<string>> ReleaseSeatsAsync(Guid userId, ReleaseSeatsRequest request)
    {
        var distinctSeatIds = request.SeatIds.Distinct().ToList();
        if (distinctSeatIds.Count == 0)
        {
            return ApiResponse<string>.ErrorResult("Vui lòng chọn ít nhất 1 ghế cần nhả.");
        }

        var seats = await _unitOfWork.ShowtimeRepository.GetShowtimeSeatsByIdsAsync(request.ShowtimeId, distinctSeatIds);

        // Chỉ nhả các ghế do chính user này đang giữ và đang ở trạng thái Held
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

        // Xóa cache để cập nhật sơ đồ ghế
        await _showtimeService.InvalidateSeatMapCacheAsync(request.ShowtimeId);

        // Bắn thông báo real-time qua SignalR cho tất cả client đang xem suất chiếu này
        await _seatNotificationService.NotifySeatsReleasedAsync(request.ShowtimeId, myHeldSeats);

        return ApiResponse<string>.SuccessResult($"Đã nhả thành công {myHeldSeats.Count} ghế.", "Thành công.");
    }
}

