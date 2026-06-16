using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.TransactionDeteiled
{

    public class TransactionSummaryResponse
    {
        public KpiDto Kpi { get; set; } = new();
        public List<CategoryDistributionDto> IssuingCategories { get; set; } = new();
        public List<BankDistributionDto> TargetBanks { get; set; } = new();
        public List<DeviceDistributionDto> AcquiringDevices { get; set; } = new();
    }

    public class KpiDto
    {
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }
        public decimal ChangePercent { get; set; }
    }

    public class CategoryDistributionDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long Count { get; set; }
        public decimal Percent { get; set; }
        public decimal ChangePercent { get; set; }
    }

    public class BankDistributionDto
    {
        public string BankName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long Count { get; set; }
        public decimal Percent { get; set; }
        public decimal ChangePercent { get; set; }
    }

    public class DeviceDistributionDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long Count { get; set; }
        public decimal Percent { get; set; }
        public decimal ChangePercent { get; set; }
    }
}
