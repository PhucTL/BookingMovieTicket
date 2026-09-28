using BookingMovieTicket.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Request;

public class CheckoutRequest
{
    [Required(ErrorMessage = "Vui lòng chọn suất chiếu (ShowtimeId).")]
    public Guid ShowtimeId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 ghế cần đặt.")]
    [MinLength(1, ErrorMessage = "Danh sách ghế không được để trống.")]
    public List<Guid> SeatIds { get; set; } = new();

    /// <summary>
    /// Phương thức thanh toán: 0 = CreditCard, 1 = EWallet (PayOS VietQR)
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.EWallet;
}

