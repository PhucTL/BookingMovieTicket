using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Authentication.Request;

public class LoginRequest
{
    [Required(ErrorMessage = "Username/Email không được để trống")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    public string Password { get; set; } = string.Empty;
}

