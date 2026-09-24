using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Redis;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase? GetDatabase()
    {
        try
        {
            if (_redis.IsConnected)
            {
                return _redis.GetDatabase();
            }
            _logger.LogWarning("Redis is not connected currently.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis database instance.");
            return null;
        }
    }

    public async Task SetUserSessionAsync(string userId, string jti, TimeSpan expiry)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return;

            var key = $"user_session:{userId}";
            await db.StringSetAsync(key, jti, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting user session in Redis for userId: {UserId}", userId);
        }
    }

    public async Task<string?> GetUserSessionAsync(string userId)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return null;

            var key = $"user_session:{userId}";
            var value = await db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user session from Redis for userId: {UserId}", userId);
            return null;
        }
    }

    public async Task RemoveUserSessionAsync(string userId)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return;

            var key = $"user_session:{userId}";
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user session from Redis for userId: {UserId}", userId);
        }
    }
}

