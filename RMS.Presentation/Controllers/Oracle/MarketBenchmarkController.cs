using Microsoft.AspNetCore.Http;
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
        public async Task<ActionResult<IEnumerable<MarketBenchmark>>> GetAll(
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank adına görə bazar benchmark məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<IEnumerable<MarketBenchmark>>> GetByBank(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Aya görə bazar benchmark məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("month")]
        public async Task<ActionResult<IEnumerable<MarketBenchmark>>> GetByMonth(
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetByMonthAsync(month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Ay + isteğe bağlı regiona görə sıralanmış bazar benchmark məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("ranked")]
        public async Task<ActionResult<IEnumerable<MarketBenchmark>>> GetRanked(
            [FromQuery] DateTime month,
            [FromQuery] string? regionNameClean = null,
            CancellationToken ct = default)
        {
            var result = await _service.GetRankedAsync(month, regionNameClean, ct);
            return Ok(result);
        }

        /// <summary>
        /// Müəyyən bankın bazar payını (%) qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/market-share")]
        public async Task<ActionResult<decimal>> GetMarketShare(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetMarketShareAsync(bankName, month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Müəyyən bankın kart payını (%) qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/card-share")]
        public async Task<ActionResult<decimal>> GetCardShare(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetCardShareAsync(bankName, month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bankın həmin aydakı sırasını (rank) qaytarır. 1 = ən böyük.
        /// </summary>
        [HttpGet("bank/{bankName}/rank")]
        public async Task<ActionResult<int>> GetBankRank(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetBankRankAsync(bankName, month, ct);
            return Ok(result);
        }
    }
}
