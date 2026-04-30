namespace RMS.Domain.Repositories;

public interface IModelStore
{
    bool Exists(string key);
    Task SaveAsync(string key, Stream modelStream, CancellationToken ct = default);
    Task<Stream?> LoadAsync(string key, CancellationToken ct = default);
    Task SaveAccuracyAsync(string key, float accuracy, CancellationToken ct);
    Task<float> LoadAccuracyAsync(string key, CancellationToken ct);
}