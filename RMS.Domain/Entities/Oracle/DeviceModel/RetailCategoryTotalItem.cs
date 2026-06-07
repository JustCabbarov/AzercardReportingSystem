using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{
    public class RetailCategoryTotalItem
    {
        public string RetailCategory { get; set; } = "";
        public long TotalDevices { get; set; }
    }
}
