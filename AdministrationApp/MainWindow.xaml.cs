using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System;

namespace AdministrationApp
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            if (status == "Frei")
                return new SolidColorBrush(Colors.Green);
            else 
                return new SolidColorBrush(Colors.Red);
            //else
               // return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class MainWindow : Window
    {
        private List<Monitor> monitore = new List<Monitor>();
        private List<Patient> patienten = new List<Patient>();
        private int naechstePatientId = 1;
        private int naechsteMonitorId = 1;

        public MainWindow()
        {
            InitializeComponent();
            // Beim Start: 0 Patienten, 0 Monitore
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

            var ausgewählterMonitor = cbFreieMonitore.SelectedItem as Monitor;
            if (ausgewählterMonitor == null)
            {
                MessageBox.Show("Bitte einen freien Monitor auswählen!");
                return;
            }

            var patient = new Patient
            {
                Id = naechstePatientId++,
                Vorname = vorname,
                Nachname = nachname,
                MonitorId = ausgewählterMonitor.Moid
            };

            patienten.Add(patient);
            ausgewählterMonitor.IstBelegt = true;

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

        private void BtnAddMonitor_Click(object sender, RoutedEventArgs e)
        {
            string modell = txtModell.Text.Trim();
            if (string.IsNullOrWhiteSpace(modell))
            {
                MessageBox.Show("Bitte einen Modellnamen eingeben!");
                return;
            }

            var monitor = new Monitor
            {
                Moid = naechsteMonitorId++,
                Modell = modell,
                IstBelegt = false
            };

            monitore.Add(monitor);
            txtModell.Clear();
            AktualisiereAnzeige();
        }

        private void BtnDeleteMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (dgMonitore.SelectedItem is MonitorView selectedView)
            {
                var monitor = monitore.FirstOrDefault(m => m.Moid == selectedView.MonitorId);
                if (monitor != null)
                {
                    if (monitor.IstBelegt)
                    {
                        MessageBox.Show("Monitor ist belegt und kann nicht gelöscht werden!");
                        return;
                    }
                    monitore.Remove(monitor);
                    AktualisiereAnzeige();
                }
            }
            else
            {
                MessageBox.Show("Bitte einen Monitor auswählen!");
            }
        }

        private void AktualisiereAnzeige()
        {
            dgPatienten.ItemsSource = null;
            dgPatienten.ItemsSource = patienten;

            var monitorAnzeige = monitore.Select(m => new MonitorView
            {
                MonitorId = m.Moid,
                Modell = m.Modell,
                Status = m.IstBelegt ? " Belegt" : "Frei",
                PatientName = patienten.FirstOrDefault(patient =>patient.MonitorId == m.Moid) is Patient p
                    ? $"{p.Vorname} {p.Nachname}"
                    : "-"
            }).ToList();

            dgMonitore.ItemsSource = null;
            dgMonitore.ItemsSource = monitorAnzeige;

            cbFreieMonitore.ItemsSource = monitore.Where(m => !m.IstBelegt).ToList();
            cbFreieMonitore.DisplayMemberPath = "Modell";
            cbFreieMonitore.SelectedValuePath = "MonitorId";
            if (cbFreieMonitore.Items.Count > 0)
                cbFreieMonitore.SelectedIndex = 0;

            int freie = monitore.Count(m => !m.IstBelegt);
            Title = $"Administration – Patienten: {patienten.Count}, Freie Monitore: {freie}";
        }
    }

   
}
