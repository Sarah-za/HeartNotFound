using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VitalDatenSimulator;

namespace VitalDatenSimulator.Tests
{

    internal sealed class SequenceRandom : Random
    {
        private readonly Queue<int> _seq;

        public SequenceRandom(params int[] seq)
        {
            _seq = new Queue<int>(seq ?? Array.Empty<int>());
        }

        public override int Next(int maxValue)
        {
            if (maxValue <= 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
            if (_seq.Count == 0) return 0;

            int v = _seq.Dequeue();
            if (v < 0) v = 0;
            if (v >= maxValue) v = maxValue - 1;
            return v;
        }
    }

    [TestClass]
    public class VitalSimulationEngineTests
    {
        private static VitalSimulationSettings S() => new VitalSimulationSettings();

        // VitalValues.Clone()
        [TestMethod]
        public void VitalValues_Clone_CopiesAllFields()
        {
            var v = new VitalValues
            {
                HeartRate = 80,
                Temperature = 37.2,
                BloodPressure = 130,
                RespRate = 18,
                SpO2 = 97
            };

            var c = v.Clone();

            Assert.AreNotSame(v, c);
            Assert.AreEqual(80, c.HeartRate, 1e-12);
            Assert.AreEqual(37.2, c.Temperature, 1e-12);
            Assert.AreEqual(130, c.BloodPressure, 1e-12);
            Assert.AreEqual(18, c.RespRate, 1e-12);
            Assert.AreEqual(97, c.SpO2, 1e-12);

            c.HeartRate = 1;
            Assert.AreEqual(80, v.HeartRate, 1e-12);
        }

        // ExtractStationId()
        [TestMethod]
        public void ExtractStationId_Works()
        {
            Assert.AreEqual("1234", VitalSimulationEngine.ExtractStationId("Station ID:  1234 "));
            Assert.AreEqual("", VitalSimulationEngine.ExtractStationId(null));
        }

        // MoveTowardsStep()
        [TestMethod]
        public void MoveTowardsStep_SnapsToTarget_WhenWithinStep()
        {
            var e = new VitalSimulationEngine(S(), new SequenceRandom(0));
            Assert.AreEqual(10.5, e.MoveTowardsStep(10.0, 10.5, 1.0), 1e-12);
        }

        [TestMethod]
        public void MoveTowardsStep_MovesByStep_WithCorrectDirection()
        {
            var e = new VitalSimulationEngine(S(), new SequenceRandom(0));
            Assert.AreEqual(13.0, e.MoveTowardsStep(10.0, 20.0, 3.0), 1e-12);
            Assert.AreEqual(17.0, e.MoveTowardsStep(20.0, 10.0, 3.0), 1e-12);
        }

        // ChangeValue()
        [TestMethod]
        public void ChangeValue_UsesChangePercent_AndIsDeterministicPlus()
        {
            var e = new VitalSimulationEngine(S(), new SequenceRandom(0));
            e.ChangePercent = 1.0;

            // range 0..100 => delta=1
            Assert.AreEqual(51.0, e.ChangeValue(50.0, 0.0, 100.0), 1e-12);
        }

        [TestMethod]
        public void ChangeValue_IsDeterministicMinus()
        {
            var e = new VitalSimulationEngine(S(), new SequenceRandom(1));
            e.ChangePercent = 1.0;

            Assert.AreEqual(49.0, e.ChangeValue(50.0, 0.0, 100.0), 1e-12);
        }

        [TestMethod]
        public void ChangeValue_ClampsToBounds()
        {
            var ePlus = new VitalSimulationEngine(S(), new SequenceRandom(0));
            ePlus.ChangePercent = 10.0; // range 100 => delta 10
            Assert.AreEqual(100.0, ePlus.ChangeValue(95.0, 0.0, 100.0), 1e-12);

            var eMinus = new VitalSimulationEngine(S(), new SequenceRandom(1));
            eMinus.ChangePercent = 10.0;
            Assert.AreEqual(0.0, eMinus.ChangeValue(5.0, 0.0, 100.0), 1e-12);
        }

        // SimulateCriticalChange()
        [TestMethod]
        public void SimulateCriticalChange_AppliesStep_AndClamps()
        {
            var e = new VitalSimulationEngine(S(), new SequenceRandom(0));

            Assert.AreEqual(160.0, e.SimulateCriticalChange(158.0, 40.0, 160.0, +10.0), 1e-12);
            Assert.AreEqual(40.0, e.SimulateCriticalChange(41.0, 40.0, 160.0, -10.0), 1e-12);
        }

        // Step()
        [TestMethod]
        public void Step_Reset_MovesTowardStd_ByFixedSteps()
        {
            var s = S();

            s.StdHR = 75; s.Hrdiff = 1; s.ResetHrStep = 1;
            s.StdTemp = 36.7; s.Tempdiff = 0.1; s.ResetTempStep = 0.1;
            s.StdBP = 120; s.Bpdiff = 1; s.ResetBpStep = 1;
            s.StdRR = 16; s.Rrdiff = 0.5; s.ResetRrStep = 0.5;
            s.StdSpO2 = 98; s.SpO2diff = 0.5; s.ResetSpO2Step = 1;

            var e = new VitalSimulationEngine(s, new SequenceRandom(0));

            var cur = new VitalValues
            {
                HeartRate = 100,
                Temperature = 40.0,
                BloodPressure = 200,
                RespRate = 25,
                SpO2 = 90
            };

            var flags = new SimulationFlags { Reset = true };

            var r1 = e.Step(cur, flags);

            Assert.AreEqual(99.0, r1.Values.HeartRate, 1e-12);
            Assert.AreEqual(39.9, r1.Values.Temperature, 1e-12);
            Assert.AreEqual(199.0, r1.Values.BloodPressure, 1e-12);
            Assert.AreEqual(24.5, r1.Values.RespRate, 1e-12);
            Assert.AreEqual(91.0, r1.Values.SpO2, 1e-12);

            Assert.IsTrue(r1.ResetStillActive);
        }

        [TestMethod]
        public void Step_Reset_Stops_WhenWithinDiff()
        {
            var s = S();
            s.StdHR = 75; s.Hrdiff = 1; s.ResetHrStep = 10;

            var e = new VitalSimulationEngine(s, new SequenceRandom(0));
            var cur = new VitalValues
            {
                HeartRate = 76.0,
                Temperature = s.StdTemp,
                BloodPressure = s.StdBP,
                RespRate = s.StdRR,
                SpO2 = s.StdSpO2
            };

            var flags = new SimulationFlags { Reset = true };

            var r1 = e.Step(cur, flags);
 
            Assert.AreEqual(75.0, r1.Values.HeartRate, 1e-12);


            Assert.IsFalse(r1.ResetStillActive);
        }

        // Step(): Kritische Flags
        [TestMethod]
        public void Step_CriticalFlags_ApplyCriticalSteps()
        {
            var s = S();
            var e = new VitalSimulationEngine(s, new SequenceRandom(0, 0, 0, 0, 0));

            var cur = new VitalValues
            {
                HeartRate = s.StdHR,       // 75
                Temperature = s.StdTemp,   // 36.7
                BloodPressure = s.StdBP,   // 120
                RespRate = s.StdRR,        // 16
                SpO2 = s.StdSpO2           // 98
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

            var r = e.Step(cur, flags);

            Assert.AreEqual(78.0, r.Values.HeartRate, 1e-12);
            Assert.AreEqual(36.95, r.Values.Temperature, 1e-12);
            Assert.AreEqual(122.0, r.Values.BloodPressure, 1e-12);
            Assert.AreEqual(15.75, r.Values.RespRate, 1e-12);
            Assert.AreEqual(96.5, r.Values.SpO2, 1e-12);

            Assert.IsFalse(r.ResetStillActive);
        }

        // Step(): Normale Variation
        [TestMethod]
        public void Step_NoCriticalFlags_UsesChangeValue()
        {
            var s = S();
            var e = new VitalSimulationEngine(s, new SequenceRandom(0, 0, 0, 0, 0));
            e.ChangePercent = 1.0;

            var cur = new VitalValues
            {
                HeartRate = s.StdHR,
                Temperature = s.StdTemp,
                BloodPressure = s.StdBP,
                RespRate = s.StdRR,
                SpO2 = s.StdSpO2
            };

            var flags = new SimulationFlags(); // alles false

            var r = e.Step(cur, flags);

            // Deltas (ChangeValue):
            // HR range = 160-40 = 120 => 1% = 1.2
            // Temp range = 42-34 = 8 => 1% = 0.08
            // BP range = 240-70 = 170 => 1% = 1.7
            // RR range = 30-6 = 24 => 1% = 0.24
            // SpO2 range = 99-80 = 19 => 1% = 0.19
            Assert.AreEqual(s.StdHR + 1.2, r.Values.HeartRate, 1e-9);
            Assert.AreEqual(s.StdTemp + 0.08, r.Values.Temperature, 1e-9);
            Assert.AreEqual(s.StdBP + 1.7, r.Values.BloodPressure, 1e-9);
            Assert.AreEqual(s.StdRR + 0.24, r.Values.RespRate, 1e-9);
            Assert.AreEqual(s.StdSpO2 + 0.19, r.Values.SpO2, 1e-9);
        }

        [TestMethod]
        public void Step_Throws_OnNullArgs()
        {
            var e = new VitalSimulationEngine(S(), new SequenceRandom(0));
            Assert.ThrowsException<ArgumentNullException>(() => e.Step(null, new SimulationFlags()));
            Assert.ThrowsException<ArgumentNullException>(() => e.Step(new VitalValues(), null));
        }

        [TestMethod]
        public void Ctor_Throws_OnNullSettings()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new VitalSimulationEngine(null));
        }
    }
}
