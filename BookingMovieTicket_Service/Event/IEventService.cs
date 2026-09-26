using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Event.Request;
using BookingMovieTicket.Contracts.DTOs.Event.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Event;

public interface IEventService
{
    /// <summary>
    /// Lấy danh sách phim/sự kiện
    /// </summary>
    Task<ApiResponse<List<EventResponse>>> GetAllEventsAsync(Guid? venueId = null, string? search = null);

    /// <summary>
    /// Lấy thông tin chi tiết một phim/sự kiện kèm rạp và danh sách suất chiếu
    /// </summary>
    Task<ApiResponse<EventDetailResponse>> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Staff tạo phim/sự kiện mới
    /// </summary>
    Task<ApiResponse<EventResponse>> CreateEventAsync(CreateEventRequest request);

    /// <summary>
    /// Staff chỉnh sửa thông tin phim/sự kiện
    /// </summary>
    Task<ApiResponse<EventResponse>> UpdateEventAsync(Guid id, UpdateEventRequest request);

    /// <summary>
    /// Staff xóa phim/sự kiện
    /// </summary>
    Task<ApiResponse<string>> DeleteEventAsync(Guid id);
}

