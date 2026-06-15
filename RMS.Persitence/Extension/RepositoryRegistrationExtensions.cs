// RMS.Persitence/Extensions/RepositoryRegistrationExtensions.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RMS.Domain.Entities;
using RMS.Domain.Repositories;
using RMS.Domain.Repositories.Oracle;
using RMS.Domain.Repositories.System;
using RMS.Persistence.Repositories.Cards;
using RMS.Persistence.Repositories.Oracle;
using RMS.Persitence.Data;
using RMS.Persitence.Repositories;
using RMS.Persitence.Repositories.Oracle;
using RMS.Persitence.Repositories.System;

namespace RMS.Persitence.Extensions
{
    public static class RepositoryRegistrationExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnityOfWork, UnityOfWork>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
            services.AddScoped<IForecastRepository, ForecastRepository>();
            services.AddScoped<IAlertRepository, AlertRepository>();
            services.AddScoped<IWorldMapRepository, WorldMapRepository>();
            services.AddScoped<IAzMapRepository, AzMapRepository>();
            services.AddScoped<ICardActivityRepository, CardActivityRepository>();
            services.AddScoped<IMarketBenchmarkRepository, MarketBenchmarkRepository>();
            services.AddScoped<ISectorSpendRepository, SectorSpendRepository>();
            services.AddScoped<INewCardRepository, NewCardRepository>();
            services.AddScoped<IForecastRepository, ForecastRepository>();
            services.AddSingleton<IModelStore, DiskModelStore>();
            services.AddScoped<ICardPortfolioRepository, CardPortfolioRepository>();


            services.AddScoped<IDevicesRepository, DevicesRepository>();
            services.AddScoped<IAcquiringDeviceRepository, AcquiringDeviceRepository>();
            services.AddScoped<ICardDashboardRepository, CardDashboardRepository>();    
            return services;
        }

       
    }
}