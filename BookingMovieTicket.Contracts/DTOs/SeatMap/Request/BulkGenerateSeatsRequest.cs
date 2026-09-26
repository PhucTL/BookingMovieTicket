using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.SeatMap.Request;

public class BulkGenerateSeatsRequest
{
    public List<string>? VipRowLabels { get; set; }
    public List<string>? CoupleRowLabels { get; set; }
    public bool ClearExisting { get; set; } = true;
}

