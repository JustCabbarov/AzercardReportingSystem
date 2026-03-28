using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MyTransactionTrend : BaseEntity
    {
        /// <summary>Dövr (YYYY-MM format?nda, m?s: 2024-01)</summary>
        public string PeriodLabel { get; set; } = string.Empty;

        /// <summary>Dövr ba?lan??c tarixi</summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>Dövr sonu tarixi</summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>Tranzaksiya say?</summary>
        public long TransactionCount { get; set; }

        /// <summary>Ümumi h?cm (m?bl??)</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Ortalama tranzaksiya m?bl??i</summary>
        public decimal AvgAmount { get; set; }

        /// <summary>Unikal kart say?</summary>
        public int UniqueCardCount { get; set; }

        /// <summary>Unikal terminal say?</summary>
        public int UniqueTerminalCount { get; set; }

        /// <summary>?vv?lki dövr? n?z?r?n tranzaksiya say? d?yi?imi (%)</summary>
        public decimal CountChangePercent { get; set; }

        /// <summary>?vv?lki dövr? n?z?r?n m?bl?? d?yi?imi (%)</summary>
        public decimal AmountChangePercent { get; set; }

        /// <summary>Proqnozla?d?r?lm?? növb?ti dövr tranzaksiya say? (linear trend)</summary>
        public long ForecastedCount { get; set; }

        /// <summary>Proqnozla?d?r?lm?? növb?ti dövr m?bl??i</summary>
        public decimal ForecastedAmount { get; set; }

        /// <summary>MV-nin son yenil?nm? vaxt?</summary>
        public DateTime LastRefreshedAt { get; set; }
    }
}
