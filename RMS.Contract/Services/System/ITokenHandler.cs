using RMS.Contract.DTOs;
using RMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RMS.Contract.Services.System
{
    public interface ITokenHandler
    {
        Task<string> CreateAccessTokenAsync(AppUser user);
        Task<string> CreateRefreshTokenAsync(AppUser user);
        Task<LoginResponseDTO> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task RevokeTokenAsync(string refreshToken);
    }
}
