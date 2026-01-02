using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remotemonitor;
using Xunit;

namespace RemoteMonitor.Tests
{
    public class VitalSample_EwsTests
    {
        [Fact]
        public void RecalculateEws_NormalValues_ResultIsZero()
        {
            var v = new VitalSample
            {
                Hr = 70,
                Spo2 = 98,
                Rr = 16,
                Temp = 37.0,
                Sys = 120
            };

            v.RecalculateEws();

            Assert.Equal(0, v.EwsHr);
            Assert.Equal(0, v.EwsSpo2);
            Assert.Equal(0, v.EwsRr);
            Assert.Equal(0, v.EwsTemp);
            Assert.Equal(0, v.EwsSys);
            Assert.Equal(0, v.EWS);
        }

        [Theory]
        [InlineData(40, 2)]
        [InlineData(50, 1)]
        [InlineData(51, 0)]
        [InlineData(91, 1)]
        [InlineData(115, 2)] 
        [InlineData(131, 3)]
        public void RecalculateEws_HrBoundaries_AsImplemented(int hr, int expected)
        {
            var v = new VitalSample { Hr = hr, Spo2 = 98, Rr = 16, Temp = 37.0, Sys = 120 };

            v.RecalculateEws();

            Assert.Equal(expected, v.EwsHr);
        }

        [Theory]
        [InlineData(91, 3)]
        [InlineData(93, 2)]
        [InlineData(95, 1)]
        [InlineData(96, 0)]
        public void RecalculateEws_Spo2Boundaries_AsImplemented(int spo2, int expected)
        {
            var v = new VitalSample { Hr = 70, Spo2 = spo2, Rr = 16, Temp = 37.0, Sys = 120 };

            v.RecalculateEws();

            Assert.Equal(expected, v.EwsSpo2);
        }

        [Theory]
        [InlineData(7, 3)]
        [InlineData(8, 1)]
        [InlineData(11, 1)]
        [InlineData(12, 0)]
        [InlineData(20, 0)]
        [InlineData(21, 2)]
        [InlineData(24, 2)]
        [InlineData(25, 3)]
        public void RecalculateEws_RrBoundaries_AsImplemented(int rr, int expected)
        {
            var v = new VitalSample { Hr = 70, Spo2 = 98, Rr = rr, Temp = 37.0, Sys = 120 };

            v.RecalculateEws();

            Assert.Equal(expected, v.EwsRr);
        }

        [Theory]
        [InlineData(34.9, 3)]
        [InlineData(35.5, 1)]
        [InlineData(38.0, 0)]
        [InlineData(38.5, 1)]
        [InlineData(39.1, 2)]
        public void RecalculateEws_TempBoundaries_AsImplemented(double temp, int expected)
        {
            var v = new VitalSample { Hr = 70, Spo2 = 98, Rr = 16, Temp = temp, Sys = 120 };

            v.RecalculateEws();

            Assert.Equal(expected, v.EwsTemp);
        }

        [Theory]
        [InlineData(90, 3)]
        [InlineData(100, 2)]
        [InlineData(110, 1)]
        [InlineData(111, 0)]
        [InlineData(219, 0)]
        [InlineData(220, 3)]
        public void RecalculateEws_SysBoundaries_AsImplemented(int sys, int expected)
        {
            var v = new VitalSample { Hr = 70, Spo2 = 98, Rr = 16, Temp = 37.0, Sys = sys };

            v.RecalculateEws();

            Assert.Equal(expected, v.EwsSys);
        }

        [Fact]
        public void RecalculateEws_Total_IsSumOfSubscores()
        {
            var v = new VitalSample
            {
                Hr = 40,
                Spo2 = 91,
                Rr = 7,
                Temp = 34.9,
                Sys = 90 
            };

            v.RecalculateEws();

            Assert.Equal(2, v.EwsHr);
            Assert.Equal(3, v.EwsSpo2);
            Assert.Equal(3, v.EwsRr);
            Assert.Equal(3, v.EwsTemp);
            Assert.Equal(3, v.EwsSys);
            Assert.Equal(14, v.EWS);
        }
    }
}
