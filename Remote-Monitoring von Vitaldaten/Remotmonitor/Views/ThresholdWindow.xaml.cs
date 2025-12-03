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
using System.Collections.Generic;
using System.Linq;
using Remotmonitor.Models;

namespace Remotmonitor.Views
{
    public partial class ThresholdWindow : Window
    {
        private readonly VitalSample _patient;
        private readonly List<Row> _rows;

        public ThresholdWindow(VitalSample patient)
        {
            InitializeComponent();
            _patient = patient;
            _rows = CreateRows(patient.Limits ?? new Threshold());
            ThresholdGrid.ItemsSource = _rows;
        }

        private static List<Row> CreateRows(Threshold t) => new()
        {
            new Row("SpO₂", t.Spo2WarningMin, t.Spo2WarningMax, t.Spo2CriticalMin, t.Spo2CriticalMax),

            new Row("HR (low)", t.HrWarningMin, t.HrWarningMax, t.HrCriticalMin, t.HrCriticalMin),
            new Row("HR (high)", t.HrWarningUpperMin, t.HrWarningUpperMax, t.HrCriticalMax, t.HrCriticalMax),

            new Row("RR (low)", t.RrWarningMin, t.RrWarningMax, t.RrCriticalMin, t.RrCriticalMin),
            new Row("RR (high)", t.RrWarningUpperMin, t.RrWarningUpperMax, t.RrCriticalMax, t.RrCriticalMax),

            new Row("Temp (low)", t.TempWarningLowMin, t.TempWarningLowMax, t.TempCriticalMin, t.TempCriticalMin),
            new Row("Temp (high)", t.TempWarningHighMin, t.TempWarningHighMax, t.TempCriticalMax, t.TempCriticalMax),

            new Row("Sys (low)", t.SysWarningLowMin, t.SysWarningLowMax, t.SysCriticalMin, t.SysCriticalMin),
            new Row("Sys (high)", t.SysWarningHighMin, t.SysWarningHighMax, t.SysCriticalMax, t.SysCriticalMax),

            new Row("Dia (low)", t.DiaWarningLowMin, t.DiaWarningLowMax, t.DiaCriticalMin, t.DiaCriticalMin),
            new Row("Dia (high)", t.DiaWarningHighMin, t.DiaWarningHighMax, t.DiaCriticalMax, t.DiaCriticalMax)
        };

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            // Werte aus Grid übernehmen
            foreach (var r in _rows)
            {
                switch (r.Parameter)
                {
                    case "SpO₂":
                        _patient.Limits.Spo2WarningMin = r.WarningMin;
                        _patient.Limits.Spo2WarningMax = r.WarningMax;
                        _patient.Limits.Spo2CriticalMin = r.CriticalMin;
                        _patient.Limits.Spo2CriticalMax = r.CriticalMax;
                        break;

                    case "HR (low)":
                        _patient.Limits.HrWarningMin = r.WarningMin;
                        _patient.Limits.HrWarningMax = r.WarningMax;
                        _patient.Limits.HrCriticalMin = r.CriticalMin;
                        break;

                    case "HR (high)":
                        _patient.Limits.HrWarningUpperMin = r.WarningMin;
                        _patient.Limits.HrWarningUpperMax = r.WarningMax;
                        _patient.Limits.HrCriticalMax = r.CriticalMax;
                        break;

                    case "RR (low)":
                        _patient.Limits.RrWarningMin = r.WarningMin;
                        _patient.Limits.RrWarningMax = r.WarningMax;
                        _patient.Limits.RrCriticalMin = r.CriticalMin;
                        break;

                    case "RR (high)":
                        _patient.Limits.RrWarningUpperMin = r.WarningMin;
                        _patient.Limits.RrWarningUpperMax = r.WarningMax;
                        _patient.Limits.RrCriticalMax = r.CriticalMax;
                        break;

                    case "Temp (low)":
                        _patient.Limits.TempWarningLowMin = r.WarningMin;
                        _patient.Limits.TempWarningLowMax = r.WarningMax;
                        _patient.Limits.TempCriticalMin = r.CriticalMin;
                        break;

                    case "Temp (high)":
                        _patient.Limits.TempWarningHighMin = r.WarningMin;
                        _patient.Limits.TempWarningHighMax = r.WarningMax;
                        _patient.Limits.TempCriticalMax = r.CriticalMax;
                        break;

                    case "Sys (low)":
                        _patient.Limits.SysWarningLowMin = r.WarningMin;
                        _patient.Limits.SysWarningLowMax = r.WarningMax;
                        _patient.Limits.SysCriticalMin = r.CriticalMin;
                        break;

                    case "Sys (high)":
                        _patient.Limits.SysWarningHighMin = r.WarningMin;
                        _patient.Limits.SysWarningHighMax = r.WarningMax;
                        _patient.Limits.SysCriticalMax = r.CriticalMax;
                        break;

                    case "Dia (low)":
                        _patient.Limits.DiaWarningLowMin = r.WarningMin;
                        _patient.Limits.DiaWarningLowMax = r.WarningMax;
                        _patient.Limits.DiaCriticalMin = r.CriticalMin;
                        break;

                    case "Dia (high)":
                        _patient.Limits.DiaWarningHighMin = r.WarningMin;
                        _patient.Limits.DiaWarningHighMax = r.WarningMax;
                        _patient.Limits.DiaCriticalMax = r.CriticalMax;
                        break;
                }
            }

            DialogResult = true;
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

            public Row(string p, double wMin, double wMax, double cMin, double cMax)
            {
                Parameter = p;
                WarningMin = wMin;
                WarningMax = wMax;
                CriticalMin = cMin;
                CriticalMax = cMax;
            }
        }
    }
}

