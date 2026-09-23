using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Authentication.Request;
using BookingMovieTicket.Contracts.DTOs.Authentication.Response;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Service.Authentication.Email;
using BookingMovieTicket_Service.Authentication.JWT;
using BookingMovieTicket_Service.Authentication.OTP;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Authentication
{
    public class AuthenService : IAuthenService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;

        public AuthenService(
            IUnitOfWork unitOfWork,
            IJwtService jwtService,
            IEmailService emailService,
            IOtpService otpService)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _emailService = emailService;
            _otpService = otpService;
        }

        // 1. Đăng ký tài khoản 
        public async Task<ApiResponse<string>> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra Username đã tồn tại trong DB chưa
            var existingUsername = await _unitOfWork.AuthenRepository.GetUserByUsernameAsync(request.Username);
            if (existingUsername != null)
            {
                return ApiResponse<string>.ErrorResult("Username này đã được sử dụng.");
            }

            // Kiểm tra Email đã tồn tại trong DB chưa
            var existingEmail = await _unitOfWork.AuthenRepository.GetUserByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return ApiResponse<string>.ErrorResult("Email này đã được sử dụng.");
            }

            // Sinh mã OTP
            var otp = _otpService.GenerateOtp();

            // Lưu thông tin đăng ký cùng OTP vào cache trong 5 phút
            _otpService.SaveRegistrationOtp(request, otp, 5);

            // Gửi OTP tới Gmail người dùng
            await _emailService.SendOtpEmailAsync(request.Email.Trim().ToLower(), otp, 5, "đăng ký tài khoản");

            return ApiResponse<string>.SuccessResult(
                $"Mã OTP xác thực đăng ký đã được gửi đến email {request.Email}. Mã có hiệu lực trong 5 phút.",
                "Gửi OTP thành công.");
        }

        // 2. Đăng nhập tài khoản
        public async Task<ApiResponse<string>> LoginAsync(LoginRequest request)
        {
            // Tìm user theo username
            var user = await _unitOfWork.AuthenRepository.GetUserByUsernameAsync(request.Username);
            if (user == null)
            {
                return ApiResponse<string>.ErrorResult("Tài khoản hoặc mật khẩu không chính xác.");
            }

            // Kiểm tra mật khẩu băm với BCrypt
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
                return ApiResponse<string>.ErrorResult("Tài khoản hoặc mật khẩu không chính xác.");
            }

            // Sinh mã OTP
            var otp = _otpService.GenerateOtp();

            // Lưu thông tin đăng nhập chờ xác thực OTP 
            _otpService.SaveLoginOtp(user, otp, 5);

            // Gửi OTP về Gmail
            await _emailService.SendOtpEmailAsync(user.Email, otp, 5, "đăng nhập");

            return ApiResponse<string>.SuccessResult(
                $"Mã OTP xác thực đăng nhập đã được gửi đến email {user.Email}. Mã có hiệu lực trong 5 phút.",
                "Xác thực mật khẩu hợp lệ. Vui lòng nhập OTP để hoàn tất đăng nhập.");
        }

        // 3. Xác thực OTP 
        public async Task<ApiResponse<object>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();

            // 1. Kiểm tra nếu là OTP Register
            var pendingReg = _otpService.GetPendingRegistration(normalizedEmail);
            if (pendingReg != null)
            {
                if (!_otpService.ValidateOtp(normalizedEmail, request.Otp))
                {
                    return ApiResponse<object>.ErrorResult("Mã OTP không chính xác.");
                }

                var existingUser = await _unitOfWork.AuthenRepository.GetUserByUsernameAsync(pendingReg.Request.Username);
                if (existingUser != null)
                {
                    return ApiResponse<object>.ErrorResult("Username này đã được sử dụng.");
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = pendingReg.Request.Username.Trim(),
                    Email = pendingReg.Request.Email.Trim().ToLower(),
                    FullName = pendingReg.Request.FullName.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(pendingReg.Request.Password),
                    Role = 0,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _unitOfWork.AuthenRepository.CreateUserAsync(user);
                _otpService.RemoveOtp(normalizedEmail);

                var registerResponse = new RegisterResponse
                {
                    Id = createdUser.Id,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    FullName = createdUser.FullName,
                    Role = createdUser.Role,
                    Message = "Đăng ký tài khoản thành công."
                };

                return ApiResponse<object>.SuccessResult(registerResponse, "Xác thực OTP thành công. Tài khoản đã được tạo.");
            }

            // 2. Kiểm tra nếu là OTP Login
            var pendingLogin = _otpService.GetPendingLogin(normalizedEmail);
            if (pendingLogin != null)
            {
                if (!_otpService.ValidateLoginOtp(normalizedEmail, request.Otp))
                {
                    return ApiResponse<object>.ErrorResult("Mã OTP không chính xác.");
                }

                _otpService.RemoveLoginOtp(normalizedEmail);

                var token = _jwtService.GenerateToken(pendingLogin.User.Id, pendingLogin.User.Username ?? pendingLogin.User.Email, pendingLogin.User.Email, pendingLogin.User.Role);
                var loginResponse = new LoginResponse
                {
                    Token = token,
                    RefreshToken = null,
                    Id = pendingLogin.User.Id,
                    Username = pendingLogin.User.Username,
                    Email = pendingLogin.User.Email,
                    FullName = pendingLogin.User.FullName,
                    Role = pendingLogin.User.Role
                };

                return ApiResponse<object>.SuccessResult(loginResponse, "Đăng nhập thành công.");
            }

            return ApiResponse<object>.ErrorResult("Mã OTP đã hết hạn hoặc không tồn tại. Vui lòng thực hiện đăng ký hoặc đăng nhập lại.");
        }

        // 4. Gửi lại mã OTP
        public async Task<ApiResponse<string>> ResendOtpAsync(ResendOtpRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();

            // Hạn chế 60 giây
            if (!_otpService.CanResendOtp(normalizedEmail, out var remainingSeconds))
            {
                return ApiResponse<string>.ErrorResult($"Vui lòng đợi {remainingSeconds} giây trước khi yêu cầu gửi lại mã OTP.");
            }

            // Kiểm tra có yêu cầu Đăng ký chờ duyệt không
            var pendingReg = _otpService.GetPendingRegistration(normalizedEmail);
            if (pendingReg != null)
            {
                var newOtp = _otpService.GenerateOtp();
                _otpService.SaveRegistrationOtp(pendingReg.Request, newOtp, 5);
                await _emailService.SendOtpEmailAsync(normalizedEmail, newOtp, 5, "đăng ký tài khoản");
                return ApiResponse<string>.SuccessResult(
                    $"Mã OTP mới đã được gửi về email {normalizedEmail}. Mã có hiệu lực trong 5 phút.",
                    "Gửi lại OTP thành công.");
            }

            // Kiểm tra có yêu cầu Đăng nhập chờ duyệt không
            var pendingLogin = _otpService.GetPendingLogin(normalizedEmail);
            if (pendingLogin != null)
            {
                var newOtp = _otpService.GenerateOtp();
                _otpService.SaveLoginOtp(pendingLogin.User, newOtp, 5);
                await _emailService.SendOtpEmailAsync(normalizedEmail, newOtp, 5, "đăng nhập");
                return ApiResponse<string>.SuccessResult(
                    $"Mã OTP mới đã được gửi về email {normalizedEmail}. Mã có hiệu lực trong 5 phút.",
                    "Gửi lại OTP thành công.");
            }

            return ApiResponse<string>.ErrorResult("Không tìm thấy yêu cầu xác thực OTP nào đang chờ cho email này.");
        }

        // 5. Đăng nhập Google
        public async Task<ApiResponse<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var user = await _unitOfWork.AuthenRepository.GetUserByEmailAsync(request.Email);

            if (user == null)
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email.Trim().ToLower(),
                    FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Email.Split('@')[0] : request.FullName.Trim(),
                    Username = null,
                    PasswordHash = string.Empty,
                    Role = 0,
                    CreatedAt = DateTime.UtcNow
                };

                user = await _unitOfWork.AuthenRepository.CreateUserAsync(newUser);
            }

            var token = _jwtService.GenerateToken(user.Id, user.Username ?? user.Email, user.Email, user.Role);

            var authResponse = new LoginResponse
            {
                Token = token,
                RefreshToken = null,
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };

            return ApiResponse<LoginResponse>.SuccessResult(authResponse, "Đăng nhập Google thành công.");
        }
    }
}

