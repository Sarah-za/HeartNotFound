using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace VitalDatenSimulator
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private GraphWindow graphWindow;
        private VitalMqttPublisher mqttPublisher;

        // --- NEU: Testbare Simulation ---
        private readonly VitalSimulationSettings _settings = new VitalSimulationSettings();
        private readonly VitalSimulationEngine _engine;
        private readonly SimulationFlags _flags = new SimulationFlags();
        private VitalValues _current;

        // --- UI / Binding Felder ---
        private string _stationID;
        private double _heartRate;
        private double _temperature;
        private double _bloodPressure;
        private double _respRate;
        private double _spO2;

        private double _updateIntervall = 1000;
        private bool isRunning;

        private bool simulateTachy;
        private bool simulateHypoxia;
        private bool simulateFever;
        private bool simulateHypertonie;
        private bool simualteBradypnoe;

        private bool reset;
        private bool crit;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            _engine = new VitalSimulationEngine(_settings, null);

            DataContext = this;

            // Station-ID
            if (Application.Current.Properties.Contains("StationID"))
                StationID = "Station ID: " + Application.Current.Properties["StationID"];
            else
                StationID = "Station ID: UNDEFINED";

            // MQTT (wie vorher)
            mqttPublisher = new VitalMqttPublisher();
            mqttPublisher.Connect();

            // Initialwerte (aus Settings)
            _current = new VitalValues
            {
                HeartRate = _settings.StdHR,
                Temperature = _settings.StdTemp,
                BloodPressure = _settings.StdBP,
                RespRate = _settings.StdRR,
                SpO2 = _settings.StdSpO2
            };

            ApplyValuesToUi(_current);

            // Timer
            timer.Interval = TimeSpan.FromMilliseconds(_updateIntervall);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Flags
            _flags.Reset = reset;
            _flags.SimulateTachy = simulateTachy;
            _flags.SimulateFever = simulateFever;
            _flags.SimulateHypertonie = simulateHypertonie;
            _flags.SimulateBradypnoe = simualteBradypnoe;
            _flags.SimulateHypoxia = simulateHypoxia;

            SimulationStepResult step = _engine.Step(_current, _flags);

            _current = step.Values;
            reset = step.ResetStillActive;

            ApplyValuesToUi(_current);

            // Graph updaten
            if (graphWindow != null && graphWindow.IsVisible)
                graphWindow.UpdateValues(HeartRate, Temperature, BloodPressure, RespRate, SpO2);

            // MQTT publish
            if (mqttPublisher != null && mqttPublisher.IsConnected)
            {
                string stationId = VitalSimulationEngine.ExtractStationId(StationID);
                mqttPublisher.PublishVitalData(stationId, HeartRate, Temperature, BloodPressure, RespRate, SpO2);
            }
        }

        private void ApplyValuesToUi(VitalValues v)
        {
            HeartRate = v.HeartRate;
            Temperature = v.Temperature;
            BloodPressure = v.BloodPressure;
            RespRate = v.RespRate;
            SpO2 = v.SpO2;
        }


        public string StationID
        {
            get { return _stationID; }
            set { _stationID = value; OnPropertyChanged(nameof(StationID)); }
        }

        public double HeartRate
        {
            get { return _heartRate; }
            set { _heartRate = value; OnPropertyChanged(nameof(HeartRate)); }
        }

        public double Temperature
        {
            get { return _temperature; }
            set { _temperature = value; OnPropertyChanged(nameof(Temperature)); }
        }

        public double BloodPressure
        {
            get { return _bloodPressure; }
            set { _bloodPressure = value; OnPropertyChanged(nameof(BloodPressure)); }
        }

        public double RespRate
        {
            get { return _respRate; }
            set { _respRate = value; OnPropertyChanged(nameof(RespRate)); }
        }

        public double SpO2
        {
            get { return _spO2; }
            set { _spO2 = value; OnPropertyChanged(nameof(SpO2)); }
        }

        public double ChangePercent
        {
            get { return _engine.ChangePercent; }
            set { _engine.ChangePercent = value; OnPropertyChanged(nameof(ChangePercent)); }
        }

        public double UpdateIntervall
        {
            get { return _updateIntervall; }
            set
            {
                _updateIntervall = value;
                timer.Interval = TimeSpan.FromMilliseconds(_updateIntervall);
                OnPropertyChanged(nameof(UpdateIntervall));
            }
        }


        // ToggleSimulation (Start/Stop)
        private void ToggleSimulation(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                timer.Stop();
                isRunning = false;

                // Simulationen stoppen
                simulateFever = false;
                simulateHypoxia = false;
                simulateTachy = false;
                simualteBradypnoe = false;
                simulateHypertonie = false;

                reset = false;
                crit = false;
            }
            else
            {
                timer.Start();
                isRunning = true;
            }

        }

        private void ShowValues(object sender, RoutedEventArgs e)
        {
            string msg =
                "Station: " + StationID + "\n\n" +
                "Heart Rate: " + HeartRate.ToString("F1") + " bpm\n" +
                "Temperature: " + Temperature.ToString("F1") + " °C\n" +
                "Blood Pressure: " + BloodPressure.ToString("F1") + " mmHg\n" +
                "Respiratory Rate: " + RespRate.ToString("F1") + " breaths/min\n" +
                "Oxygen Saturation: " + SpO2.ToString("F1") + "%";

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

            // Alles aus, dann Reset aktiv
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

            simulateTachy = !simulateTachy;
            crit = false;
        }

        private void SimulateFever(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            simulateFever = !simulateFever;
            crit = false;
        }

        private void SimulateHypoxia(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            simulateHypoxia = !simulateHypoxia;
            crit = false;
        }

        private void SimulateHypertonie(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            simulateHypertonie = !simulateHypertonie;
            crit = false;
        }

        private void SimulateBradypnoe(object sender, RoutedEventArgs e)
        {
            if (!timer.IsEnabled)
                return;

            simualteBradypnoe = !simualteBradypnoe;
            crit = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (mqttPublisher != null && mqttPublisher.IsConnected)
                mqttPublisher.Disconnect();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
