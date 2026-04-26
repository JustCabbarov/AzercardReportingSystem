//using Microsoft.Extensions.DependencyInjection;
//using RMS.Application.BackgroundServices;
//using RMS.Application.Services.Oracle.MLForcasting;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RMS.Application.Extensions
//{
//    public static IServiceCollection AddMLForecastServices(
//           this IServiceCollection services,
//           Microsoft.Extensions.Configuration.IConfiguration configuration)
//    {
//        // ── Konfiqurasiya ──────────────────────────────────────────────────
//        services.Configure<MLForecastOptions>(
//            configuration.GetSection(MLForecastOptions.Section));

//        // ── Servisler ──────────────────────────────────────────────────────
//        // ModelStore: disk I/O — Singleton (thread-safe, stateless)
//        services.AddSingleton<ModelStore>();

//        // MLForecastService: static ConcurrentDictionary cache var — Singleton
//        services.AddSingleton<IMLForecastService, MLForecastService>();

//        // ── Arxa plan xidməti (gecəlik retrain) ───────────────────────────
//        services.AddHostedService<ModelRetrainBackgroundService>();

//        return services;
//    }
//}
