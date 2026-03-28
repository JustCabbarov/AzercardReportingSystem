using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MvAzerbaijanTerminalMap : BaseEntity
    {
        public string RegionName { get; set; } = string.Empty;

        /// <summary>X?rit? üçün t?mizl?nmi? region ad?</summary>
        public string RegionNameClean { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;
        public string MccName { get; set; } = string.Empty;
        public string RetailCategory { get; set; } = string.Empty;
        public string TransactionClass { get; set; } = string.Empty;

        /// <summary>CONTACTLESS | STANDARD</summary>
        public string CtlsStatus { get; set; } = string.Empty;

        public DateTime ReportMonth { get; set; }

        // Terminal say? metrikalar?
        public decimal TotalTerminalCount { get; set; }
        public decimal CtlsTerminalRatioPct { get; set; }

        // MoM müqayis?
        public decimal? PrevMonthTerminalCount { get; set; }
        public decimal? MomChangePct { get; set; }

        public DateTime LastRefresh { get; set; }
    }
}
