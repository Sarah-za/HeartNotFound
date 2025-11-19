using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;

namespace VitalDatenSim
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Random rnd = new Random();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private GraphWindow graphWindow;
        private VitalMqttPublisher mqttPublisher;

        private string _stationID;
        public string StationID
        {
            get => _stationID;
            set
            {
                _stationID = value;
                OnPropertyChanged(nameof(StationID));
            }
        }
        private double _heartRate;
        private double _temperature;
        private double _bloodPressure;
        private double _respRate;
        private double _spO2;
        private double _changePercent = 1.0;
        private double _updateIntervall = 1000;
        private bool isRunning = false;

        private double minHR = 40, maxHR = 160, stdHR = 75;
        private double minTemp = 34, maxTemp = 42, stdTemp = 37.0;
        private double minBP = 70, maxBP = 240, stdBP = 120;
        private double minRR = 8, maxRR = 30, stdRespRate = 16;
        private double minSpO2 = 80, maxSpO2 = 99, stdSpO2 = 98;

        private bool simulateTachy = false;
        private bool simulateHypoxia = false;
        private bool simulateFever = false;
        private bool simulateHypertonie = false;
        private bool simualteBradypnoe = false;

        private bool reset = false;
        private bool crit = false;

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
            // StationID = $"Station ID: {rnd.Next(1000, 9999)}";

            DataContext = this;

            var inputDialog = new InputDialog();
            bool? result = inputDialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(inputDialog.EnteredID))
            {
                StationID = $"Station ID: {inputDialog.EnteredID}";
            }
            else
            {
                StationID = $"Station ID: {rnd.Next(1000, 9999)}";
            }


            mqttPublisher = new VitalMqttPublisher();
            mqttPublisher.Connect();


            HeartRate = stdHR;
            BloodPressure = stdBP;
            RespRate = stdRespRate;
            Temperature = stdTemp;
            SpO2 = stdSpO2;

            timer.Interval = TimeSpan.FromMilliseconds(_updateIntervall);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (reset)
            {
                HeartRate = MoveTowards(HeartRate, stdHR, 0.05);
                Temperature = MoveTowards(Temperature, stdTemp, 0.05);
                BloodPressure = MoveTowards(BloodPressure, stdBP, 0.05);
                RespRate = MoveTowards(RespRate, stdRespRate, 0.05);
                SpO2 = MoveTowards(SpO2, stdSpO2, 0.05);

                if (Math.Abs(HeartRate - stdHR) < 0.5 &&
                    Math.Abs(Temperature - stdTemp) < 0.05 &&
                    Math.Abs(BloodPressure - stdBP) < 0.5 &&
                    Math.Abs(RespRate - stdRespRate) < 0.2 &&
                    Math.Abs(SpO2 - stdSpO2) < 0.5)
                {
                    reset = false;
                }

            }


            else
            {
                if (simulateTachy)
                    HeartRate = SimulateCriticalChange(HeartRate, minHR, maxHR, +3);
                else
                    HeartRate = ChangeValue(HeartRate, minHR, maxHR);

                if (simulateFever)
                    Temperature = SimulateCriticalChange(Temperature, minTemp, maxTemp, +0.25);
                else
                    Temperature = ChangeValue(Temperature, minTemp, maxTemp);

                if (simulateHypertonie)
                    BloodPressure = SimulateCriticalChange(BloodPressure, minBP, maxBP, +2);
                else
                    BloodPressure = ChangeValue(BloodPressure, minBP, maxBP);

                if (simualteBradypnoe)
                    RespRate = SimulateCriticalChange(RespRate, minRR, maxRR, -0.25);
                else
                    RespRate = ChangeValue(RespRate, minRR, maxRR);

                if (simulateHypoxia)
                    SpO2 = SimulateCriticalChange(SpO2, minSpO2, maxSpO2, -1.5);
                else
                    SpO2 = ChangeValue(SpO2, minSpO2, maxSpO2);

            }

            if (graphWindow != null && graphWindow.IsVisible)
            {
                graphWindow.UpdateValues(HeartRate, Temperature, BloodPressure, RespRate, SpO2);
            }

            if (mqttPublisher != null && mqttPublisher.IsConnected)
            {
                string id = StationID.Replace("Station ID:", "").Trim();
                mqttPublisher.PublishVitalData(id, HeartRate, Temperature, BloodPressure, RespRate, SpO2);
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

        private double MoveTowards(double current, double target, double fraction)
        {
            double diff = target - current;
            return current + diff * fraction;
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
                simualteBradypnoe = false;
                simulateHypertonie = false;
                reset = false;
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

        private void Reset(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
                return;

            simulateFever = false;
            simulateHypoxia = false;
            simulateTachy = false;
            simualteBradypnoe = false;
            simulateHypertonie = false;
            crit = false;
            reset = true;
        }

        private void AlmostDead(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
                return;

            if (!crit)
            {
                simulateFever = true;
                simulateHypoxia = true;
                simulateTachy = true;
                simualteBradypnoe = true;
                simulateHypertonie = true;
                reset = false;
                crit = true;
            }
            else
            {
                simulateFever = false;
                simulateHypoxia = false;
                simulateTachy = false;
                simualteBradypnoe = false;
                simulateHypertonie = false;
                reset = false;
                crit = false;
            }

        }

        private void SimulateTachycardia(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            if (!simulateTachy)
                simulateTachy = true;
            else
                simulateTachy = false;
            crit = false;
        }

        private void SimulateFever(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            if (!simulateFever)
                simulateFever = true;
            else
                simulateFever = false;
            crit = false;
        }

        private void SimulateHypoxia(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            if (!simulateHypoxia)
                simulateHypoxia = true;
            else
                simulateHypoxia = false;
            crit = false;
        }

        private void SimulateHypertonie(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            if (!simulateHypertonie)
                simulateHypertonie = true;
            else
                simulateHypertonie = false;
            crit = false;
        }

        private void SimulateBradypnoe(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            if (!simualteBradypnoe)
                simualteBradypnoe = true;
            else
                simualteBradypnoe = false;
            crit = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnClosed(EventArgs e)
        {

            base.OnClosed(e);
            if (mqttPublisher != null && mqttPublisher.IsConnected)
                mqttPublisher.Disconnect();
        }
    }
}
