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
using Microsoft.UI.Xaml.Input;
using Remotmonitor.Models;


namespace Remotmonitor.Views
{
    /// <summary>
    /// Interaktionslogik für VitalHistoryWindow.xaml
    /// </summary>
    public partial class VitalHistoryWindow : Window
    {
        private readonly VitalSample _patient;
        private readonly Dictionary<string, List<double>> _data = new();
        private readonly Dictionary<string, Polyline> _lines = new();
        private readonly Dictionary<string, (Color color, double min, double max, Canvas canvas)> _info = new();

        private int _visibleSeconds = 60;
        private readonly Random _rng = new();
        public VitalHistoryWindow(VitalSample patient)
        {
            InitializeComponent();
            _patient = patient;
            Title = $"Verlauf: {_patient.DisplayName}";
            Loaded += OnLoaded;
            
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _info.Clear();
            _info["HR"] = (Colors.Yellow, 40, 160, Canvas_HR);
            _info["Temp"] = (Colors.Orange, 35, 43, Canvas_Temp);
            _info["BP"] = (Colors.Red, 80, 200, Canvas_BP);
            _info["RR"] = (Colors.LightBlue, 5, 30, Canvas_RR);
            _info["SpO2"] = (Colors.Purple, 80, 100, Canvas_SPO2);


            InitializeGraphs();
            FillDummyData();
            DrawAll();
        }

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

        private void FillDummyData()
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < 60; i++)
            {
                _data["HR"].Add(_patient.Hr + _rng.Next(-5,6));
                _data["Temp"].Add(_patient.Temp + (_rng.NextDouble() -0.5) * 0.2);
                _data["BP"].Add(_patient.Sys + _rng.Next(-5, 6));
                _data["RR"].Add(_patient.Rr + _rng.Next(-2, 3));
                _data["SpO2"].Add(_patient.Spo2 + _rng.Next(-1, 2));
            }
        }

        public void AddNewSample(VitalSample s)
        {
            _data["HR"].Add(s.Hr);
            _data["Temp"].Add(s.Temp);
            _data["BP"].Add(s.Sys);
            _data["RR"].Add(s.Rr);
            _data["SPO2"].Add(s.Spo2);

            foreach (var k in _data.Keys.ToList())
            {
                while (_data[k].Count > 3600)
                    _data[k].RemoveAt(0);
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
                canvas.Loaded += (_, _) => DrawSingle(key);
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

            var mid = (min + max) / 2;
            var midY = height / 2;

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

            var tMax = new TextBlock {Text = $"{max:F1}", Foreground= Brushes.White };
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
