using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using VitalDatenSim;

namespace VitalDatenSim.Tests
{
    /// <summary>
    /// Deterministischer Random für Tests:
    /// Gibt eine Sequenz an Next(maxValue) zurück (bei dir wird Next(2) verwendet).
    /// </summary>
    internal class SequenceRandom : Random
    {
        private readonly Queue<int> _values;

        public SequenceRandom(params int[] values)
        {
            _values = new Queue<int>(values ?? new int[0]);
        }

        public override int Next(int maxValue)
        {
            if (_values.Count == 0)
                return 0; // Default

            int v = _values.Dequeue();

            // clamp in [0, maxValue-1]
            if (v < 0) v = 0;
            if (v >= maxValue) v = maxValue - 1;

            return v;
        }
    }

    [TestClass]
    public class VitalSimulationEngineTests
    {
        private VitalSimulationSettings CreateDefaultSettings()
        {
            return new VitalSimulationSettings();
        }

        // -------------------------
        // VitalValues.Clone()
        // -------------------------
        [TestMethod]
        public void VitalValues_Clone_CreatesDeepCopy()
        {
            var v = new VitalValues
            {
                HeartRate = 80,
                Temperature = 37.2,
                BloodPressure = 130,
                RespRate = 18,
                SpO2 = 97
            };

            VitalValues c = v.Clone();

            Assert.IsNotNull(c);
            Assert.AreNotSame(v, c);

            Assert.AreEqual(80, c.HeartRate, 1e-12);
            Assert.AreEqual(37.2, c.Temperature, 1e-12);
            Assert.AreEqual(130, c.BloodPressure, 1e-12);
            Assert.AreEqual(18, c.RespRate, 1e-12);
            Assert.AreEqual(97, c.SpO2, 1e-12);

            // Änderung im Clone darf Original nicht verändern
            c.HeartRate = 10;
            Assert.AreEqual(80, v.HeartRate, 1e-12);
        }

        // -------------------------
        // MoveTowards()
        // -------------------------
        [TestMethod]
        public void MoveTowards_FractionZero_ReturnsCurrent()
        {
            var engine = new VitalSimulationEngine(CreateDefaultSettings(), new SequenceRandom(0));
            double r = engine.MoveTowards(50, 100, 0.0);
            Assert.AreEqual(50, r, 1e-12);
        }

        [TestMethod]
        public void MoveTowards_MovesCorrectFraction()
        {
            var engine = new VitalSimulationEngine(CreateDefaultSettings(), new SequenceRandom(0));
            // 50 -> 100 mit fraction 0.1 => 55
            double r = engine.MoveTowards(50, 100, 0.1);
            Assert.AreEqual(55, r, 1e-12);
        }

        // -------------------------
        // SimulateCriticalChange()
        // -------------------------
        [TestMethod]
        public void SimulateCriticalChange_ClampsToMax()
        {
            var engine = new VitalSimulationEngine(CreateDefaultSettings(), new SequenceRandom(0));
            double r = engine.SimulateCriticalChange(158, 40, 160, 10);
            Assert.AreEqual(160, r, 1e-12);
        }

        [TestMethod]
        public void SimulateCriticalChange_ClampsToMin()
        {
            var engine = new VitalSimulationEngine(CreateDefaultSettings(), new SequenceRandom(0));
            double r = engine.SimulateCriticalChange(41, 40, 160, -10);
            Assert.AreEqual(40, r, 1e-12);
        }

        // -------------------------
        // ChangeValue()
        // -------------------------
        [TestMethod]
        public void ChangeValue_Random0_IncreasesByDelta()
        {
            var settings = CreateDefaultSettings();
            var engine = new VitalSimulationEngine(settings, new SequenceRandom(0));
            engine.ChangePercent = 1.0;

            // range 0..100 => delta 1
            double r = engine.ChangeValue(50, 0, 100);
            Assert.AreEqual(51, r, 1e-12);
        }

        [TestMethod]
        public void ChangeValue_Random1_DecreasesByDelta()
        {
            var settings = CreateDefaultSettings();
            var engine = new VitalSimulationEngine(settings, new SequenceRandom(1));
            engine.ChangePercent = 1.0;

            double r = engine.ChangeValue(50, 0, 100);
            Assert.AreEqual(49, r, 1e-12);
        }

        [TestMethod]
        public void ChangeValue_ClampsToBounds()
        {
            var settings = CreateDefaultSettings();

            var plus = new VitalSimulationEngine(settings, new SequenceRandom(0));
            plus.ChangePercent = 10.0; // range 100 => delta 10
            Assert.AreEqual(100, plus.ChangeValue(95, 0, 100), 1e-12);

            var minus = new VitalSimulationEngine(settings, new SequenceRandom(1));
            minus.ChangePercent = 10.0;
            Assert.AreEqual(0, minus.ChangeValue(5, 0, 100), 1e-12);
        }

        // -------------------------
        // ExtractStationId()
        // -------------------------
        [TestMethod]
        public void ExtractStationId_RemovesPrefixAndTrims()
        {
            string id = VitalSimulationEngine.ExtractStationId("Station ID:  1234 ");
            Assert.AreEqual("1234", id);
        }

        [TestMethod]
        public void ExtractStationId_Null_ReturnsEmpty()
        {
            string id = VitalSimulationEngine.ExtractStationId(null);
            Assert.AreEqual("", id);
        }

        // -------------------------
        // Step()
        // -------------------------
        [TestMethod]
        public void Step_Reset_MovesToStdValues_AndStopsResetWhenWithinEps()
        {
            var settings = CreateDefaultSettings();
            settings.ResetFraction = 1.0; // in einem Schritt exakt auf Std

            var engine = new VitalSimulationEngine(settings, new SequenceRandom(0));
            var current = new VitalValues
            {
                HeartRate = 100,
                Temperature = 40,
                BloodPressure = 200,
                RespRate = 25,
                SpO2 = 90
            };

            var flags = new SimulationFlags { Reset = true };

            SimulationStepResult res = engine.Step(current, flags);

            Assert.AreEqual(settings.StdHR, res.Values.HeartRate, 1e-12);
            Assert.AreEqual(settings.StdTemp, res.Values.Temperature, 1e-12);
            Assert.AreEqual(settings.StdBP, res.Values.BloodPressure, 1e-12);
            Assert.AreEqual(settings.StdRR, res.Values.RespRate, 1e-12);
            Assert.AreEqual(settings.StdSpO2, res.Values.SpO2, 1e-12);

            Assert.IsFalse(res.ResetStillActive);
        }

        [TestMethod]
        public void Step_CriticalFlags_UseCriticalSteps()
        {
            var settings = CreateDefaultSettings();
            var engine = new VitalSimulationEngine(settings, new SequenceRandom(0, 0, 0, 0, 0));

            var current = new VitalValues
            {
                HeartRate = settings.StdHR,       // 75
                Temperature = settings.StdTemp,   // 36.7
                BloodPressure = settings.StdBP,   // 120
                RespRate = settings.StdRR,        // 16
                SpO2 = settings.StdSpO2           // 98
            };

            var flags = new SimulationFlags
            {
                Reset = false,
                SimulateTachy = true,       // +3
                SimulateFever = true,       // +0.25
                SimulateHypertonie = true,  // +2
                SimulateBradypnoe = true,   // -0.25
                SimulateHypoxia = true      // -1.5
            };

            SimulationStepResult res = engine.Step(current, flags);

            Assert.AreEqual(78, res.Values.HeartRate, 1e-12);
            Assert.AreEqual(36.95, res.Values.Temperature, 1e-12);
            Assert.AreEqual(122, res.Values.BloodPressure, 1e-12);
            Assert.AreEqual(15.75, res.Values.RespRate, 1e-12);
            Assert.AreEqual(96.5, res.Values.SpO2, 1e-12);

            Assert.IsFalse(res.ResetStillActive);
        }

        [TestMethod]
        public void Step_NoCriticalFlags_UsesChangeValue_DeterministicPlus()
        {
            var settings = CreateDefaultSettings();

            // ChangeValue wird 5x aufgerufen, wir geben überall 0 => +delta
            var engine = new VitalSimulationEngine(settings, new SequenceRandom(0, 0, 0, 0, 0));
            engine.ChangePercent = 1.0;

            var current = new VitalValues
            {
                HeartRate = settings.StdHR,
                Temperature = settings.StdTemp,
                BloodPressure = settings.StdBP,
                RespRate = settings.StdRR,
                SpO2 = settings.StdSpO2
            };

            var flags = new SimulationFlags(); // alles false

            SimulationStepResult res = engine.Step(current, flags);

            // Deltas (1%):
            // HR range 120 => 1.2
            // Temp range 8 => 0.08
            // BP range 170 => 1.7
            // RR range 24 => 0.24
            // SpO2 range 19 => 0.19
            Assert.AreEqual(settings.StdHR + 1.2, res.Values.HeartRate, 1e-9);
            Assert.AreEqual(settings.StdTemp + 0.08, res.Values.Temperature, 1e-9);
            Assert.AreEqual(settings.StdBP + 1.7, res.Values.BloodPressure, 1e-9);
            Assert.AreEqual(settings.StdRR + 0.24, res.Values.RespRate, 1e-9);
            Assert.AreEqual(settings.StdSpO2 + 0.19, res.Values.SpO2, 1e-9);
        }
    }
}
