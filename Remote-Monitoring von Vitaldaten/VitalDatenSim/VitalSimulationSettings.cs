using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalDatenSim
{
    public class VitalSimulationSettings
    {
        //Min/Max/Std Vitalwerte
        public double MinHR { get; set; } = 40;
        public double MaxHR { get; set; } = 160;
        public double StdHR { get; set; } = 75;

        public double MinTemp { get; set; } = 34;
        public double MaxTemp { get; set; } = 42;
        public double StdTemp { get; set; } = 36.7;

        public double MinBP { get; set; } = 70;
        public double MaxBP { get; set; } = 240;
        public double StdBP { get; set; } = 120;

        public double MinRR { get; set; } = 6;
        public double MaxRR { get; set; } = 30;
        public double StdRR { get; set; } = 16;

        public double MinSpO2 { get; set; } = 80;
        public double MaxSpO2 { get; set; } = 99;
        public double StdSpO2 { get; set; } = 98;

        // Reset
        public double ResetFraction { get; set; } = 0.05;

        // Reset-Step
        public double HrEps { get; set; } = 0.5;
        public double TempEps { get; set; } = 0.05;
        public double BpEps { get; set; } = 0.5;
        public double RrEps { get; set; } = 0.2;
        public double SpO2Eps { get; set; } = 0.5;
    }
}
