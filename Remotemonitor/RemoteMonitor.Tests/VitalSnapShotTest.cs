using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Remotemonitor;
using Xunit;

namespace RemoteMonitor.Tests
{
    public class VitalSnapshotTests
    {
        [Fact]
        public void VitalSnapshot_InitProperties_AssignedCorrectly()
        {
            var ts = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var snap = new VitalSnapshot
            {
                Ts = ts,
                Hr = 80,
                Spo2 = 97,
                Rr = 14,
                Temp = 37.2,
                Sys = 120
            };

            Assert.Equal(ts, snap.Ts);
            Assert.Equal(80, snap.Hr);
            Assert.Equal(97, snap.Spo2);
            Assert.Equal(14, snap.Rr);
            Assert.Equal(37.2, snap.Temp);
            Assert.Equal(120, snap.Sys);
        }
    }
}
