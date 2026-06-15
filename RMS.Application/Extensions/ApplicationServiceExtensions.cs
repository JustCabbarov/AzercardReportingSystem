using Microsoft.Extensions.DependencyInjection;
using RMS.Application.Services;
using RMS.Application.Services.Oracle;
using RMS.Application.Services.System;
using RMS.Contract.Services;
using RMS.Contract.Services.Oracle;
using RMS.Contract.Services.System;
using RMS.Domain.Repositories.Oracle;
using RMS.Persitence.Repositories;
using RMS.Persitence.Repositories.Oracle;
using RMS.Presentation.BackgroundServices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenHandler, TokenHandler>();
            services.AddScoped(typeof(IGenericService<,>), typeof(GenericService<,>));
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<ITokenHandler, TokenHandler>();
            services.AddScoped<IEmailSender, EmailSender>();

            services.AddScoped<IAlertService, AlertService>();
            services.AddScoped<IWorldMapService, WorldMapService>();
            services.AddScoped<IAzMapService, AzMapService>();
            services.AddScoped<ICardActivityService, CardActivityService>();
            services.AddScoped<IMarketBenchmarkService, MarketBenchmarkService>();
            services.AddScoped<ISectorSpendService, SectorSpendService>();
            services.AddScoped<INewCardService, NewCardService>();
            services.AddScoped<IForecastingService, SsaForecastingService>();
         services.AddHostedService<ForecastTrainingBackgroundService>();
            services.AddScoped<IDevicesService, DevicesService>();

            services.AddScoped<ICardPortfolioService, CardPortfolioService>();
            services.AddScoped<IAcquiringDeviceService, AcquiringDeviceService>();
            services.AddScoped<ICardDashboardService, CardDashboardService>();


            return services;
        }


    }
}
