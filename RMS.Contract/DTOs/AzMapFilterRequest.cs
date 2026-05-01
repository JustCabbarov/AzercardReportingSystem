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
        public DateTime? ReportMonth { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
