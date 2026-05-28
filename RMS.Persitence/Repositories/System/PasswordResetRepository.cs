using Microsoft.EntityFrameworkCore;
using RMS.Domain.Entities;
using RMS.Domain.Repositories.System;
using RMS.Persitence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories.System
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly AppDbContext _context;

        public PasswordResetRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PasswordResetOTP otp)
        {
            await  _context.PasswordResetOtps.AddAsync(otp);
            
        }

        public async Task<PasswordResetOTP> GetValidOtpAsync(Guid userId, string code)
        {
            return await  _context.PasswordResetOtps
            .Where(x => x.AppUserId == userId && x.Code == code && x.Expiration > DateTime.UtcNow)
            .FirstOrDefaultAsync();
        }

        public async Task RemoveAsync(PasswordResetOTP otp)
        {
            var existingOtp = await _context.PasswordResetOtps.FindAsync(otp.Id);

            if (existingOtp != null)
            {
                existingOtp.IsUsed = true;
            }
            else
            {
                throw new InvalidOperationException("OTP not found.");
            }
        }


    }
}
