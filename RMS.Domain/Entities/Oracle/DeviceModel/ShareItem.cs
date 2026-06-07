using RMS.Domain.Entities.Oracle.DeviceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{
    public class ShareItem
    {

            public string RetailCategory { get; set; } = "";
            public int TotalDevices { get; set; }
            public decimal SharePct { get; set; }
        
    }
}