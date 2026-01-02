using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remotemonitor;
using Remotemonitor.Trends;
using System;
using Xunit;

namespace RemoteMonitor.Tests
{
    public class TrendRecorderTests
    {
        [Fact]
        public void Ctor_FillsAllLists_WithMaxPoints()
        {
            var v = new VitalSample
            {
                Hr = 0,
                Spo2 = 0,
                Rr = 0,
                Temp = 0,
                Sys = 0
            };

            using var tr = new TrendRecorder(v);

            Assert.Equal(3600, tr.Hr.Count);
            Assert.Equal(3600, tr.Spo2.Count);
            Assert.Equal(3600, tr.Rr.Count);
            Assert.Equal(3600, tr.Temp.Count);
            Assert.Equal(3600, tr.Sys.Count);


            Assert.NotEqual(0.0, tr.Hr[0]);
            Assert.NotEqual(0.0, tr.Spo2[0]);
            Assert.NotEqual(0.0, tr.Rr[0]);
            Assert.NotEqual(0.0, tr.Temp[0]);
            Assert.NotEqual(0.0, tr.Sys[0]);
        }

        [Fact]
        public void OnVitalChanged_AddsNewValue_ForHr_AndTrimsToMaxPoints()
        {
            var v = new VitalSample { Hr = 80, Spo2 = 97, Rr = 16, Temp = 36.8, Sys = 120 };
            using var tr = new TrendRecorder(v);

            Assert.Equal(3600, tr.Hr.Count);

            v.Hr = 123;

            Assert.Equal(3600, tr.Hr.Count); 
            Assert.Equal(123, tr.Hr[^1]);
        }

        [Fact]
        public void OnVitalChanged_AddsToCorrectSeries_Spo2()
        {
            var v = new VitalSample { Spo2 = 97 };
            using var tr = new TrendRecorder(v);

            v.Spo2 = 92;

            Assert.Equal(3600, tr.Spo2.Count);
            Assert.Equal(92, tr.Spo2[^1]);
        }

        [Fact]
        public void Dispose_Unsubscribes_FromPropertyChanged()
        {
            var v = new VitalSample { Hr = 80 };
            var tr = new TrendRecorder(v);

            // ein Change vor Dispose -> wird aufgenommen
            v.Hr = 81;
            Assert.Equal(81, tr.Hr[^1]);

            tr.Dispose();

            // Change nach Dispose -> darf NICHT mehr aufgenommen werden
            var last = tr.Hr[^1];
            v.Hr = 82;

            Assert.Equal(last, tr.Hr[^1]);
        }

        [Fact]
        public void ChangingIrrelevantProperty_DoesNotChangeAnySeriesCount()
        {
            var v = new VitalSample();
            using var tr = new TrendRecorder(v);

            var hrCount = tr.Hr.Count;
            var spo2Count = tr.Spo2.Count;
            var rrCount = tr.Rr.Count;
            var tempCount = tr.Temp.Count;
            var sysCount = tr.Sys.Count;

            v.PatientId = "P-0001"; 

            Assert.Equal(hrCount, tr.Hr.Count);
            Assert.Equal(spo2Count, tr.Spo2.Count);
            Assert.Equal(rrCount, tr.Rr.Count);
            Assert.Equal(tempCount, tr.Temp.Count);
            Assert.Equal(sysCount, tr.Sys.Count);
        }
    }
}
