using System;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Showtime.Request;

public class UpdateShowtimeRequest
{
    [Required(ErrorMessage = "Thời gian bắt đầu chiếu (StartTime) không được để trống.")]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Giá vé tiêu chuẩn (BasePriceStandard) phải lớn hơn 0.")]
    public decimal BasePriceStandard { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Giá vé VIP (BasePriceVip) phải lớn hơn 0.")]
    public decimal BasePriceVip { get; set; }
}

