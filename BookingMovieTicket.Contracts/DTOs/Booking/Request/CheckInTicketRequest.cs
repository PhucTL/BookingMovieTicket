using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Request;

public class CheckInTicketRequest
{
    /// <summary>
    /// Mã QR code vé hoặc ID đơn đặt vé
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập hoặc quét mã vé / QR Code.")]
    public string TicketCode { get; set; } = string.Empty;
}

