using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Response;

public class CheckoutResponse
{
    public Guid BookingId { get; set; }
    public long OrderCode { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public List<string> SeatCodes { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

