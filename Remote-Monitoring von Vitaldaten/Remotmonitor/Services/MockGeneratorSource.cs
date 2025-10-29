using Remotmonitor.Models;
using System.Timers;

namespace Remotmonitor.Services;

public sealed class MockGeneratorSource : IDataSource
{
    private readonly System.Timers.Timer _timer = new(1000);
    private readonly Random _rng = new();

    public event Action<VitalSample>? OnSample;

    public MockGeneratorSource()
    {
        _timer.AutoReset = true;
        _timer.Elapsed += (_, __) => Tick();
    }

    public Task StartAsync(CancellationToken ct)
    {
        _timer.Start();
        return Task.CompletedTask;
    }

    private void Tick()
    {
        var t = DateTime.UtcNow;

        int hr = 75 + (int)(10 * Math.Sin(t.Second / 3.0)) + _rng.Next(-2, 3);
        int spo2 = Math.Clamp(96 + _rng.Next(-1, 2), 85, 100);
        int rr = 14 + _rng.Next(-1, 2);
        double temp = Math.Round(36.6 + _rng.NextDouble() * 0.4, 1);

        OnSample?.Invoke(new VitalSample
        {
            PatientId = "P-0001",
            MonitorId = "MON-01",
            Ts = t,
            Hr = hr,
            Spo2 = spo2,
            Rr = rr,
            Temp = temp
        });
    }

    public ValueTask DisposeAsync()
    {
        _timer.Stop();
        _timer.Dispose();
        return ValueTask.CompletedTask;
    }
}


