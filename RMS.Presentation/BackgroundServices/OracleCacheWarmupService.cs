using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RMS.Presentation.Caching;

namespace RMS.Presentation.BackgroundServices;

public sealed class OracleCacheWarmupService : BackgroundService
{
    private static readonly string[] WarmupRoutes =
    [
        "/api/General/GetBanks",
        "/api/General/GetMccGroups",
        "/api/cards/filter-options",
        "/api/devices/filters",
        "/api/devices/latest-month",
        "/api/CardDashboard/filters",
        "/api/AcquiringDevice/filter-options",
        "/api/TransactionDetailed/banks",
        "/api/TransactionDetailed/devices",
        "/api/AzMap/cities",
        "/api/AzMap/regions",
        "/api/AzMap/banks",
        "/api/SummaryTransaction/summary",
        "/api/TransactionDetailed/summary",
        "/api/AcquiringDevice/dashboard",
        "/api/CardDashboard/dashboard",
        "/api/Forecasting?horizon=6"
    ];

    private readonly IServiceProvider _services;
    private readonly InMemoryCacheOptions _options;
    private readonly CacheStatsTracker _stats;
    private readonly ILogger<OracleCacheWarmupService> _logger;

    public OracleCacheWarmupService(
        IServiceProvider services,
        IOptions<InMemoryCacheOptions> options,
        CacheStatsTracker stats,
        ILogger<OracleCacheWarmupService> logger)
    {
        _services = services;
        _options = options.Value;
        _stats = stats;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.WarmupOnStartup)
            return;

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            await WarmupAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Oracle in-memory cache warmup zamanı gözlənilməz xəta.");
        }
    }

    public async Task<(int WarmedCount, long ElapsedMs)> WarmupAsync(CancellationToken ct = default)
    {
        var baseUrl = ResolveLocalBaseUrl();
        var sw = Stopwatch.StartNew();
        int warmed = 0;

        using var http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(120)
        };

        using var concurrency = new SemaphoreSlim(3, 3);
        var tasks = WarmupRoutes.Select(async route =>
        {
            await concurrency.WaitAsync(ct);
            try
            {
                using var resp = await http.GetAsync(route, ct);
                if (resp.IsSuccessStatusCode)
                {
                    Interlocked.Increment(ref warmed);
                }
                else
                {
                    _logger.LogWarning("Cache warmup {Route} HTTP {Status}", route, (int)resp.StatusCode);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache warmup xətası: {Route}", route);
            }
            finally
            {
                concurrency.Release();
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        _stats.RecordWarmup(warmed, sw.ElapsedMilliseconds);
        _logger.LogInformation(
            "In-memory cache warmup tamamlandı: {Warmed}/{Total} endpoint, {ElapsedMs} ms",
            warmed, WarmupRoutes.Length, sw.ElapsedMilliseconds);

        return (warmed, sw.ElapsedMilliseconds);
    }

    private string ResolveLocalBaseUrl()
    {
        var server = _services.GetService<IServer>();
        var addresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses;
        var raw = addresses?.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(raw))
            return "http://127.0.0.1:8080";

        return raw
            .Replace("://+:", "://127.0.0.1:")
            .Replace("://*:", "://127.0.0.1:")
            .Replace("://[::]:", "://127.0.0.1:")
            .Replace("://0.0.0.0:", "://127.0.0.1:");
    }
}

