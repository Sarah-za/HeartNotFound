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

namespace VitalDatenSimulator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Random rnd = new Random(); 

        public string StationID { get; set; }
        private double _heartRate;
        private double _temperature;
        private double _bloodPressure;
        private double _respRate;
        private double _spO2;

        public double HeartRate { get => _heartRate; set { _heartRate = value; OnPropertyChanged(nameof(HeartRate)); } }
        public double Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(nameof(Temperature)); } }
        public double BloodPressure { get => _bloodPressure; set { _bloodPressure = value; OnPropertyChanged(nameof(BloodPressure)); } }
        public double RespRate { get => _respRate; set { _respRate = value; OnPropertyChanged(nameof(RespRate)); } }
        public double SpO2 { get => _spO2; set { _spO2 = value; OnPropertyChanged(nameof(SpO2)); } }

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
        }

        private void ShowValues(object sender, EventArgs e)
        {
            string msg = $"Station: {StationID}\n\n" +
                         $"Heart Rate: {HeartRate} bpm\n" +
                         $"Temperature: {Temperature} °C\n" +
                         $"Blood Pressure: {BloodPressure} mmHg\n" +
                         $"Respiratory Rate: {RespRate} breaths/min\n" +
                         $"Oxygen Saturation: {SpO2}%";


            MessageBox.Show(msg, "Current Vital Parameters", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
