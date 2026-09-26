using BookingMovieTicket.Contracts.DTOs.Realtime;
using BookingMovieTicket.Contracts.Enums;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Service.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Hubs;

public class SeatNotificationService : ISeatNotificationService
{
    private readonly IHubContext<SeatHub> _hubContext;
    private readonly ILogger<SeatNotificationService> _logger;

    public SeatNotificationService(IHubContext<SeatHub> hubContext, ILogger<SeatNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifySeatsHeldAsync(Guid showtimeId, Guid heldByUserId, DateTime heldUntil, IEnumerable<ShowtimeSeat> seats)
    {
        try
        {
            var groupName = $"Showtime_{showtimeId}";
            var updateDto = new SeatRealtimeUpdateDto
            {
                ShowtimeId = showtimeId,
                Action = "HELD",
                Timestamp = DateTime.UtcNow,
                Seats = seats.Select(s => new SeatStatusChangeDto
                {
                    ShowtimeSeatId = s.Id,
                    SeatId = s.SeatId,
                    SeatCode = s.Seat != null ? $"{s.Seat.RowLabel}{s.Seat.Number}" : string.Empty,
                    Status = (short)SeatStatus.Held,
                    StatusName = SeatStatus.Held.ToString(),
                    HeldByUserId = heldByUserId,
                    HeldUntil = heldUntil
                }).ToList()
            };

            await _hubContext.Clients.Group(groupName).SendAsync("OnSeatsStatusChanged", updateDto);
            _logger.LogInformation("Đã bắn event SignalR OnSeatsStatusChanged (HELD) cho {Count} ghế thuộc {GroupName}", updateDto.Seats.Count, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắn event SignalR NotifySeatsHeldAsync cho suất chiếu {ShowtimeId}", showtimeId);
        }
    }

    public async Task NotifySeatsReleasedAsync(Guid showtimeId, IEnumerable<ShowtimeSeat> seats)
    {
        try
        {
            var groupName = $"Showtime_{showtimeId}";
            var updateDto = new SeatRealtimeUpdateDto
            {
                ShowtimeId = showtimeId,
                Action = "RELEASED",
                Timestamp = DateTime.UtcNow,
                Seats = seats.Select(s => new SeatStatusChangeDto
                {
                    ShowtimeSeatId = s.Id,
                    SeatId = s.SeatId,
                    SeatCode = s.Seat != null ? $"{s.Seat.RowLabel}{s.Seat.Number}" : string.Empty,
                    Status = (short)SeatStatus.Available,
                    StatusName = SeatStatus.Available.ToString(),
                    HeldByUserId = null,
                    HeldUntil = null
                }).ToList()
            };

            await _hubContext.Clients.Group(groupName).SendAsync("OnSeatsStatusChanged", updateDto);
            _logger.LogInformation("Đã bắn event SignalR OnSeatsStatusChanged (RELEASED) cho {Count} ghế thuộc {GroupName}", updateDto.Seats.Count, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắn event SignalR NotifySeatsReleasedAsync cho suất chiếu {ShowtimeId}", showtimeId);
        }
    }

    public async Task NotifySeatsBookedAsync(Guid showtimeId, IEnumerable<ShowtimeSeat> seats)
    {
        try
        {
            var groupName = $"Showtime_{showtimeId}";
            var updateDto = new SeatRealtimeUpdateDto
            {
                ShowtimeId = showtimeId,
                Action = "BOOKED",
                Timestamp = DateTime.UtcNow,
                Seats = seats.Select(s => new SeatStatusChangeDto
                {
                    ShowtimeSeatId = s.Id,
                    SeatId = s.SeatId,
                    SeatCode = s.Seat != null ? $"{s.Seat.RowLabel}{s.Seat.Number}" : string.Empty,
                    Status = (short)SeatStatus.Booked,
                    StatusName = SeatStatus.Booked.ToString(),
                    HeldByUserId = null,
                    HeldUntil = null
                }).ToList()
            };

            await _hubContext.Clients.Group(groupName).SendAsync("OnSeatsStatusChanged", updateDto);
            _logger.LogInformation("Đã bắn event SignalR OnSeatsStatusChanged (BOOKED) cho {Count} ghế thuộc {GroupName}", updateDto.Seats.Count, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắn event SignalR NotifySeatsBookedAsync cho suất chiếu {ShowtimeId}", showtimeId);
        }
    }
}

