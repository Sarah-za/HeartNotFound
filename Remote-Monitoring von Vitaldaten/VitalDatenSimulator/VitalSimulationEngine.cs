using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace VitalDatenSimulator
{
    public class VitalSimulationEngine
    {
        private readonly Random _rnd;
        private readonly VitalSimulationSettings _s;

        public VitalSimulationEngine(VitalSimulationSettings settings, Random rnd = null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _s = settings;
            _rnd = rnd ?? new Random();
        }

        public double ChangePercent { get; set; } = 1.0;

        // - Reset: MoveTowards zu Std-Werten
        // - sonst: kritische Änderungen oder ChangeValue

        public SimulationStepResult Step(VitalValues current, SimulationFlags flags)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            if (flags == null)
                throw new ArgumentNullException(nameof(flags));

            VitalValues next = current.Clone();

            if (flags.Reset)
            {
                next.HeartRate = MoveTowardsStep(next.HeartRate, _s.StdHR, _s.ResetHrStep);
                next.Temperature = MoveTowardsStep(next.Temperature, _s.StdTemp, _s.ResetTempStep);
                next.BloodPressure = MoveTowardsStep(next.BloodPressure, _s.StdBP, _s.ResetBpStep);
                next.RespRate = MoveTowardsStep(next.RespRate, _s.StdRR, _s.ResetRrStep);
                next.SpO2 = MoveTowardsStep(next.SpO2, _s.StdSpO2, _s.ResetSpO2Step);

                bool resetDone =
                    Math.Abs(next.HeartRate - _s.StdHR) < _s.Hrdiff &&
                    Math.Abs(next.Temperature - _s.StdTemp) < _s.Tempdiff &&
                    Math.Abs(next.BloodPressure - _s.StdBP) < _s.Bpdiff &&
                    Math.Abs(next.RespRate - _s.StdRR) < _s.Rrdiff &&
                    Math.Abs(next.SpO2 - _s.StdSpO2) < _s.SpO2diff;

                return new SimulationStepResult
                {
                    Values = next,
                    ResetStillActive = !resetDone
                };
            }

            next.HeartRate = flags.SimulateTachy
                ? SimulateCriticalChange(next.HeartRate, _s.MinHR, _s.MaxHR, +3)
                : ChangeValue(next.HeartRate, _s.MinHR, _s.MaxHR);

            next.Temperature = flags.SimulateFever
                ? SimulateCriticalChange(next.Temperature, _s.MinTemp, _s.MaxTemp, +0.25)
                : ChangeValue(next.Temperature, _s.MinTemp, _s.MaxTemp);

            next.BloodPressure = flags.SimulateHypertonie
                ? SimulateCriticalChange(next.BloodPressure, _s.MinBP, _s.MaxBP, +2)
                : ChangeValue(next.BloodPressure, _s.MinBP, _s.MaxBP);

            next.RespRate = flags.SimulateBradypnoe
                ? SimulateCriticalChange(next.RespRate, _s.MinRR, _s.MaxRR, -0.25)
                : ChangeValue(next.RespRate, _s.MinRR, _s.MaxRR);

            next.SpO2 = flags.SimulateHypoxia
                ? SimulateCriticalChange(next.SpO2, _s.MinSpO2, _s.MaxSpO2, -1.5)
                : ChangeValue(next.SpO2, _s.MinSpO2, _s.MaxSpO2);

            return new SimulationStepResult
            {
                Values = next,
                ResetStillActive = false
            };
        }


        public double ChangeValue(double value, double min, double max)
        {
            double range = max - min;
            double delta = range * (ChangePercent / 100.0);

            if (_rnd.Next(2) == 0)
                value += delta;
            else
                value -= delta;

            if (value < min) value = min;
            if (value > max) value = max;

            return value;
        }

        public double SimulateCriticalChange(double value, double min, double max, double step)
        {
            value += step;
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        public double MoveTowards(double current, double target, double fraction)
        {
            double diff = target - current;
            return current + diff * fraction;
        }


        public static string ExtractStationId(string stationIdLabel)
        {
            if (stationIdLabel == null) return "";
            return stationIdLabel.Replace("Station ID:", "").Trim();
        }

        public double MoveTowardsStep(double current, double target, double step)
        {
            double diff = target - current;
            if (Math.Abs(diff) <= step)
                return target;

            return current + Math.Sign(diff) * step;
        }
    }
}
