using RMS.Domain.Entities.Oracle.DeviceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.DeviceModel
{
    public class ShareItem
    {            public string DimensionValue { get; set; } = string.Empty;
            public long TotalDevices { get; set; }
            public double SharePct { get; set; }
        
    }

    
}