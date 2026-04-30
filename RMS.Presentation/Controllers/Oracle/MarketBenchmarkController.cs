using Microsoft.AspNetCore.Mvc;
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
        /// Bütün bazar benchmark məlumatlarını qaytarır.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<MarketBenchmark>>> GetAll(
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank adına görə bazar benchmark məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<PagedResult<MarketBenchmark>>> GetByBank(
            string bankName,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, pageReq, ct);
            return Ok(result);
        }

        /// <summary>
        /// Aya görə bazar benchmark məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("month")]
        public async Task<ActionResult<PagedResult<MarketBenchmark>>> GetByMonth(
            [FromQuery] DateTime month,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.GetByMonthAsync(month, pageReq, ct);
            return Ok(result);
        }

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
            [FromQuery] MarketBenchmark f,
            [FromQuery] PageRequest pageReq,
            CancellationToken ct = default)
        {
            var result = await _service.FilterAsync(f, pageReq, ct);
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