using System;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Event.Request;

public class CreateEventRequest
{
    [Required(ErrorMessage = "Vui lòng chọn rạp chiếu (VenueId).")]
    public Guid VenueId { get; set; }

    [Required(ErrorMessage = "Tiêu đề phim/sự kiện không được để trống.")]
    [StringLength(300, ErrorMessage = "Tiêu đề không được vượt quá 300 ký tự.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 600, ErrorMessage = "Thời lượng phải từ 1 đến 600 phút.")]
    public int DurationMinutes { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn poster không được vượt quá 500 ký tự.")]
    [Url(ErrorMessage = "Đường dẫn poster không hợp lệ.")]
    public string? PosterUrl { get; set; }
}

