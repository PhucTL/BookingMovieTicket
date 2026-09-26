using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Venue.Request;

public class UpdateVenueRequest
{
    [Required(ErrorMessage = "Tên rạp không được để trống.")]
    [StringLength(200, ErrorMessage = "Tên rạp không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string? Address { get; set; }
}

