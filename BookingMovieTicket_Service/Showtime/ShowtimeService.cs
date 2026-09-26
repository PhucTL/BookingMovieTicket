using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Showtime.Response;
using BookingMovieTicket.Contracts.Enums;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Service.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Showtime;

public class ShowtimeService : IShowtimeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;

    public ShowtimeService(IUnitOfWork unitOfWork, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
    }

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

    public async Task InvalidateSeatMapCacheAsync(Guid showtimeId)
    {
        var cacheKey = $"showtime_seatmap:{showtimeId}";
        await _redisService.RemoveCacheAsync(cacheKey);
    }
}

