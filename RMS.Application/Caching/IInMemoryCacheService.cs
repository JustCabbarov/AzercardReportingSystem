namespace RMS.Application.Caching;

public interface IInMemoryCacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    void Remove(string key);
    void Clear();
    InMemoryCacheStats GetStats();
}

public sealed record InMemoryCacheStats(
    int ActiveKeys,
    long Hits,
    long Misses);

