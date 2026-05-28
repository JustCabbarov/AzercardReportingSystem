using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Services.System
{
    public class JwtOption
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Key { get; set; }
        public int ExpiryMinutes { get; set; } = 60;
        public int RefreshTokenExpiryDays { get; set; } = 20;
    }
}
