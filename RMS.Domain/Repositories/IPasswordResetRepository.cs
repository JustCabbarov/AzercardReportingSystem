using RMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories
{
    public interface IPasswordResetRepository
    {
        Task AddAsync(PasswordResetOTP otp);
        Task<PasswordResetOTP> GetValidOtpAsync(Guid userId, string code);
        Task RemoveAsync(PasswordResetOTP otp);
    }
}
