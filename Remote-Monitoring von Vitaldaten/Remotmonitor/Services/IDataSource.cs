using Remotmonitor.Models;

namespace Remotmonitor.Services;

public interface IDataSource : IAsyncDisposable
{
    event Action<VitalSample>? OnSample;
    Task StartAsync(CancellationToken ct);
}