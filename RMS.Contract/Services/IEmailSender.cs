using System;
using System.Collections.Generic;
using System.Text;

namespace RMS.Contract.Services
{
    public interface IEmailSender
    {
        public Task SendOtpEmailAsync(string toEmail, string otp);
    }
}
