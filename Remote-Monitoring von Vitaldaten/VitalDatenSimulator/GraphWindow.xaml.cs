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
    public partial class GraphWindow : Window
    {
        private const int MaxPoints = 100;
        private readonly Dictionary<string, List<double>> data = new Dictionary<string, List<double>>();
        private readonly Dictionary<string, Polyline> lines = new Dictionary<string, Polyline>();

        private readonly Dictionary<string, Tuple<Color, double, double, Canvas>> parameterInfo =
            new Dictionary<string, Tuple<Color, double, double, Canvas>>();

        public GraphWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            parameterInfo.Clear();
            parameterInfo.Add("HeartRate", Tuple.Create(Colors.Yellow, 40.0, 160.0, Canvas_HeartRate));
            parameterInfo.Add("Temperature", Tuple.Create(Colors.Orange, 34.0, 42.0, Canvas_Temperature));
            parameterInfo.Add("BloodPressure", Tuple.Create(Colors.Red, 70.0, 240.0, Canvas_BloodPressure));
            parameterInfo.Add("RespRate", Tuple.Create(Colors.Blue, 8.0, 30.0, Canvas_RespRate));
            parameterInfo.Add("SpO2", Tuple.Create(Colors.Purple, 80.0, 99.0, Canvas_SpO2));

            InitializeGraph();
        }

        private void InitializeGraph()
        {


            foreach (var kvp in parameterInfo)
            {
                var key = kvp.Key;
                Canvas canvas = kvp.Value.Item4;

                data[key] = new List<double>();

                Polyline line = new Polyline();
                line.Stroke = new SolidColorBrush(kvp.Value.Item1);
                line.StrokeThickness = 2;

                lines[key] = line;
                canvas.Children.Add(line);

            }
        }

        public void UpdateValues(double heart, double temp, double bp, double resp, double spo2)
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
            if (list.Count > MaxPoints)
                list.RemoveAt(0);
        }

        private void DrawGraph()
        {
            foreach (var kvp in parameterInfo)
            {
                string key = kvp.Key;
                Canvas canvas = kvp.Value.Item4;
                double width = canvas.ActualWidth;
                double height = canvas.ActualHeight;

                if (width <= 0 || height <= 0)
                    continue;

                List<double> values = data[key];
                if (values.Count == 0)
                    continue;

                double min = kvp.Value.Item2;
                double max = kvp.Value.Item3;

                Polyline line = lines[key];
                line.Points.Clear();

                DrawHorizontalGrid(canvas, height, min, max);

                for (int i = 0; i < values.Count; i++)
                {
                    double x = (i / (double)MaxPoints) * width;
                    double normalized = (values[i] - min) / (max - min);
                    double y = height - (normalized * height);
                    line.Points.Add(new Point(x, y));
                }

            }
        }

        private void DrawHorizontalGrid(Canvas canvas, double height, double min, double max)
        {
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
            {
                if (!(canvas.Children[i] is Polyline))
                    canvas.Children.RemoveAt(i);
            }

            double midValue = (max + min) / 2.0;
            double midY = height / 2.0;

            Line gridLine = new Line();
            gridLine.X1 = 0;
            gridLine.X2 = canvas.ActualWidth;
            gridLine.Y1 = midY;
            gridLine.Y2 = midY;
            gridLine.Stroke = Brushes.LightGray;
            gridLine.StrokeDashArray = new DoubleCollection() { 2, 4 };
            gridLine.StrokeThickness = 1;
            canvas.Children.Insert(0, gridLine);

            TextBlock txtMin = new TextBlock();
            txtMin.Text = string.Format("{0:F1}", min);
            Canvas.SetLeft(txtMin, 2);
            Canvas.SetTop(txtMin, height - 16);

            TextBlock txtMax = new TextBlock();
            txtMax.Text = string.Format("{0:F1}", max);
            Canvas.SetLeft(txtMax, 2);
            Canvas.SetTop(txtMax, 0);

            canvas.Children.Insert(0, txtMin);
            canvas.Children.Insert(0, txtMax);
        }
    }
}
