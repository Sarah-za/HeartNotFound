using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Remotmonitor.Models;
using Remotmonitor.Services;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;


namespace Remotmonitor.Services
{
    public class MqttDataSource : IDataSource
    {
        private readonly string broker = "mqtt.inftech.hs-mannheim.de";
        private readonly int port = 8883;
        private readonly string username = "25pms02";
        private readonly string password = "cf0fc303";

        private readonly IMqttClient client;
        private readonly MqttFactory factory = new();

        public event Action<VitalSample>? OnSample;

        // Zeitpunkt, ab dem wir Daten akzeptieren
        private readonly DateTime _programStart = DateTime.UtcNow;

        private readonly Random _rng = new();

        // StationID → PatientID
        private readonly Dictionary<string, string> _stationToPatient = new();

        // PatientID → demographic data
        private readonly Dictionary<string, (string Gender, int Age)> _demo = new();

        // PatientID → room / bed
        private readonly Dictionary<string, string> _room = new();
        private readonly Dictionary<string, int> _bed = new();

        private readonly Dictionary<string, PartialVital> _buffer = new();

        private int _nextPatientNr = 1;

        private class PartialVital
        {
            public DateTime? Ts;
            public double? Hr;
            public double? Temp;
            public double? Sys;
            public double? Rr;
            public double? Spo2;

            public bool Complete =>
                Ts.HasValue && Hr.HasValue && Temp.HasValue &&
                Sys.HasValue && Rr.HasValue && Spo2.HasValue;
        }

        public MqttDataSource()
        {
            client = factory.CreateMqttClient();
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(broker, port)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                .WithCredentials(username, password)
                .WithTlsOptions(o =>
                {
                    o.UseTls(true);
                    o.WithCertificateValidationHandler(_ => true);
                })
                .Build();

            var result = await client.ConnectAsync(options, ct);

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                Console.WriteLine("[MQTT] Connect failed: " + result.ResultCode);
                return;
            }

            Console.WriteLine("[MQTT] Connected.");

            client.ApplicationMessageReceivedAsync += OnMqttMessage;

            // Topics abonnieren
            await client.SubscribeAsync("25pms02/+/heartrate");
            await client.SubscribeAsync("25pms02/+/temperature");
            await client.SubscribeAsync("25pms02/+/bloodpressure");
            await client.SubscribeAsync("25pms02/+/resprate");
            await client.SubscribeAsync("25pms02/+/spo2");

            Console.WriteLine("[MQTT] Subscribed.");
        }

        private Task OnMqttMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

           
            // Alle vorherigen Nachrichten ignorieren, sodass keine Patienten mit alten Daten erstellt werden
            
            if (e.ApplicationMessage.Retain)
            {
                Console.WriteLine("[MQTT] Ignoring retained: " + topic);
                return Task.CompletedTask;
            }

            // Topic: 25pms02/<station>/<param>
            var parts = topic.Split('/');
            if (parts.Length < 3) return Task.CompletedTask;

            string stationId = parts[1];
            string parameter = parts[2];


            // Patient nur erzeugen, wenn neue MQTT-Daten kommen

            if (!_stationToPatient.ContainsKey(stationId))
            {
                string pid = $"P-{_nextPatientNr:0000}";
                _nextPatientNr++;

                _stationToPatient[stationId] = pid;

                // Demographie
                string gender = _rng.NextDouble() < 0.5 ? "m" : "w";
                int age = _rng.Next(18, 90);
                _demo[pid] = (gender, age);

                // Zimmer zufällig (Mock-ähnlich)
                string[] rooms = { "101", "102", "103", "104" };
                string room = rooms[_rng.Next(rooms.Length)];
                int bed = _rng.Next(1, 5);

                _room[pid] = room;
                _bed[pid] = bed;

                Console.WriteLine($"[MQTT] New patient created: {pid} from station {stationId}");
            }

            string patientId = _stationToPatient[stationId];

            // Buffer erstellen
            if (!_buffer.ContainsKey(stationId))
                _buffer[stationId] = new PartialVital();

            var b = _buffer[stationId];


            // 3) Timestamp nur akzeptieren, wenn nach Programmstart

            if (!b.Ts.HasValue)
            {
                var now = DateTime.UtcNow;

                if (now < _programStart)
                {
                    Console.WriteLine("[MQTT] Ignoring old message (before program start)");
                    return Task.CompletedTask;
                }

                b.Ts = now;
            }


            // Vitlwerte die von dem Simulator kommen eintragen

            try
            {
                switch (parameter)
                {
                    case "heartrate": b.Hr = double.Parse(payload); break;
                    case "temperature": b.Temp = double.Parse(payload); break;
                    case "bloodpressure": b.Sys = double.Parse(payload); break;
                    case "resprate": b.Rr = double.Parse(payload); break;
                    case "spo2": b.Spo2 = double.Parse(payload); break;
                }
            }
            catch
            {
                return Task.CompletedTask;
            }

            // Noch nicht alle Werte angekommen
            if (!b.Complete) return Task.CompletedTask;


            // Fertigen VitalSample erzeugen, restliche daten werden zufällig erzeugt wie bei Mockdaten

            var (gender2, age2) = _demo[patientId];

            var sample = new VitalSample
            {
                PatientId = patientId,
                MonitorId = stationId,

                Gender = gender2,
                Age = age2,

                Room = _room[patientId],
                Bed = _bed[patientId],

                Ts = b.Ts.Value,

                Hr = (int)b.Hr.Value,
                Spo2 = (int)b.Spo2.Value,
                Rr = (int)b.Rr.Value,
                Temp = b.Temp.Value,
                Sys = (int)b.Sys.Value,
                Dia = (int)(b.Sys.Value - 50),
            };

            // An MainViewModel senden

            OnSample?.Invoke(sample);

            // Buffer zurücksetzen
            _buffer[stationId] = new PartialVital();

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (client != null && client.IsConnected)
                await client.DisconnectAsync();
        }
    }
}
