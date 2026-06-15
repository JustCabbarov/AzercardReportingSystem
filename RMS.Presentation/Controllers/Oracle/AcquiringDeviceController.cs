using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.AcquiringTransaction;

namespace RMS.Presentation.Controllers.Oracle
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcquiringDeviceController : ControllerBase
    {


        private readonly IAcquiringDeviceService _service;

        public AcquiringDeviceController(IAcquiringDeviceService service)
        {
            _service = service;
        }

        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var result = await _service.GetFilterOptionsAsync();
            return Ok(result);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] AcquiringDeviceFilter f)
        {
            var result = await _service.GetDashboardAsync(f);
            return Ok(result);
        }

        [HttpGet("trend")]
        public async Task<IActionResult> GetTrend([FromQuery] AcquiringTrendRequest r)
        {
            var result = await _service.GetTrendAsync(r);
            return Ok(result);
        }
    }
}
