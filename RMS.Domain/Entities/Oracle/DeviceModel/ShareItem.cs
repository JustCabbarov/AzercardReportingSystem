using RMS.Domain.Entities.Oracle.DeviceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{
    public class ShareItem : SummaryItem
    {
        public decimal SharePctByBank { get; set; }
        public decimal SharePctByRegion { get; set; }
        public decimal SharePctByMcc { get; set; }
        public decimal SharePctByRetailCat { get; set; }
    }
}
