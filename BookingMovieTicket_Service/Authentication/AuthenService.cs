using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Authentication.Request;
using BookingMovieTicket.Contracts.DTOs.Authentication.Response;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Authentication
{
    public class AuthenService : IAuthenService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;

        public AuthenService(IUnitOfWork unitOfWork, IJwtService jwtService)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
        }

        public async Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request)
        {
            // 1. Kiểm tra Username đã tồn tại chưa
            var existingUsername = await _unitOfWork.AuthenRepository.GetUserByUsernameAsync(request.Username);
            if (existingUsername != null)
            {
                return ApiResponse<RegisterResponse>.ErrorResult("Username này đã được sử dụng.");
            }

            // 2. Kiểm tra Email đã tồn tại chưa
            var existingEmail = await _unitOfWork.AuthenRepository.GetUserByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return ApiResponse<RegisterResponse>.ErrorResult("Email này đã được sử dụng.");
            }

            // 3. Chuyển đổi từ Request DTO sang Entity User
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username.Trim(),
                Email = request.Email.Trim().ToLower(),
                FullName = request.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = 0, // Mặc định role = 0 (Khách hàng)
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _unitOfWork.AuthenRepository.CreateUserAsync(user);

            // 4. Trả về RegisterResponse DTO
            var response = new RegisterResponse
            {
                Id = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                Role = createdUser.Role,
                Message = "Đăng ký tài khoản thành công."
            };

            return ApiResponse<RegisterResponse>.SuccessResult(response, "Đăng ký thành công.");
        }

        /// <summary>
        /// Đăng nhập truyền thống: sử dụng Username và Password
        /// </summary>
        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            // 1. Tìm User theo Username
            var user = await _unitOfWork.AuthenRepository.GetUserByUsernameAsync(request.Username);
            if (user == null)
            {
                return ApiResponse<AuthResponse>.ErrorResult("Tài khoản hoặc mật khẩu không chính xác.");
            }

            // 2. Kiểm tra mật khẩu băm với BCrypt (có fallback so sánh chuỗi nếu mật khẩu chưa hash)
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                isPasswordValid = (user.PasswordHash == request.Password);
            }

            if (!isPasswordValid)
            {
                return ApiResponse<AuthResponse>.ErrorResult("Tài khoản hoặc mật khẩu không chính xác.");
            }

            // 3. Sinh JWT Bearer Token
            var token = _jwtService.GenerateToken(user.Id, user.Username ?? user.Email, user.Email, user.Role);

            // 4. Trả về AuthResponse DTO kèm Bearer Token
            var authResponse = new AuthResponse
            {
                Token = token,
                RefreshToken = null,
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };

            return ApiResponse<AuthResponse>.SuccessResult(authResponse, "Đăng nhập thành công.");
        }

        /// <summary>
        /// Đăng nhập bằng Google: sử dụng Email
        /// </summary>
        public async Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request)
        {
            // 1. Tìm User theo Email
            var user = await _unitOfWork.AuthenRepository.GetUserByEmailAsync(request.Email);

            // 2. Nếu tài khoản chưa từng đăng nhập Google thì tự động tạo mới
            if (user == null)
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email.Trim().ToLower(),
                    FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Email.Split('@')[0] : request.FullName.Trim(),
                    Username = null, // Tài khoản Google có thể chưa có username
                    PasswordHash = string.Empty, // Đăng nhập Google không cần mật khẩu cục bộ
                    Role = 0,
                    CreatedAt = DateTime.UtcNow
                };

                user = await _unitOfWork.AuthenRepository.CreateUserAsync(newUser);
            }

            // 3. Sinh JWT Bearer Token
            var token = _jwtService.GenerateToken(user.Id, user.Username ?? user.Email, user.Email, user.Role);

            // 4. Trả về AuthResponse DTO kèm Bearer Token
            var authResponse = new AuthResponse
            {
                Token = token,
                RefreshToken = null,
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };

            return ApiResponse<AuthResponse>.SuccessResult(authResponse, "Đăng nhập Google thành công.");
        }
    }
}
