using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Authentication.Request;
using BookingMovieTicket.Contracts.DTOs.Authentication.Response;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Authentication
{
    public interface IAuthenService
    {
        // 1. Đăng ký tài khoản
        Task<ApiResponse<string>> RegisterAsync(RegisterRequest request);

        // 2. Đăng nhập tài khoản
        Task<ApiResponse<string>> LoginAsync(LoginRequest request);

        // 3. Xác thực OTP 
        Task<ApiResponse<object>> VerifyOtpAsync(VerifyOtpRequest request);

        // 4. Gửi lại mã OTP
        Task<ApiResponse<string>> ResendOtpAsync(ResendOtpRequest request);

        // 5. Đăng nhập Google
        Task<ApiResponse<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request);
    }
}

