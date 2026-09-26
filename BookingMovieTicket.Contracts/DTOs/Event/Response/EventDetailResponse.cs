using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Event.Response;

public class EventDetailResponse
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string? VenueAddress { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public string? PosterUrl { get; set; }
    public List<EventShowtimeItemDto> Showtimes { get; set; } = new();
}

public class EventShowtimeItemDto
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public decimal BasePriceStandard { get; set; }
    public decimal BasePriceVip { get; set; }
    public Guid SeatMapId { get; set; }
    public string SeatMapName { get; set; } = string.Empty;
}

