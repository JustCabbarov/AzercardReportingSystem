using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{

    public class TrendItem
    {
        public DateTime ReportMonth { get; set; }
        public string? BankName { get; set; }
        public string? RegionName { get; set; }
        public string? MccName { get; set; }
        public string? RetailCategory { get; set; }
        public string? TransactionClass { get; set; }
        public string? CtlsStatus { get; set; }
        public long TotalDevices { get; set; }
        public long? PrevMonthDevices { get; set; }
        public long? MomDiff { get; set; }
        public decimal? MomPctChange { get; set; }
    }

}
