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
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace VitalDatenSimulator
{
    public partial class InputDialog : Window
    {
        public string EnteredID { get; private set; } = string.Empty;

        public string Broker { get; private set; } = string.Empty;
        public int Port { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;

        private static string ConfigPath =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mqtt_config.txt");

        public InputDialog()
        {
            InitializeComponent();

             /// Automatisches Ausfüllen mqttt_config.txt für testen, damit es schneller geht
            if (TryReadConfig(ConfigPath, out var cfg))
            {
                BrokerBox.Text = cfg.broker;
                PortBox.Text = cfg.port.ToString();
                UsernameBox.Text = cfg.username;
                PasswordBox.Password = cfg.password;
            }
            
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var stationId = StationIdBox.Text.Trim();
            var broker = BrokerBox.Text.Trim();
            var portText = PortBox.Text.Trim();
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(stationId) ||
                string.IsNullOrWhiteSpace(broker) ||
                string.IsNullOrWhiteSpace(portText) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Bitte alle Felder ausfüllen. Das Programm startet nicht, solange ein Feld leer ist.",
                    "Fehlende Eingabe",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show(
                    "Port muss eine gültige Zahl zwischen 1 und 65535 sein.",
                    "Ungültiger Port",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Werte übernehmen
            EnteredID = stationId;
            Broker = broker;
            Port = port;
            Username = username;
            Password = password;


            try
            {
                var fileContent =
                    $@"broker = ""{Broker}"";
                    port = {Port};
                    username = ""{Username}"";
                    password = ""{Password}"";";
                File.WriteAllText(ConfigPath, fileContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Konnte mqtt_config.txt nicht schreiben:\n" + ex.Message,
                    "Dateifehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            EnteredID = string.Empty;
            DialogResult = false;
            Close();
        }

        private void ShowConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(ConfigPath))
            {
                MessageBox.Show(
                    $"Datei nicht gefunden:\n{ConfigPath}",
                    "mqtt_config.txt fehlt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!TryReadConfig(ConfigPath, out var cfg))
            {
                MessageBox.Show(
                    "mqtt_config.txt konnte nicht gelesen werden (Format prüfen).",
                    "Lesefehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(
                $"broker: {cfg.broker}\nport: {cfg.port}\nusername: {cfg.username}\npassword: {cfg.password}",
                "Inhalt von mqtt_config.txt",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static bool TryReadConfig(string path, out (string broker, int port, string username, string password) cfg)
        {
            cfg = (string.Empty, 0, string.Empty, string.Empty);

            try
            {
                var txt = File.ReadAllText(path);

                string GetString(string key)
                {
                    var m = Regex.Match(txt, $@"\b{key}\s*=\s*""([^""]*)""\s*;", RegexOptions.IgnoreCase);
                    return m.Success ? m.Groups[1].Value : string.Empty;
                }

                int GetInt(string key)
                {
                    var m = Regex.Match(txt, $@"\b{key}\s*=\s*(\d+)\s*;", RegexOptions.IgnoreCase);
                    return (m.Success && int.TryParse(m.Groups[1].Value, out int v)) ? v : 0;
                }

                var broker = GetString("broker");
                var port = GetInt("port");
                var username = GetString("username");
                var password = GetString("password");

                if (string.IsNullOrWhiteSpace(broker) ||
                    port <= 0 ||
                    string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                    return false;

                cfg = (broker, port, username, password);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
