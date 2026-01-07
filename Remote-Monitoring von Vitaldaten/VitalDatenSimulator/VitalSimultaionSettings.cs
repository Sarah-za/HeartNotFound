using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalDatenSimulator
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

        // Diff zu Std Werte
        public double Hrdiff { get; set; } = 1;
        public double Tempdiff { get; set; } = 0.1;
        public double Bpdiff { get; set; } = 1;
        public double Rrdiff { get; set; } = 0.5;
        public double SpO2diff { get; set; } = 0.5;

        // Reset Step Size
        public double ResetHrStep { get; set; } = 1.0;     // bpm pro Tick
        public double ResetTempStep { get; set; } = 0.1;   // °C pro Tick
        public double ResetBpStep { get; set; } = 1.0;     // mmHg pro Tick
        public double ResetRrStep { get; set; } = 0.5;     // /min pro Tick
        public double ResetSpO2Step { get; set; } = 1.0;   // % pro Tick
    }
}
