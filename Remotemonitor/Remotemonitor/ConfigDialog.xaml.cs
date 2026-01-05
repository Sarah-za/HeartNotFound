using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace Remotemonitor
{
    public partial class ConfigDialog : Window
    {
        // Ergebnisse
        public string MqttBroker { get; private set; } = "";
        public int MqttPort { get; private set; }
        public string MqttUsername { get; private set; } = "";
        public string MqttPassword { get; private set; } = "";

        public string DbServer { get; private set; } = "";
        public string DbName { get; private set; } = "";
        public string DbUser { get; private set; } = "";
        public string DbPassword { get; private set; } = "";

        private static string MqttPath =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mqtt_config.txt");

        private static string DbPath =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dB_config.txt");

        public ConfigDialog()
        {
            InitializeComponent();
            // Start: alles leer
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Wenn nicht OK -> Abbruch
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
            // MQTT
            var broker = MqttBrokerBox.Text.Trim();
            var portTxt = MqttPortBox.Text.Trim();
            var mu = MqttUserBox.Text.Trim();
            var mp = MqttPassBox.Password;

            // DB
            var server = DbServerBox.Text.Trim();
            var db = DbNameBox.Text.Trim();
            var du = DbUserBox.Text.Trim();
            var dp = DbPassBox.Password;

            // Leere Felder -> Fehler + nicht schließen
            if (string.IsNullOrWhiteSpace(broker) ||
                string.IsNullOrWhiteSpace(portTxt) ||
                string.IsNullOrWhiteSpace(mu) ||
                string.IsNullOrWhiteSpace(mp) ||
                string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(db) ||
                string.IsNullOrWhiteSpace(du) ||
                string.IsNullOrWhiteSpace(dp))
            {
                MessageBox.Show(
                    "Bitte alle Felder ausfüllen. Das Programm startet nicht, solange ein Feld leer ist.",
                    "Fehlende Eingabe",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(portTxt, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show(
                    "MQTT Port muss eine gültige Zahl zwischen 1 und 65535 sein.",
                    "Ungültiger Port",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // übernehmen
            MqttBroker = broker;
            MqttPort = port;
            MqttUsername = mu;
            MqttPassword = mp;

            DbServer = server;
            DbName = db;
            DbUser = du;
            DbPassword = dp;

            DialogResult = true;
            Close();
        }

        private void LoadMqtt_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(MqttPath))
            {
                MessageBox.Show($"mqtt_config.txt nicht gefunden:\n{MqttPath}",
                    "Config fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryReadMqtt(MqttPath, out var cfg))
            {
                MessageBox.Show("mqtt_config.txt konnte nicht gelesen werden (Format prüfen).",
                    "Lesefehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MqttBrokerBox.Text = cfg.broker;
            MqttPortBox.Text = cfg.port.ToString();
            MqttUserBox.Text = cfg.username;
            MqttPassBox.Password = cfg.password;
        }

        private void LoadDb_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(DbPath))
            {
                MessageBox.Show($"dB_conifg.txt nicht gefunden:\n{DbPath}",
                    "Config fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryReadDb(DbPath, out var cfg))
            {
                MessageBox.Show("dB_conifg.txt konnte nicht gelesen werden (Format prüfen).",
                    "Lesefehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DbServerBox.Text = cfg.server;
            DbNameBox.Text = cfg.db;
            DbUserBox.Text = cfg.user;
            DbPassBox.Password = cfg.password;
        }

        private static bool TryReadMqtt(string path, out (string broker, int port, string username, string password) cfg)
        {
            cfg = ("", 0, "", "");
            try
            {
                var txt = File.ReadAllText(path);

                string GetString(string key)
                {
                    var m = Regex.Match(txt, $@"\b{key}\s*=\s*""([^""]*)""\s*;", RegexOptions.IgnoreCase);
                    return m.Success ? m.Groups[1].Value : "";
                }

                int GetInt(string key)
                {
                    var m = Regex.Match(txt, $@"\b{key}\s*=\s*(\d+)\s*;", RegexOptions.IgnoreCase);
                    return (m.Success && int.TryParse(m.Groups[1].Value, out int v)) ? v : 0;
                }

                var broker = GetString("broker");
                var port = GetInt("port");
                var user = GetString("username");
                var pass = GetString("password");

                if (string.IsNullOrWhiteSpace(broker) || port <= 0 ||
                    string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                    return false;

                cfg = (broker, port, user, pass);
                return true;
            }
            catch { return false; }
        }

        private static bool TryReadDb(string path, out (string server, string db, string user, string password) cfg)
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
                map.TryGetValue("password", out var password);

                if (string.IsNullOrWhiteSpace(server) ||
                    string.IsNullOrWhiteSpace(db) ||
                    string.IsNullOrWhiteSpace(user) ||
                    string.IsNullOrWhiteSpace(password))
                    return false;

                cfg = (server, db, user, password);
                return true;
            }
            catch { return false; }
        }
    }
}
