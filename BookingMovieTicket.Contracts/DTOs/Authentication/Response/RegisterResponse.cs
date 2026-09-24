using System;

namespace BookingMovieTicket.Contracts.DTOs.Authentication.Response;

public class RegisterResponse
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public short Role { get; set; }
    public string RoleName => Enum.IsDefined(typeof(BookingMovieTicket.Contracts.Enums.UserRole), Role)
        ? ((BookingMovieTicket.Contracts.Enums.UserRole)Role).ToString()
        : Role.ToString();
    public string Message { get; set; } = "Đăng ký tài khoản thành công.";
}

