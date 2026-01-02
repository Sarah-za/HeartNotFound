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

namespace Remotemonitor
{
    public partial class ThresholdWindow : Window
    {
        private readonly VitalSample _patient;
        private readonly List<Row> _rows;

        public ThresholdWindow(VitalSample patient)
        {
            InitializeComponent();
            _patient = patient;
            _rows = CreateRows(patient.Limits);
            ThresholdGrid.ItemsSource = _rows;
        }

        private static List<Row> CreateRows(Threshold t) => new()
        {
            new Row("Temp (°C)", t.TempWarningMin, t.TempWarningMax, t.TempCriticalMin, t.TempCriticalMax),
            new Row("HR (bpm)", t.HrWarningMin, t.HrWarningMax, t.HrCriticalMin, t.HrCriticalMax),
            new Row("SpO₂ (%)", t.Spo2WarningMin, t.Spo2WarningMax, t.Spo2CriticalMin, t.Spo2CriticalMax),
            new Row("RR (/min)", t.RrWarningMin, t.RrWarningMax, t.RrCriticalMin, t.RrCriticalMax),
            new Row("Sys (mmHg)", t.SysWarningMin, t.SysWarningMax, t.SysCriticalMin, t.SysCriticalMax),
            new Row("Dia (mmHg)", t.DiaWarningMin, t.DiaWarningMax, t.DiaCriticalMin, t.DiaCriticalMax)
        };

        private static bool ValidateRow(Row r, out string error)
        {
            error = "";

            if (r.WarningMin > r.WarningMax)
            {
                error =
                    $"{r.Parameter}: Warning-Min ({r.WarningMin}) darf nicht größer sein als Warning-Max ({r.WarningMax}).";
                return false;
            }

            if (r.WarningMin > r.CriticalMax)
            {
                error =
                    $"{r.Parameter}: Warning-Min ({r.WarningMin}) darf nicht größer sein als Critical-Max ({r.CriticalMax}).";
                return false;
            }
            if (r.CriticalMax < r.WarningMax)
            {
                error =
                    $"{r.Parameter}: Critical-Max ({r.CriticalMax}) darf nicht kleiner sein als Warning-Max ({r.WarningMax}).";
                return false;
            }
            if (r.CriticalMin > r.WarningMax)
            {
                error =
                    $"{r.Parameter}: Critical-Min ({r.CriticalMin}) darf nicht größer sein als Warning-Max ({r.WarningMax}).";
                return false;
            }
            if (r.CriticalMin > r.CriticalMax)
            {
                error =
                    $"{r.Parameter}: Critical-Min ({r.CriticalMin}) darf nicht größer sein als Critical-Max ({r.CriticalMax}).";
                return false;
            }

            if (r.WarningMin < r.CriticalMin)
            {
                error =
                    $"{r.Parameter}: Warning-Min ({r.WarningMin}) darf nicht kleiner sein als Critical-Min ({r.CriticalMin}).";
                return false;
            }

            if (r.WarningMax > r.CriticalMax)
            {
                error =
                    $"{r.Parameter}: Warning-Max ({r.WarningMax}) darf nicht größer sein als Critical-Max ({r.CriticalMax}).";
                return false;
            }

            return true;
        }


        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            // Erst ALLE Zeilen validieren
            foreach (var r in _rows)
            {
                if (!ValidateRow(r, out var msg))
                {
                    MessageBox.Show(
                        msg,
                        "Ungültige Schwellenwerte",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return; // NICHT speichern / Fenster bleibt offen
                }
            }

            //Wenn alles gültig ist: übernehmen
            foreach (var r in _rows)
            {
                switch (r.Parameter)
                {
                    case "Temp (°C)":
                        _patient.Limits.TempWarningMin = r.WarningMin;
                        _patient.Limits.TempWarningMax = r.WarningMax;
                        _patient.Limits.TempCriticalMin = r.CriticalMin;
                        _patient.Limits.TempCriticalMax = r.CriticalMax;
                        break;

                    case "HR (bpm)":
                        _patient.Limits.HrWarningMin = r.WarningMin;
                        _patient.Limits.HrWarningMax = r.WarningMax;
                        _patient.Limits.HrCriticalMin = r.CriticalMin;
                        _patient.Limits.HrCriticalMax = r.CriticalMax;
                        break;

                    case "SpO₂ (%)":
                        _patient.Limits.Spo2WarningMin = r.WarningMin;
                        _patient.Limits.Spo2WarningMax = r.WarningMax;
                        _patient.Limits.Spo2CriticalMin = r.CriticalMin;
                        _patient.Limits.Spo2CriticalMax = r.CriticalMax;
                        break;

                    case "RR (/min)":
                        _patient.Limits.RrWarningMin = r.WarningMin;
                        _patient.Limits.RrWarningMax = r.WarningMax;
                        _patient.Limits.RrCriticalMin = r.CriticalMin;
                        _patient.Limits.RrCriticalMax = r.CriticalMax;
                        break;

                    case "Sys (mmHg)":
                        _patient.Limits.SysWarningMin = r.WarningMin;
                        _patient.Limits.SysWarningMax = r.WarningMax;
                        _patient.Limits.SysCriticalMin = r.CriticalMin;
                        _patient.Limits.SysCriticalMax = r.CriticalMax;
                        break;

                    case "Dia (mmHg)":
                        _patient.Limits.DiaWarningMin = r.WarningMin;
                        _patient.Limits.DiaWarningMax = r.WarningMax;
                        _patient.Limits.DiaCriticalMin = r.CriticalMin;
                        _patient.Limits.DiaCriticalMax = r.CriticalMax;
                        break;
                }
            }

            DialogResult = true;
            _patient.RefreshAlarmProperties();
            Close();

        }


        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public class Row
        {
            public string Parameter { get; set; }
            public double WarningMin { get; set; }
            public double WarningMax { get; set; }
            public double CriticalMin { get; set; }
            public double CriticalMax { get; set; }

            public Row(string param, double wmin, double wmax, double cmin, double cmax)
            {
                Parameter = param;
                WarningMin = wmin;
                WarningMax = wmax;
                CriticalMin = cmin;
                CriticalMax = cmax;
            }
        }
    }
}
