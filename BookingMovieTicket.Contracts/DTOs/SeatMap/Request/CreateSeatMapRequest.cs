using System;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.SeatMap.Request;

public class CreateSeatMapRequest
{
    [Required(ErrorMessage = "Vui lòng chọn rạp chiếu (VenueId).")]
    public Guid VenueId { get; set; }

    [Required(ErrorMessage = "Tên phòng chiếu/sơ đồ ghế không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên phòng chiếu không được vượt quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 26, ErrorMessage = "Số hàng ghế (Rows) phải từ 1 đến 26 (tương ứng hàng A -> Z).")]
    public int Rows { get; set; }

    [Range(1, 50, ErrorMessage = "Số cột ghế (Columns) phải từ 1 đến 50.")]
    public int Columns { get; set; }
}

