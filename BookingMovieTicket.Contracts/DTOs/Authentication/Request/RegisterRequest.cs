using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Authentication.Request;

public class RegisterRequest
{
    [Required(ErrorMessage = "Username không được để trống")]
    [MinLength(3, ErrorMessage = "Username phải có ít nhất 3 ký tự")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống")]
    public string FullName { get; set; } = string.Empty;
}

