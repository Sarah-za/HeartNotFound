using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Remotmonitor.Models;

namespace Remotmonitor.Views
{
    public partial class VitalHistoryWindow : Window
    {
        private readonly VitalSample _patient;
        private readonly Dictionary<string, List<double>> _data = new();
        private readonly Dictionary<string, Polyline> _lines = new();
        private readonly Dictionary<string, (Color color, double min, double max, Canvas canvas)> _info = new();

        private readonly DispatcherTimer _timer;
        private int _visibleSeconds = 60;
        private readonly Random _rng = new();

        private const int MaxSecondsStored = 3600; // 1h max

        public VitalHistoryWindow(VitalSample patient)
        {
            InitializeComponent();
            _patient = patient;
            Title = $"Verlauf: {_patient.DisplayName}";

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            Loaded += OnLoaded;
            Unloaded += (_, __) => _timer.Stop();
            Closed += (_, __) => _timer.Stop();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _info.Clear();
            _info["HR"] = (Colors.Yellow, 40, 160, Canvas_HR);
            _info["SpO2"] = (Colors.Purple, 80, 100, Canvas_SPO2);
            _info["RR"] = (Colors.LightBlue, 5, 30, Canvas_RR);
            _info["Temp"] = (Colors.Orange, 35, 43, Canvas_Temp);
            _info["Sys"] = (Colors.Red, 80, 200, Canvas_BP);


            // 🔹 Patientendetails anzeigen
            TxtName.Text = $"{_patient.DisplayName}";
            TxtRoom.Text = $"Zimmer/Bett: {_patient.RoomBed}";
            TxtInfo.Text = $"Alter: {_patient.Age} Jahre   Geschlecht: {_patient.Gender.ToUpper()}   Monitor: {_patient.MonitorId}";

            InitializeGraphs();
            InitializeHistoryData();
            DrawAll();
            _timer.Start();
        }

        /// Initialisiert die Datenlisten für alle Parameter.

        private void InitializeGraphs()
        {
            foreach (var (key, value) in _info)
            {
                _data[key] = new List<double>();
                var line = new Polyline
                {
                    Stroke = new SolidColorBrush(value.color),
                    StrokeThickness = 2
                };
                value.canvas.Children.Add(line);
                _lines[key] = line;
            }
        }


        /// Prüft, wie viele reale Werte schon empfangen wurden (z. B. 30 Sek.),
        /// ergänzt bis 60 Sek. mit Zufallswerten.

        private void InitializeHistoryData()
        {

            int existingCount = 0; 
            int needed = 60 - existingCount;
            if (needed < 0) needed = 0;


            /// Fehlende Werte zufällig generieren um Verlauf aufzufüllen
            for (int i = 0; i < needed; i++)
            {
                _data["HR"].Add(RandomizedPercent(_patient.Hr));
                _data["SpO2"].Add(RandomizedPercent(_patient.Spo2));
                _data["RR"].Add(RandomizedPercent(_patient.Rr));
                _data["Temp"].Add(RandomizedPercent(_patient.Temp));
                _data["Sys"].Add(RandomizedPercent(_patient.Sys));
            }
        }

        private double RandomizedPercent(double value)
        {
            // ±1 % zufällige Abweichung
            double factor = 1.0 + (_rng.NextDouble() * 0.02 - 0.01); // Bereich 0.99–1.01
            return value * factor;
        }


        /// Wird jede Sekunde aufgerufen und speichert den aktuellen Zustand des Patienten.

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Aktuellen Wert hinzufügen
            _data["HR"].Add(_patient.Hr);
            _data["SpO2"].Add(_patient.Spo2);
            _data["RR"].Add(_patient.Rr);
            _data["Temp"].Add(_patient.Temp);
            _data["Sys"].Add(_patient.Sys);

            // Zu viele Werte entfernen (älter als 1h)
            foreach (var key in _data.Keys.ToList())
            {
                while (_data[key].Count > MaxSecondsStored)
                    _data[key].RemoveAt(0);
            }

            DrawAll();
        }

        private void DrawAll()
        {
            foreach (var kvp in _info)
                DrawSingle(kvp.Key);
        }

        private void DrawSingle(string key)
        {
            var (color, min, max, canvas) = _info[key];
            if (!_data.ContainsKey(key)) return;

            double width = canvas.ActualWidth;
            double height = canvas.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                canvas.Loaded += (_, __) => DrawSingle(key);
                return;
            }

            var values = _data[key].TakeLast(_visibleSeconds).ToList();
            if (values.Count == 0) return;

            Polyline line = _lines[key];
            line.Points.Clear();
            DrawGrid(canvas, height, min, max);

            for (int i = 0; i < values.Count; i++)
            {
                double x = (i / (double)_visibleSeconds) * width;
                double normalized = (values[i] - min) / (max - min);
                double y = height - (normalized * height);
                line.Points.Add(new Point(x, y));
            }
        }

        private void DrawGrid(Canvas canvas, double height, double min, double max)
        {
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
                if (!(canvas.Children[i] is Polyline))
                    canvas.Children.RemoveAt(i);

            double mid = (min + max) / 2;
            double midY = height / 2;

            var grid = new Line
            {
                X1 = 0,
                X2 = canvas.ActualWidth,
                Y1 = midY,
                Y2 = midY,
                Stroke = Brushes.Gray,
                StrokeDashArray = new DoubleCollection { 2, 4 },
                StrokeThickness = 1
            };
            canvas.Children.Insert(0, grid);

            var tMin = new TextBlock { Text = $"{min:F1}", Foreground = Brushes.White };
            Canvas.SetLeft(tMin, 2);
            Canvas.SetTop(tMin, height - 18);
            canvas.Children.Insert(0, tMin);

            var tMax = new TextBlock { Text = $"{max:F1}", Foreground = Brushes.White };
            Canvas.SetLeft(tMax, 2);
            Canvas.SetTop(tMax, 0);
            canvas.Children.Insert(0, tMax);
        }

        private void TimeRange_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (int.TryParse(b.Tag.ToString(), out int s))
            {
                _visibleSeconds = s;
                DrawAll();
            }
        }
    }
}
