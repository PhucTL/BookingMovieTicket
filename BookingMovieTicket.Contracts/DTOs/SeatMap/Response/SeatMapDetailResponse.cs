using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.SeatMap.Response;

public class SeatMapDetailResponse
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int TotalSeats { get; set; }
    public List<SeatItemDto> Seats { get; set; } = new();
}

public class SeatItemDto
{
    public Guid Id { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int Number { get; set; }
    public string SeatCode => $"{RowLabel}{Number}";
    public short SeatType { get; set; }
    public string SeatTypeName { get; set; } = string.Empty;
}

