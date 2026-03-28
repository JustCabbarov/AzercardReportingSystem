// RMS.Persitence/Extensions/RepositoryRegistrationExtensions.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RMS.Domain.Entities;
using RMS.Domain.Repositories;
using RMS.Persitence.Data;
using RMS.Persitence.Repositories;

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


            return services;
        }

       
    }
}