using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class SectorSpendTrend
    {
        public DateTime ReportMonth { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalCount { get; set; }
        public decimal AvgTicket { get; set; }
        public decimal SectorMarketSharePct { get; set; }
    }
}
