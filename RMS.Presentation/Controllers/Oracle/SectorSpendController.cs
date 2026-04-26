using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class SectorSpendController : ControllerBase
    {
        private readonly ISectorSpendService _service;

        public SectorSpendController(ISectorSpendService service)
        {
            _service = service;
        }

        /// <summary>
        /// Bütün sektor xərc məlumatlarını qaytarır.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SectorSpend>>> GetAll(
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank adına görə sektor xərc məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<IEnumerable<SectorSpend>>> GetByBank(
            string bankName, CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank + aya görə sektor xərc məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/month")]
        public async Task<ActionResult<IEnumerable<SectorSpend>>> GetByBankAndMonth(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetByBankAndMonthAsync(bankName, month, ct);
            return Ok(result);
        }

        /// <summary>
        /// MCC qrupuna görə sektor xərc məlumatlarını qaytarır.
        /// </summary>
        [HttpGet("mcc/{mccGroup}")]
        public async Task<ActionResult<IEnumerable<SectorSpend>>> GetByMccGroup(
            string mccGroup, CancellationToken ct = default)
        {
            var result = await _service.GetByMccGroupAsync(mccGroup, ct);
            return Ok(result);
        }

        /// <summary>
        /// Share of Wallet məlumatları ilə birlikdə sektor xərclərini qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/share-of-wallet")]
        public async Task<ActionResult<IEnumerable<SectorSpend>>> GetWithShareOfWallet(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetWithShareOfWalletAsync(bankName, month, ct);
            return Ok(result);
        }

        /// <summary>
        /// Bank + ay üzrə ən yüksək xərcli top N MCC qrupunu qaytarır. Default: top 10.
        /// </summary>
        [HttpGet("bank/{bankName}/top-mcc")]
        public async Task<ActionResult<IEnumerable<SectorSpend>>> GetTopMccGroups(
            string bankName,
            [FromQuery] DateTime month,
            [FromQuery] int topN = 10,
            CancellationToken ct = default)
        {
            var result = await _service.GetTopMccGroupsAsync(bankName, month, topN, ct);
            return Ok(result);
        }

        /// <summary>
        /// Onlayn vs oflayn kanal paylanmasını (%) qaytarır.
        /// </summary>
        [HttpGet("bank/{bankName}/channel-distribution")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetChannelDistribution(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetChannelDistributionAsync(bankName, month, ct);
            return Ok(result);
        }
    }
}
