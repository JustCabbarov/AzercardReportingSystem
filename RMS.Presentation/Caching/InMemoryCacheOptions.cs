namespace RMS.Presentation.Caching;

public sealed class InMemoryCacheOptions
{
    public const string SectionName = "InMemoryCache";

    public int DefaultExpirationMinutes { get; set; } = 1200;
    public int LookupExpirationMinutes { get; set; } = 1200;
    public int SizeLimitMb { get; set; } = 512;
    public int MaximumBodySizeMb { get; set; } = 32;
    public bool WarmupOnStartup { get; set; } = true;
}

