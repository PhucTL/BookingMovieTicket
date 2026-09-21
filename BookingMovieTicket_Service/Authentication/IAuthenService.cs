using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Authentication.Request;
using BookingMovieTicket.Contracts.DTOs.Authentication.Response;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Authentication
{
    public interface IAuthenService
    {
        Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request);
    }
}
