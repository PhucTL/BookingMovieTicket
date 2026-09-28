using BookingMovieTicket.Contracts.Enums;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Service.Realtime;
using BookingMovieTicket_Service.Showtime;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.BackgroundJobs;

public class SeatExpirationJob : ISeatExpirationJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShowtimeService _showtimeService;
    private readonly ISeatNotificationService _seatNotificationService;
    private readonly ILogger<SeatExpirationJob> _logger;

    public SeatExpirationJob(
        IUnitOfWork unitOfWork,
        IShowtimeService showtimeService,
        ISeatNotificationService seatNotificationService,
        ILogger<SeatExpirationJob> logger)
    {
        _unitOfWork = unitOfWork;
        _showtimeService = showtimeService;
        _seatNotificationService = seatNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Tiến trình nền quét và giải phóng các ghế giữ tạm (Held) đã quá 5 phút mà chưa hoàn tất thanh toán
    /// </summary>
    public async Task ReleaseExpiredSeatsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredSeats = await _unitOfWork.ShowtimeRepository.GetExpiredHeldSeatsAsync(now, (short)SeatStatus.Held);

            if (expiredSeats == null || expiredSeats.Count == 0)
            {
                return;
            }

            _logger.LogInformation("[Hangfire] Phát hiện {Count} ghế hết hạn giữ tạm thời cần giải phóng.", expiredSeats.Count);

            var groupedByShowtime = expiredSeats.GroupBy(s => s.ShowtimeId);

            foreach (var group in groupedByShowtime)
            {
                var showtimeId = group.Key;
                var seatsToRelease = group.ToList();

                try
                {
                    foreach (var seat in seatsToRelease)
                    {
                        seat.Status = (short)SeatStatus.Available;
                        seat.HeldByUserId = null;
                        seat.HeldUntil = null;
                    }

                    await _unitOfWork.ShowtimeRepository.UpdateShowtimeSeatsAsync(seatsToRelease);
                    await _unitOfWork.SaveChangesAsync();

                    await _showtimeService.InvalidateSeatMapCacheAsync(showtimeId);

                    await _seatNotificationService.NotifySeatsReleasedAsync(showtimeId, seatsToRelease);

                    _logger.LogInformation("[Hangfire] Đã giải phóng thành công {Count} ghế hết hạn của Suất chiếu {ShowtimeId}.",
                        seatsToRelease.Count, showtimeId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Hangfire] Lỗi khi xử lý giải phóng ghế cho Suất chiếu {ShowtimeId}.", showtimeId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Hangfire] Lỗi ngoại lệ trong tiến trình ReleaseExpiredSeatsAsync.");
            throw;
        }
    }
}

