using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class WorldMapTransaction
    {
        public DateTime ReportMonth { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string BankName { get; set; } = "";
        public string? SourceCountry { get; set; }
        public string? SourceCountryCategory { get; set; }
        public string? TargetCountry { get; set; }
        public string? TargetCountryCategory { get; set; }
        public string? PaymentSystem { get; set; }
        public string? IsIssuingCategory { get; set; }
        public string? CardProductTypeCategory { get; set; }
        public string? CardAccountCurrency { get; set; }
        public string? SourceBankName { get; set; }
        public string? AcquiringDeviceType { get; set; }
        public string? TransGroup { get; set; }
        public string? OperationType { get; set; }
        public string? TokenStatus { get; set; }
        public bool? IsIssuing { get; set; }
        public bool? IsAcquiring { get; set; }
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }

        // Koordinatlar
        public decimal? SourceLatitude { get; set; }
        public decimal? SourceLongitude { get; set; }
        public decimal? TargetLatitude { get; set; }
        public decimal? TargetLongitude { get; set; }
    }
}
