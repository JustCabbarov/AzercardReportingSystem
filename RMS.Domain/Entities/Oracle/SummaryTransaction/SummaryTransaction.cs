using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.SummaryTransaction
{

    // ─── Filter Request ────────────────────────────────────────────────────────────
    public class SummaryFilterRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? TargetBankName { get; set; }
        public string? SourceBankName { get; set; }
        public string? CardProductType { get; set; }
        public string? PaymentSystem { get; set; }
        public string? TransGroup { get; set; }
        public string? OperationType { get; set; }
        public string? TokenStatus { get; set; }
        public string? IsContactless { get; set; }
        public string? TransactionCurrency { get; set; }
        public AmountScale Scale { get; set; } = AmountScale.Mln;
    }

    public enum AmountScale
    {
        Min = 1,
        Mln = 1_000_000,
        Mlrd = 1_000_000_000
    }

    // ─── KPI Dto ──────────────────────────────────────────────────────────────────
    public class KpiBlockDto
    {
        public decimal Amount { get; set; }
        public long Count { get; set; }
    }

    // ─── Device breakdown Dto ─────────────────────────────────────────────────────
    public class DeviceBreakdownDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long Count { get; set; }
    }

    // ─── Ana response ─────────────────────────────────────────────────────────────
    public class SummaryTransactionResponse
    {
        // 1. Total AZC
        public KpiBlockDto Total { get; set; } = new();

        // 2. Issuing only (IS_ISSUING=1, IS_ACQUIRING=0)
        public KpiBlockDto Issuing { get; set; } = new();
        public List<DeviceBreakdownDto> IssuingByDevice { get; set; } = [];

        // 3. Inner (IS_ISSUING=1, IS_ACQUIRING=1)
        public KpiBlockDto Inner { get; set; } = new();
        public List<DeviceBreakdownDto> InnerByDevice { get; set; } = [];

        // 4. Acquiring only (IS_ISSUING=0, IS_ACQUIRING=1)
        public KpiBlockDto Acquiring { get; set; } = new();
        public List<DeviceBreakdownDto> AcquiringByDevice { get; set; } = [];
    }

    // ─── Internal raw ─────────────────────────────────────────────────────────────
    public class SummaryRaw
    {
        public string IsIssuing { get; set; } = string.Empty;
        public string IsAcquiring { get; set; } = string.Empty;
        public string? AcquiringDeviceType { get; set; }
        public decimal TotalLocalAmount { get; set; }
        public long TotalCount { get; set; }
    }
}
