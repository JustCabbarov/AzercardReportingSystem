using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.CardTransaction
{


    // ── ENUMS ────────────────────────────────────────────────────────────────────

    public enum DateGrouping { Monthly, Quarterly, Yearly }
    public enum RoundTo { None, Thousands, Millions, Billions }
    public enum BreakdownType { ProductType, PaymentSystem, PaymentType, CashNonCash }

    // ── REQUEST ──────────────────────────────────────────────────────────────────

    public class DashboardFilterRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateGrouping DateGrouping { get; set; } = DateGrouping.Monthly;
        public RoundTo RoundTo { get; set; } = RoundTo.None;

        // Filterlər (Slide 5)
        public string? TargetBankName { get; set; }
        public string? TransGroup { get; set; }
        public string? CardTypeCategory { get; set; }
        public string? PaymentSystem { get; set; }
        public string? PaymentType { get; set; }
        public string? OperationType { get; set; }
        public string? TokenStatus { get; set; }
        public bool? IsContactless { get; set; }
    }

    // ── KPI  (Widget 1-4) ────────────────────────────────────────────────────────

    public class KpiSummaryResponse
    {
        public decimal TotalAmount { get; set; }
        public long TotalTxCount { get; set; }
        public decimal AvgAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    // ── TREND  (Widget 5) ────────────────────────────────────────────────────────

    public class TrendPoint
    {
        public DateTime PeriodDate { get; set; }
        public string PeriodLabel { get; set; } = "";

        // Real data (mövcud aylar)
        public decimal TotalAmount { get; set; }
        public long TotalTxCount { get; set; }
        public decimal? PrevAmount { get; set; }
        public decimal? PrevTxCount { get; set; }
        public decimal? AmountChangePct { get; set; }
        public decimal? TxChangePct { get; set; }

        // SSA Forecast (gələcək aylar — real datalar null olur)
        public bool IsForecast { get; set; } = false;
        public decimal? ForecastAmount { get; set; }
        public decimal? ForecastAmountLow { get; set; }
        public decimal? ForecastAmountHigh { get; set; }
        public decimal? ForecastTxCount { get; set; }
        public decimal? ForecastTxCountLow { get; set; }
        public decimal? ForecastTxCountHigh { get; set; }
        public float? ForecastAccuracy { get; set; }
    }

    // ── BREAKDOWN  (Widget 6-9) ──────────────────────────────────────────────────

    public class BreakdownItem
    {
        public string Category { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public long TotalTxCount { get; set; }
        public decimal AmountPct { get; set; }
        public decimal TxCountPct { get; set; }
    }

    public class BreakdownResponse
    {
        public string GroupBy { get; set; } = "";
        public List<BreakdownItem> Items { get; set; } = new();
    }

    // ── FILTER LOOKUPS  (Slide 5) ────────────────────────────────────────────────

    public class FilterLookupResponse
    {
        public List<string> PaymentSystems { get; set; } = new();
        public List<string> PaymentTypes { get; set; } = new();
        public List<string> OperationTypes { get; set; } = new();
        public List<string> CardTypeCategories { get; set; } = new();
        public List<string> TokenStatuses { get; set; } = new();
        public List<string> TransGroups { get; set; } = new();
        public List<string> TargetBankNames { get; set; } = new();  // Slide 5-də var
    }

    // ── DASHBOARD (bütün widgetlər bir response) ─────────────────────────────────

    public class DashboardResponse
    {
        public KpiSummaryResponse Kpi { get; set; } = new();
        public List<TrendPoint> Trend { get; set; } = new();
        public BreakdownResponse ProductBreakdown { get; set; } = new();
        public BreakdownResponse PaymentSystem { get; set; } = new();
        public BreakdownResponse PaymentType { get; set; } = new();
        public BreakdownResponse CashNonCash { get; set; } = new();
    }
}
