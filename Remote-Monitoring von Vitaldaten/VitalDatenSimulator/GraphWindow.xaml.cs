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

namespace VitalDatenSimulator
{
    /// <summary>
    /// Interaktionslogik für GraphWindow.xaml
    /// </summary>
    public partial class GraphWindow : Window
    {
        private const int MaxPoints = 100;
        private readonly Dictionary<string, List<double>> data = new Dictionary<string, List<double>>();
        private readonly Dictionary<string, Polyline> lines = new Dictionary<string, Polyline>();

        private readonly Dictionary<string, Tuple<Color, double, double>> parameterInfo =
            new Dictionary<string, Tuple<Color, double, double>>()
        {
            {"HeartRate", Tuple.Create(Colors.Yellow, 40.0, 160.0) },
            {"Temperature", Tuple.Create(Colors.Orange, 34.0, 42.0) },
            {"BloodPressure", Tuple.Create(Colors.Red, 70.0, 240.0)},
            {"RespRate", Tuple.Create(Colors.Blue, 8.0, 30.0) },
            {"SpO2", Tuple.Create(Colors.Purple, 80.0, 99.0) }
        };

        public GraphWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeGraph();
        }

        private void InitializeGraph()
        {
            GraphCanvas.Children.Clear();
            data.Clear();
            lines.Clear();

            foreach (var kvp in parameterInfo)
            {
                data[kvp.Key] = new List<double>();
                Polyline line = new Polyline();
                line.Stroke = new SolidColorBrush(kvp.Value.Item1);
                line.StrokeThickness = 2;
                lines[kvp.Key] = line;
                GraphCanvas.Children.Add(line);
            
            }
        }

        public void UpdateValues (double heart, double temp, double bp, double resp, double spo2)
        {
            AddData("HeartRate", heart);
            AddData("Temperature", temp);
            AddData("BloodPressure", bp);
            AddData("RespRate", resp);
            AddData("SpO2", spo2);

            DrawGraph();


        }

        private void AddData(string key, double value)
        {
            if (!data.ContainsKey(key)) return;
            var list = data[key];
            list.Add(value);
            if (list.Count > MaxPoints) list.RemoveAt(0);
        }

        private void DrawGraph()
        {
            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;

            if (width <= 0 || height <= 0) return;

            foreach (var kvp in data)
            {
                var values = kvp.Value;
                var info = parameterInfo[kvp.Key];
                Polyline polyline = lines[kvp.Key];
                polyline.Points.Clear();

                if (values.Count == 0) continue;

                for (int i = 0; i < values.Count; i++)
                {
                    double x = (i / (double)MaxPoints) * width;
                    double normalized = (values[i] - info.Item2) / (info.Item3 - info.Item2);
                    double y = height - (normalized * height);
                    polyline.Points.Add(new Point(x, y));
                }
            }
        }
    }
}
