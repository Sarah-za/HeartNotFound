using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remotemonitor;

public interface IDataSource : IAsyncDisposable
{
    event Action<VitalSample>? OnSample;
    Task StartAsync(CancellationToken ct);
}
