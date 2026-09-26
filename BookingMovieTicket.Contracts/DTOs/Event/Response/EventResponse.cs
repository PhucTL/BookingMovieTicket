using System;

namespace BookingMovieTicket.Contracts.DTOs.Event.Response;

public class EventResponse
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public string? PosterUrl { get; set; }
    public int TotalShowtimes { get; set; }
}

