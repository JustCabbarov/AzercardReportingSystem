using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MvThresholdAnomaly : BaseEntity
    {
        public DateTime ReportMonth { get; set; }
        public string PaymentSystem { get; set; } = string.Empty;
        public string CardProductTypeCategory { get; set; } = string.Empty;
        public string MccGroup { get; set; } = string.Empty;
        public string SourceCountry { get; set; } = string.Empty;

        // Cari dövr metrikalar?
        public decimal TxnCount { get; set; }
        public decimal TxnAmount { get; set; }
        public decimal LocalAmt { get; set; }

        // ?vv?lki dövr metrikalar?
        public decimal? PrevTxnCount { get; set; }
        public decimal? PrevTxnAmount { get; set; }

        // D?yi?im faizl?ri
        public decimal? PctChangeCount { get; set; }
        public decimal? PctChangeAmount { get; set; }

        /// <summary>1 = art?m alert, -1 = azalma alert, 0 = normal</summary>
        public int AlertFlag { get; set; }

        /// <summary>INCREASE_ALERT | DECREASE_ALERT | NORMAL</summary>
        public string AlertType { get; set; } = string.Empty;

        /// <summary>CRITICAL (?50%) | HIGH (?25%) | MEDIUM (?10%) | LOW</summary>
        public string AlertSeverity { get; set; } = string.Empty;

        public DateTime AlertCreatedAt { get; set; }
    }
}
