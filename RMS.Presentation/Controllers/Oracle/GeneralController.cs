using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.DTOs;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeneralController : ControllerBase
    {

        private readonly ISectorSpendService _service;

        public GeneralController(ISectorSpendService service)
        {
            _service = service;
        }


        [HttpGet("GetBanks")]
        public async Task<ActionResult<IEnumerable<BankDto>>> GetBanks(
            CancellationToken ct = default)
            => Ok(await _service.GetDistinctBanksAsync(ct));

        [HttpGet("GetMccGroups")]
        public async Task<ActionResult<IEnumerable<MccGroupDto>>> GetMccGroups(
            CancellationToken ct = default)
            => Ok(await _service.GetDistinctMccGroupsAsync(ct));
    }
}
