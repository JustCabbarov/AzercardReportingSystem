// RMS.Domain/Entities/Oracle/ForecastResult.cs
namespace RMS.Domain.Entities.Oracle;

public sealed class ForecastResult
{
    public string BankName { get; set; } = "";
    public string MccGroup { get; set; } = "";
    public string ModelUsed { get; set; } = "";   // "Bank-Specific" | "Global"
    public float Accuracy { get; set; }          // MAPE əsaslı dəqiqlik %
    public float ConfidenceLevel { get; set; }        // 0.95 → 95%
    public IReadOnlyList<MonthlyForecast> Forecasts { get; set; } = [];
}