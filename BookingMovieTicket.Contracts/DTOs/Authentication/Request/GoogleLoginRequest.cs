using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Authentication.Request;

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? IdToken { get; set; }
}

