using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.DTOs
{
    
    public class MarketBenchmarkFilterRequest
    {
        public string? BankName { get; set; }
        public string? RegionNameClean { get; set; }
        public string? CardBrandName { get; set; }
        public string? ProductType { get; set; }
        public DateTime? ReportMonth { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
