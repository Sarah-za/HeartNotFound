using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdministrationApp
{
    public class Patient
    {
        public int Id { get; set; }
        public string Vorname { get; set; } = string.Empty;
        public string Nachname { get; set; } = string.Empty;
        public int Moid { get; set; }// Zugeordneter Monitor
        public string MonitorName { get; set; }
    }
}
