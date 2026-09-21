using System;

namespace BookingMovieTicket.Contracts.DTOs.Authentication.Response;

public class RegisterResponse
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public short Role { get; set; }
    public string Message { get; set; } = "Đăng ký tài khoản thành công.";
}

