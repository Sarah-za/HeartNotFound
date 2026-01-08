using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace AdministrationApp2
{
    public partial class DbConfigWindow : Window
    {
        public string Server { get; private set; } = "";
        public string Database { get; private set; } = "";
        public string User { get; private set; } = "";
        public string Password { get; private set; } = "";

        private static string ConfigPath =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_config.txt");

        public DbConfigWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // X zählt als Abbruch
            if (DialogResult != true)
                DialogResult = false;

            base.OnClosing(e);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var server = ServerBox.Text.Trim();
            var db = DbBox.Text.Trim();
            var user = UserBox.Text.Trim();
            var pass = PassBox.Password;

            if (string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(db) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Bitte alle Felder ausfüllen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Server = server;
            Database = db;
            User = user;
            Password = pass;

            DialogResult = true;
            Close();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(ConfigPath))
            {
                MessageBox.Show(
                    $"db_config.txt nicht gefunden:\n{ConfigPath}\n\nTipp: Datei auf 'Copy if newer' setzen.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!TryReadConfig(ConfigPath, out var cfg))
            {
                MessageBox.Show("db_config.txt konnte nicht gelesen werden (Format prüfen).",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ServerBox.Text = cfg.server;
            DbBox.Text = cfg.db;
            UserBox.Text = cfg.user;
            PassBox.Password = cfg.password;
        }

        private static bool TryReadConfig(string path, out (string server, string db, string user, string password) cfg)
        {
            cfg = ("", "", "", "");
            try
            {
                var lines = File.ReadAllLines(path);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("#")) continue;

                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var k = line.Substring(0, idx).Trim();
                    var v = line.Substring(idx + 1).Trim();
                    map[k] = v;
                }

                map.TryGetValue("server", out var server);
                map.TryGetValue("datenbank", out var db);
                map.TryGetValue("user", out var user);
                map.TryGetValue("password", out var pass);

                if (string.IsNullOrWhiteSpace(server) ||
                    string.IsNullOrWhiteSpace(db) ||
                    string.IsNullOrWhiteSpace(user) ||
                    string.IsNullOrWhiteSpace(pass))
                    return false;

                cfg = (server, db, user, pass);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
