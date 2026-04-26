using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RMS.Contract.DTOs;
using RMS.Contract.Services.System;
using RMS.Domain.Entities;
using RMS.Domain.Repositories.System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RMS.Application.Services.System
{
    public class TokenHandler : ITokenHandler
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUser> _userManager;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<TokenHandler> _logger;

        public TokenHandler(
            IConfiguration configuration,
            UserManager<AppUser> userManager,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<TokenHandler> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }


        public async Task<string> CreateAccessTokenAsync(AppUser user)
        {
            _logger.LogInformation("Access token yaradılır. UserId: {UserId}", user.Id);

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["AccessTokenExpirationMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> CreateRefreshTokenAsync(AppUser user)
        {
            _logger.LogInformation("Refresh token yaradılır. UserId: {UserId}", user.Id);

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return Task.FromResult(refreshToken);
        }


        public async Task<LoginResponseDTO> RefreshTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Refresh token yenilənir.");

           
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Etibarsız refresh token. Token: {Token}", refreshToken);
                throw new UnauthorizedAccessException("Refresh token yanlış və ya müddəti bitib.");
            }

            
            await _refreshTokenRepository.RevokeTokenAsync(refreshToken);

            var newAccessToken = await CreateAccessTokenAsync(storedToken.User!);
            var newRefreshToken = await CreateRefreshTokenAsync(storedToken.User!);

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                Token = newRefreshToken,
                UserId = storedToken.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    Convert.ToDouble(_configuration["JwtSettings:RefreshTokenExpirationDays"])),
                IsRevoked = false
            });

            return new LoginResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpireAt = DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:AccessTokenExpirationMinutes"]))
            };
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Token ləğv edilir.");
            await _refreshTokenRepository.RevokeTokenAsync(refreshToken);
            _logger.LogInformation("Token uğurla ləğv edildi.");
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            _logger.LogInformation("Token doğrulanır.");

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = secretKey,
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            _logger.LogInformation("Token uğurla doğrulandı.");

            return Task.FromResult(true);
        }
    }
}