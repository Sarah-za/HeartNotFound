using Remotemonitor;
using Remotemonitor.Converters;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;
using Xunit.Sdk;


namespace RemoteMonitor.Tests
{
    public class RemoteMonitorSystemTests
    {
        // Fake / Test  DataSource
        private sealed class FakeDataSource : IDataSource, IAsyncDisposable
        {
            public event Action<VitalSample>? OnSample;

            public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

            public void Emit(VitalSample s) => OnSample?.Invoke(s);

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }


        // Helper


        private static void RunSta(Action action)
        {
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                        _ = new Application();

                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (ex != null) throw new Exception("STA/WPF test failed.", ex);
        }

        // RM_20: Bis zu max. 8 Patienten gleichzeitig auswählen und angezeigt bekommen

        [Fact]
        public void RM_20_Max8_Patient_Selected_T2()
        {
            RunSta(() =>
            {
                var src = new FakeDataSource();
                var vm = new MainViewModel(src);

                // Simuliere 9 Patienten
                for (int i = 1; i <= 9; i++)
                {
                    vm.Selected.Add(new VitalSample { PatientId = $"P{i}", Room = "101", Bed = i });
                }

                Assert.Equal(9, vm.Selected.Count);

                Assert.Equal(8, vm.SelectedTop8.Count());

                Assert.Equal(4, vm.Columns);
            });
        }

        // RM_30: Vitaldaten eines Patienten schnell unterscheiden können
        
        [Fact]
        public void RM_30_AlarmLevels_With_DifferentBackground_T3()
        {
            var p = new VitalSample
            {
                Limits = new Threshold
                {
                    HrWarningMin = 60,
                    HrWarningMax = 90,
                    HrCriticalMin = 50,
                    HrCriticalMax = 110
                }
            };

            p.Hr = 80;
            Assert.Equal(VitalSample.VitalAlarmLevel.Normal, p.HrAlarmLevel);

            p.Hr = 95; // > warningMax => Warning
            Assert.Equal(VitalSample.VitalAlarmLevel.Warning, p.HrAlarmLevel);

            p.Hr = 120; // > criticalMax => Critical
            Assert.Equal(VitalSample.VitalAlarmLevel.Critical, p.HrAlarmLevel);

            Assert.True((int)VitalSample.VitalAlarmLevel.Normal < (int)VitalSample.VitalAlarmLevel.Warning);
            Assert.True((int)VitalSample.VitalAlarmLevel.Warning < (int)VitalSample.VitalAlarmLevel.Critical);
        }

        // RM_40: Vitaldaten eindeutig zuordnen können
        // -> PatientId + MonitorId + RoomBed/CardHeaderLine
        

        [Fact]
        public void RM_40_VitalsAreUnique_to_Patient_T4()
        {
            var p = new VitalSample
            {
                PatientId = "P-0007",
                MonitorId = "MON-12",
                Room = "101",
                Bed = 3
            };

            Assert.Equal("101-3", p.RoomBed);
            Assert.Contains("P-0007", p.CardHeaderLine);
            Assert.Contains("101-3", p.CardHeaderLine);
            Assert.Contains("MON-12", p.CardHeaderLine);
        }

        // RM_50: 2-stufige Alarmregeln patientenindividuell einstellen & erkennen
        // RM_60: Überschreitung von Grenzwerten erkennen
        [Fact]
        public void RM_50_RM_60_TwoStageThresholds_T5()
        {
            var a = new VitalSample
            {
                Limits = new Threshold
                {
                    HrWarningMin = 60,
                    HrWarningMax = 90,
                    HrCriticalMin = 50,
                    HrCriticalMax = 110
                }
            };

            var b = new VitalSample
            {
                Limits = new Threshold
                {
                    HrWarningMin = 40,
                    HrWarningMax = 130,
                    HrCriticalMin = 30,
                    HrCriticalMax = 160
                }
            };

            a.Hr = 95;
            b.Hr = 95;

            // Warning
            Assert.Equal(VitalSample.VitalAlarmLevel.Warning, a.HrAlarmLevel);
            Assert.Equal(VitalSample.VitalAlarmLevel.Normal, b.HrAlarmLevel);

            // Critical
            a.Hr = 120;
            Assert.Equal(VitalSample.VitalAlarmLevel.Critical, a.HrAlarmLevel);
        }


        // RM_70: Erkennen, wenn keine Vitaldaten mehr gesendet werden

        [Fact]
        public void RM_70_NetworkStatus_T6()
        {
            var p = new VitalSample { Ts = DateTime.UtcNow.AddSeconds(-31) };

            Assert.True(p.IsStale);

            var statusConv = new BoolToStatusConverter();
            var colorConv = new BoolToColorConverter();

            var status = (string)statusConv.Convert(true, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal("Keine Daten (> 30s)", status);

            var brush = (SolidColorBrush)colorConv.Convert(true, typeof(SolidColorBrush), null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(Colors.Red, brush.Color);
        }

        // RM_80: EWS pro Patient sehen inkl. Zusammensetzung
        [Fact]
        public void RM_80_EWS_T7()
        {
            var p = new VitalSample();

            p.Hr = 135;     // => 3
            p.Spo2 = 92;    // => 2
            p.Rr = 25;      // => 3
            p.Temp = 39.2;  // => 2
            p.Sys = 95;     // => 2

            p.RecalculateEws();

            Assert.Equal(3, p.EwsHr);
            Assert.Equal(2, p.EwsSpo2);
            Assert.Equal(3, p.EwsRr);
            Assert.Equal(2, p.EwsTemp);
            Assert.Equal(2, p.EwsSys);

            Assert.Equal(p.EwsHr + p.EwsSpo2 + p.EwsRr + p.EwsTemp + p.EwsSys, p.EWS);
        }

        // RM_90: Möglichst wenig False Positives

        [Fact]
        public void RM_90_FewFalsePositives_T8()
        {
            var p = new VitalSample
            {
                Limits = new Threshold
                {
                    TempWarningMin = 36.0,
                    TempWarningMax = 38.0,
                    TempCriticalMin = 35.0,
                    TempCriticalMax = 39.0
                }
            };

            // exakt auf Warning-Min/Max -> Normal
            p.Temp = 36.0;
            Assert.Equal(VitalSample.VitalAlarmLevel.Normal, p.TempAlarmLevel);

            p.Temp = 38.0;
            Assert.Equal(VitalSample.VitalAlarmLevel.Normal, p.TempAlarmLevel);
        }
    }
}
