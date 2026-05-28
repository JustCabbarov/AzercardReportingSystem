using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
  
    public class Notification : BaseEntity
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }


        public Guid TypeId { get; set; }
        public EnumValue? Type { get; set; }

        public Guid UserId { get; set; }
        public AppUser? User { get; set; }

        public Guid? NotificationThresholdId { get; set; }
        public NotificationThreshold? NotificationThreshold { get; set; }
    }
}
