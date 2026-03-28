using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
    // SyncLog.cs
    public class SyncLog : BaseEntity
    {
        public string? ErrorMessage { get; set; }
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
        public int RecordsProcessed { get; set; }

        // EnumCategory: "SyncStatus" → EnumValues: "Success", "Failed", "Pending"
        public Guid StatusId { get; set; }                      
        public EnumValue? Status { get; set; }

    }
}
