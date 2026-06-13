using RMS.Domain.Entities.Oracle.CardPortfolio.RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface ICardPortfolioRepository
    {
        Task<FilterOptionsResponse> GetFilterOptionsAsync();
        Task<DateTime> GetLatestReportMonthAsync();
        Task<IEnumerable<TopSchemeCardDto>> GetTopCardsAsync(
      CardPortfolioFilter f,
      string? dimension);
        Task<CrossTableResponse> GetCrossTableAsync(CrossTableRequest request);
        Task<PayChartResponse> GetPayChartAsync(PayChartRequest request);
        Task<TrendChartResponse> GetTrendAsync(TrendChartRequest request);
        Task<XyChartResponse> GetXyChartAsync(XyChartRequest request);
    }
}
