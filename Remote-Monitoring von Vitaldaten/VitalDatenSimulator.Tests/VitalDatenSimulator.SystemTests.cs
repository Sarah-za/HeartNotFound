using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VitalDatenSimulator;

namespace VitalDatenSimulator.Tests
{
    [TestClass]
    public class VitalSimulatorSystemTests
    {
        private static VitalSimulationSettings S() => new VitalSimulationSettings();

        // SIM10:
        // typische Vitaldaten in realistischen Wertebereichen
        [TestMethod]
        public void SIM10_PlausibleVitalData_T9()
        {
            var s = S();
            var engine = new VitalSimulationEngine(s, rnd: new Random(12345)) { ChangePercent = 1.0 };

            var flags = new SimulationFlags();

            var cur = new VitalValues
            {
                HeartRate = s.StdHR,
                Temperature = s.StdTemp,
                BloodPressure = s.StdBP,
                RespRate = s.StdRR,
                SpO2 = s.StdSpO2
            };

            double dHr = (s.MaxHR - s.MinHR) * (engine.ChangePercent / 100.0);
            double dTemp = (s.MaxTemp - s.MinTemp) * (engine.ChangePercent / 100.0);
            double dBp = (s.MaxBP - s.MinBP) * (engine.ChangePercent / 100.0);
            double dRr = (s.MaxRR - s.MinRR) * (engine.ChangePercent / 100.0);
            double dSpO2 = (s.MaxSpO2 - s.MinSpO2) * (engine.ChangePercent / 100.0);

            for (int i = 0; i < 250; i++)
            {
                var next = engine.Step(cur, flags).Values;

                AssertInRange(next.HeartRate, s.MinHR, s.MaxHR, "HeartRate out of range");
                AssertInRange(next.Temperature, s.MinTemp, s.MaxTemp, "Temperature out of range");
                AssertInRange(next.BloodPressure, s.MinBP, s.MaxBP, "BloodPressure out of range");
                AssertInRange(next.RespRate, s.MinRR, s.MaxRR, "RespRate out of range");
                AssertInRange(next.SpO2, s.MinSpO2, s.MaxSpO2, "SpO2 out of range");

                Assert.IsTrue(Math.Abs(next.HeartRate - cur.HeartRate) <= dHr + 1e-9, "HR delta too large");
                Assert.IsTrue(Math.Abs(next.Temperature - cur.Temperature) <= dTemp + 1e-9, "Temp delta too large");
                Assert.IsTrue(Math.Abs(next.BloodPressure - cur.BloodPressure) <= dBp + 1e-9, "BP delta too large");
                Assert.IsTrue(Math.Abs(next.RespRate - cur.RespRate) <= dRr + 1e-9, "RR delta too large");
                Assert.IsTrue(Math.Abs(next.SpO2 - cur.SpO2) <= dSpO2 + 1e-9, "SpO2 delta too large");

                cur = next;
            }
        }

        // SIM20:
        // Als Tester will ich die Vitaldaten sehen können
        [TestMethod]
        public void SIM20_GraphWindow_T10()
        {
            Exception threadEx = null;

            int? heartCount = null;
            int? tempCount = null;
            int? bpCount = null;
            int? rrCount = null;
            int? spo2Count = null;

            int? polyHrPoints = null;

            bool? pointsWithinCanvas = null;

            var t = new Thread(() =>
            {
                try
                {
                    var w = new GraphWindow
                    {
                        Width = 800,
                        Height = 600,
                        WindowStyle = WindowStyle.None,
                        ShowInTaskbar = false
                    };

                    w.Show();

                    w.Dispatcher.Invoke(() =>
                    {
                        w.UpdateLayout();
                        w.Measure(new Size(800, 600));
                        w.Arrange(new Rect(0, 0, 800, 600));
                        w.UpdateLayout();
                    });


                    for (int i = 0; i < 150; i++)
                        w.UpdateValues(80 + i % 3, 36.7 + (i % 2) * 0.1, 120 + i % 5, 16 + i % 2, 98 - (i % 4));


                    var data = (IDictionary)GetPrivateField(w, "data");
                    var lines = (IDictionary)GetPrivateField(w, "lines");

                    heartCount = ((IList<double>)data["HeartRate"]).Count;
                    tempCount = ((IList<double>)data["Temperature"]).Count;
                    bpCount = ((IList<double>)data["BloodPressure"]).Count;
                    rrCount = ((IList<double>)data["RespRate"]).Count;
                    spo2Count = ((IList<double>)data["SpO2"]).Count;

 
                    Assert.AreEqual(100, heartCount.Value);
                    Assert.AreEqual(100, tempCount.Value);
                    Assert.AreEqual(100, bpCount.Value);
                    Assert.AreEqual(100, rrCount.Value);
                    Assert.AreEqual(100, spo2Count.Value);

                    var hrLine = (Polyline)lines["HeartRate"];
                    polyHrPoints = hrLine.Points.Count;
                    Assert.IsTrue(polyHrPoints.Value > 0);

                    var canvas = (Canvas)w.FindName("Canvas_HeartRate");
                    double width = canvas.ActualWidth;
                    double height = canvas.ActualHeight;


                    if (width > 0 && height > 0)
                    {
                        pointsWithinCanvas = hrLine.Points.All(p =>
                            p.X >= -1e-6 && p.X <= width + 1e-6 &&
                            p.Y >= -1e-6 && p.Y <= height + 1e-6);
                        Assert.IsTrue(pointsWithinCanvas.Value);
                    }
                    else
                    {

                        pointsWithinCanvas = null;
                    }

                    w.Close();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (threadEx != null)
                throw new AssertFailedException("WPF system test failed", threadEx);

            Assert.AreEqual(100, heartCount);
            Assert.AreEqual(100, tempCount);
            Assert.AreEqual(100, bpCount);
            Assert.AreEqual(100, rrCount);
            Assert.AreEqual(100, spo2Count);
            Assert.IsTrue(polyHrPoints.HasValue && polyHrPoints.Value > 0);

        }

        // SIM30:
        // kritische Situationen simulieren
        [TestMethod]
        public void SIM30_CriticalSituations_T11()
        {
            var s = new VitalSimulationSettings();
            var engine = new VitalSimulationEngine(s, rnd: new Random(7));

            VitalValues BaseValues() => new VitalValues
            {
                HeartRate = s.StdHR,
                Temperature = s.StdTemp,
                BloodPressure = s.StdBP,
                RespRate = s.StdRR,
                SpO2 = s.StdSpO2
            };

            // 1) Tachy: HeartRate steigt pro Tick (+3) bis MaxHR
            {
                var cur = BaseValues();
                var flags = new SimulationFlags { SimulateTachy = true };

                double last = cur.HeartRate;
                for (int i = 0; i < 50; i++)
                {
                    cur = engine.Step(cur, flags).Values;
                    Assert.IsTrue(cur.HeartRate >= last - 1e-12);
                    Assert.IsTrue(cur.HeartRate >= s.MinHR - 1e-12 && cur.HeartRate <= s.MaxHR + 1e-12);
                    last = cur.HeartRate;
                }
                Assert.AreEqual(s.MaxHR, cur.HeartRate, 1e-9);
            }

            // 2) Fever: Temperature steigt pro Tick (+0.25) bis MaxTemp
            {
                var cur = BaseValues();
                var flags = new SimulationFlags { SimulateFever = true };

                double last = cur.Temperature;
                for (int i = 0; i < 50; i++)
                {
                    cur = engine.Step(cur, flags).Values;
                    Assert.IsTrue(cur.Temperature >= last - 1e-12);
                    Assert.IsTrue(cur.Temperature >= s.MinTemp - 1e-12 && cur.Temperature <= s.MaxTemp + 1e-12);
                    last = cur.Temperature;
                }
                Assert.AreEqual(s.MaxTemp, cur.Temperature, 1e-9);
            }

            // 3) Hypertonie: BloodPressure steigt pro Tick (+2) bis MaxBP
            {
                var cur = BaseValues();
                var flags = new SimulationFlags { SimulateHypertonie = true };

                double last = cur.BloodPressure;
                for (int i = 0; i < 80; i++)
                {
                    cur = engine.Step(cur, flags).Values;
                    Assert.IsTrue(cur.BloodPressure >= last - 1e-12);
                    Assert.IsTrue(cur.BloodPressure >= s.MinBP - 1e-12 && cur.BloodPressure <= s.MaxBP + 1e-12);
                    last = cur.BloodPressure;
                }
                Assert.AreEqual(s.MaxBP, cur.BloodPressure, 1e-9);
            }

            // 4) Bradypnoe: RespRate sinkt pro Tick (-0.25) bis MinRR
            {
                var cur = BaseValues();
                var flags = new SimulationFlags { SimulateBradypnoe = true };

                double last = cur.RespRate;
                for (int i = 0; i < 200; i++)
                {
                    cur = engine.Step(cur, flags).Values;
                    Assert.IsTrue(cur.RespRate <= last + 1e-12);
                    Assert.IsTrue(cur.RespRate >= s.MinRR - 1e-12 && cur.RespRate <= s.MaxRR + 1e-12);
                    last = cur.RespRate;
                }
                Assert.AreEqual(s.MinRR, cur.RespRate, 1e-9);
            }

            // 5) Hypoxia: SpO2 sinkt pro Tick (-1.5) bis MinSpO2
            {
                var cur = BaseValues();
                var flags = new SimulationFlags { SimulateHypoxia = true };

                double last = cur.SpO2;
                for (int i = 0; i < 80; i++)
                {
                    cur = engine.Step(cur, flags).Values;
                    Assert.IsTrue(cur.SpO2 <= last + 1e-12);
                    Assert.IsTrue(cur.SpO2 >= s.MinSpO2 - 1e-12 && cur.SpO2 <= s.MaxSpO2 + 1e-12);
                    last = cur.SpO2;
                }
                Assert.AreEqual(s.MinSpO2, cur.SpO2, 1e-9);
            }
        }


        // helpers

        private static void AssertInRange(double v, double min, double max, string msg)
        {
            Assert.IsTrue(v >= min - 1e-12 && v <= max + 1e-12, $"{msg}: {v} not in [{min},{max}]");
        }

        private static object GetPrivateField(object instance, string fieldName)
        {
            var f = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) throw new MissingFieldException(instance.GetType().FullName, fieldName);
            return f.GetValue(instance);
        }
    }
}
