namespace RMS.Domain.Entities.Oracle.SummaryTransaction;

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

// ─── Device breakdown Dto ─────────────────────────────────────────────────────
public class DeviceBreakdownDto
{
    public string DeviceType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long Count { get; set; }
}

// ─── KPI Dto ──────────────────────────────────────────────────────────────────
public class KpiBlockDto
{
    public decimal Amount { get; set; }
    public long Count { get; set; }
    public List<DeviceBreakdownDto> Devices { get; set; } = [];
}

// ─── Ana response ─────────────────────────────────────────────────────────────
public class SummaryTransactionResponse
{
    public KpiBlockDto Total { get; set; } = new();  // 1
    public KpiBlockDto Issuing { get; set; } = new();  // 2 + 2.1–2.5
    public KpiBlockDto Inner { get; set; } = new();  // 3 + 3.1–3.5
    public KpiBlockDto Acquiring { get; set; } = new();  // 4 + 4.1–4.5
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