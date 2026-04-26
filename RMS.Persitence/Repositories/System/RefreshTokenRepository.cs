using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RMS.Domain.Entities;
using RMS.Domain.Repositories.System;
using RMS.Persitence.Data;
using RMS.Persitence.Repositories.System;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    private readonly ILogger<RefreshTokenRepository> _logger;

    public RefreshTokenRepository(AppDbContext context, ILogger<RefreshTokenRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        _logger.LogInformation("Refresh token axtarılır.");

        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task RevokeTokenAsync(string token)
    {
        _logger.LogInformation("Refresh token ləğv edilir.");

        var storedToken = await _dbSet.FirstOrDefaultAsync(rt => rt.Token == token);

        if (storedToken is null || storedToken.IsRevoked)
        {
            _logger.LogWarning("Token tapılmadı və ya artıq ləğv edilib. Token: {Token}", token);
            throw new UnauthorizedAccessException("Token tapılmadı və ya artıq ləğv edilib.");
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Token uğurla ləğv edildi.");
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        _logger.LogInformation("Yeni refresh token əlavə edilir. UserId: {UserId}", refreshToken.UserId);

        await _dbSet.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token uğurla əlavə edildi. UserId: {UserId}", refreshToken.UserId);
    }
}