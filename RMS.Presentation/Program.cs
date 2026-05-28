using Application.Profiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.ExceptionHandler;
using RMS.Application.Exceptions;
using RMS.Application.Extensions;
using RMS.Application.Services.System;

using RMS.Domain.Entities;
using RMS.Persitence.Data;
using RMS.Persitence.Extensions;
using RMS.Presentation.ExceptionHandler;
using Serilog;
using System.Text;

namespace RMS.Presentation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection");

            // LOGGING
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Host.UseSerilog();

            // DATABASE
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // LAYERS
            builder.Services.AddApplicationServices();
            builder.Services.AddRepositories();

            // CONTROLLERS
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // IDENTITY
            builder.Services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // JWT
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
                };
            });

            // SWAGGER
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Task Management API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Bearer {token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.Configure<JwtOption>(builder.Configuration.GetSection("JwtOption"));

            // AUTOMAPPER
            builder.Services.AddAutoMapper(m => m.AddProfile(new CustomProfile()));

            builder.Services.AddExceptionHandler<NullExceptionHandler>();
            builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
            builder.Services.AddExceptionHandler<UnauthorizedExceptionHandler>();

            builder.Services.AddExceptionHandler<InvalidCredentialsExceptionHandler>();

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // ?n sona
            builder.Services.AddProblemDetails();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("cors",
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });

            var app = builder.Build();

            app.UseExceptionHandler();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();

            app.UseCors("cors");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}