using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    public class ForecastInput
    {
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = "";
        public string MccGroup { get; set; } = "";
        public float TotalAmount { get; set; }
        public float TotalCount { get; set; }
        public float TotalCards { get; set; }
        public float AmountLag1 { get; set; }
        public float AmountLag3 { get; set; }
        public float AmountLag12 { get; set; }
        public float RollingAvg3M { get; set; }
        public float RollingAvg6M { get; set; }
        public float SeasonMonth { get; set; }
        public int TimeIndex { get; set; }
    }
}
