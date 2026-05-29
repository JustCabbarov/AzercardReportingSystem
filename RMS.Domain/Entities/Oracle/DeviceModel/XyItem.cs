using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{
    public class XyItem
    {
        public string XValue { get; set; } = null!;
        public string YValue { get; set; } = null!;
        public long DeviceCount { get; set; }
        public decimal SharePct { get; set; }
        public long? MomDiff { get; set; }        // ← ox işarəsi üçün
        public decimal? MomPctChange { get; set; } // ← ox işarəsi üçün
    }
}
