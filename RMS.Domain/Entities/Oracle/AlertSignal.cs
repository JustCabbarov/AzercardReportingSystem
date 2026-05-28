using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class AlertSignal
    {
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = "";
        public string MccGroup { get; set; } = "";
        public string? RegionNameClean { get; set; }
        public decimal CurAmount { get; set; }
        public decimal CurCount { get; set; }
        public decimal CurCards { get; set; }
        public decimal? PrevAmount { get; set; }
        public decimal? PrevCount { get; set; }
        public decimal? PrevCards { get; set; }
        public DateTime GeneratedAt { get; set; }

        // C# tərəfindən hesablanır
        public decimal? AmountChangePct =>
            PrevAmount > 0
                ? Math.Round((CurAmount - PrevAmount.Value) / PrevAmount.Value * 100, 2)
                : null;

        public bool IsAlert => AmountChangePct.HasValue && Math.Abs(AmountChangePct.Value) >= 10;

        public string SignalType =>
            AmountChangePct > 0 ? "Spike" : "Drop";

        public string Severity =>
            Math.Abs(AmountChangePct ?? 0) >= 50 ? "Critical" :
            Math.Abs(AmountChangePct ?? 0) >= 25 ? "High" : "Medium";
    }

}
