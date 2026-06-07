using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Entities.Oracle.CardPortfolio.RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.Oracle;

namespace RMS.Application.Services.Oracle
{
    public class CardPortfolioService : ICardPortfolioService
    {
        private readonly ICardPortfolioRepository _repo;

        public CardPortfolioService(ICardPortfolioRepository repo)
        {
            _repo = repo;
        }

        public Task<FilterOptionsResponse> GetFilterOptionsAsync() => _repo.GetFilterOptionsAsync();
        public Task<DateTime> GetLatestReportMonthAsync() => _repo.GetLatestReportMonthAsync();
        public Task<TopCardsResponse> GetTopCardsAsync(CardPortfolioFilter f) => _repo.GetTopCardsAsync(f);
        public Task<CrossTableResponse> GetCrossTableAsync(CrossTableRequest r) => _repo.GetCrossTableAsync(r);
        public Task<PayChartResponse> GetPayChartAsync(PayChartRequest r) => _repo.GetPayChartAsync(r);
        public Task<TrendChartResponse> GetTrendAsync(TrendChartRequest r) => _repo.GetTrendAsync(r);
        public Task<XyChartResponse> GetXyChartAsync(XyChartRequest r) => _repo.GetXyChartAsync(r);
    }
}