using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.SummaryTransaction;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class SummaryTransactionController : ControllerBase
    {

        private readonly ISummaryTransactionService _service;

        public SummaryTransactionController(ISummaryTransactionService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET api/summarytransaction/summary
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] SummaryFilterRequest filter)
        {
            var result = await _service.GetSummaryAsync(filter);
            return Ok(result);
        }
    }
}
