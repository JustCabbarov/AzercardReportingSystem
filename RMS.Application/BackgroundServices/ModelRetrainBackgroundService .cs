//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using RMS.Application.Services.Oracle.MLForcasting;
//using RMS.Contract.Services.Oracle;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RMS.Application.BackgroundServices
//{
//    public sealed class ModelRetrainBackgroundService : BackgroundService
//    {
//        private readonly IServiceScopeFactory _scopeFactory;
//        private readonly MLForecastOptions _opts;
//        private readonly ILogger<ModelRetrainBackgroundService> _logger;

//        public ModelRetrainBackgroundService(
//            IServiceScopeFactory scopeFactory,
//            IOptions<MLForecastOptions> opts,
//            ILogger<ModelRetrainBackgroundService> logger)
//        {
//            _scopeFactory = scopeFactory;
//            _opts = opts.Value;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("ModelRetrainBackgroundService başladı.");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                // ── Növbəti işləmə vaxtını hesabla (TTL əsasında)
//                var delay = CalculateNextDelay();
//                _logger.LogInformation(
//                    "Növbəti yenidən öyrənmə: {Next} ({Hours:F1} saat sonra)",
//                    DateTime.Now.Add(delay), delay.TotalHours);

//                await Task.Delay(delay, stoppingToken);

//                if (stoppingToken.IsCancellationRequested) break;

//                await RetrainAllModelsAsync(stoppingToken);
//            }
//        }

//        private async Task RetrainAllModelsAsync(CancellationToken ct)
//        {
//            _logger.LogInformation("=== Toplu yenidən öyrənmə başladı ===");

//            try
//            {
//                await using var scope = _scopeFactory.CreateAsyncScope();

//                var mlService = scope.ServiceProvider.GetRequiredService<IMLForecastService>();
//                var forecastService = scope.ServiceProvider.GetRequiredService<IForecastService>();

//                // Bütün bankları tap (ForecastInput-da bank adları var)
//                var allData = await forecastService.GetAllAsync(ct);
//                var banks = allData.Select(x => x.BankName).Distinct().ToList();

//                foreach (var bank in banks)
//                {
//                    var mccGroups = (await forecastService.GetMccGroupsAsync(bank, ct)).ToList();

//                    _logger.LogInformation(
//                        "Bank={Bank} üçün {Count} MCC yenidən öyrənir...", bank, mccGroups.Count);

//                    var tasks = mccGroups.Select(async mcc =>
//                    {
//                        try { await mlService.RetrainAsync(bank, mcc, ct: ct); }
//                        catch (Exception ex)
//                        {
//                            _logger.LogWarning(
//                                "Retrain uğursuz: Bank={Bank}, MCC={Mcc}. Xəta: {Msg}",
//                                bank, mcc, ex.Message);
//                        }
//                    });

//                    await Task.WhenAll(tasks);
//                }

//                _logger.LogInformation("=== Toplu yenidən öyrənmə tamamlandı ===");
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogInformation("Yenidən öyrənmə ləğv edildi.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Toplu yenidən öyrənmə zamanı kritik xəta.");
//            }
//        }

//        /// <summary>
//        /// TTL əsasında növbəti işləmə vaxtını hesablayır.
//        /// Minimum 1 dəqiqə, maksimum TTL qədər gözlər.
//        /// </summary>
//        private TimeSpan CalculateNextDelay()
//        {
//            var ttl = _opts.ModelTtl;
//            // TTL-in 90%-i qədər gözlə ki, modellər vaxtında hazır olsun
//            var delay = TimeSpan.FromTicks((long)(ttl.Ticks * 0.9));
//            return delay < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delay;
//        }
//    }
//}

