using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VitalDatenSimulator
{
    internal class VitalMqttPublisher
    {
        private string broker = "mqtt.inftech.hs-mannheim.de";
        private int port = 8883;
        private string username = "25pms02";
        private string password = "cf0fc303";
        private readonly string clientId = Guid.NewGuid().ToString();

        private IMqttClient myClient;
        private MqttFactory myFactory;
        private bool isConnected = false;

        public bool IsConnected { get { return isConnected; } }

        private static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mqtt_config.txt");

        public VitalMqttPublisher()
        {
            // Falls mqtt_config.txt existiert, überschreibt es die Defaults
            TryLoadConfigFromFile(ConfigPath);

            myFactory = new MqttFactory();
            myClient = myFactory.CreateMqttClient();
        }

        // Optional: Falls du später direkt Werte übergeben willst
        public VitalMqttPublisher(string broker, int port, string username, string password) : this()
        {
            if (!string.IsNullOrWhiteSpace(broker)) this.broker = broker.Trim();
            if (port > 0 && port <= 65535) this.port = port;
            if (!string.IsNullOrWhiteSpace(username)) this.username = username.Trim();
            if (!string.IsNullOrWhiteSpace(password)) this.password = password;
        }

        public void Connect()
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(broker, port)
                    .WithClientId(clientId)
                    .WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true))
                    .WithCredentials(username, password)
                    .Build();

                var result = myClient.ConnectAsync(options).GetAwaiter().GetResult();

                if (myClient.IsConnected)
                {
                    isConnected = true;
                    Console.WriteLine("MQTT connected to " + broker);
                }
                else
                {
                    Console.WriteLine("MQTT connection failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MQTT connect error: " + ex.Message);
            }
        }

        public void Disconnect()
        {
            try
            {
                if (myClient != null && myClient.IsConnected)
                {
                    myClient.DisconnectAsync().GetAwaiter().GetResult();
                    isConnected = false;
                    Console.WriteLine("MQTT disconnected");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MQTT disconnect error: " + ex.Message);
            }
        }

        public void PublishVitalData(string stationId, double heartRate, double temperature, double bloodPressure, double respRate, double spo2)
        {
            if (!isConnected || string.IsNullOrWhiteSpace(stationId))
                return;

            // Topic-Prefix dynamisch aus username (vorher fest "25pms02")
            Publish($"{username}/{stationId}/heartrate", heartRate.ToString("F1"));
            Publish($"{username}/{stationId}/temperature", temperature.ToString("F1"));
            Publish($"{username}/{stationId}/bloodpressure", bloodPressure.ToString("F1"));
            Publish($"{username}/{stationId}/resprate", respRate.ToString("F1"));
            Publish($"{username}/{stationId}/spo2", spo2.ToString("F1"));
            Console.WriteLine();
        }

        private void Publish(string topic, string message)
        {
            try
            {
                var payload = Encoding.UTF8.GetBytes(message);

                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag()
                    .Build();

                myClient.PublishAsync(mqttMessage).GetAwaiter().GetResult();

                Console.WriteLine($"Published: {topic} = {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("MQTT publish error: " + ex.Message);
            }
        }

        private bool TryLoadConfigFromFile(string path)
        {
            if (!File.Exists(path))
                return false;

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

                var b = GetString("broker");
                var p = GetInt("port");
                var u = GetString("username");
                var pw = GetString("password");

                // Nur überschreiben, wenn valide
                if (!string.IsNullOrWhiteSpace(b)) broker = b.Trim();
                if (p > 0 && p <= 65535) port = p;
                if (!string.IsNullOrWhiteSpace(u)) username = u.Trim();
                if (!string.IsNullOrWhiteSpace(pw)) password = pw;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}