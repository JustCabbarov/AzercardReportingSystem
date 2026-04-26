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
        public string BankName { get; set; } = "";
        public string? SourceCountry { get; set; }
        public string? SourceCountryCategory { get; set; }
        public string? TargetCountry { get; set; }
        public string? TargetCountryCategory { get; set; }
        public string? PaymentSystem { get; set; }
        public int IsIssuing { get; set; }
        public int IsAcquiring { get; set; }
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }
    }
}
