using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using RMS.Application.Caching;
using RMS.Presentation.BackgroundServices;
using RMS.Presentation.Caching;

namespace RMS.Presentation.Controllers.System;

[ApiController]
[Route("api/[controller]")]
public sealed class CacheController : ControllerBase
{
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly IInMemoryCacheService _memoryCache;
    private readonly CacheStatsTracker _stats;
    private readonly InMemoryCacheOptions _options;
    private readonly OracleCacheWarmupService _warmupService;

    public CacheController(
        IOutputCacheStore outputCacheStore,
        IInMemoryCacheService memoryCache,
        CacheStatsTracker stats,
        IOptions<InMemoryCacheOptions> options,
        OracleCacheWarmupService warmupService)
    {
        _outputCacheStore = outputCacheStore;
        _memoryCache = memoryCache;
        _stats = stats;
        _options = options.Value;
        _warmupService = warmupService;
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var snap = _stats.Snapshot();
        var memStats = _memoryCache.GetStats();

        return Ok(new
        {
            outputCache = new
            {
                hits = snap.Hits,
                misses = snap.Misses,
                hitRatioPct = snap.HitRatioPct,
                defaultTtlMinutes = _options.DefaultExpirationMinutes,
                lookupTtlMinutes = _options.LookupExpirationMinutes,
                sizeLimitMb = _options.SizeLimitMb,
                maximumBodySizeMb = _options.MaximumBodySizeMb
            },
            memoryCache = new
            {
                activeKeys = memStats.ActiveKeys,
                hits = memStats.Hits,
                misses = memStats.Misses
            },
            warmup = new
            {
                enabledOnStartup = _options.WarmupOnStartup,
                lastWarmupAt = snap.LastWarmupAt,
                lastWarmupDurationMs = snap.LastWarmupDurationMs,
                warmedEndpointsCount = snap.WarmedEndpointsCount
            }
        });
    }

    [HttpPost("clear")]
    [Authorize]
    public async Task<IActionResult> Clear(
        [FromQuery] string? tag = null,
        CancellationToken ct = default)
    {
        var effectiveTag = string.IsNullOrWhiteSpace(tag)
            ? OracleOutputCachePolicy.AllOracleTag
            : tag.Trim();

        await _outputCacheStore.EvictByTagAsync(effectiveTag, ct);

        if (effectiveTag.Equals(OracleOutputCachePolicy.AllOracleTag, StringComparison.OrdinalIgnoreCase))
        {
            _memoryCache.Clear();
        }

        return Ok(new
        {
            message = "In-memory cache təmizləndi.",
            evictedTag = effectiveTag
        });
    }

    [HttpPost("warmup")]
    [Authorize]
    public async Task<IActionResult> Warmup(CancellationToken ct = default)
    {
        var (warmedCount, elapsedMs) = await _warmupService.WarmupAsync(ct);
        return Ok(new
        {
            message = "In-memory cache warmup tamamlandı.",
            warmedCount,
            elapsedMs
        });
    }
}

