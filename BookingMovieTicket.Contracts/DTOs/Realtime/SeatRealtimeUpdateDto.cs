using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Realtime;

public class SeatRealtimeUpdateDto
{
    public Guid ShowtimeId { get; set; }
    public string Action { get; set; } = string.Empty; // "HELD", "RELEASED", "BOOKED"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<SeatStatusChangeDto> Seats { get; set; } = new();
}

public class SeatStatusChangeDto
{
    public Guid ShowtimeSeatId { get; set; }
    public Guid SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public short Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public Guid? HeldByUserId { get; set; }
    public DateTime? HeldUntil { get; set; }
}

