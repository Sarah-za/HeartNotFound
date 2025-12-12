using System;
using System.Collections.Generic;
using System.ComponentModel;
using Remotmonitor.Models;

namespace Remotmonitor.Trends
{
    public sealed class TrendRecorder : IDisposable
    {
        private readonly VitalSample _v;
        private const int MaxPoints = 3600; // 1 Stunde, 1 Punkt pro Sekunde

        public List<double> Hr { get; } = new();
        public List<double> Spo2 { get; } = new();
        public List<double> Rr { get; } = new();
        public List<double> Temp { get; } = new();
        public List<double> Sys { get; } = new();

        public TrendRecorder(VitalSample v)
        {
            _v = v;

            // Plausible Anfangswerte
            double hr = v.Hr > 0 ? v.Hr : 80;
            double spo2 = v.Spo2 > 0 ? v.Spo2 : 97;
            double rr = v.Rr > 0 ? v.Rr : 16;
            double temp = v.Temp > 0 ? v.Temp : 36.8;
            double sys = v.Sys > 0 ? v.Sys : 120;

            // mit Initialdaten füllen, keine Nullwerte
            for (int i = 0; i < MaxPoints; i++)
            {
                Hr.Add(hr + Rand(-2, 2));
                Spo2.Add(spo2 + Rand(-1, 1));
                Rr.Add(rr + Rand(-1, 1));
                Temp.Add(temp + Rand(-0.1, 0.1));
                Sys.Add(sys + Rand(-3, 3));
            }

            _v.PropertyChanged += OnVitalChanged;
        }

        private static double Rand(double min, double max)
        {
            return new Random(Guid.NewGuid().GetHashCode()).NextDouble() * (max - min) + min;
        }

        private void OnVitalChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(VitalSample.Hr): Hr.Add(_v.Hr); break;
                case nameof(VitalSample.Spo2): Spo2.Add(_v.Spo2); break;
                case nameof(VitalSample.Rr): Rr.Add(_v.Rr); break;
                case nameof(VitalSample.Temp): Temp.Add(_v.Temp); break;
                case nameof(VitalSample.Sys): Sys.Add(_v.Sys); break;
            }
            Trim();
        }

        private void Trim()
        {
            if (Hr.Count > MaxPoints) Hr.RemoveRange(0, Hr.Count - MaxPoints);
            if (Spo2.Count > MaxPoints) Spo2.RemoveRange(0, Spo2.Count - MaxPoints);
            if (Rr.Count > MaxPoints) Rr.RemoveRange(0, Rr.Count - MaxPoints);
            if (Temp.Count > MaxPoints) Temp.RemoveRange(0, Temp.Count - MaxPoints);
            if (Sys.Count > MaxPoints) Sys.RemoveRange(0, Sys.Count - MaxPoints);
        }

        public void Dispose() => _v.PropertyChanged -= OnVitalChanged;
    }
}
