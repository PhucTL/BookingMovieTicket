using System;

namespace BookingMovieTicket.Contracts.DTOs.SeatMap.Response;

public class SeatMapResponse
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int TotalSeats { get; set; }
}

