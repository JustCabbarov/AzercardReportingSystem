public class DeviceTrendPoint
{
    public DateTime Period { get; set; }
    public long Count { get; set; }
    public long? PrevMonthDevices { get; set; }
    public long? MomDiff { get; set; }
    public decimal? MomPctChange { get; set; }
}

public class DeviceTrendSeries
{
    public string Label { get; set; } = null!;
    public List<DeviceTrendPoint> Points { get; set; } = [];
}

public class DeviceTrendResponse
{
    public List<DeviceTrendSeries> Series { get; set; } = [];
}

public class DeviceTrendRequest
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Dimension { get; set; }
    public List<string>? DimensionValues { get; set; }
}