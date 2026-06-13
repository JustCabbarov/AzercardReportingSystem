using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.AcquiringTransaction;
using RMS.Domain.Repositories.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Services.Oracle
{

    public class AcquiringDeviceService : IAcquiringDeviceService
    {
        private readonly IAcquiringDeviceRepository _repo;

        public AcquiringDeviceService(IAcquiringDeviceRepository repo)
        {
            _repo = repo;
        }

        public Task<AcquiringFilterOptionsResponse> GetFilterOptionsAsync()
            => _repo.GetFilterOptionsAsync();

        public Task<AcquiringDashboardResponse> GetDashboardAsync(AcquiringDeviceFilter f)
            => _repo.GetDashboardAsync(f);

        public Task<AcqTrendResponse> GetTrendAsync(AcquiringTrendRequest r)
            => _repo.GetTrendAsync(r);
    }
}
