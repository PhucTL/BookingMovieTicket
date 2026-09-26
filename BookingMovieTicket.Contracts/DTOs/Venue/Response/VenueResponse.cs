using System;

namespace BookingMovieTicket.Contracts.DTOs.Venue.Response;

public class VenueResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalSeatMaps { get; set; }
    public int TotalEvents { get; set; }
}

