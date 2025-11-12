using Remotmonitor.Models;
using System.Timers;

namespace Remotmonitor.Services;

public sealed class MockGeneratorSource : IDataSource
{
    private readonly System.Timers.Timer _timer = new(1000); // 1 Hz
    private readonly Random _rng = new();

    // 16 Patienten (P-0001 ... P-0016)
    private readonly string[] _patients;
    private readonly Dictionary<string, double> _phase = new();

    // Demographie je Patient
    private readonly Dictionary<string, (string Gender, int Age)> _demo = new();

    // Zimmer je Patient (z. B. "101") + Bett (1..N)
    private readonly Dictionary<string, string> _room = new();
    private readonly Dictionary<string, int> _bed = new();

    public event Action<VitalSample>? OnSample;

    public MockGeneratorSource()
    {
        _timer.AutoReset = true;
        _timer.Elapsed += (_, __) => Tick();

        _patients = Enumerable.Range(1, 16).Select(i => $"P-{i:0000}").ToArray();

        // Verteilung der Patienten auf Zimmer (nur Zimmernummern!)
        string[] roomsSequence =
        {
            "101","101","101","101","101", // 5 Patienten in Zimmer 101  => Betten 1..5
            "102","102","102",             // 3 Patienten in Zimmer 102  => Betten 1..3
            "103","103","103","103",       // 4 Patienten in Zimmer 103  => Betten 1..4
            "104","104","104","104"        // 4 Patienten in Zimmer 104  => Betten 1..4
        };

        // Zähler pro Zimmer, um die Bett-Nr. fortlaufend zu vergeben
        var bedCounter = new Dictionary<string, int>();

        for (int i = 0; i < _patients.Length; i++)
        {
            var p = _patients[i];
            _phase[p] = _rng.NextDouble() * Math.PI * 2.0;

            // Gender-Verteilung grob 48% m, 48% w, 4% d
            double g = _rng.NextDouble();
            string gender = g < 0.48 ? "m" : (g < 0.96 ? "w" : "d");

            // Alter 18..90
            int age = _rng.Next(18, 91);

            _demo[p] = (gender, age);

            // Zimmer bestimmen + Bett durchnummerieren (1..N pro Zimmer)
            string room = roomsSequence[i % roomsSequence.Length];
            if (!bedCounter.TryGetValue(room, out int current))
                current = 0;
            current++;
            bedCounter[room] = current;

            _room[p] = room;        // z. B. "101"
            _bed[p] = current;     // z. B. 1..N
        }
    }

    public Task StartAsync(CancellationToken ct)
    {
        _timer.Start();
        return Task.CompletedTask;
    }

    private void Tick()
    {
        var now = DateTime.UtcNow;

        for (int i = 0; i < _patients.Length; i++)
        {
            var pid = _patients[i];
            _phase[pid] += 0.25;

            // Baselines mit sanften Sinusvariationen + etwas Rauschen
            int hr = 72 + (int)(8 * Math.Sin(_phase[pid])) + _rng.Next(-2, 3);
            int rr = 14 + (int)(2 * Math.Sin(_phase[pid] / 3.0)) + _rng.Next(-1, 2);
            int spo2 = Math.Clamp(96 + _rng.Next(-2, 2), 85, 100);
            double temp = Math.Round(36.6 + 0.2 * Math.Sin(_phase[pid] / 2.0) + _rng.NextDouble() * 0.2, 1);

            // Blutdruck um 120/80 schwankend
            int sys = 118 + (int)(6 * Math.Sin(_phase[pid] / 1.5)) + _rng.Next(-3, 4);
            int dia = 78 + (int)(4 * Math.Sin(_phase[pid] / 2.2)) + _rng.Next(-2, 3);

            // optionale Events zur Demo
            if ((i % 5) == 2 && now.Second % 20 == 0) spo2 = 88;               // sporadischer O2-Drop
            if ((i % 7) == 3 && now.Second % 30 == 0) { sys += 30; dia += 15; } // kurzer BP-Peak

            var (gender, age) = _demo[pid];
            var room = _room[pid];
            var bed = _bed[pid];

            OnSample?.Invoke(new VitalSample
            {
                PatientId = pid,
                MonitorId = $"MON-{i + 1:00}",

                Gender = gender,
                Age = age,

                Room = room, // z. B. "101"
                Bed = bed,  // 1..N pro Zimmer

                Ts = now,
                Hr = hr,
                Spo2 = spo2,
                Rr = rr,
                Temp = temp,
                Sys = sys,
                Dia = dia
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
