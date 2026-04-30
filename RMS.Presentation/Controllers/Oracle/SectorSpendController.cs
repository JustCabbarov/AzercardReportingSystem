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

        [HttpGet]
        public async Task<ActionResult<PagedResult<SectorSpend>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _service.GetAllAsync(new PageRequest { Page = page, PageSize = pageSize }, ct);
            return Ok(result);
        }

        [HttpGet("bank/{bankName}")]
        public async Task<ActionResult<PagedResult<SectorSpend>>> GetByBank(
            string bankName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _service.GetByBankAsync(bankName, new PageRequest { Page = page, PageSize = pageSize }, ct);
            return Ok(result);
        }

        [HttpGet("bank/{bankName}/month")]
        public async Task<ActionResult<PagedResult<SectorSpend>>> GetByBankAndMonth(
            string bankName,
            [FromQuery] DateTime month,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _service.GetByBankAndMonthAsync(bankName, month, new PageRequest { Page = page, PageSize = pageSize }, ct);
            return Ok(result);
        }

        [HttpGet("mcc/{mccGroup}")]
        public async Task<ActionResult<PagedResult<SectorSpend>>> GetByMccGroup(
            string mccGroup,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _service.GetByMccGroupAsync(mccGroup, new PageRequest { Page = page, PageSize = pageSize }, ct);
            return Ok(result);
        }

        [HttpGet("filter")]
        public async Task<ActionResult<PagedResult<SectorSpend>>> Filter(
            [FromQuery] string? bankName = null,
            [FromQuery] DateTime? month = null,
            [FromQuery] string? mccGroup = null,
            [FromQuery] string? sourceChannel = null,
            [FromQuery] string? operationType = null,
            [FromQuery] string? sourceCityCategory = null,
            [FromQuery] string? sourceCountryCategory = null,
            [FromQuery] int? isAcquiring = null,
            [FromQuery] int? isIssuing = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var f = new SectorSpend
            {
                BankName = bankName ?? "",
                ReportMonth = month ?? default,
                MccGroup = mccGroup ?? "",
                SourceChannel = sourceChannel ?? "",
                OperationType = operationType ?? "",
                SourceCityCategory = sourceCityCategory ?? "",
                SourceCountryCategory = sourceCountryCategory ?? "",
                IsAcquiring = isAcquiring,
                IsIssuing = isIssuing
            };

            var result = await _service.FilterAsync(f, new PageRequest { Page = page, PageSize = pageSize }, ct);
            return Ok(result);
        }

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

        [HttpGet("bank/{bankName}/channel-distribution")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetChannelDistribution(
            string bankName,
            [FromQuery] DateTime month,
            CancellationToken ct = default)
        {
            var result = await _service.GetChannelDistributionAsync(bankName, month, ct);
            return Ok(result);
        }

        [HttpGet("bank/{bankName}/trend")]
        public async Task<ActionResult<IEnumerable<SectorSpendTrend>>> GetTrend(
            string bankName,
            [FromQuery] string mccGroup,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mccGroup))
                return BadRequest("mccGroup parametri mütləqdir.");

            var result = await _service.GetTrendAsync(bankName, mccGroup, ct);
            return Ok(result);
        }
    }
}