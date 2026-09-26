using System;

namespace BookingMovieTicket.Contracts.DTOs.Showtime.Response;

public class ShowtimeItemResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public int DurationMinutes { get; set; }
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public Guid SeatMapId { get; set; }
    public string SeatMapName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePriceStandard { get; set; }
    public decimal BasePriceVip { get; set; }
    public int AvailableSeats { get; set; }
    public int TotalSeats { get; set; }
}

