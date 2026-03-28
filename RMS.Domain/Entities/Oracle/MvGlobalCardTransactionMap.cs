using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MvGlobalCardTransactionMap : BaseEntity
    {


        /// <summary>Ölk? kateqoriyas? (TARGET_COUNTRY_CATEGORY)</summary>
        public string CountryCategory { get; set; } = string.Empty;

        public string PaymentSystem { get; set; } = string.Empty;
        public string CardProductTypeCategory { get; set; } = string.Empty;
        public string MccGroup { get; set; } = string.Empty;
        public string SourceChannel { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public DateTime ReportMonth { get; set; }

        // H?cm metrikalar?
        public decimal TotalTxnCount { get; set; }
        public decimal TotalTxnAmount { get; set; }
        public decimal TotalSettlementAmt { get; set; }
        public decimal TotalLocalAmt { get; set; }
        public decimal AvgTxnAmount { get; set; }

        // Nisb?t metrikalar?
        public decimal ContactlessRatioPct { get; set; }
        public decimal ChipRatioPct { get; set; }
        public decimal OnlineRatioPct { get; set; }

        public int DistinctCardProducts { get; set; }
        public DateTime LastRefresh { get; set; }
    }
}
