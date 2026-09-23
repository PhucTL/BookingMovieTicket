using BookingMovieTicket.Contracts.DTOs.Authentication.Request;
using BookingMovieTicket_Repository.Entities;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Security.Cryptography;

namespace BookingMovieTicket_Service.Authentication.OTP;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private const string OtpRegKeyPrefix = "OTP_REG_";
    private const string OtpLoginKeyPrefix = "OTP_LOGIN_";
    private const string OtpForgotKeyPrefix = "OTP_FORGOT_";
    private const string CooldownKeyPrefix = "OTP_COOLDOWN_";
    private const string ResetSessionKeyPrefix = "RESET_SESSION_";

    public OtpService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string GenerateOtp(int length = 6)
    {
        // Sinh ngẫu nhiên OTP
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var randomInt = Math.Abs(BitConverter.ToInt32(bytes, 0));
        var otp = (randomInt % 900000 + 100000).ToString();
        return otp;
    }

    // 1. Quản lý OTP cho (Register)
    public void SaveRegistrationOtp(RegisterRequest request, string otp, int expireMinutes = 5)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var cacheKey = OtpRegKeyPrefix + normalizedEmail;
        var cooldownKey = CooldownKeyPrefix + normalizedEmail;

        var pending = new PendingRegistration
        {
            Request = request,
            Otp = otp,
            CreatedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, pending, TimeSpan.FromMinutes(expireMinutes));
        _cache.Set(cooldownKey, DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(60));
    }

    public PendingRegistration? GetPendingRegistration(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = OtpRegKeyPrefix + normalizedEmail;
        return _cache.TryGetValue<PendingRegistration>(cacheKey, out var pending) ? pending : null;
    }

    public bool ValidateOtp(string email, string otp)
    {
        var pending = GetPendingRegistration(email);
        if (pending == null)
        {
            return false;
        }

        return string.Equals(pending.Otp.Trim(), otp.Trim(), StringComparison.Ordinal);
    }

    public void RemoveOtp(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = OtpRegKeyPrefix + normalizedEmail;
        var cooldownKey = CooldownKeyPrefix + normalizedEmail;

        _cache.Remove(cacheKey);
        _cache.Remove(cooldownKey);
    }

    // 2. Quản lý OTP cho (Login)
    public void SaveLoginOtp(User user, string otp, int expireMinutes = 5)
    {
        var normalizedEmail = user.Email.Trim().ToLower();
        var cacheKey = OtpLoginKeyPrefix + normalizedEmail;
        var cooldownKey = CooldownKeyPrefix + normalizedEmail;

        var pending = new PendingLogin
        {
            User = user,
            Otp = otp,
            CreatedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, pending, TimeSpan.FromMinutes(expireMinutes));
        _cache.Set(cooldownKey, DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(60));
    }

    public PendingLogin? GetPendingLogin(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = OtpLoginKeyPrefix + normalizedEmail;
        return _cache.TryGetValue<PendingLogin>(cacheKey, out var pending) ? pending : null;
    }

    public bool ValidateLoginOtp(string email, string otp)
    {
        var pending = GetPendingLogin(email);
        if (pending == null)
        {
            return false;
        }

        return string.Equals(pending.Otp.Trim(), otp.Trim(), StringComparison.Ordinal);
    }

    public void RemoveLoginOtp(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = OtpLoginKeyPrefix + normalizedEmail;
        var cooldownKey = CooldownKeyPrefix + normalizedEmail;

        _cache.Remove(cacheKey);
        _cache.Remove(cooldownKey);
    }

    // 3. Quản lý OTP cho Quên mật khẩu (Forgot Password)
    public void SaveForgotPasswordOtp(User user, string otp, int expireMinutes = 5)
    {
        var normalizedEmail = user.Email.Trim().ToLower();
        var cacheKey = OtpForgotKeyPrefix + normalizedEmail;
        var cooldownKey = CooldownKeyPrefix + normalizedEmail;

        var pending = new PendingForgotPassword
        {
            User = user,
            Otp = otp,
            CreatedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, pending, TimeSpan.FromMinutes(expireMinutes));
        _cache.Set(cooldownKey, DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(60));
    }

    public PendingForgotPassword? GetPendingForgotPassword(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = OtpForgotKeyPrefix + normalizedEmail;
        return _cache.TryGetValue<PendingForgotPassword>(cacheKey, out var pending) ? pending : null;
    }

    public bool ValidateForgotPasswordOtp(string email, string otp)
    {
        var pending = GetPendingForgotPassword(email);
        if (pending == null)
        {
            return false;
        }

        return string.Equals(pending.Otp.Trim(), otp.Trim(), StringComparison.Ordinal);
    }

    public void RemoveForgotPasswordOtp(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = OtpForgotKeyPrefix + normalizedEmail;
        var cooldownKey = CooldownKeyPrefix + normalizedEmail;

        _cache.Remove(cacheKey);
        _cache.Remove(cooldownKey);
    }

    // 4. Quản lý phiên đặt lại mật khẩu sau khi Verify OTP thành công (Reset Password)
    public string SavePasswordResetSession(string email, int expireMinutes = 10)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = ResetSessionKeyPrefix + normalizedEmail;
        var resetToken = Guid.NewGuid().ToString("N");

        var session = new PasswordResetSession
        {
            Email = normalizedEmail,
            ResetToken = resetToken,
            VerifiedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, session, TimeSpan.FromMinutes(expireMinutes));
        return resetToken;
    }

    public bool ValidateResetSession(string email, string? resetToken)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = ResetSessionKeyPrefix + normalizedEmail;

        if (_cache.TryGetValue<PasswordResetSession>(cacheKey, out var session) && session != null)
        {
            if (string.IsNullOrWhiteSpace(resetToken))
            {
                return true; // Đã verify OTP hợp lệ
            }

            return string.Equals(session.ResetToken, resetToken.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public void RemoveResetSession(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cacheKey = ResetSessionKeyPrefix + normalizedEmail;
        _cache.Remove(cacheKey);
    }

    // 5. Quản lý Cooldown gửi lại OTP
    public bool CanResendOtp(string email, out int remainingCooldownSeconds)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cooldownKey = CooldownKeyPrefix + normalizedEmail;

        if (_cache.TryGetValue<DateTime>(cooldownKey, out var expireTime))
        {
            var remaining = (int)Math.Ceiling((expireTime - DateTime.UtcNow).TotalSeconds);
            remainingCooldownSeconds = remaining > 0 ? remaining : 0;
            return remainingCooldownSeconds <= 0;
        }

        remainingCooldownSeconds = 0;
        return true;
    }
}

