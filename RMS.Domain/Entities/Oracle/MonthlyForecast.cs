
namespace RMS.Domain.Entities.Oracle;

public sealed class MonthlyForecast
{
    public DateTime Month { get; set; }
    public float PredictedAmount { get; set; }
    public float AmountLowerBound { get; set; }
    public float AmountUpperBound { get; set; }
    public float PredictedCount { get; set; }
    public float CountLowerBound { get; set; }
    public float CountUpperBound { get; set; }
}