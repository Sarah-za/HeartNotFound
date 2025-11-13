using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AdministrationApp;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AdministrationApp
{
    public partial class MainWindow : Window
    {
        private List<Monitor> monitore = new List<Monitor>();
        private List<Patient> patienten = new List<Patient>();
        private int naechstePatientId = 1;

        public MainWindow()
        {
            InitializeComponent();
            for (int i = 1; i <= 16; i++)
            {
                monitore.Add(new Monitor { Moid = i, Modell = $"Modell {i}", IstBelegt = false });
            }
            AktualisiereAnzeige();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string vorname = txtVorname.Text.Trim();
            string nachname = txtNachname.Text.Trim();

            if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname))
            {
                MessageBox.Show("Bitte Vorname und Nachname eingeben!");
                return;
            }

            var freierMonitor = monitore.FirstOrDefault(m => !m.IstBelegt);
            if (freierMonitor == null)
            {
                MessageBox.Show("❌ Keine freien Monitore verfügbar!");
                return;
            }

            var patient = new Patient
            {
                Id = naechstePatientId++,
                Vorname = vorname,
                Nachname = nachname,
                MonitorId = freierMonitor.Moid
            };

            patienten.Add(patient);
            freierMonitor.IstBelegt = true;

            txtVorname.Clear();
            txtNachname.Clear();

            AktualisiereAnzeige();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgPatienten.SelectedItem is Patient patient)
            {
                patienten.Remove(patient);
                var monitor = monitore.FirstOrDefault(m => m.Moid == patient.MonitorId);
                if (monitor != null)
                    monitor.IstBelegt = false;
                AktualisiereAnzeige();
            }
            else
            {
                MessageBox.Show("Bitte einen Patienten auswählen!");
            }
        }

        private void AktualisiereAnzeige()
        {
            dgPatienten.ItemsSource = null;
            dgPatienten.ItemsSource = patienten;

            // Monitore mit Status auflisten
            var monitorAnzeige = monitore.Select(m => new
            {
                m.Moid,
                m.Modell,
                Status = m.IstBelegt ? "🔴 Belegt" : "🟢 Frei",
                PatientName = patienten.FirstOrDefault(p => p.MonitorId == m.Moid) is Patient patient
                    ? $"{patient.Vorname} {patient.Nachname}"
                    : "-"
            }).ToList();

            dgMonitore.ItemsSource = null;
            dgMonitore.ItemsSource = monitorAnzeige;

            int freie = monitore.Count(m => !m.IstBelegt);
            Title = $"Administration – Patienten: {patienten.Count}, Freie Monitore: {freie}";
        }
    }
}
