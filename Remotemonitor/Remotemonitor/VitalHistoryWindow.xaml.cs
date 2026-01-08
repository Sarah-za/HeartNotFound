using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Remotemonitor
{
    public partial class VitalHistoryWindow : Window
    {
        private const int MaxSecondsStored = 3600; // 1 Stunde @ 1Hz
        private const double YAxisMargin = 50; // Platz für Min/Max links

        private readonly VitalSample _patient;
        private readonly DispatcherTimer _refreshTimer;

        private readonly Dictionary<string, Polyline> _lines = new();
        private readonly Dictionary<string, (Color color, double min, double max, Canvas canvas)> _info = new();

        private int _visibleSeconds = 60;

        public VitalHistoryWindow(VitalSample patient)
        {
            InitializeComponent();
            _patient = patient;
            Title = $"Verlauf: {_patient.DisplayName}";

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += (_, __) => DrawAll();

            Loaded += OnLoaded;
            Closed += (_, __) => _refreshTimer.Stop();
        }

        private static double Clamp(double v, double min, double max)
    => Math.Max(min, Math.Min(max, v));

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            TxtName.Text = $"{_patient.PatientId} – {_patient.FirstName} {_patient.LastName}";
            TxtRoom.Text = $"Zimmer/Bett: {_patient.RoomBed}";
            TxtInfo.Text =
                $"Alter: {_patient.Age} Jahre   " +
                $"Geschlecht: {_patient.Gender.ToUpper()}   " +
                $"Monitor: {_patient.MonitorId}";

            _info["HR"] = (Colors.Lime, 40, 160, Canvas_HR);
            _info["SpO2"] = (Colors.DeepSkyBlue, 80, 100, Canvas_SPO2);
            _info["RR"] = (Colors.Gold, 6, 30, Canvas_RR);
            _info["Temp"] = (Colors.White, 34, 43, Canvas_Temp);
            _info["Sys"] = (Colors.Tomato, 70, 240, Canvas_BP);

            foreach (var kvp in _info)
            {
                var line = new Polyline
                {
                    Stroke = new SolidColorBrush(kvp.Value.color),
                    StrokeThickness = 2
                };

                kvp.Value.canvas.Children.Add(line);
                _lines[kvp.Key] = line;
            }

            DrawAll();
            _refreshTimer.Start();
        }

        private void DrawAll()
        {
            foreach (var key in _info.Keys)
                DrawSingle(key);

            DrawTimeAxis();
        }

        private void DrawSingle(string key)
        {
            var (color, min, max, canvas) = _info[key];

            if (_patient.History.Count == 0)
                return;

            double width = canvas.ActualWidth;
            double height = canvas.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            var values = _patient.History
                .TakeLast(_visibleSeconds)
                .Select(s => key switch
                {
                    "HR" => (double)s.Hr,
                    "SpO2" => s.Spo2,
                    "RR" => s.Rr,
                    "Temp" => s.Temp,
                    "Sys" => s.Sys,
                    _ => 0
                })
                .ToList();

            var line = _lines[key];
            line.Points.Clear();

            DrawGrid(canvas, height, min, max);

            for (int i = 0; i < values.Count; i++)
            {
                double plotWidth = width - YAxisMargin;

                double x = YAxisMargin
                         + (i / (double)(_visibleSeconds - 1)) * plotWidth;

                double norm = (values[i] - min) / (max - min);
                double y = height - norm * height;

                line.Points.Add(new Point(x, y));
            }

            var limits = _patient.Limits;

            // WARNING (orange)
            switch (key)
            {
                case "HR":
                    DrawLimitLine(canvas, limits.HrWarningMin, min, max, Brushes.Orange);
                    DrawLimitLine(canvas, limits.HrWarningMax, min, max, Brushes.Orange);
                    break;

                case "RR":
                    DrawLimitLine(canvas, limits.RrWarningMin, min, max, Brushes.Orange);
                    DrawLimitLine(canvas, limits.RrWarningMax, min, max, Brushes.Orange);
                    break;

                case "Temp":
                    DrawLimitLine(canvas, limits.TempWarningMin, min, max, Brushes.Orange);
                    DrawLimitLine(canvas, limits.TempWarningMax, min, max, Brushes.Orange);
                    break;

                case "Sys":
                    DrawLimitLine(canvas, limits.SysWarningMin, min, max, Brushes.Orange);
                    DrawLimitLine(canvas, limits.SysWarningMax, min, max, Brushes.Orange);
                    break;

                case "SpO2":
                    DrawLimitLine(canvas, limits.Spo2WarningMin, min, max, Brushes.Orange);
                    // obere SpO2-Warn-Grenze NICHT zeichnen, da nicht über 100% gehen kann
                    break;
            }

            // CRITICAL (rot)
            switch (key)
            {
                case "HR":
                    DrawLimitLine(canvas, limits.HrCriticalMin, min, max, Brushes.Red);
                    DrawLimitLine(canvas, limits.HrCriticalMax, min, max, Brushes.Red);
                    break;

                case "RR":
                    DrawLimitLine(canvas, limits.RrCriticalMin, min, max, Brushes.Red);
                    DrawLimitLine(canvas, limits.RrCriticalMax, min, max, Brushes.Red);
                    break;

                case "Temp":
                    DrawLimitLine(canvas, limits.TempCriticalMin, min, max, Brushes.Red);
                    DrawLimitLine(canvas, limits.TempCriticalMax, min, max, Brushes.Red);
                    break;

                case "Sys":
                    DrawLimitLine(canvas, limits.SysCriticalMin, min, max, Brushes.Red);
                    DrawLimitLine(canvas, limits.SysCriticalMax, min, max, Brushes.Red);
                    break;

                case "SpO2":
                    DrawLimitLine(canvas, limits.Spo2CriticalMin, min, max, Brushes.Red);
                    // obere SpO2-Kritisch-Grenze NICHT zeichnen, da nicht über 100% gehen kann
                    break;
            }

        }

        private void TimeRange_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (!int.TryParse(b.Tag?.ToString(), out int seconds)) return;

            _visibleSeconds = seconds;
            DrawAll();
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
                X1 = YAxisMargin,
                X2 = canvas.ActualWidth,
                Y1 = midY,
                Y2 = midY,
                Stroke = Brushes.Gray,
                StrokeDashArray = new DoubleCollection { 2, 4 },
                StrokeThickness = 1
            };
            canvas.Children.Insert(0, grid);

            var lblMin = new TextBlock
            {
                Text = min.ToString("F0"),
                Foreground = Brushes.White,
                FontSize = 14
            };
            Canvas.SetLeft(lblMin, 5);
            Canvas.SetTop(lblMin, height - 16);
            canvas.Children.Add(lblMin);

            var lblMax = new TextBlock
            {
                Text = max.ToString("F0"),
                Foreground = Brushes.White,
                FontSize = 14
            };
            Canvas.SetLeft(lblMax, 5);
            Canvas.SetTop(lblMax, 0);
            canvas.Children.Add(lblMax);
        }


        private void Threshold_Click(object sender, RoutedEventArgs e)
        {
            var win = new ThresholdWindow(_patient)
            {
                Owner = this
            };

            bool? result = win.ShowDialog();

            if (result == true)
            {
                _patient.EvaluateAlarmBrush();
            }
        }

        private void DrawTimeAxis()
        {
            Canvas_TimeAxis.Children.Clear();

            double width = Canvas_TimeAxis.ActualWidth;
            double height = Canvas_TimeAxis.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            int totalSeconds = _visibleSeconds;

            int sections = totalSeconds switch
            {
                <= 60 => 6,   // 6 × 10 s
                <= 600 => 6,   // 6 × 100 s
                <= 1800 => 6,   // 6 × 5 min
                _ => 6    // 6 × 10 min
            };

            double plotWidth = width - YAxisMargin;

            for (int i = 0; i <= sections; i++)
            {
                double x = YAxisMargin + i / (double)sections * plotWidth;

                int sec = totalSeconds - i * totalSeconds / sections;

                // Tick
                Canvas_TimeAxis.Children.Add(new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = 6,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                });

                // Label
                var lbl = new TextBlock
                {
                    Text = $"{sec}s",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };

                Canvas.SetLeft(lbl, x - 12);
                Canvas.SetTop(lbl, 8);
                Canvas_TimeAxis.Children.Add(lbl);
            }
        }


        private void DrawLimitLine(
            Canvas canvas,
            double value,
            double min,
            double max,
            Brush color)
        {
            double height = canvas.ActualHeight;
            double width = canvas.ActualWidth;

            // Wert auf sichtbaren Bereich clampen
            double v = Clamp(value, min, max);

            double y = height - (v - min) / (max - min) * height;

            canvas.Children.Add(new Line
            {
                X1 = YAxisMargin,
                X2 = width,
                Y1 = y,
                Y2 = y,
                Stroke = color,
                StrokeThickness = 1.2,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                Opacity = 0.9
            });
        }


    }
}
