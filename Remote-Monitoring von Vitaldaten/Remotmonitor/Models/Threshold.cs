using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remotmonitor.Models
{
    public class Threshold
    {
        // Temperatur (°C)
        public double TempWarningMin { get; set; } = 36.5;
        public double TempWarningMax { get; set; } = 37.0;
        public double TempCriticalMin { get; set; } = 35.0;
        public double TempCriticalMax { get; set; } = 39.0;

        // Herzfrequenz (bpm)
        public double HrWarningMin { get; set; } = 50;
        public double HrWarningMax { get; set; } = 110;
        public double HrCriticalMin { get; set; } = 40;
        public double HrCriticalMax { get; set; } = 130;

        // SpO₂ (%)
        public double Spo2WarningMin { get; set; } = 93;
        public double Spo2WarningMax { get; set; } = 100;
        public double Spo2CriticalMin { get; set; } = 90;
        public double Spo2CriticalMax { get; set; } = 100;

        // Atemfrequenz (RR, /min)
        public double RrWarningMin { get; set; } = 10;
        public double RrWarningMax { get; set; } = 20;
        public double RrCriticalMin { get; set; } = 8;
        public double RrCriticalMax { get; set; } = 25;

        // Blutdruck systolisch (mmHg)
        public double SysWarningMin { get; set; } = 90;
        public double SysWarningMax { get; set; } = 140;
        public double SysCriticalMin { get; set; } = 80;
        public double SysCriticalMax { get; set; } = 180;

        // Blutdruck diastolisch (mmHg)
        public double DiaWarningMin { get; set; } = 60;
        public double DiaWarningMax { get; set; } = 90;
        public double DiaCriticalMin { get; set; } = 50;
        public double DiaCriticalMax { get; set; } = 110;
    }
}
