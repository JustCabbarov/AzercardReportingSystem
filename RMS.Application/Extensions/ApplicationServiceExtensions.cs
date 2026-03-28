using Application.Services;
using Contract.Services;
using Microsoft.Extensions.DependencyInjection;
using RMS.Application.Services;
using RMS.Contract.Services;
using RMS.Persitence.Repositories;
using System;
using System.Collections.Generic;
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



            return services;
        }


    }
}
