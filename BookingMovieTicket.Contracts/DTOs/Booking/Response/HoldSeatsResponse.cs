using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Response;

public class HoldSeatsResponse
{
    public Guid ShowtimeId { get; set; }
    public List<Guid> ShowtimeSeatIds { get; set; } = new();
    public List<string> SeatCodes { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public DateTime HeldUntil { get; set; }
    public int ExpirySeconds { get; set; } = 300;
    public string Message { get; set; } = "Giữ ghế thành công. Vui lòng thanh toán trong vòng 5 phút.";
}

