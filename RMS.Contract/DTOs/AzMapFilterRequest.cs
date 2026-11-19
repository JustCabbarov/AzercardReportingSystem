using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.DTOs
{
    public class AzMapFilterRequest
    {
        public string? BankName { get; set; }
        public string? SourceCity { get; set; }
        public string? SourceCityClean { get; set; }
        public string? SourceCityCategory { get; set; }
        public string? AcquiringDeviceType { get; set; }
        public string? TransGroup { get; set; }
        public string? OperationType { get; set; }
        public string? TokenStatus { get; set; }
        public string? Mcc { get; set; }
        public string? IsAcquiringCategory { get; set; }
        public string? TargetBankName { get; set; }
        public string? SourceBankName { get; set; }
        public string? TargetCity { get; set; }
        public string? PaymentSystem { get; set; }
        public DateTime? ReportMonth { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int? PageSize { get; set; }
    }
}
