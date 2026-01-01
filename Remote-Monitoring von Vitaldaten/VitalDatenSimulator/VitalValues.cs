using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalDatenSimulator
{
    public class VitalValues
    {
        public double HeartRate { get; set; }
        public double Temperature { get; set; }
        public double BloodPressure { get; set; }
        public double RespRate { get; set; }
        public double SpO2 { get; set; }

        public VitalValues Clone() => new VitalValues
        {
            HeartRate = HeartRate,
            Temperature = Temperature,
            BloodPressure = BloodPressure,
            RespRate = RespRate,
            SpO2 = SpO2
        };
    }
}
