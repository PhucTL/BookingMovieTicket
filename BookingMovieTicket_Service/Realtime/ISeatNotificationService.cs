using BookingMovieTicket_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Realtime;

public interface ISeatNotificationService
{
    /// <summary>
    /// Thông báo real-time qua SignalR khi có ghế vừa được giữ tạm thời
    /// </summary>
    Task NotifySeatsHeldAsync(Guid showtimeId, Guid heldByUserId, DateTime heldUntil, IEnumerable<ShowtimeSeat> seats);

    /// <summary>
    /// Thông báo real-time qua SignalR khi có ghế vừa được nhả/giải phóng
    /// </summary>
    Task NotifySeatsReleasedAsync(Guid showtimeId, IEnumerable<ShowtimeSeat> seats);

    /// <summary>
    /// Thông báo real-time qua SignalR khi có ghế đã được thanh toán và bán thành công
    /// </summary>
    Task NotifySeatsBookedAsync(Guid showtimeId, IEnumerable<ShowtimeSeat> seats);
}

