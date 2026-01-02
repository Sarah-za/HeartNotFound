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
    public class VitalSample_AlarmBrushTests
    {
        private static Color ColorOf(SolidColorBrush b) => b.Color;

        [Fact]
        public void EvaluateAlarmBrush_DefaultLimits_Normal_ReturnsLime()
        {
            var v = new VitalSample
            {
                Limits = new Threshold(),
                Hr = 70,
                Spo2 = 98,
                Rr = 16,
                Temp = 37.0,
                Sys = 120,
                Dia = 80
            };

            var brush = v.EvaluateAlarmBrush();

            Assert.Equal(Colors.Lime, ColorOf(brush));
        }

        [Fact]
        public void EvaluateAlarmBrush_DefaultLimits_Warning_ReturnsGold()
        {
            var v = new VitalSample
            {
                Limits = new Threshold(),
                Hr = 55,
                Spo2 = 93,  // Warning (zwischen 94 und 91)
                Rr = 16,
                Temp = 37.0,
                Sys = 120,
                Dia = 80
            };

            var brush = v.EvaluateAlarmBrush();

            Assert.Equal(Colors.Gold, ColorOf(brush));
        }

        [Fact]
        public void EvaluateAlarmBrush_DefaultLimits_Critical_ReturnsRed()
        {
            var v = new VitalSample
            {
                Limits = new Threshold(),
                Hr = 70,
                Spo2 = 90, // <= 91 => Critical
                Rr = 16,
                Temp = 37.0,
                Sys = 120,
                Dia = 80
            };

            var brush = v.EvaluateAlarmBrush();

            Assert.Equal(Colors.Red, ColorOf(brush));
        }

        [Fact]
        public void EvaluateAlarmBrush_CriticalHasPriorityOverWarning()
        {
            var t = new Threshold
            {
                Spo2WarningMin = 94,
                Spo2CriticalMin = 91
            };

            var v = new VitalSample
            {
                Limits = t,
                Spo2 = 90,  // Critical
                Hr = 45,    
                Rr = 16,
                Temp = 37.0,
                Sys = 120,
                Dia = 80
            };

            var brush = v.EvaluateAlarmBrush();

            Assert.Equal(Colors.Red, ColorOf(brush));
        }

        [Fact]
        public void EvaluateAlarmBrush_WhenLimitsIsNull_FallsBackToNewThreshold()
        {
            var v = new VitalSample
            {
                Limits = null!,
                Hr = 70,
                Spo2 = 98,
                Rr = 16,
                Temp = 37.0,
                Sys = 120,
                Dia = 80
            };

            var brush = v.EvaluateAlarmBrush();

            Assert.Equal(Colors.Lime, ColorOf(brush));
        }
    }
}
