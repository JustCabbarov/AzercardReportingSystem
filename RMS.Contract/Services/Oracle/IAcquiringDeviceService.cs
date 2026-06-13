using RMS.Domain.Entities.Oracle.AcquiringTransaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{

    public interface IAcquiringDeviceService
    {
        Task<AcquiringFilterOptionsResponse> GetFilterOptionsAsync();
        Task<AcquiringDashboardResponse> GetDashboardAsync(AcquiringDeviceFilter f);
        Task<AcqTrendResponse> GetTrendAsync(AcquiringTrendRequest r);
    }

}
