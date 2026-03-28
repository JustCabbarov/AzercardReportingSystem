using Contract.Services;

using Microsoft.AspNetCore.Identity;
using RMS.Contract.DTOs;
using RMS.Contract.Services;
using RMS.Domain.Entities;
using RMS.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IPasswordResetRepository _otpRepo;
        private readonly IEmailSender _emailSender;
        private readonly IUnityOfWork _unityOfWork;

        public PasswordResetService(
            UserManager<AppUser> userManager,
            IPasswordResetRepository otpRepo,
            IEmailSender emailSender,
            IUnityOfWork unityOfWork)
        {
            _userManager = userManager;
            _otpRepo = otpRepo;
            _emailSender = emailSender;
            _unityOfWork = unityOfWork;
        }

        public async Task SendOtpAsync(OTPrequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                throw new InvalidOperationException("User tapılmadı");

            var otp = new Random().Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(5);

            var otpEntity = new PasswordResetOTP
            {
                AppUserId = user.Id,
                Code = otp,
                Expiration = expiration
            };

            await _otpRepo.AddAsync(otpEntity);
           await _unityOfWork.SaveChangesAsync();

            await _emailSender.SendOtpEmailAsync(request.Email,otp );
        }

        public async Task<IdentityResult> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new InvalidOperationException("User tapılmadı");

            var otpEntity = await _otpRepo.GetValidOtpAsync(user.Id, otp);
            if (otpEntity == null)
                throw new InvalidOperationException("Kod yanlışdır və ya müddəti bitib");

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                await _otpRepo.RemoveAsync(otpEntity);
            }

            return result;
        }
      
    }

}
