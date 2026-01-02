using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Remotemonitor
{
    public partial class VitalSample
    {
        // pro Patient zur Laufzeit
        public Threshold Limits { get; set; } = new Threshold();

        public SolidColorBrush EvaluateAlarmBrush()
        {
            var t = Limits ?? new Threshold();

            // Kritisch: ≤ criticalMin oder ≥ criticalMax
            bool critical =
                Spo2 <= t.Spo2CriticalMin || Spo2 >= t.Spo2CriticalMax ||
                Hr <= t.HrCriticalMin || Hr >= t.HrCriticalMax ||
                Rr <= t.RrCriticalMin || Rr >= t.RrCriticalMax ||
                Temp <= t.TempCriticalMin || Temp >= t.TempCriticalMax ||
                Sys <= t.SysCriticalMin || Sys >= t.SysCriticalMax ||
                Dia <= t.DiaCriticalMin || Dia >= t.DiaCriticalMax;

            if (critical) return new SolidColorBrush(Colors.Red);

            // Warnung: (zwischen critical und warning außerhalb der Normalzone)
            bool warning =
                (Spo2 < t.Spo2WarningMin && Spo2 > t.Spo2CriticalMin) || (Spo2 > t.Spo2WarningMax && Spo2 < t.Spo2CriticalMax) ||
                (Hr < t.HrWarningMin && Hr > t.HrCriticalMin) || (Hr > t.HrWarningMax && Hr < t.HrCriticalMax) ||
                (Rr < t.RrWarningMin && Rr > t.RrCriticalMin) || (Rr > t.RrWarningMax && Rr < t.RrCriticalMax) ||
                (Temp < t.TempWarningMin && Temp > t.TempCriticalMin) || (Temp > t.TempWarningMax && Temp < t.TempCriticalMax) ||
                (Sys < t.SysWarningMin && Sys > t.SysCriticalMin) || (Sys > t.SysWarningMax && Sys < t.SysCriticalMax) ||
                (Dia < t.DiaWarningMin && Dia > t.DiaCriticalMin) || (Dia > t.DiaWarningMax && Dia < t.DiaCriticalMax);

            if (warning) return new SolidColorBrush(Colors.Gold);

            return new SolidColorBrush(Colors.Lime);
        }

        // von außen aufrufbar (z. B. nach Threshold-Änderung)
        public void RefreshAlarmColor() => OnPropertyChanged(nameof(AlarmColor));
    }
}
