using RMS.Domain.Entities.Oracle.CardPortfolio.RMS.Domain.Entities.Oracle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.Services.Oracle
{

    public interface ICardPortfolioService
    {
        Task<FilterOptionsResponse> GetFilterOptionsAsync();
        Task<DateTime> GetLatestReportMonthAsync();
        Task<TopCardsResponse> GetTopCardsAsync(CardPortfolioFilter filter);
        Task<CrossTableResponse> GetCrossTableAsync(CrossTableRequest request);
        Task<PayChartResponse> GetPayChartAsync(PayChartRequest request);
        Task<TrendChartResponse> GetTrendAsync(TrendChartRequest request);
        Task<XyChartResponse> GetXyChartAsync(XyChartRequest request);
    }
}
