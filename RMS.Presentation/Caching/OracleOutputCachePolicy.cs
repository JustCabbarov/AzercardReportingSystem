using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace RMS.Presentation.Caching;

/// <summary>
/// Full in-memory OutputCache policy for all Oracle analytical endpoints.
/// Intentionally caches requests carrying Authorization headers (shared BI data)
/// while excluding Auth, System CRUD, Cache management, and Swagger routes.
/// </summary>
public sealed class OracleOutputCachePolicy : IOutputCachePolicy
{
    public const string AllOracleTag = "oracle-analytics";
    public const string ForecastingTag = "oracle-forecasting";

    private static readonly string[] OraclePrefixes =
    [
        "/api/AcquiringDevice",
        "/api/AzMap",
        "/api/CardActivity",
        "/api/CardDashboard",
        "/api/cards",
        "/api/devices",
        "/api/Forecasting",
        "/api/General",
        "/api/MarketBenchmark",
        "/api/NewCard",
        "/api/SectorSpend",
        "/api/SummaryTransaction",
        "/api/TransactionDetailed",
        "/api/WorldMap"
    ];

    private static readonly string[] LookupSegments =
    [
        "/filter-options",
        "/filters",
        "/latest-month",
        "/first-month",
        "/GetBanks",
        "/GetMccGroups",
        "/banks",
        "/devices",
        "/cities",
        "/regions"
    ];

    private readonly TimeSpan _defaultTtl;
    private readonly TimeSpan _lookupTtl;
    private readonly CacheStatsTracker _stats;

    public OracleOutputCachePolicy(InMemoryCacheOptions options, CacheStatsTracker stats)
    {
        _defaultTtl = TimeSpan.FromMinutes(Math.Max(1, options.DefaultExpirationMinutes));
        _lookupTtl = TimeSpan.FromMinutes(Math.Max(1, options.LookupExpirationMinutes));
        _stats = stats;
    }

    public static bool IsOracleAnalyticsPath(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var prefix in OraclePrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var request = context.HttpContext.Request;

        if ((!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) ||
            !IsOracleAnalyticsPath(request.Path))
        {
            return ValueTask.CompletedTask;
        }

        var pathValue = request.Path.Value ?? string.Empty;
        var isLookup = LookupSegments.Any(s => pathValue.EndsWith(s, StringComparison.OrdinalIgnoreCase));

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;
        context.AllowLocking = !isLookup; // Stampede protection on concurrent 600M-row queries; keep lookups lock-free
        context.CacheVaryByRules.QueryKeys = "*";
        context.CacheVaryByRules.VaryByHost = false;

        context.ResponseExpirationTimeSpan = isLookup ? _lookupTtl : _defaultTtl;

        context.Tags.Add(AllOracleTag);
        if (pathValue.StartsWith("/api/Forecasting", StringComparison.OrdinalIgnoreCase))
        {
            context.Tags.Add(ForecastingTag);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        _stats.RecordHit();
        context.HttpContext.Response.Headers["X-Cache"] = "HIT";
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;

        if (response.StatusCode != StatusCodes.Status200OK ||
            !StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        _stats.RecordMiss();
        return ValueTask.CompletedTask;
    }
}

