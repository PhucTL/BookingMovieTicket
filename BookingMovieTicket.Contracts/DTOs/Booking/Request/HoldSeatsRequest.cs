using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Request;

public class HoldSeatsRequest
{
    [Required(ErrorMessage = "Mã suất chiếu (ShowtimeId) là bắt buộc.")]
    public Guid ShowtimeId { get; set; }

    [Required(ErrorMessage = "Danh sách ghế không được để trống.")]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 ghế.")]
    [MaxLength(8, ErrorMessage = "Mỗi lần giữ chỗ chỉ được chọn tối đa 8 ghế.")]
    public List<Guid> SeatIds { get; set; } = new();
}

