using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMS.Contract.Services.Oracle;

namespace RMS.Presentation.BackgroundServices;

/// <summary>
/// Hər gecə müəyyən saatda avtomatik train işlədir.
///
/// appsettings.json:
/// "ForecastTraining": { "RunAtHour": 2, "RunAtMinute": 30 }
///
/// Axış:
///   1. TrainGlobalAsync  → GLOBAL.zip
///   2. TrainAllAsync     → KAPITAL_FOOD.zip, ABB_RETAIL.zip ...
/// </summary>
public sealed class ForecastTrainingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ForecastTrainingBackgroundService> _logger;
    private readonly int _runAtHour;
    private readonly int _runAtMinute;

    public ForecastTrainingBackgroundService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<ForecastTrainingBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
        _runAtHour = configuration.GetValue("ForecastTraining:RunAtHour", 2);
        _runAtMinute = configuration.GetValue("ForecastTraining:RunAtMinute", 30);
    }

    // ----------------------------------------------------------------
    // Ana döngü
    // ----------------------------------------------------------------
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ForecastTrainingBackgroundService başladı. " +
            "Hər gecə {Hour:D2}:{Minute:D2}-də işləyəcək.",
            _runAtHour, _runAtMinute);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();

            _logger.LogInformation(
                "Növbəti train: {Hours}s {Minutes}d sonra ({NextRun:dd.MM.yyyy HH:mm}).",
                (int)delay.TotalHours,
                delay.Minutes,
                DateTime.Now.Add(delay));

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Servis dayandırılır — normal çıxış
                break;
            }

            await RunTrainingAsync(stoppingToken);
        }

        _logger.LogInformation("ForecastTrainingBackgroundService dayandı.");
    }

    // ----------------------------------------------------------------
    // Train axışı
    // BackgroundService Singleton-dur, IForecastingService isə Scoped —
    // hər run üçün yeni scope açılır
    // ----------------------------------------------------------------
    private async Task RunTrainingAsync(CancellationToken ct)
    {
        _logger.LogInformation("=== Gecəlik train başladı: {Time} ===", DateTime.Now);

        using var scope = _services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IForecastingService>();

        await RunStepAsync("Global model", () => svc.TrainGlobalAsync(ct), ct);
        await RunStepAsync("Bank-specific modellər", () => svc.TrainAllAsync(ct), ct);

        _logger.LogInformation("=== Gecəlik train tamamlandı: {Time} ===", DateTime.Now);
    }

    // ----------------------------------------------------------------
    // Hər addımı ayrıca try/catch ilə əhatə edir —
    // biri uğursuz olsa digəri işləməyə davam edir.
    // OperationCanceledException yenidən throw olunur ki,
    // servis düzgün dayansın.
    // ----------------------------------------------------------------
    private async Task RunStepAsync(
        string stepName, Func<Task> step, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            _logger.LogInformation("[{Step}] başladı.", stepName);
            await step();
            _logger.LogInformation("[{Step}] tamamlandı.", stepName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{Step}] ləğv edildi.", stepName);
            throw; // yuxarı ötür ki döngü düzgün çıxsın
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Step}] zamanı xəta baş verdi.", stepName);
        }
    }

    // ----------------------------------------------------------------
    // Növbəti run-a qədər olan vaxtı hesablayır
    // ----------------------------------------------------------------
    private TimeSpan TimeUntilNextRun()
    {
        var now = DateTime.Now;
        var next = new DateTime(now.Year, now.Month, now.Day, _runAtHour, _runAtMinute, 0);

        if (next <= now)
            next = next.AddDays(1);

        return next - now;
    }
}