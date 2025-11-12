using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Remotmonitor.Widgets
{
    public partial class Sparkline : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Sparkline()
        {
            InitializeComponent();
            SizeChanged += (_, __) => Redraw();
            Loaded += (_, __) => Redraw();
        }

        // Daten (letzte N Werte)
        public ObservableCollection<double> Data
        {
            get => (ObservableCollection<double>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(ObservableCollection<double>), typeof(Sparkline),
                new PropertyMetadata(null, (o, e) =>
                {
                    if (e.OldValue is ObservableCollection<double> oldObs)
                        oldObs.CollectionChanged -= ((Sparkline)o).Data_CollectionChanged;
                    if (e.NewValue is ObservableCollection<double> newObs)
                        newObs.CollectionChanged += ((Sparkline)o).Data_CollectionChanged;
                    ((Sparkline)o).Redraw();
                }));

        private void Data_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Redraw();
            OnPropertyChanged(nameof(CurrentText));
        }

        // Anzeige-Min/Max
        public double Min { get => (double)GetValue(MinProperty); set => SetValue(MinProperty, value); }
        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register(nameof(Min), typeof(double), typeof(Sparkline), new PropertyMetadata(0.0, (o, e) => ((Sparkline)o).Redraw()));

        public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register(nameof(Max), typeof(double), typeof(Sparkline), new PropertyMetadata(100.0, (o, e) => ((Sparkline)o).Redraw()));

        // Linienfarbe
        public Brush LineBrush { get => (Brush)GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(nameof(LineBrush), typeof(Brush), typeof(Sparkline),
                new PropertyMetadata(Brushes.Lime, (o, e) => ((Sparkline)o).Redraw()));

        // Label rechts oben
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(Sparkline),
                new PropertyMetadata(string.Empty, (o, e) => ((Sparkline)o).OnPropertyChanged(nameof(CurrentText))));

        public string CurrentText
        {
            get
            {
                if (Data == null || Data.Count == 0) return $"{Label}: –";
                return $"{Label}: {Data[^1]:0.##}";
            }
        }

        private void Redraw()
        {
            if (PlotCanvas == null) return;
            PlotCanvas.Children.Clear();

            if (Data == null || Data.Count < 2 || Max <= Min) return;

            double w = ActualWidth <= 0 ? 1 : ActualWidth;
            double h = ActualHeight <= 0 ? 1 : ActualHeight;

            var poly = new Polyline
            {
                Stroke = LineBrush,
                StrokeThickness = 2,
                SnapsToDevicePixels = true
            };

            int n = Data.Count;
            double dx = (w - 2) / (n - 1);
            for (int i = 0; i < n; i++)
            {
                double v = Data[i];
                double t = (v - Min) / (Max - Min);
                t = t < 0 ? 0 : (t > 1 ? 1 : t);
                double x = 1 + i * dx;
                double y = h - 1 - t * (h - 2);
                poly.Points.Add(new System.Windows.Point(x, y));
            }

            PlotCanvas.Children.Add(poly);
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
