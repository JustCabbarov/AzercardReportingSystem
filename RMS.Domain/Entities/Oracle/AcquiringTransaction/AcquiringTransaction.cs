using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.AcquiringTransaction
{

    // ── Filter ───────────────────────────────────────────────────────────────────
    public class AcquiringDeviceFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Kateqoriya parametri: "volume" | "count" | "avg"
        public string Category { get; set; } = "volume";

        // Məbləğ vahidi: "min" | "mln" | "mlrd"
        public string AmountUnit { get; set; } = "mln";

        public List<string>? AcquiringDeviceTypes { get; set; }
        public List<string>? TransGroups { get; set; }
        public List<string>? OperationTypes { get; set; }
        public List<string>? TokenStatuses { get; set; }
        public List<string>? Mccs { get; set; }
        public List<string>? AcquiringCategories { get; set; }
        public List<string>? TargetBankNames { get; set; }
        public List<string>? SourceBankNames { get; set; }
        public List<string>? TargetCities { get; set; }
        public List<string>? PaymentSystems { get; set; }
        public List<string>? ContactlessStatuses { get; set; }
    }

    // CrossTable / Trend / XY üçün dimension request-ləri
    public class AcquiringTrendRequest : AcquiringDeviceFilter
    {
        public string Dimension { get; set; } = "acquiring_device_type_calc";
        public List<string>? DimValues { get; set; }
        public string Granularity { get; set; } = "month";
    }

    // ── Filter Options ────────────────────────────────────────────────────────────
    public class AcquiringFilterOptionsResponse
    {
        public List<string> AcquiringDeviceTypes { get; set; } = new();
        public List<string> TransGroups { get; set; } = new();
        public List<string> OperationTypes { get; set; } = new();
        public List<string> TokenStatuses { get; set; } = new();
        public List<string> Mccs { get; set; } = new();
        public List<string> AcquiringCategories { get; set; } = new();
        public List<string> TargetBanks { get; set; } = new();
        public List<string> SourceBanks { get; set; } = new();
        public List<string> TargetCities { get; set; } = new();
        public List<string> PaymentSystems { get; set; } = new();
        public List<string> ContactlessStatuses { get; set; } = new();
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
    }

    // ── Device Summary (sağ panel cədvəli) ───────────────────────────────────────
    public class DeviceSummaryRow
    {
        public string DeviceType { get; set; } = "";
        public decimal Volume { get; set; }
        public long Count { get; set; }
        public decimal AvgAmount { get; set; }
    }

    // ── Qrafik 1: Pay qrafiki ────────────────────────────────────────────────────
    public class AcqPieItem
    {
        public string Label { get; set; } = "";
        public decimal Value { get; set; }
        public long Count { get; set; }
        public double Percent { get; set; }
    }

    // ── Qrafik 2: Trans Group ────────────────────────────────────────────────────
    public class TransGroupItem
    {
        public string TransGroup { get; set; } = "";
        public decimal Value { get; set; }
        public long Count { get; set; }
        public double Percent { get; set; }
    }

    // ── Qrafik 3: Payment System ─────────────────────────────────────────────────
    public class PaymentSystemItem
    {
        public string PaymentSystem { get; set; } = "";
        public decimal Value { get; set; }
        public long Count { get; set; }
        public double Percent { get; set; }
    }

    // ── Qrafik 4: Trend + Proqnoz ────────────────────────────────────────────────
    public class AcqTrendSeries
    {
        public string Label { get; set; } = "";
        public List<AcqTrendPoint> Points { get; set; } = new();
    }

    public class AcqTrendPoint
    {
        public DateTime Period { get; set; }
        public decimal Actual { get; set; }
        public decimal Forecast { get; set; }
        public bool IsForecast { get; set; }
        public decimal ForecastLower { get; set; }
        public decimal ForecastUpper { get; set; }
        public float ForecastAccuracy { get; set; }
        public string? ForecastModel { get; set; }
    }

    public class AcqTrendResponse
    {
        public List<AcqTrendSeries> Series { get; set; } = new();
    }

    // ── Qrafik 5 & 6: Bank bölgüsü ───────────────────────────────────────────────
    public class BankItem
    {
        public string BankName { get; set; } = "";
        public decimal Value { get; set; }
        public long Count { get; set; }
        public double Percent { get; set; }
    }

    // ── Tam Dashboard cavabı ──────────────────────────────────────────────────────
    public class AcquiringDashboardResponse
    {
        public List<DeviceSummaryRow> DeviceSummary { get; set; } = new();
        public List<AcqPieItem> PieChart { get; set; } = new();
        public List<TransGroupItem> TransGroupChart { get; set; } = new();
        public List<PaymentSystemItem> PaymentSysChart { get; set; } = new();
        public List<BankItem> SourceBankChart { get; set; } = new();
        public List<BankItem> TargetBankChart { get; set; } = new();
    }



}
