namespace BookingMovieTicket.Contracts.DTOs.Authentication.Response;

public class VerifyOtpForgotPasswordResponse
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public string Message { get; set; } = "Xác thực OTP thành công. Vui lòng tiến hành đặt lại mật khẩu mới.";
}

