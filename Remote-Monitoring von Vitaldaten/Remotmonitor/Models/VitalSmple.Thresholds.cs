using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Remotmonitor.Models
{
    public partial class VitalSample
    {
        // Patientenspezifische Schwellwerte
        public Threshold Limits { get; set; } = new Threshold();

        /// <summary>
        /// Berechnet den Alarmstatus anhand der individuellen Schwellenwerte.
        /// </summary>
        public SolidColorBrush EvaluateAlarmBrush()
        {
            var t = Limits ?? new Threshold();

            bool critical =
                Spo2 < t.Spo2CriticalMin || Spo2 > t.Spo2CriticalMax ||
                Hr < t.HrCriticalMin || Hr > t.HrCriticalMax ||
                Rr < t.RrCriticalMin || Rr > t.RrCriticalMax ||
                Temp < t.TempCriticalMin || Temp > t.TempCriticalMax ||
                Sys < t.SysCriticalMin || Sys > t.SysCriticalMax ||
                Dia < t.DiaCriticalMin || Dia > t.DiaCriticalMax;

            if (critical)
                return new SolidColorBrush(Colors.Red);

            bool warning =
                (Spo2 >= t.Spo2WarningMin && Spo2 <= t.Spo2WarningMax) ||

                (Hr >= t.HrWarningMin && Hr <= t.HrWarningMax) ||
                (Hr >= t.HrWarningUpperMin && Hr <= t.HrWarningUpperMax) ||

                (Rr >= t.RrWarningMin && Rr <= t.RrWarningMax) ||
                (Rr >= t.RrWarningUpperMin && Rr <= t.RrWarningUpperMax) ||

                (Temp >= t.TempWarningLowMin && Temp <= t.TempWarningLowMax) ||
                (Temp >= t.TempWarningHighMin && Temp <= t.TempWarningHighMax) ||

                (Sys >= t.SysWarningLowMin && Sys <= t.SysWarningLowMax) ||
                (Sys >= t.SysWarningHighMin && Sys <= t.SysWarningHighMax) ||

                (Dia >= t.DiaWarningLowMin && Dia <= t.DiaWarningLowMax) ||
                (Dia >= t.DiaWarningHighMin && Dia <= t.DiaWarningHighMax);

            if (warning)
                return new SolidColorBrush(Colors.Gold);

            return new SolidColorBrush(Colors.Lime);
        }
    }
}
