using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
    public class PasswordResetOTP :BaseEntity
    {
        public int Id { get; set; }
      
        public string Code { get; set; }
        public DateTime Expiration { get; set; }
        public bool IsUsed { get; set; } = false;
        public Guid AppUserId { get; set; }
        public AppUser User { get; set; }
    }
}
