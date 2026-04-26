using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class AzMapTransaction
    {
        public DateTime ReportDate { get; set; }
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = "";
        public string? RegionNameClean { get; set; }
        public string? SourceCity { get; set; }
        public string? SourceCityClean { get; set; }
        public string? SourceCityCategory { get; set; }
        public string? AcquiringDeviceType { get; set; }
        public string? MccGroup { get; set; }
        public string? RetailCategory { get; set; }
        public string? TransactionClass { get; set; }
        public string? CtlsStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }
        public long? DeviceCount { get; set; }
    }
}
