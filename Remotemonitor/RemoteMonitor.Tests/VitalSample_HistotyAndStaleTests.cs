using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remotemonitor;
using System;
using Xunit;

namespace RemoteMonitor.Tests
{
    public class VitalSample_HistoryAndStaleTests
    {
        [Fact]
        public void IsStale_WhenOlderThan30Seconds_IsTrue()
        {
            var v = new VitalSample
            {
                Ts = DateTime.UtcNow.AddSeconds(-35)
            };

            Assert.True(v.IsStale);
            Assert.Equal("Keine Daten", v.Status);
        }

        [Fact]
        public void IsStale_WhenRecent_IsFalse()
        {
            var v = new VitalSample
            {
                Ts = DateTime.UtcNow.AddSeconds(-5)
            };

            Assert.False(v.IsStale);
            Assert.Equal("OK", v.Status);
        }

        [Fact]
        public void AddSnapshot_AddsEntry_AndCopiesCurrentValues()
        {
            var v = new VitalSample
            {
                Hr = 77,
                Spo2 = 99,
                Rr = 15,
                Temp = 37.4,
                Sys = 123
            };

            v.AddSnapshot();

            Assert.Single(v.History);
            Assert.Equal(77, v.History[0].Hr);
            Assert.Equal(99, v.History[0].Spo2);
            Assert.Equal(15, v.History[0].Rr);
            Assert.Equal(37.4, v.History[0].Temp);
            Assert.Equal(123, v.History[0].Sys);
            Assert.True((DateTime.UtcNow - v.History[0].Ts).TotalSeconds < 2);
        }

        [Fact]
        public void AddSnapshot_TrimsHistoryBeyondMaxHistorySeconds()
        {
            var v = new VitalSample();

            // MaxHistorySeconds ist 3600
            for (int i = 0; i < 3605; i++)
            {
                v.Hr = i;
                v.AddSnapshot();
            }

            Assert.Equal(3600, v.History.Count);
            // nach dem Trimmen ist das erste Element nicht mehr der ursprüngliche 0er
            Assert.NotEqual(0, v.History[0].Hr);
        }
    }
}
