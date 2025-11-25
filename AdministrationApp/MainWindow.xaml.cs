using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Npgsql;

namespace AdministrationApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 🔗 Verbindung zur PostgreSQL-Datenbank
        private string connString =
        "Host=db.inftech.hs-mannheim.de;Username=pms_hnf;Password=pms_hnf;Database=pms_hnf;SslMode=Require;Trust Server Certificate=true;";

        public MainWindow()
        {
            InitializeComponent();
            AktualisiereAnzeige();
        }

        // 🔄 Lädt Patienten und Monitore aus der DB
        private void AktualisiereAnzeige()
        {
            var conn = new NpgsqlConnection(connString);
            conn.Open();

            // --- Patienten laden ---
            var patientenListe = new List<Patient>();
            using (var cmdP = new NpgsqlCommand("SELECT pid, vorname, name FROM patients ORDER BY pid", conn))
            using (var drP = cmdP.ExecuteReader())
            {
                while (drP.Read())
                {
                    patientenListe.Add(new Patient
                    {
                        Id = drP.GetInt32(0),
                        Vorname = drP.GetString(1),
                        Nachname = drP.GetString(2)
                    });
                }
            }
            dgPatienten.ItemsSource = patientenListe;

            // --- Monitore laden und Belegung prüfen ---
            var monitorListe = new List<Monitor>();
            using (var cmdM = new NpgsqlCommand(@"
            SELECT m.moid, m.model,
                   COALESCE(p.vorname || ' ' || p.name, '-') AS patient,
                   CASE WHEN p.pid IS NULL THEN false ELSE true END AS istBelegt
            FROM monitors m
            LEFT JOIN belegung b ON m.moid = b.moid
            LEFT JOIN patients p ON b.pid = p.pid
            ORDER BY m.moid;", conn))
            using (var drM = cmdM.ExecuteReader())
            {
                while (drM.Read())
                {
                    monitorListe.Add(new Monitor
                    {
                        Moid = drM.GetInt32(0),
                        Modell = drM.GetString(1),
                        PatientName = drM.GetString(2),
                        IstBelegt = drM.GetBoolean(3),
                        Status = drM.GetBoolean(3) ? "🔴 Belegt" : "🟢 Frei"
                    });
                }
            }
            dgMonitore.ItemsSource = monitorListe;

            // --- ComboBox für freie Monitore ---
            cbFreieMonitore.ItemsSource = monitorListe
                .Where(m => m.IstBelegt == false) // nur freie Monitore
                .ToList();
            cbFreieMonitore.DisplayMemberPath = "Modell";
            cbFreieMonitore.SelectedValuePath = "Moid";
        }

        // ➕ PATIENT HINZUFÜGEN
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string vorname = txtVorname.Text.Trim();
            string nachname = txtNachname.Text.Trim();

            if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname))
            {
                MessageBox.Show("Bitte Vorname und Nachname eingeben!");
                return;
            }

            Monitor monitor = cbFreieMonitore.SelectedItem as Monitor;
            if (monitor == null)
            {
                MessageBox.Show("Bitte einen freien Monitor auswählen!");
                return;
            }

            var conn = new NpgsqlConnection(connString);
            conn.Open();

            // Patient in DB speichern
            var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO patients (pid, vorname, name)
            VALUES ((SELECT COALESCE(MAX(pid),0)+1 FROM patients), @v, @n)
            RETURNING pid;", conn);
            cmdInsert.Parameters.AddWithValue("@v", vorname);
            cmdInsert.Parameters.AddWithValue("@n", nachname);
            int newPid = (int)cmdInsert.ExecuteScalar();

            // Monitor zuweisen
            var cmdBel = new NpgsqlCommand("INSERT INTO belegung (moid, pid) VALUES (@m, @p)", conn);
            cmdBel.Parameters.AddWithValue("@m", monitor.Moid);
            cmdBel.Parameters.AddWithValue("@p", newPid);
            cmdBel.ExecuteNonQuery();

            txtVorname.Clear();
            txtNachname.Clear();
            AktualisiereAnzeige();
        }

        // 🗑 PATIENT LÖSCHEN
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            Patient p = dgPatienten.SelectedItem as Patient;
            if (p == null)
            {
                MessageBox.Show("Bitte einen Patienten auswählen!");
                return;
            }

            var conn = new NpgsqlConnection(connString);
            conn.Open();

            // Belegung löschen
            var cmdBel = new NpgsqlCommand("DELETE FROM belegung WHERE pid=@p", conn);
            cmdBel.Parameters.AddWithValue("@p", p.Id);
            cmdBel.ExecuteNonQuery();

            // Patient löschen
            var cmdPat = new NpgsqlCommand("DELETE FROM patients WHERE pid=@p", conn);
            cmdPat.Parameters.AddWithValue("@p", p.Id);
            cmdPat.ExecuteNonQuery();

            AktualisiereAnzeige();
        }

        // ➕ MONITOR HINZUFÜGEN
        private void BtnAddMonitor_Click(object sender, RoutedEventArgs e)
        {
            string modell = txtModell.Text.Trim();
            if (string.IsNullOrWhiteSpace(modell))
            {
                MessageBox.Show("Bitte ein Monitor-Modell eingeben!");
                return;
            }

            var conn = new NpgsqlConnection(connString);
            conn.Open();
            var cmd = new NpgsqlCommand("INSERT INTO monitors (model) VALUES (@m)", conn);
            cmd.Parameters.AddWithValue("@m", modell);
            cmd.ExecuteNonQuery();

            txtModell.Clear();
            AktualisiereAnzeige();
        }

        // 🗑 MONITOR LÖSCHEN
        private void BtnDeleteMonitor_Click(object sender, RoutedEventArgs e)
        {
            Monitor m = dgMonitore.SelectedItem as Monitor;
            if (m == null)
            {
                MessageBox.Show("Bitte einen Monitor auswählen!");
                return;
            }

            if (m.IstBelegt)
            {
                MessageBox.Show("Dieser Monitor ist belegt und kann nicht gelöscht werden!");
                return;
            }

            var conn = new NpgsqlConnection(connString);
            conn.Open();
            var cmd = new NpgsqlCommand("DELETE FROM monitors WHERE moid=@m", conn);
            cmd.Parameters.AddWithValue("@m", m.Moid);
            cmd.ExecuteNonQuery();

            AktualisiereAnzeige();
        }
    }

}
