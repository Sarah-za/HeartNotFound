using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdministrationApp2
{
    public class Monitor
    {
        public string Modell { get; set; }

        public string Status { get; set; } // "🟢 Frei" oder "🔴 Belegt"

        public string PatientName { get; set; }
        public int Moid { get; set; }

        public bool IstBelegt { get; set; } = false;

    }

}

