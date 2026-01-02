using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remotemonitor;
using Xunit;

namespace RemoteMonitor.Tests
{
    public class ThresholdTests
    {
        [Fact]
        public void Threshold_Defaults_AreExpected()
        {
            var t = new Threshold();

            Assert.Equal(36.0, t.TempWarningMin);
            Assert.Equal(38.0, t.TempWarningMax);
            Assert.Equal(35.0, t.TempCriticalMin);
            Assert.Equal(39.0, t.TempCriticalMax);

            Assert.Equal(50, t.HrWarningMin);
            Assert.Equal(110, t.HrWarningMax);
            Assert.Equal(40, t.HrCriticalMin);
            Assert.Equal(130, t.HrCriticalMax);

            Assert.Equal(94, t.Spo2WarningMin);
            Assert.Equal(100, t.Spo2WarningMax);
            Assert.Equal(91, t.Spo2CriticalMin);
            Assert.Equal(100, t.Spo2CriticalMax);

            Assert.Equal(10, t.RrWarningMin);
            Assert.Equal(21, t.RrWarningMax);
            Assert.Equal(8, t.RrCriticalMin);
            Assert.Equal(25, t.RrCriticalMax);

            Assert.Equal(110, t.SysWarningMin);
            Assert.Equal(220, t.SysWarningMax);
            Assert.Equal(90, t.SysCriticalMin);
            Assert.Equal(220, t.SysCriticalMax);

            Assert.Equal(60, t.DiaWarningMin);
            Assert.Equal(170, t.DiaWarningMax);
            Assert.Equal(40, t.DiaCriticalMin);
            Assert.Equal(170, t.DiaCriticalMax);
        }
    }
}
