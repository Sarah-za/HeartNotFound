using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalDatenSimulator
{
    public class SimulationStepResult
    {
        public VitalValues Values { get; set; }
        public bool ResetStillActive { get; set; }
    }
}
