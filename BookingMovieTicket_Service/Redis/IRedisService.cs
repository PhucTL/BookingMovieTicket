using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Redis;

public interface IRedisService
{
    /// <summary>
    /// Lưu định danh phiên đăng nhập (JTI) của người dùng vào Redis kèm thời gian hết hạn (TTL)
    /// </summary>
    Task SetUserSessionAsync(string userId, string jti, TimeSpan expiry);

    /// <summary>
    /// Lấy JTI của phiên đăng nhập hiện tại từ Redis
    /// </summary>
    Task<string?> GetUserSessionAsync(string userId);

    /// <summary>
    /// Xóa phiên đăng nhập của người dùng khỏi Redis (khi logout hoặc đổi mật khẩu)
    /// </summary>
    Task RemoveUserSessionAsync(string userId);
}

