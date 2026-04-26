using System;
using System.Collections.Generic;
using System.Text;

namespace RMS.Contract.Services.System
{
    public interface IEmailSender
    {
        public Task SendOtpEmailAsync(string toEmail, string otp);
    }
}
