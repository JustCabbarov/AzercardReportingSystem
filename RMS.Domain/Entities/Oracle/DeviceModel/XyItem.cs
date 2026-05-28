using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{
    public class XyItem
    {
        public string? XValue { get; set; }
        public string? YValue { get; set; }
        public long DeviceCount { get; set; }
        public decimal SharePct { get; set; }
    }
}
