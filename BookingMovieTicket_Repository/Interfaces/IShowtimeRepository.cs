using BookingMovieTicket_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Interfaces;

public interface IShowtimeRepository
{
    /// <summary>
    /// Lấy chi tiết suất chiếu kèm Phim, Sơ đồ ghế, Rạp và danh sách ghế của suất chiếu
    /// </summary>
    Task<Showtime?> GetShowtimeWithDetailsAsync(Guid showtimeId);

    /// <summary>
    /// Lấy thông tin cơ bản của suất chiếu
    /// </summary>
    Task<Showtime?> GetShowtimeByIdAsync(Guid showtimeId);

    /// <summary>
    /// Lấy danh sách toàn bộ ghế của một suất chiếu kèm thông tin hàng/cột của ghế gốc
    /// </summary>
    Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(Guid showtimeId);

    /// <summary>
    /// Lấy thông tin 1 ghế cụ thể của suất chiếu
    /// </summary>
    Task<ShowtimeSeat?> GetShowtimeSeatByIdAsync(Guid showtimeSeatId);

    /// <summary>
    /// Lấy danh sách nhiều ghế của suất chiếu theo danh sách Id
    /// </summary>
    Task<List<ShowtimeSeat>> GetShowtimeSeatsByIdsAsync(Guid showtimeId, IEnumerable<Guid> seatIds);

    /// <summary>
    /// Cập nhật thông tin ghế
    /// </summary>
    Task UpdateShowtimeSeatAsync(ShowtimeSeat seat);

    /// <summary>
    /// Cập nhật hàng loạt ghế
    /// </summary>
    Task UpdateShowtimeSeatsAsync(IEnumerable<ShowtimeSeat> seats);

    /// <summary>
    /// Lấy danh sách các ghế đang ở trạng thái Held và đã quá hạn giữ
    /// </summary>
    Task<List<ShowtimeSeat>> GetExpiredHeldSeatsAsync(DateTime cutoffTime, short status = 1);

    /// <summary>
    /// Lấy danh sách suất chiếu
    /// </summary>
    Task<List<Showtime>> GetShowtimesAsync(Guid? eventId = null, DateTime? date = null);

    /// <summary>
    /// Lấy thông tin suất chiếu có tracking để cập nhật hoặc xóa
    /// </summary>
    Task<Showtime?> GetShowtimeForUpdateAsync(Guid id);

    /// <summary>
    /// Thêm suất chiếu mới
    /// </summary>
    Task AddShowtimeAsync(Showtime showtime);

    /// <summary>
    /// Cập nhật thông tin suất chiếu
    /// </summary>
    Task UpdateShowtimeAsync(Showtime showtime);

    /// <summary>
    /// Xóa suất chiếu
    /// </summary>
    Task DeleteShowtimeAsync(Showtime showtime);

    /// <summary>
    /// Thêm danh sách ghế cho suất chiếu
    /// </summary>
    Task AddShowtimeSeatsRangeAsync(IEnumerable<ShowtimeSeat> seats);

    /// <summary>
    /// Lưu thay đổi
    /// </summary>
    Task<int> SaveChangesAsync();
}

