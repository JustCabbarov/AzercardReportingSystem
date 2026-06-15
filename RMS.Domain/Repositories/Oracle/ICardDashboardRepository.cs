using RMS.Domain.Entities.Oracle.CardTransaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{

    public interface ICardDashboardRepository
    {
        Task<KpiSummaryResponse> GetKpiSummaryAsync(DashboardFilterRequest filter);
        Task<List<TrendPoint>> GetTrendAsync(DashboardFilterRequest filter);
        Task<BreakdownResponse> GetBreakdownAsync(DashboardFilterRequest filter, BreakdownType type);
        Task<FilterLookupResponse> GetFilterLookupsAsync();
    }
}
