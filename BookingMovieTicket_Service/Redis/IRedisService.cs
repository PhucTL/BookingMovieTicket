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
    /// Xóa phiên đăng nhập của người dùng khỏi Redis
    /// </summary>
    Task RemoveUserSessionAsync(string userId);

    /// <summary>
    /// Lưu đối tượng bất kỳ vào Redis Cache dưới dạng JSON
    /// </summary>
    Task SetCacheAsync<T>(string key, T data, TimeSpan expiry);

    /// <summary>
    /// Lấy đối tượng từ Redis Cache theo Key
    /// </summary>
    Task<T?> GetCacheAsync<T>(string key);

    /// <summary>
    /// Xóa cache theo Key
    /// </summary>
    Task RemoveCacheAsync(string key);

    /// <summary>
    /// Chiếm Distributed Lock nguyên tử trên Redis (dùng SET NX PX)
    /// </summary>
    Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan expiry);

    /// <summary>
    /// Giải phóng Distributed Lock an toàn bằng Lua Script (chỉ xóa nếu lockValue khớp)
    /// </summary>
    Task<bool> ReleaseLockAsync(string lockKey, string lockValue);
}

