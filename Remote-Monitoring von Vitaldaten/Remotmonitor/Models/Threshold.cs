using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remotmonitor.Models
{
    public class Threshold
    {
        public double HrWarningMin { get; set; } = 40;
        public double HrWarningMax { get; set; } = 49;
        public double HrWarningUpperMin { get; set; } = 111;
        public double HrWarningUpperMax { get; set; } = 130;
        public double HrCriticalMin { get; set; } = 40;
        public double HrCriticalMax { get; set; } = 130;

        public double TempWarningLowMin { get; set; } = 35.5;
        public double TempWarningLowMax { get; set; } = 35.9;
        public double TempWarningHighMin { get; set; } = 37.6;
        public double TempWarningHighMax { get; set; } = 38.5;
        public double TempCriticalMin { get; set; } = 35.5;
        public double TempCriticalMax { get; set; } = 38.5;

        public double SysWarningLowMin { get; set; } = 80;
        public double SysWarningLowMax { get; set; } = 89;
        public double SysWarningHighMin { get; set; } = 140;
        public double SysWarningHighMax { get; set; } = 180;
        public double SysCriticalMin { get; set; } = 80;
        public double SysCriticalMax { get; set; } = 180;

        public double DiaWarningLowMin { get; set; } = 50;
        public double DiaWarningLowMax { get; set; } = 59;
        public double DiaWarningHighMin { get; set; } = 90;
        public double DiaWarningHighMax { get; set; } = 110;
        public double DiaCriticalMin { get; set; } = 50;
        public double DiaCriticalMax { get; set; } = 110;

        public double RrWarningMin { get; set; } = 8;
        public double RrWarningMax { get; set; } = 9;
        public double RrWarningUpperMin { get; set; } = 21;
        public double RrWarningUpperMax { get; set; } = 25;
        public double RrCriticalMin { get; set; } = 8;
        public double RrCriticalMax { get; set; } = 25;

        public double Spo2WarningMin { get; set; } = 90;
        public double Spo2WarningMax { get; set; } = 93;
        public double Spo2CriticalMin { get; set; } = double.MinValue; // <90
        public double Spo2CriticalMax { get; set; } = 90;
    }
}
