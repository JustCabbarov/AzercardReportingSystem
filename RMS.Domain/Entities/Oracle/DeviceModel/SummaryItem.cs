using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{
    

   

    public class SummaryItem
    {
        public DateTime ReportMonth { get; set; }
        public string? BankName { get; set; }
        public string? RegionName { get; set; }
        public string? MccName { get; set; }
        public string? RetailCategory { get; set; }
        public string? TransactionClass { get; set; }
        public string? CtlsStatus { get; set; }
        public long TotalDevices { get; set; }
        public long GrandTotalMonth { get; set; }
    }

  

   
}
