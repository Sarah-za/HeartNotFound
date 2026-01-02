using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remotemonitor
{
    public class VitalSnapshot
    {
        public DateTime Ts { get; init; }
        public int Hr { get; init; }
        public int Spo2 { get; init; }
        public int Rr { get; init; }
        public double Temp { get; init; }
        public int Sys { get; init; }
    }
}
