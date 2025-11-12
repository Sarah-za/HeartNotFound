using System.Windows;
using Remotmonitor.Models;
using Remotmonitor.Trends;

namespace Remotmonitor
{
    public partial class PatientDetailWindow : Window
    {
        private TrendRecorder? _recorder;

        public PatientDetailWindow()
        {
            InitializeComponent();
            Loaded += PatientDetailWindow_Loaded;
            Unloaded += (_, __) => _recorder?.Dispose();
            Closed += (_, __) => _recorder?.Dispose();
        }

        private void PatientDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is VitalSample v)
            {
                _recorder = new TrendRecorder(v, capacitySeconds: 60);

                HrSpark.Data = _recorder.HR;
                SpO2Spark.Data = _recorder.SpO2;
                RrSpark.Data = _recorder.RR;
                TempSpark.Data = _recorder.Temp;
                SysSpark.Data = _recorder.Sys;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
