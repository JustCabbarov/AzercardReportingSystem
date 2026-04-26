using Microsoft.AspNetCore.Identity;
using RMS.Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace RMS.Contract.Services.System
{
    public interface IPasswordResetService
    {
        Task SendOtpAsync( OTPrequest request);
        Task<IdentityResult> ResetPasswordAsync(string email, string otp, string newPassword);
    }
}
