using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdministrationApp
{
    public class Monitor
    {
        public int Moid { get; set; }
        public string Modell { get; set; }
        public bool IstBelegt { get; set; } = false;
    }
}
