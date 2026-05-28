using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
    // NotificationThreshold.cs
    public class NotificationThreshold : BaseEntity
    {
        public double ThresholdPercentage { get; set; }
        public bool IsActive { get; set; } = true;

        // EnumCategory: "ThresholdDirection" → EnumValues: "Increase", "Decrease", "Both"
        public Guid DirectionId { get; set; }
        public EnumValue? Direction { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
