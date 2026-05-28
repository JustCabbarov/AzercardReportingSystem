using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class MarketBenchmark
    {
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = "";
        public string? RegionNameClean { get; set; }
        public string? CardBrandName { get; set; }
        public string? ProductType { get; set; }
        public decimal BankTransAmount { get; set; }
        public long BankTransCount { get; set; }
        public long BankCardCount { get; set; }
        public decimal MarketTotalAmount { get; set; }
        public long MarketTotalCards { get; set; }

        // C# tərəfindən hesablanır
        public decimal MarketSharePct =>
            MarketTotalAmount > 0
                ? Math.Round(BankTransAmount / MarketTotalAmount * 100, 2)
                : 0;

        public decimal CardSharePct =>
            MarketTotalCards > 0
                ? Math.Round((decimal)BankCardCount / MarketTotalCards * 100, 2)
                : 0;

        // C# .OrderByDescending(x => x.BankTransAmount) ilə hesablanır
        public int BankRank { get; set; }
    }

}
