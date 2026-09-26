using System;

namespace BookingMovieTicket.Contracts.DTOs.SeatMap.Response;

public class SeatResponse
{
    public Guid Id { get; set; }
    public Guid SeatMapId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int Number { get; set; }
    public string SeatCode => $"{RowLabel}{Number}";
    public short SeatType { get; set; }
    public string SeatTypeName { get; set; } = string.Empty;
}

