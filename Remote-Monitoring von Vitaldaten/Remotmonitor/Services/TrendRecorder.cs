using System.Collections.ObjectModel;
using System.ComponentModel;
using Remotmonitor.Models;

namespace Remotmonitor.Trends
{
    public sealed class TrendRecorder : System.IDisposable
    {
        private readonly VitalSample _v;
        private readonly int _capacity;

        // 60-Sekunden-Buffer für jede Kurve
        public ObservableCollection<double> HR { get; } = new();
        public ObservableCollection<double> SpO2 { get; } = new();
        public ObservableCollection<double> RR { get; } = new();
        public ObservableCollection<double> Temp { get; } = new();
        public ObservableCollection<double> Sys { get; } = new();   // systolischer Blutdruck

        public TrendRecorder(VitalSample v, int capacitySeconds = 60)
        {
            _v = v;
            _capacity = capacitySeconds;

            _v.PropertyChanged += OnVitalChanged;

            // initialer Punkt
            AddValue(HR, _v.Hr);
            AddValue(SpO2, _v.Spo2);
            AddValue(RR, _v.Rr);
            AddValue(Temp, _v.Temp);
            AddValue(Sys, _v.Sys);
        }

        private void OnVitalChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Quelle tickt ca. 1 Hz – wir hängen einfach den aktuellen Wert hinten dran
            switch (e.PropertyName)
            {
                case nameof(VitalSample.Hr):
                    AddValue(HR, _v.Hr);
                    break;
                case nameof(VitalSample.Spo2):
                    AddValue(SpO2, _v.Spo2);
                    break;
                case nameof(VitalSample.Rr):
                    AddValue(RR, _v.Rr);
                    break;
                case nameof(VitalSample.Temp):
                    AddValue(Temp, _v.Temp);
                    break;
                case nameof(VitalSample.Sys):
                    AddValue(Sys, _v.Sys);
                    break;
            }
        }

        private void AddValue(ObservableCollection<double> target, double value)
        {
            // immer auf dem UI-Thread
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                target.Add(value);
                while (target.Count > _capacity)
                    target.RemoveAt(0);
            });
        }

        public void Dispose() => _v.PropertyChanged -= OnVitalChanged;
    }
}
