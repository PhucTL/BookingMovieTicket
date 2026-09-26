using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Showtime.Request;
using BookingMovieTicket.Contracts.DTOs.Showtime.Response;
using BookingMovieTicket.Contracts.Enums;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Service.Redis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Showtime;

public class ShowtimeService : IShowtimeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly ILogger<ShowtimeService> _logger;

    public ShowtimeService(IUnitOfWork unitOfWork, IRedisService redisService, ILogger<ShowtimeService> logger)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy sơ đồ ghế chi tiết của suất chiếu 
    /// </summary>
    public async Task<ApiResponse<ShowtimeSeatMapResponse>> GetSeatMapByShowtimeIdAsync(Guid showtimeId, Guid? currentUserId = null)
    {
        var cacheKey = $"showtime_seatmap:{showtimeId}";

        // 1. Thử lấy từ Redis Cache
        var cached = await _redisService.GetCacheAsync<ShowtimeSeatMapResponse>(cacheKey);
        if (cached != null)
        {
            // Cập nhật trạng thái Seat
            if (currentUserId.HasValue)
            {
                // Nếu có user đăng nhập, kiểm tra lại cờ IsHeldByMe
            }
            return ApiResponse<ShowtimeSeatMapResponse>.SuccessResult(cached, "Lấy sơ đồ ghế từ cache thành công.");
        }

        // 2. Nếu cache miss, query Database
        var showtime = await _unitOfWork.ShowtimeRepository.GetShowtimeWithDetailsAsync(showtimeId);
        if (showtime == null)
        {
            return ApiResponse<ShowtimeSeatMapResponse>.ErrorResult("Không tìm thấy thông tin suất chiếu.");
        }

        var now = DateTime.UtcNow;

        var seatDtos = showtime.ShowtimeSeats
            .OrderBy(ss => ss.Seat.RowLabel)
            .ThenBy(ss => ss.Seat.Number)
            .Select(ss =>
            {
                // Kiểm tra ghế tạm giữ (Held) nhưng đã quá hạn HeldUntil thì coi như Available
                var isExpired = ss.Status == (short)SeatStatus.Held && ss.HeldUntil.HasValue && ss.HeldUntil.Value <= now;
                var effectiveStatus = isExpired ? (short)SeatStatus.Available : ss.Status;

                var statusName = Enum.IsDefined(typeof(SeatStatus), effectiveStatus)
                    ? ((SeatStatus)effectiveStatus).ToString()
                    : "Unknown";

                var seatTypeName = Enum.IsDefined(typeof(SeatType), ss.Seat.SeatType)
                    ? ((SeatType)ss.Seat.SeatType).ToString()
                    : "Standard";

                var isHeldByMe = currentUserId.HasValue
                                 && ss.HeldByUserId == currentUserId.Value
                                 && effectiveStatus == (short)SeatStatus.Held;

                return new SeatDto
                {
                    ShowtimeSeatId = ss.Id,
                    SeatId = ss.SeatId,
                    RowLabel = ss.Seat.RowLabel,
                    Number = ss.Seat.Number,
                    SeatType = ss.Seat.SeatType,
                    SeatTypeName = seatTypeName,
                    Price = ss.Price,
                    Status = effectiveStatus,
                    StatusName = statusName,
                    IsHeldByMe = isHeldByMe
                };
            })
            .ToList();

        var totalSeats = seatDtos.Count;
        var availableSeats = seatDtos.Count(s => s.Status == (short)SeatStatus.Available);

        var response = new ShowtimeSeatMapResponse
        {
            ShowtimeId = showtime.Id,
            EventId = showtime.EventId,
            EventTitle = showtime.Event?.Title ?? "N/A",
            PosterUrl = showtime.Event?.PosterUrl,
            VenueName = showtime.SeatMap?.Venue?.Name ?? "N/A",
            VenueAddress = showtime.SeatMap?.Venue?.Address ?? string.Empty,
            SeatMapName = showtime.SeatMap?.Name ?? "N/A",
            StartTime = showtime.StartTime,
            EndTime = showtime.EndTime,
            BasePriceStandard = showtime.BasePriceStandard,
            BasePriceVip = showtime.BasePriceVip,
            Rows = showtime.SeatMap?.Rows ?? 0,
            Columns = showtime.SeatMap?.Columns ?? 0,
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats,
            Seats = seatDtos
        };

        // 3. Lưu vào Redis Cache trong 10 giây 
        await _redisService.SetCacheAsync(cacheKey, response, TimeSpan.FromSeconds(10));

        return ApiResponse<ShowtimeSeatMapResponse>.SuccessResult(response, "Lấy sơ đồ ghế thành công.");
    }

    /// <summary>
    /// Xóa cache sơ đồ ghế của suất chiếu trên Redis
    /// </summary>
    public async Task InvalidateSeatMapCacheAsync(Guid showtimeId)
    {
        var cacheKey = $"showtime_seatmap:{showtimeId}";
        await _redisService.RemoveCacheAsync(cacheKey);
    }

    /// <summary>
    /// Lấy danh sách suất chiếu 
    /// </summary>
    public async Task<ApiResponse<List<ShowtimeItemResponse>>> GetShowtimesAsync(Guid? eventId = null, DateTime? date = null)
    {
        try
        {
            var showtimes = await _unitOfWork.ShowtimeRepository.GetShowtimesAsync(eventId, date);

            var response = showtimes.Select(s => new ShowtimeItemResponse
            {
                Id = s.Id,
                EventId = s.EventId,
                EventTitle = s.Event?.Title ?? string.Empty,
                PosterUrl = s.Event?.PosterUrl,
                DurationMinutes = s.Event?.DurationMinutes ?? 0,
                VenueId = s.Event?.VenueId ?? s.SeatMap?.VenueId ?? Guid.Empty,
                VenueName = s.Event?.Venue?.Name ?? s.SeatMap?.Venue?.Name ?? string.Empty,
                SeatMapId = s.SeatMapId,
                SeatMapName = s.SeatMap?.Name ?? string.Empty,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BasePriceStandard = s.BasePriceStandard,
                BasePriceVip = s.BasePriceVip,
                AvailableSeats = s.ShowtimeSeats?.Count(ss => ss.Status == (short)SeatStatus.Available) ?? 0,
                TotalSeats = s.ShowtimeSeats?.Count ?? 0
            }).ToList();

            return ApiResponse<List<ShowtimeItemResponse>>.SuccessResult(response, "Lấy danh sách suất chiếu thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách suất chiếu.");
            return ApiResponse<List<ShowtimeItemResponse>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách suất chiếu.");
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết một suất chiếu
    /// </summary>
    public async Task<ApiResponse<ShowtimeItemResponse>> GetShowtimeByIdAsync(Guid id)
    {
        try
        {
            var s = await _unitOfWork.ShowtimeRepository.GetShowtimeWithDetailsAsync(id);
            if (s == null)
            {
                return ApiResponse<ShowtimeItemResponse>.ErrorResult("Không tìm thấy thông tin suất chiếu.");
            }

            var response = new ShowtimeItemResponse
            {
                Id = s.Id,
                EventId = s.EventId,
                EventTitle = s.Event?.Title ?? string.Empty,
                PosterUrl = s.Event?.PosterUrl,
                DurationMinutes = s.Event?.DurationMinutes ?? 0,
                VenueId = s.Event?.VenueId ?? s.SeatMap?.VenueId ?? Guid.Empty,
                VenueName = s.Event?.Venue?.Name ?? s.SeatMap?.Venue?.Name ?? string.Empty,
                SeatMapId = s.SeatMapId,
                SeatMapName = s.SeatMap?.Name ?? string.Empty,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BasePriceStandard = s.BasePriceStandard,
                BasePriceVip = s.BasePriceVip,
                AvailableSeats = s.ShowtimeSeats?.Count(ss => ss.Status == (short)SeatStatus.Available) ?? 0,
                TotalSeats = s.ShowtimeSeats?.Count ?? 0
            };

            return ApiResponse<ShowtimeItemResponse>.SuccessResult(response, "Lấy thông tin suất chiếu thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết suất chiếu {ShowtimeId}.", id);
            return ApiResponse<ShowtimeItemResponse>.ErrorResult("Đã xảy ra lỗi khi lấy chi tiết suất chiếu.");
        }
    }

    /// <summary>
    /// Staff tạo suất chiếu mới 
    /// </summary>
    public async Task<ApiResponse<ShowtimeItemResponse>> CreateShowtimeAsync(CreateShowtimeRequest request)
    {
        try
        {
            var ev = await _unitOfWork.EventRepository.GetEventByIdAsync(request.EventId);
            if (ev == null)
            {
                return ApiResponse<ShowtimeItemResponse>.ErrorResult("Phim/Sự kiện (EventId) không tồn tại.");
            }

            var seatMap = await _unitOfWork.SeatMapRepository.GetSeatMapWithSeatsAsync(request.SeatMapId);
            if (seatMap == null)
            {
                return ApiResponse<ShowtimeItemResponse>.ErrorResult("Phòng chiếu (SeatMapId) không tồn tại.");
            }

            if (seatMap.Seats == null || seatMap.Seats.Count == 0)
            {
                return ApiResponse<ShowtimeItemResponse>.ErrorResult("Phòng chiếu này chưa được thiết lập danh sách ghế. Vui lòng sinh ghế cho phòng chiếu trước khi tạo suất chiếu.");
            }

            var startTimeUtc = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
            var endTimeUtc = request.EndTime.HasValue
                ? DateTime.SpecifyKind(request.EndTime.Value, DateTimeKind.Utc)
                : startTimeUtc.AddMinutes(ev.DurationMinutes);

            if (endTimeUtc <= startTimeUtc)
            {
                return ApiResponse<ShowtimeItemResponse>.ErrorResult("Thời gian kết thúc suất chiếu phải sau thời gian bắt đầu.");
            }

            var showtimeId = Guid.NewGuid();
            var showtime = new BookingMovieTicket_Repository.Entities.Showtime
            {
                Id = showtimeId,
                EventId = request.EventId,
                SeatMapId = request.SeatMapId,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                BasePriceStandard = request.BasePriceStandard,
                BasePriceVip = request.BasePriceVip
            };

            await _unitOfWork.ShowtimeRepository.AddShowtimeAsync(showtime);

            // Tự động sinh toàn bộ ShowtimeSeat từ danh sách Seat của SeatMap
            var showtimeSeats = seatMap.Seats.Select(seat =>
            {
                decimal seatPrice = seat.SeatType switch
                {
                    (short)SeatType.Vip => request.BasePriceVip,
                    (short)SeatType.Couple => request.BasePriceVip * 2,
                    _ => request.BasePriceStandard
                };

                return new ShowtimeSeat
                {
                    Id = Guid.NewGuid(),
                    ShowtimeId = showtimeId,
                    SeatId = seat.Id,
                    Status = (short)SeatStatus.Available,
                    Price = seatPrice,
                    HeldByUserId = null,
                    HeldUntil = null
                };
            }).ToList();

            await _unitOfWork.ShowtimeRepository.AddShowtimeSeatsRangeAsync(showtimeSeats);
            await _unitOfWork.SaveChangesAsync();

            var response = new ShowtimeItemResponse
            {
                Id = showtime.Id,
                EventId = showtime.EventId,
                EventTitle = ev.Title,
                PosterUrl = ev.PosterUrl,
                DurationMinutes = ev.DurationMinutes,
                VenueId = seatMap.VenueId,
                VenueName = seatMap.Venue?.Name ?? string.Empty,
                SeatMapId = showtime.SeatMapId,
                SeatMapName = seatMap.Name,
                StartTime = showtime.StartTime,
                EndTime = showtime.EndTime,
                BasePriceStandard = showtime.BasePriceStandard,
                BasePriceVip = showtime.BasePriceVip,
                AvailableSeats = showtimeSeats.Count,
                TotalSeats = showtimeSeats.Count
            };

            return ApiResponse<ShowtimeItemResponse>.SuccessResult(response, $"Tạo suất chiếu mới thành công với {showtimeSeats.Count} ghế.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo suất chiếu mới.");
            return ApiResponse<ShowtimeItemResponse>.ErrorResult("Đã xảy ra lỗi khi tạo suất chiếu mới.");
        }
    }

    /// <summary>
    /// Staff cập nhật giờ chiếu, giá vé
    /// </summary>
    public async Task<ApiResponse<ShowtimeItemResponse>> UpdateShowtimeAsync(Guid id, UpdateShowtimeRequest request)
    {
        try
        {
            var showtime = await _unitOfWork.ShowtimeRepository.GetShowtimeForUpdateAsync(id);
            if (showtime == null)
            {
                return ApiResponse<ShowtimeItemResponse>.ErrorResult("Không tìm thấy suất chiếu cần cập nhật.");
            }

            var startTimeUtc = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
            var endTimeUtc = request.EndTime.HasValue
                ? DateTime.SpecifyKind(request.EndTime.Value, DateTimeKind.Utc)
                : showtime.EndTime;

            if (endTimeUtc <= startTimeUtc)
            {
                return ApiResponse<ShowtimeItemResponse>.ErrorResult("Thời gian kết thúc suất chiếu phải sau thời gian bắt đầu.");
            }

            showtime.StartTime = startTimeUtc;
            showtime.EndTime = endTimeUtc;
            showtime.BasePriceStandard = request.BasePriceStandard;
            showtime.BasePriceVip = request.BasePriceVip;

            await _unitOfWork.ShowtimeRepository.UpdateShowtimeAsync(showtime);
            await _unitOfWork.SaveChangesAsync();

            // Xóa cache sơ đồ ghế trên Redis
            await InvalidateSeatMapCacheAsync(id);

            var updatedShowtime = await _unitOfWork.ShowtimeRepository.GetShowtimeWithDetailsAsync(id);
            var response = new ShowtimeItemResponse
            {
                Id = updatedShowtime!.Id,
                EventId = updatedShowtime.EventId,
                EventTitle = updatedShowtime.Event?.Title ?? string.Empty,
                PosterUrl = updatedShowtime.Event?.PosterUrl,
                DurationMinutes = updatedShowtime.Event?.DurationMinutes ?? 0,
                VenueId = updatedShowtime.Event?.VenueId ?? Guid.Empty,
                VenueName = updatedShowtime.Event?.Venue?.Name ?? string.Empty,
                SeatMapId = updatedShowtime.SeatMapId,
                SeatMapName = updatedShowtime.SeatMap?.Name ?? string.Empty,
                StartTime = updatedShowtime.StartTime,
                EndTime = updatedShowtime.EndTime,
                BasePriceStandard = updatedShowtime.BasePriceStandard,
                BasePriceVip = updatedShowtime.BasePriceVip,
                AvailableSeats = updatedShowtime.ShowtimeSeats?.Count(ss => ss.Status == (short)SeatStatus.Available) ?? 0,
                TotalSeats = updatedShowtime.ShowtimeSeats?.Count ?? 0
            };

            return ApiResponse<ShowtimeItemResponse>.SuccessResult(response, "Cập nhật suất chiếu thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật suất chiếu {ShowtimeId}.", id);
            return ApiResponse<ShowtimeItemResponse>.ErrorResult("Đã xảy ra lỗi khi cập nhật suất chiếu.");
        }
    }

    /// <summary>
    /// Staff hủy/xóa suất chiếu
    /// </summary>
    public async Task<ApiResponse<string>> DeleteShowtimeAsync(Guid id)
    {
        try
        {
            var showtime = await _unitOfWork.ShowtimeRepository.GetShowtimeForUpdateAsync(id);
            if (showtime == null)
            {
                return ApiResponse<string>.ErrorResult("Không tìm thấy suất chiếu cần xóa.");
            }

            if (showtime.Bookings != null && showtime.Bookings.Any(b => b.Status != (short)BookingStatus.Cancelled))
            {
                return ApiResponse<string>.ErrorResult("Không thể xóa suất chiếu này vì đã có vé được đặt.");
            }

            // Xóa các ghế của suất chiếu trước
            if (showtime.ShowtimeSeats != null && showtime.ShowtimeSeats.Count > 0)
            {
                foreach (var ss in showtime.ShowtimeSeats.ToList())
                {
                    showtime.ShowtimeSeats.Remove(ss);
                }
            }

            await _unitOfWork.ShowtimeRepository.DeleteShowtimeAsync(showtime);
            await _unitOfWork.SaveChangesAsync();

            // Xóa cache Redis
            await InvalidateSeatMapCacheAsync(id);

            return ApiResponse<string>.SuccessResult("Đã hủy/xóa suất chiếu thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa suất chiếu {ShowtimeId}.", id);
            return ApiResponse<string>.ErrorResult("Đã xảy ra lỗi khi xóa suất chiếu.");
        }
    }
}

