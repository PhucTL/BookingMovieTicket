using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Venue.Response;

public class VenueDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<VenueSeatMapItemDto> SeatMaps { get; set; } = new();
    public List<VenueEventItemDto> Events { get; set; } = new();
}

public class VenueSeatMapItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int TotalSeats => Rows * Columns;
}

public class VenueEventItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public string? PosterUrl { get; set; }
}

