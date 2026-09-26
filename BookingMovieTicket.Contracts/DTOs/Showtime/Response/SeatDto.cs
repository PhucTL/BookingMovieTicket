using System;

namespace BookingMovieTicket.Contracts.DTOs.Showtime.Response;

public class SeatDto
{
    public Guid ShowtimeSeatId { get; set; }
    public Guid SeatId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int Number { get; set; }
    public string SeatCode => $"{RowLabel}{Number}";
    public short SeatType { get; set; }
    public string SeatTypeName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public short Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsHeldByMe { get; set; }
}

