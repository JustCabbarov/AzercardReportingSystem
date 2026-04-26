using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class SectorSpend
    {
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = "";
        public string? RegionNameClean { get; set; }
        public string? MccGroup { get; set; }
        public string? Mcc { get; set; }
        public string? MccName { get; set; }
        public string? RetailCategory { get; set; }
        public string? TransactionClass { get; set; }
        public string? SourceChannel { get; set; }
        public string? OperationType { get; set; }
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }
        public decimal TotalLocalAmount { get; set; }

        // C# tərəfindən hesablanır
        public decimal? ShareOfWalletPct { get; set; }  // bank daxilində MCC payı
    }
}
