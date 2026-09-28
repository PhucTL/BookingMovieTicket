using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Response;

public class BookingHistoryResponse
{
    public Guid Id { get; set; }
    public Guid ShowtimeId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string SeatMapName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal TotalAmount { get; set; }
    public short Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> SeatCodes { get; set; } = new();
}

