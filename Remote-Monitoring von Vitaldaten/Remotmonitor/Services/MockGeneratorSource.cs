using Remotmonitor.Models;
using System.Timers;

namespace Remotmonitor.Services;

public sealed class MockGeneratorSource : IDataSource
{
    private readonly System.Timers.Timer _timer = new(1000); // 1 Hz
    private readonly Random _rng = new();

    // >>> Neu: vier Patienten & Phasen für unterschiedliche Verläufe
    private readonly string[] _patients = { "P-0001", "P-0002", "P-0003", "P-0004" };
    private readonly Dictionary<string, double> _phase = new();

    public event Action<VitalSample>? OnSample;

    public MockGeneratorSource()
    {
        _timer.AutoReset = true;
        _timer.Elapsed += (_, __) => Tick();

        // Startphasen randomisieren, damit die Kurven nicht identisch sind
        foreach (var p in _patients) _phase[p] = _rng.NextDouble() * Math.PI * 2.0;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _timer.Start();
        return Task.CompletedTask;
    }

    private void Tick()
    {
        var now = DateTime.UtcNow;

        // Pro Tick für JEDEN Patienten ein Sample erzeugen
        for (int i = 0; i < _patients.Length; i++)
        {
            var pid = _patients[i];
            _phase[pid] += 0.25; // Verlauf weiterdrehen

            // leichte Sinusvariationen + Rauschen
            int hr = 72 + (int)(8 * Math.Sin(_phase[pid])) + _rng.Next(-2, 3);
            int rr = 14 + (int)(2 * Math.Sin(_phase[pid] / 3.0)) + _rng.Next(-1, 2);
            int spo2 = Math.Clamp(96 + _rng.Next(-2, 2), 85, 100);
            double temp = Math.Round(36.6 + 0.2 * Math.Sin(_phase[pid] / 2.0) + _rng.NextDouble() * 0.2, 1);

            // optional: ab und zu einen kurzen kritischen Drop simulieren (nur P-0003)
            if (pid == "P-0003" && now.Second % 20 == 0) spo2 = 88;

            OnSample?.Invoke(new VitalSample
            {
                PatientId = pid,
                MonitorId = $"MON-0{i + 1}",
                Ts = now,
                Hr = hr,
                Spo2 = spo2,
                Rr = rr,
                Temp = temp
            });
        }
    }

    public ValueTask DisposeAsync()
    {
        _timer.Stop();
        _timer.Dispose();
        return ValueTask.CompletedTask;
    }
}
