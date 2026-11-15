namespace RMS.Presentation.Caching;

public sealed class CacheStatsTracker
{
    private long _outputHits;
    private long _outputMisses;
    private long _lastWarmupDurationMs;
    private int _warmedEndpointsCount;
    private DateTimeOffset? _lastWarmupAt;

    public void RecordHit() => Interlocked.Increment(ref _outputHits);
    public void RecordMiss() => Interlocked.Increment(ref _outputMisses);

    public void RecordWarmup(int warmedCount, long durationMs)
    {
        Interlocked.Exchange(ref _warmedEndpointsCount, warmedCount);
        Interlocked.Exchange(ref _lastWarmupDurationMs, durationMs);
        _lastWarmupAt = DateTimeOffset.UtcNow;
    }

    public (long Hits, long Misses, double HitRatioPct, DateTimeOffset? LastWarmupAt, long LastWarmupDurationMs, int WarmedEndpointsCount) Snapshot()
    {
        var hits = Interlocked.Read(ref _outputHits);
        var misses = Interlocked.Read(ref _outputMisses);
        var total = hits + misses;
        var ratio = total > 0 ? Math.Round(hits * 100.0 / total, 2) : 0.0;

        return (
            hits,
            misses,
            ratio,
            _lastWarmupAt,
            Interlocked.Read(ref _lastWarmupDurationMs),
            Volatile.Read(ref _warmedEndpointsCount));
    }
}

