using Microsoft.AspNetCore.Mvc;
using RMS.Contract.Services.Oracle;
using RMS.Domain.Entities.Oracle;

namespace RMS.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ForecastingController : ControllerBase
{
    private readonly IForecastingService _svc;
    private readonly ILogger<ForecastingController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ForecastingController(
        IForecastingService svc,
        ILogger<ForecastingController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _svc = svc;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // GET /api/Forecasting?bank=KAPITAL&mcc=FOOD&horizon=6
    [HttpGet]
    public async Task<ActionResult<ForecastResult>> Get(
        [FromQuery] string bank,
        [FromQuery] string mcc,
        [FromQuery] int horizon = 6,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bank) || string.IsNullOrWhiteSpace(mcc))
            return BadRequest("bank və mcc parametrləri mütləqdir.");

        if (horizon < 1 || horizon > 24)
            return BadRequest("horizon 1 ilə 24 arasında olmalıdır.");

        var result = await _svc.ForecastAsync(bank, mcc, horizon, ct);
        return Ok(result);
    }

    // GET /api/Forecasting/all?horizon=6
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<ForecastResult>>> GetAll(
        [FromQuery] int horizon = 6,
        CancellationToken ct = default)
    {
        if (horizon < 1 || horizon > 24)
            return BadRequest("horizon 1 ilə 24 arasında olmalıdır.");

        var results = await _svc.ForecastAllAsync(horizon, ct);
        return Ok(results);
    }

    // POST /api/Forecasting/train/global
    [HttpPost("train/global")]
    public IActionResult TrainGlobal()
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IForecastingService>();
            var logger = scope.ServiceProvider
                              .GetRequiredService<ILogger<ForecastingController>>();
            await svc.TrainGlobalAsync(CancellationToken.None);
            logger.LogInformation("Global model train tamamlandı.");
        });

        return Accepted(new { message = "Global model train arxa planda başladı." });
    }

    // POST /api/Forecasting/train/all
    [HttpPost("train/all")]
    public IActionResult TrainAll()
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IForecastingService>();
            var logger = scope.ServiceProvider
                              .GetRequiredService<ILogger<ForecastingController>>();
            await svc.TrainAllAsync(CancellationToken.None);
            logger.LogInformation("Bank-specific modellər train tamamlandı.");
        });

        return Accepted(new { message = "Bank-specific modellər arxa planda train başladı." });
    }

    // POST /api/Forecasting/train?bank=KAPITAL&mcc=FOOD
    [HttpPost("train")]
    public async Task<IActionResult> Train(
        [FromQuery] string bank,
        [FromQuery] string mcc,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bank) || string.IsNullOrWhiteSpace(mcc))
            return BadRequest("bank və mcc parametrləri mütləqdir.");

        await _svc.TrainAsync(bank, mcc, ct);
        return Ok(new { message = $"{bank}/{mcc} modeli train edildi." });
    }
}