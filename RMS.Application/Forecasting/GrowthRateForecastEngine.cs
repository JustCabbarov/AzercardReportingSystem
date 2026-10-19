namespace RMS.Application.Forecasting;

/// <summary>
/// Əvvəlki ilin ardıcıl ayları arası artım faizinə əsaslanan proqnoz mühərriki.
/// <para>
/// Hesablama məntiqi:
///   GrowthRate(M) = actual(M, prev_year) / actual(M-1, prev_year)
///   Forecast(M)   = Forecast_or_Actual(M-1) × GrowthRate(M)
/// </para>
/// <para>
/// Nümunə:
///   GrowthRate = Feb-2025 / Jan-2025
///   Forecast(Feb-2026) = Actual(Jan-2026) × GrowthRate
/// </para>
/// </summary>
public static class GrowthRateForecastEngine
{
    /// <summary>
    /// <paramref name="endDate"/>-dən sonrakı <paramref name="horizonMonths"/> ay üçün proqnoz hesablayır.
    /// </summary>
    /// <param name="actuals">Mövcud aylıq tarix + dəyər cütlükləri (artıqlıqla çeşidlənmiş olmalıdır).</param>
    /// <param name="endDate">Seçilmiş son tarix; proqnoz bundan sonra başlayır.</param>
    /// <param name="horizonMonths">Neçə ay irəli proqnozlaşdırılacaq (default 12).</param>
    public static List<GrowthForecastPoint> Compute(
        IList<(DateTime Month, decimal Value)> actuals,
        DateTime endDate,
        int horizonMonths = 12)
    {
        var result = new List<GrowthForecastPoint>(horizonMonths);

        if (actuals == null || actuals.Count < 2)
            return result;

        // Actual dataları lüğətə çevir — sürətli axtarış üçün (yalnız il+ay əsasında)
        var lookup = actuals
            .GroupBy(a => new DateTime(a.Month.Year, a.Month.Month, 1))
            .ToDictionary(g => g.Key, g => g.Last().Value);

        // Son məlum dəyər — zəncirvari hesab üçün baza
        var baseMonth = new DateTime(endDate.Year, endDate.Month, 1);
        decimal lastKnownValue = lookup.TryGetValue(baseMonth, out var bv)
            ? bv
            : actuals.Last().Value;   // təqdim edilən son actual

        for (int i = 1; i <= horizonMonths; i++)
        {
            var forecastMonth = baseMonth.AddMonths(i);

            // Əvvəlki ildəki müvafiq ay
            var prevYearThisMonth = forecastMonth.AddYears(-1);
            // Ondan bir əvvəlki ay
            var prevYearPrevMonth = prevYearThisMonth.AddMonths(-1);

            decimal growthRate = 1m;   // artım faizi məlum deyilsə neytral (1 = dəyişiklik yox)

            if (lookup.TryGetValue(prevYearThisMonth, out decimal vThis) &&
                lookup.TryGetValue(prevYearPrevMonth, out decimal vPrev) &&
                vPrev != 0m)
            {
                growthRate = vThis / vPrev;
            }

            // Mənfi və ya sıfır proqnoz məntiqsizdir — ən azı 0 saxla
            decimal forecastValue = Math.Max(0m, lastKnownValue * growthRate);

            result.Add(new GrowthForecastPoint
            {
                Month      = forecastMonth,
                Forecast   = Math.Round(forecastValue, 2),
                GrowthRate = Math.Round(growthRate, 6),
                IsForecast = true
            });

            // Növbəti ayın bazası bu proqnozdur (zəncirvari)
            lastKnownValue = forecastValue;
        }

        return result;
    }
}

/// <summary>Growth-Rate mühərriki tərəfindən qaytarılan proqnoz nöqtəsi.</summary>
public sealed class GrowthForecastPoint
{
    /// <summary>Proqnoz ayı (ayın 1-i).</summary>
    public DateTime Month { get; init; }

    /// <summary>Hesablanmış proqnoz dəyəri.</summary>
    public decimal Forecast { get; init; }

    /// <summary>Tətbiq edilən artım faizi (1.05 = 5% artım).</summary>
    public decimal GrowthRate { get; init; }

    /// <summary>Həmişə true — proqnoz nöqtəsini real datadan ayırd etmək üçün.</summary>
    public bool IsForecast { get; init; } = true;
}
