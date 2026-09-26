using BookingMovieTicket_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Interfaces;

public interface IEventRepository
{
    /// <summary>
    /// Lấy danh sách phim/sự kiện
    /// </summary>
    Task<List<Event>> GetAllEventsAsync(Guid? venueId = null, string? search = null);

    /// <summary>
    /// Lấy thông tin cơ bản của phim/sự kiện theo Id
    /// </summary>
    Task<Event?> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Lấy thông tin chi tiết phim/sự kiện kèm rạp chiếu (Venue) và các suất chiếu (Showtimes)
    /// </summary>
    Task<Event?> GetEventWithDetailsAsync(Guid id);

    /// <summary>
    /// Thêm phim/sự kiện mới
    /// </summary>
    Task AddEventAsync(Event ev);

    /// <summary>
    /// Cập nhật thông tin phim/sự kiện
    /// </summary>
    Task UpdateEventAsync(Event ev);

    /// <summary>
    /// Xóa phim/sự kiện
    /// </summary>
    Task DeleteEventAsync(Event ev);

    /// <summary>
    /// Lưu thay đổi
    /// </summary>
    Task<int> SaveChangesAsync();
}

