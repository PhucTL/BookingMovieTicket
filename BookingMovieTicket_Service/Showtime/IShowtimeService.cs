using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Showtime.Response;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Showtime;

public interface IShowtimeService
{
    /// <summary>
    /// Lấy sơ đồ ghế chi tiết của suất chiếu (kết hợp Redis Cache để tối ưu tải cao)
    /// </summary>
    Task<ApiResponse<ShowtimeSeatMapResponse>> GetSeatMapByShowtimeIdAsync(Guid showtimeId, Guid? currentUserId = null);

    /// <summary>
    /// Xóa cache sơ đồ ghế của suất chiếu trên Redis (khi có trạng thái ghế thay đổi)
    /// </summary>
    Task InvalidateSeatMapCacheAsync(Guid showtimeId);
}

