using Microsoft.Extensions.Caching.Memory;
using WebTemplate.Domain.Interfaces;

namespace WebTemplate.Infrastructure.Services.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _keys = new();
    private readonly object _lock = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = _cache.Get<T>(key);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30); // Default 30 minutes
        }

        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            lock (_lock)
            {
                _keys.Remove(evictedKey.ToString()!);
            }
        });

        _cache.Set(key, value, options);

        lock (_lock)
        {
            _keys.Add(key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);

        lock (_lock)
        {
            _keys.Remove(key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        List<string> keysToRemove;

        lock (_lock)
        {
            keysToRemove = _keys.Where(k => k.StartsWith(prefixKey)).ToList();
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        lock (_lock)
        {
            foreach (var key in keysToRemove)
            {
                _keys.Remove(key);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory();
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
