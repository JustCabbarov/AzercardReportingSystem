using Microsoft.AspNetCore.Mvc;
using RMS.Contract.DTOs;
using RMS.Contract.Services.Oracle;

using RMS.Domain.Entities.Oracle;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketBenchmarkController : ControllerBase
    {
        private readonly IMarketBenchmarkService _service;

        public MarketBenchmarkController(IMarketBenchmarkService service)
        {
            _service = service;
        }

        

        
        /// <summary>
        /// Aya görə bazar benchmark məlumatlarını qaytarır.
        /// </summary>
      
        /// <summary>
        /// Ay + isteğe bağlı regiona görə sıralanmış bazar benchmark məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("ranked")]
        public async Task<ActionResult<PagedResult<MarketBenchmark>>> GetRanked(
            [FromQuery] DateTime month,
            [FromQuery] PageRequest pageReq,
            [FromQuery] string? regionNameClean = null,
            CancellationToken ct = default)
        {
            var result = await _service.GetRankedAsync(month, pageReq, regionNameClean, ct);
            return Ok(result);
        }

        /// <summary>
        /// Universal filter — bütün parametrlər opsionaldır.
        /// </summary>
        [HttpGet("filter")]
        public async Task<ActionResult<PagedResult<MarketBenchmark>>> Filter(
     [FromQuery] MarketBenchmarkFilterRequest f,
     CancellationToken ct = default)
        {
            var pageReq = new PageRequest { Page = f.Page, PageSize = f.PageSize };

            var entity = new MarketBenchmark
            {
                BankName = f.BankName,
                RegionNameClean = f.RegionNameClean,
                CardBrandName = f.CardBrandName,
                ProductType = f.ProductType,
                ReportMonth = f.ReportMonth ?? default
            };

            var result = await _service.FilterAsync(entity, pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bankın aylıq market share və card share trendi.
        /// </summary>
        [HttpGet("bank/{bankName}/trend")]
        public async Task<ActionResult<IEnumerable<MarketBenchmarkTrend>>> GetTrend(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetTrendAsync(bankName, ct);
            return Ok(result);
        }
    }
}