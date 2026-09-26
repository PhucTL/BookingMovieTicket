using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Showtime.Response;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Showtime;

public interface IShowtimeService
{
    /// <summary>
    /// Lấy sơ đồ ghế chi tiết của suất chiếu 
    /// </summary>
    Task<ApiResponse<ShowtimeSeatMapResponse>> GetSeatMapByShowtimeIdAsync(Guid showtimeId, Guid? currentUserId = null);

    /// <summary>
    /// Xóa cache sơ đồ ghế của suất chiếu trên Redis
    /// </summary>
    Task InvalidateSeatMapCacheAsync(Guid showtimeId);

    /// <summary>
    /// Lấy danh sách suất chiếu 
    /// </summary>
    Task<ApiResponse<List<ShowtimeItemResponse>>> GetShowtimesAsync(Guid? eventId = null, DateTime? date = null);

    /// <summary>
    /// Lấy thông tin chi tiết một suất chiếu
    /// </summary>
    Task<ApiResponse<ShowtimeItemResponse>> GetShowtimeByIdAsync(Guid id);

    /// <summary>
    /// Staff tạo suất chiếu mới 
    /// </summary>
    Task<ApiResponse<ShowtimeItemResponse>> CreateShowtimeAsync(BookingMovieTicket.Contracts.DTOs.Showtime.Request.CreateShowtimeRequest request);

    /// <summary>
    /// Staff cập nhật giờ chiếu, giá vé
    /// </summary>
    Task<ApiResponse<ShowtimeItemResponse>> UpdateShowtimeAsync(Guid id, BookingMovieTicket.Contracts.DTOs.Showtime.Request.UpdateShowtimeRequest request);

    /// <summary>
    /// Staff hủy/xóa suất chiếu
    /// </summary>
    Task<ApiResponse<string>> DeleteShowtimeAsync(Guid id);
}

