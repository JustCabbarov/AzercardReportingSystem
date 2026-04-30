using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RMS.Domain.Repositories;

namespace RMS.Persistence.Repositories.Oracle;

/// <summary>
/// ML modellərini diske saxlayan IModelStore implementasiyası.
///
/// appsettings.json:
/// "ModelStore": { "BasePath": "models" }
///
/// Fayl strukturu:
///   models/GLOBAL.zip
///   models/KAPITAL_FOOD.zip
/// </summary>
public sealed class DiskModelStore : IModelStore
{
    private readonly string _basePath;
    private readonly ILogger<DiskModelStore> _logger;

    public DiskModelStore(IConfiguration configuration, ILogger<DiskModelStore> logger)
    {
        var configured = configuration["ModelStore:BasePath"];

        _basePath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "models")
            : Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured); // ← əsas fix

        Directory.CreateDirectory(_basePath);
        _logger = logger;
        _logger.LogInformation("DiskModelStore hazır. BasePath={Path}", _basePath);
    }

    public bool Exists(string key) =>
      File.Exists(FilePath(key + "_AMOUNT")) &&
      File.Exists(FilePath(key + "_COUNT"));

    public async Task SaveAsync(string key, Stream modelStream, CancellationToken ct = default)
    {
        var path = FilePath(key);
        var tmpPath = path + ".tmp";

        // Əvvəlcə temp faylına yaz — yazma yarımçıq qalsa köhnə model salamat qalır
        await using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await modelStream.CopyToAsync(fs, ct);
        }

        File.Move(tmpPath, path, overwrite: true);
        _logger.LogInformation("Model saxlandı: {Path}", path);
    }

    public Task<Stream?> LoadAsync(string key, CancellationToken ct = default)
    {
        var path = FilePath(key);

        if (!File.Exists(path))
        {
            _logger.LogWarning("Model tapılmadı: {Path}", path);
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    private string FilePath(string key)
    {
        var safe = string.Concat(key.Split(Path.GetInvalidFileNameChars()))
                         .Replace(' ', '_');
        return Path.Combine(_basePath, safe + ".zip");
    }

    public async Task SaveAccuracyAsync(string key, float accuracy, CancellationToken ct = default)
    {
        var path = AccuracyPath(key);
        await File.WriteAllTextAsync(path, accuracy.ToString("F1"), ct);
        _logger.LogInformation("Accuracy saxlandı: {Path} = {Accuracy}%", path, accuracy);
    }

    public async Task<float> LoadAccuracyAsync(string key, CancellationToken ct = default)
    {
        var path = AccuracyPath(key);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Accuracy faylı tapılmadı: {Path}", path);
            return 0f;
        }
        var text = await File.ReadAllTextAsync(path, ct);
        return float.TryParse(text, out var value) ? value : 0f;
    }

    private string AccuracyPath(string key)
    {
        var safe = string.Concat(key.Split(Path.GetInvalidFileNameChars()))
                         .Replace(' ', '_');
        return Path.Combine(_basePath, safe + "_accuracy.txt");
    }
}