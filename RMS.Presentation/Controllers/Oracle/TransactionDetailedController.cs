using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle.TransactionDeteiled;

namespace RMS.API.Controllers.Oracle;

[ApiController]
[Route("api/[controller]")]
public class TransactionDetailedController : ControllerBase
{
    private readonly ITransactionDetailedService _service;

    public TransactionDetailedController(ITransactionDetailedService service)
    {
        _service = service;
    }

    // GET api/transactiondetailed/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] TransactionFilterRequest filter)
    {
        var result = await _service.GetSummaryAsync(filter);
        return Ok(result);
    }

    // GET api/transactiondetailed/banks
    [HttpGet("banks")]
    public async Task<IActionResult> GetTargetBanks([FromQuery] TransactionFilterRequest filter)
    {
        var result = await _service.GetTargetBanksAsync(filter);
        return Ok(result);
    }

    // GET api/transactiondetailed/devices
    [HttpGet("devices")]
    public async Task<IActionResult> GetAcquiringDevices([FromQuery] TransactionFilterRequest filter)
    {
        var result = await _service.GetAcquiringDevicesAsync(filter);
        return Ok(result);
    }
}