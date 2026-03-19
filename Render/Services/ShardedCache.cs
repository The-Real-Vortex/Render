using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Render.Server.Services;

/// <summary>
/// Routes each cache key to one of N Redis databases using consistent hashing (key % DatabaseCount).
/// This spreads data evenly across all available Redis databases.
/// </summary>
public interface IShardedCache
{
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null);
    Task<T?> GetAsync<T>(string key);
    Task RemoveAsync(string key);
}

public class ShardedCache : IShardedCache, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly int _databaseCount;
    private bool _disposed;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ShardedCache(IConnectionMultiplexer redis, int databaseCount = 16)
    {
        _redis = redis;
        _databaseCount = databaseCount;
    }

    /// <summary>
    /// Selects the Redis database index for a given key.
    /// Uses Math.Abs to avoid negative results from GetHashCode().
    /// </summary>
    private int GetDbIndex(string key) => Math.Abs(key.GetHashCode()) % _databaseCount;

    private IDatabase GetDb(string key) => _redis.GetDatabase(GetDbIndex(key));

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var expiry = absoluteExpiration ?? TimeSpan.FromMinutes(30);
        await GetDb(key).StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await GetDb(key).StringGetAsync(key);
        if (!value.HasValue || value.IsNullOrEmpty)
            return default;
        return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
    }

    public async Task RemoveAsync(string key)
    {
        await GetDb(key).KeyDeleteAsync(key);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _redis.Dispose();
            _disposed = true;
        }
    }
}
