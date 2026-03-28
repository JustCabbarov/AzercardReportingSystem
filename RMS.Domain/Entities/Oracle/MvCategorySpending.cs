using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MvCategorySpending : BaseEntity
    {
        public DateTime ReportMonth { get; set; }
        public string MccGroup { get; set; } = string.Empty;
        public string Mcc { get; set; } = string.Empty;


        public string CategoryLabel { get; set; } = string.Empty;

        public string PaymentSystem { get; set; } = string.Empty;
        public string CardProductTypeCategory { get; set; } = string.Empty;
        public string SourceChannel { get; set; } = string.Empty;
        public string SourceCountry { get; set; } = string.Empty;

        /// <summary>Y | N</summary>
        public string IsContactless { get; set; } = string.Empty;

        // M?bl?? metrikalar?
        public decimal TxnCount { get; set; }
        public decimal TxnAmount { get; set; }
        public decimal LocalAmount { get; set; }
        public decimal AvgTicketSize { get; set; }

        // MoM müqayis?
        public decimal? PrevMonthAmount { get; set; }
        public decimal? MomChangePct { get; set; }

        /// <summary>H?min ay?n ümumi x?rcl?rind?n bu kateqoriyan?n pay? (%)</summary>
        public decimal CategorySharePct { get; set; }

        public DateTime LastRefresh { get; set; }
    }
}

