using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remotemonitor;
using System.Windows.Media;
using Xunit;

namespace RemoteMonitor.Tests
{
    public class VitalSample_AlarmLevelBrushesTests
    {
        [Fact]
        public void Spo2AlarmLevel_Warning_UsesBlackForeground()
        {
            var v = new VitalSample
            {
                Limits = new Threshold(),
                Spo2 = 93 // Warning, weil < 94 und > 91 (bei den Defaults)
            };

            Assert.Equal(VitalSample.VitalAlarmLevel.Warning, v.Spo2AlarmLevel);
            Assert.Same(Brushes.Black, v.Spo2AlarmForeground);
        }

        [Fact]
        public void Spo2AlarmLevel_Critical_UsesWhiteForeground()
        {
            var v = new VitalSample
            {
                Limits = new Threshold(),
                Spo2 = 90 // Critical
            };

            Assert.Equal(VitalSample.VitalAlarmLevel.Critical, v.Spo2AlarmLevel);
            Assert.Same(Brushes.White, v.Spo2AlarmForeground);
        }

        [Fact]
        public void HrAlarmLevel_Normal_UsesTransparentBackground()
        {
            var v = new VitalSample
            {
                Limits = new Threshold(),
                Hr = 70
            };

            Assert.Equal(VitalSample.VitalAlarmLevel.Normal, v.HrAlarmLevel);
            Assert.Same(Brushes.Transparent, v.HrAlarmBackground);
        }
    }
}
