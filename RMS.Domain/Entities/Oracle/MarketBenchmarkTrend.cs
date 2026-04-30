using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class MarketBenchmarkTrend
    {
        public DateTime Month { get; set; }
        public decimal MarketSharePct { get; set; }
        public decimal CardSharePct { get; set; }
    }
}
