using System;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Showtime.Request;

public class CreateShowtimeRequest
{
    [Required(ErrorMessage = "Vui lòng chọn phim/sự kiện (EventId).")]
    public Guid EventId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phòng chiếu (SeatMapId).")]
    public Guid SeatMapId { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu chiếu (StartTime) không được để trống.")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Thời gian kết thúc. Nếu bỏ trống, hệ thống sẽ tự động tính theo thời lượng phim.
    /// </summary>
    public DateTime? EndTime { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Giá vé tiêu chuẩn (BasePriceStandard) phải lớn hơn 0.")]
    public decimal BasePriceStandard { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Giá vé VIP (BasePriceVip) phải lớn hơn 0.")]
    public decimal BasePriceVip { get; set; }
}

