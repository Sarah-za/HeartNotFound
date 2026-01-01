using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitalDatenSim
{

    // Flags für kritische Situationen
    public class SimulationFlags
    {
        public bool Reset { get; set; }

        public bool SimulateTachy { get; set; }
        public bool SimulateFever { get; set; }
        public bool SimulateHypertonie { get; set; }
        public bool SimulateBradypnoe { get; set; }
        public bool SimulateHypoxia { get; set; }
    }
}
