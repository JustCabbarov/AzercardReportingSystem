using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace RMS.Application.Caching;

public sealed class InMemoryCacheService : IInMemoryCacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    private long _hits;
    private long _misses;

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            Interlocked.Increment(ref _hits);
            return cached;
        }

        var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null)
            {
                Interlocked.Increment(ref _hits);
                return cached;
            }

            Interlocked.Increment(ref _misses);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));
            var value = await factory(timeoutCts.Token);

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl,
                Size = 1
            };

            options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                if (evictedKey is string k)
                    _keys.TryRemove(k, out _);
            });

            _cache.Set(key, value, options);
            _keys.TryAdd(key, 0);
            return value;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void Clear()
    {
        foreach (var key in _keys.Keys.ToList())
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
    }

    public InMemoryCacheStats GetStats()
        => new(_keys.Count, Interlocked.Read(ref _hits), Interlocked.Read(ref _misses));
}

