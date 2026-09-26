using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Showtime.Response;

public class ShowtimeSeatMapResponse
{
    public Guid ShowtimeId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string VenueAddress { get; set; } = string.Empty;
    public string SeatMapName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePriceStandard { get; set; }
    public decimal BasePriceVip { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public List<SeatDto> Seats { get; set; } = new();
}

