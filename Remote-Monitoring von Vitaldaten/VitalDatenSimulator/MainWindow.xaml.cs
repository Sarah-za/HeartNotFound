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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Threading;

namespace VitalDatenSimulator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Random rnd = new Random(); 
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private GraphWindow graphWindow;

        public string StationID { get; set; }
        private double _heartRate;
        private double _temperature;
        private double _bloodPressure;
        private double _respRate;
        private double _spO2;
        private double _changePercent = 1.0;
        private double _updateIntervall = 1000;
        private bool isRunning = false;

        private double minHR = 40, maxHR = 160;
        private double minTemp = 34, maxTemp = 42;
        private double minBP = 70, maxBP = 240;
        private double minRR = 8, maxRR = 30;
        private double minSpO2 = 80, maxSpO2 = 99; 

        private bool simulateTachy = false;
        private bool simulateHypoxia = false;
        private bool simulateFever = false;

        public string SimulationButtonText => isRunning ? "Stop Simulation" : "Start Simulation";

        public double HeartRate { get => _heartRate; set { _heartRate = value; OnPropertyChanged(nameof(HeartRate)); } }
        public double Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(nameof(Temperature)); } }
        public double BloodPressure { get => _bloodPressure; set { _bloodPressure = value; OnPropertyChanged(nameof(BloodPressure)); } }
        public double RespRate { get => _respRate; set { _respRate = value; OnPropertyChanged(nameof(RespRate)); } }
        public double SpO2 { get => _spO2; set { _spO2 = value; OnPropertyChanged(nameof(SpO2)); } }

        public double ChangePercent { get => _changePercent; set { _changePercent = value; OnPropertyChanged(nameof(ChangePercent)); } }
        public double UpdateIntervall
        {
            get => _updateIntervall;
            set { _updateIntervall = value; OnPropertyChanged(nameof(UpdateIntervall)); timer.Interval = TimeSpan.FromMilliseconds(_updateIntervall); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            StationID = $"Staion ID: {rnd.Next(1000, 9999)}";
            HeartRate = 75;
            BloodPressure = 120;
            RespRate = 16;
            Temperature = 37.0;
            SpO2 = 98;

            timer.Interval = TimeSpan.FromMilliseconds(_updateIntervall);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (simulateTachy)
                HeartRate = SimulateCriticalChange(HeartRate, minHR, maxHR, +5);
            else
                HeartRate = ChangeValue(HeartRate, minHR, maxHR);

            if (simulateFever)
                Temperature = SimulateCriticalChange(Temperature, minTemp, maxTemp, +0.5);
            else
                Temperature = ChangeValue(Temperature, minTemp, maxTemp);

            BloodPressure = ChangeValue(BloodPressure, minBP, maxBP);
            RespRate = ChangeValue(RespRate, minRR, maxRR);

            if (simulateHypoxia)
                SpO2 = SimulateCriticalChange(SpO2, minSpO2, maxSpO2, -3);
            else
                SpO2 = ChangeValue(SpO2, minSpO2, maxSpO2);

            if (graphWindow != null && graphWindow.IsVisible)
            {
                graphWindow.UpdateValues(HeartRate, Temperature, BloodPressure, RespRate, SpO2);
            }
        }

        private double ChangeValue(double value, double min, double max)
        {
            double range = max - min;
            double delta = range * (ChangePercent / 100.0);

            if (rnd.Next(2) == 0)
                value += delta;
            else
                value -= delta;

            if (value < min)
                value = min;
            if (value > max)
                value = max;

            return value;
        }

        private double SimulateCriticalChange(double value, double min, double max, double step)
        {
            value += step;
            if (value < min)
                value = min;
            if (value > max)
                value = max;
            return value;
        }

        private void ToggleSimulation(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
                simulateFever = false;
                simulateHypoxia = false;
                simulateTachy = false;
            }
            else
            {
                timer.Start();
                isRunning = true;
            }

            OnPropertyChanged(nameof(SimulationButtonText));
        }

        private void ShowValues(object sender, EventArgs e)
        {
            string msg = $"Station: {StationID}\n\n" +
                         $"Heart Rate: {HeartRate:F1} bpm\n" +
                         $"Temperature: {Temperature:F1} °C\n" +
                         $"Blood Pressure: {BloodPressure:F1} mmHg\n" +
                         $"Respiratory Rate: {RespRate:F1} breaths/min\n" +
                         $"Oxygen Saturation: {SpO2:F1}%";


            MessageBox.Show(msg, "Current Vital Parameters", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenGraph(object sender, RoutedEventArgs e)
        {
            if (graphWindow == null || !graphWindow.IsVisible)
            {
                graphWindow = new GraphWindow();
                graphWindow.Show();
            }
            else
            {
                graphWindow.Focus();
            }
        }

        private void SimulateTachycardia(object sender , RoutedEventArgs e)
        {
            if (!timer.IsEnabled) 
                return;

            if (!simulateTachy)
                simulateTachy = true;
            else
                simulateTachy = false;
            simulateFever = false;
            simulateHypoxia = false;

        }

        private void SimulateFever(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;
            simulateTachy = false;

            if (!simulateFever)
                simulateFever = true;
            else
                simulateFever = false;

            simulateHypoxia = false;

        }

        private void SimulateHypoxia(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;
            simulateTachy = false;
            simulateFever = false;

            if(!simulateHypoxia)
                simulateHypoxia = true;
            else
                simulateHypoxia= false;

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
