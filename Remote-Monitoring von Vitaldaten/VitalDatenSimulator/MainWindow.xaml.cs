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

        public string StationID { get; set; }
        private double _heartRate;
        private double _temperature;
        private double _bloodPressure;
        private double _respRate;
        private double _spO2;
        private double _changePercent = 2.0;
        private double _updateIntervall = 1000;
        private bool isRunning = false;

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
            HeartRate = ChangeValue(HeartRate, 40, 160);
            Temperature = ChangeValue(Temperature, 34, 42);
            BloodPressure = ChangeValue(BloodPressure, 70, 240);
            RespRate = ChangeValue(RespRate, 8, 30);
            SpO2 = ChangeValue(SpO2, 80, 99);
        }

        private double ChangeValue(double value, double min, double max)
        {
            double factor = 1 + ((rnd.NextDouble() * 2 - 1) * (ChangePercent / 100.0));
            double newValue = value * factor;

            if (newValue < min) { newValue = min; }
            if (newValue > max) { newValue = max; }
          
            return newValue;
        }

        private void ToggleSimulation(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
