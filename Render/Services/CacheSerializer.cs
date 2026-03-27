using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Render.Server.Services
{
    public interface ICacheSerializer
    {
        Task SetAsync<T>(IDistributedCache cache, string key, T value, TimeSpan? absoluteExpiration = null);
        Task<T?> GetAsync<T>(IDistributedCache cache, string key);
        Task RemoveAsync(IDistributedCache cache, string key);
    }

    public class CacheSerializer : ICacheSerializer
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task SetAsync<T>(IDistributedCache cache, string key, T value, TimeSpan? absoluteExpiration = null)
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            var options = new DistributedCacheEntryOptions();
            if (absoluteExpiration.HasValue)
                options.SetAbsoluteExpiration(absoluteExpiration.Value);
            await cache.SetAsync(key, bytes, options);
        }

        public async Task<T?> GetAsync<T>(IDistributedCache cache, string key)
        {
            var bytes = await cache.GetAsync(key);
            if (bytes == null || bytes.Length == 0)
                return default;
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public Task RemoveAsync(IDistributedCache cache, string key)
        {
            return cache.RemoveAsync(key);
        }
    }
}
