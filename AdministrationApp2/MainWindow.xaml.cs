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

namespace AdministrationApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 🔗 Verbindung zur PostgreSQL-Datenbank
        private readonly string connString;


        public MainWindow()
        {
            InitializeComponent();

            var server = (string)(Application.Current.Properties["DB_SERVER"] ?? "");
            var db = (string)(Application.Current.Properties["DB_NAME"] ?? "");
            var user = (string)(Application.Current.Properties["DB_USER"] ?? "");
            var pass = (string)(Application.Current.Properties["DB_PASS"] ?? "");

            connString = $"Host={server};Username={user};Password={pass};Database={db};SslMode=Require;Trust Server Certificate=true;";

            AktualisiereAnzeige();
        }


        // 🔄 Lädt Patienten und Monitore aus der DB
        private void AktualisiereAnzeige()
        {
            var conn = new NpgsqlConnection(connString);
            conn.Open();


            // --- Patienten laden ---
            var patientenListe = new List<Patient>();
            using (var cmdP = new NpgsqlCommand("SELECT p.pid, p.vorname, p.name, p.alter, p.geschlecht \r\n,       COALESCE(m.model, '-') AS monitor\r\nFROM patients p\r\nLEFT JOIN belegung b ON p.pid = b.pid\r\nLEFT JOIN monitors m ON b.moid = m.moid\r\nORDER BY p.pid\r\n", conn))
            using (var drP = cmdP.ExecuteReader())
            {
                while (drP.Read())
                {
                    patientenListe.Add(new Patient
                    {
                        Id = drP.GetInt32(0),
                        Vorname = drP.GetString(1),
                        Nachname = drP.GetString(2),
                        Alter = drP.GetInt32(3),
                        Geschlecht = drP.GetString(4),
                        MonitorName = drP.GetString(5)

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
                        Status = drM.GetBoolean(3) ? "Belegt" : "Frei"
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
            string alterText = txtAlter.Text.Trim();
            string geschlecht = (cbGeschlecht.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname))
            {
                MessageBox.Show("Bitte Vorname und Nachname eingeben!");
                return;
            }
            if (!int.TryParse(alterText, out int alter) || alter < 0 || alter > 120)
            {
                MessageBox.Show("Bitte ein gültiges Alter (0–120) eingeben!");
                return;
            }
            if (string.IsNullOrEmpty(geschlecht))
            {
                MessageBox.Show("Bitte Geschlecht auswählen!");
                return;
            }
            Monitor monitor = cbFreieMonitore.SelectedItem as Monitor;
            /*if (monitor == null)
            {
                MessageBox.Show("Bitte einen freien Monitor auswählen!");
                return;
            }*/

            var conn = new NpgsqlConnection(connString);
            conn.Open();

            // Patient in DB speichern
            var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO patients (pid, vorname, name, alter, geschlecht)
            VALUES ((SELECT COALESCE(MAX(pid),0)+1 FROM patients), @v, @n, @a, @g)
            RETURNING pid;", conn);
            cmdInsert.Parameters.AddWithValue("@v", vorname);
            cmdInsert.Parameters.AddWithValue("@n", nachname);
            cmdInsert.Parameters.AddWithValue("@a", alter);
            cmdInsert.Parameters.AddWithValue("@g", geschlecht);
            int newPid = (int)cmdInsert.ExecuteScalar();

            // Monitor zuweisen
            if (monitor != null)
            {
                var cmdBel = new NpgsqlCommand("INSERT INTO belegung (moid, pid) VALUES (@m, @p)", conn);
                cmdBel.Parameters.AddWithValue("@m", monitor.Moid);
                cmdBel.Parameters.AddWithValue("@p", newPid);
                cmdBel.ExecuteNonQuery();
            }

            txtVorname.Clear();
            txtNachname.Clear();
            txtAlter.Clear();
            AktualisiereAnzeige();
        }
        private void TxtAlter_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // erlaubt nur Zahlen
            e.Handled = !int.TryParse(e.Text, out _);
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
            var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO monitors (moid, model)
            VALUES ((SELECT COALESCE(MAX(moid),0)+1 FROM monitors), @m)
            RETURNING moid;", conn);

            cmdInsert.Parameters.AddWithValue("@m", modell);

            cmdInsert.ExecuteNonQuery();

            txtModell.Clear();
            AktualisiereAnzeige();
        }
        // 🗑 MONITOR LÖSCHEN
        private void BtnDeleteMonitor_Click(object sender, RoutedEventArgs e)
        {
            // 1) Ausgewählten Monitor holen
            Monitor m = dgMonitore.SelectedItem as Monitor;
            if (m == null)
            {
                MessageBox.Show("Bitte einen Monitor auswählen!");
                return;
            }

            // **⚠️ Der Monitor wird gelöscht – Patient muss vorher zugewiesen werden, wenn gewollt**

            var conn = new NpgsqlConnection(connString);
            conn.Open();

            // 2) Falls Monitor einem Patienten zugeordnet war → Belegung entfernen
            using (var cmdBel = new NpgsqlCommand("DELETE FROM belegung WHERE moid=@m", conn))
            {
                cmdBel.Parameters.AddWithValue("@m", m.Moid);
                cmdBel.ExecuteNonQuery();
            }

            // 3) Monitor selbst löschen
            using (var cmdMon = new NpgsqlCommand("DELETE FROM monitors WHERE moid=@m", conn))
            {
                cmdMon.Parameters.AddWithValue("@m", m.Moid);
                cmdMon.ExecuteNonQuery();
            }

            MessageBox.Show("Monitor wurde gelöscht.");

            // 4) UI aktualisieren
            AktualisiereAnzeige();
        }


        private void BtnAssignMonitor_Click(object sender, RoutedEventArgs e)
        {
            Patient p = dgPatienten.SelectedItem as Patient;
            Monitor m = cbFreieMonitore.SelectedItem as Monitor;

            if (p == null)
            {
                MessageBox.Show("Bitte einen Patienten auswählen!");
                return;
            }

            if (m == null)
            {
                MessageBox.Show("Bitte einen freien Monitor auswählen!");
                return;
            }

            var conn = new NpgsqlConnection(connString);
            conn.Open();

            // Alte Zuordnung entfernen
            var cmdDelete = new NpgsqlCommand("DELETE FROM belegung WHERE pid=@p", conn);
            cmdDelete.Parameters.AddWithValue("@p", p.Id);
            cmdDelete.ExecuteNonQuery();

            // Neue Zuordnung speichern
            var cmdInsert = new NpgsqlCommand("INSERT INTO belegung (moid, pid) VALUES (@m, @p)", conn);
            cmdInsert.Parameters.AddWithValue("@m", m.Moid);
            cmdInsert.Parameters.AddWithValue("@p", p.Id);
            cmdInsert.ExecuteNonQuery();

            MessageBox.Show("Monitor wurde neu zugewiesen.");

            AktualisiereAnzeige();
        }

    }

}
