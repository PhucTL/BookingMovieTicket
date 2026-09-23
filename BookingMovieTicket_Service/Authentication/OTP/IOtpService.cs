using BookingMovieTicket.Contracts.DTOs.Authentication.Request;
using BookingMovieTicket_Repository.Entities;
using System;

namespace BookingMovieTicket_Service.Authentication.OTP;

public class PendingRegistration
{
    public RegisterRequest Request { get; set; } = null!;
    public string Otp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PendingLogin
{
    public User User { get; set; } = null!;
    public string Otp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public interface IOtpService
{
    string GenerateOtp(int length = 6);

    // Quản lý OTP cho Đăng ký (Register)
    void SaveRegistrationOtp(RegisterRequest request, string otp, int expireMinutes = 5);
    PendingRegistration? GetPendingRegistration(string email);
    bool ValidateOtp(string email, string otp);
    void RemoveOtp(string email);

    // Quản lý OTP cho Đăng nhập (Login)
    void SaveLoginOtp(User user, string otp, int expireMinutes = 5);
    PendingLogin? GetPendingLogin(string email);
    bool ValidateLoginOtp(string email, string otp);
    void RemoveLoginOtp(string email);

    // Quản lý cooldown giãn cách gửi lại OTP (60s)
    bool CanResendOtp(string email, out int remainingCooldownSeconds);
}

