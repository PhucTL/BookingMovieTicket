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

    public async Task SetCacheAsync<T>(string key, T data, TimeSpan expiry)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return;

            var json = System.Text.Json.JsonSerializer.Serialize(data);
            await db.StringSetAsync(key, json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache in Redis for key: {Key}", key);
        }
    }

    public async Task<T?> GetCacheAsync<T>(string key)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return default;

            var value = await db.StringGetAsync(key);
            if (!value.HasValue) return default;

            return System.Text.Json.JsonSerializer.Deserialize<T>(value.ToString()!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache from Redis for key: {Key}", key);
            return default;
        }
    }

    public async Task RemoveCacheAsync(string key)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return;

            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache from Redis for key: {Key}", key);
        }
    }

    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan expiry)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return false;

            // SET key value NX PX (Chỉ set nếu chưa tồn tại, tự hủy sau expiry)
            return await db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring distributed lock on Redis for key: {Key}", lockKey);
            return false;
        }
    }

    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return false;

            // Dùng Lua script để giải phóng lock an toàn (nguyên tử):
            // Chỉ xóa lock nếu giá trị trong Redis vẫn đúng là lockValue của caller hiện tại
            const string luaScript = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            var result = await db.ScriptEvaluateAsync(luaScript, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
            return (int)result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing distributed lock on Redis for key: {Key}", lockKey);
            return false;
        }
    }
}

