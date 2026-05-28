using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class NewCardActivation
    {
        public string BankName { get; set; } = "";
        public string CardProductName { get; set; } = "";
        public string? CardBrandName { get; set; }
        public string? ProductType { get; set; }
        public string? RegionNameClean { get; set; }
        public DateTime FirstMonth { get; set; }

        // Ay 1
        public long M1Cards { get; set; }
        public long M1Trans { get; set; }
        public decimal M1Amount { get; set; }
        public bool M1Active { get; set; }

        // Ay 2
        public long M2Trans { get; set; }
        public decimal M2Amount { get; set; }
        public bool M2Active { get; set; }

        // Ay 3
        public long M3Trans { get; set; }
        public decimal M3Amount { get; set; }
        public bool M3Active { get; set; }

        // C# tərəfindən hesablanır
        public int? MonthsToFirstUse =>
            M1Active ? 1 :
            M2Active ? 2 :
            M3Active ? 3 : null;

        public decimal Avg3MActiveRate =>
            M1Cards > 0
                ? Math.Round(((M1Active ? 1m : 0) +
                              (M2Active ? 1m : 0) +
                              (M3Active ? 1m : 0)) / 3 * 100, 1)
                : 0;

        public string ActivationSegment =>
            M1Trans > 5 ? "EarlyActive" :
            M2Trans > 0 ? "DelayedActive" :
            M3Trans > 0 ? "SlowActive" : "Inactive";
    }
}
