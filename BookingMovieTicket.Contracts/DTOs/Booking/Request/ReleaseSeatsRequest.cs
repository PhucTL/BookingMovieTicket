using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Request;

public class ReleaseSeatsRequest
{
    [Required(ErrorMessage = "Mã suất chiếu (ShowtimeId) là bắt buộc.")]
    public Guid ShowtimeId { get; set; }

    [Required(ErrorMessage = "Danh sách ghế cần nhả không được để trống.")]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 ghế cần nhả.")]
    public List<Guid> SeatIds { get; set; } = new();
}

