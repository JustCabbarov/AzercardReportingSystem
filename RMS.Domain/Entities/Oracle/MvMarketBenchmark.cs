using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MvMarketBenchmark : BaseEntity
    {

        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string PaymentScheme { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string CardBrandName { get; set; } = string.Empty;

        // Bank göst?ricil?ri
        public decimal BankCardCount { get; set; }
        public decimal BankTxnCount { get; set; }
        public decimal BankTxnAmount { get; set; }

        // Bazar c?mi
        public decimal MarketTotalCards { get; set; }
        public decimal MarketTotalTxnCount { get; set; }
        public decimal MarketTotalTxnAmount { get; set; }

        // Bazar paylar? (%)
        public decimal MarketShareCardsPct { get; set; }
        public decimal MarketShareTxnCntPct { get; set; }
        public decimal MarketShareTxnAmtPct { get; set; }

        // Ranking
        public int RankByAmount { get; set; }
        public int RankByCardCount { get; set; }

        /// <summary>?vv?lki ay bazar pay? (trend üçün)</summary>
        public decimal? PrevMarketSharePct { get; set; }

        public DateTime LastRefresh { get; set; }
    }
}
