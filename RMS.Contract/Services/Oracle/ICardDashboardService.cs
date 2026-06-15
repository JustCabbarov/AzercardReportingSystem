using RMS.Domain.Entities.Oracle.CardTransaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{
    public interface ICardDashboardService
    {
        Task<KpiSummaryResponse> GetKpiAsync(DashboardFilterRequest filter);
        Task<List<TrendPoint>> GetTrendWithForecastAsync(DashboardFilterRequest filter);
        Task<BreakdownResponse> GetBreakdownAsync(DashboardFilterRequest filter, BreakdownType type);
        Task<DashboardResponse> GetFullDashboardAsync(DashboardFilterRequest filter);
        Task<FilterLookupResponse> GetFilterLookupsAsync();
    }
}
