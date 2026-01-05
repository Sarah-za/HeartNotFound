using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;

namespace Remotemonitor
{
    public class MqttDataSource : IDataSource
    {
        private readonly string broker;
        private readonly int port;
        private readonly string username;
        private readonly string password;

        private readonly IMqttClient client;
        private readonly MqttFactory factory = new();

        public event Action<VitalSample>? OnSample;

        // Nur Daten akzeptieren, die nach Start eintreffen
        private readonly DateTime _programStart = DateTime.UtcNow;

        // StationID → PatientID
        private readonly Dictionary<string, string> _stationToPatient = new();

        // PatientID → demographic data
        private readonly Dictionary<string, (string Gender, int Age)> _demo = new();

        // PatientID → room / bed
        private readonly Dictionary<string, string> _room = new();
        private readonly Dictionary<string, int> _bed = new();

        // StationID → Buffer
        private readonly Dictionary<string, PartialVital> _buffer = new();

        // PatientID → Thresholds
        private readonly Dictionary<string, Threshold> _savedThresholds = new();

        // PatientID → VitalSample
        private readonly Dictionary<string, VitalSample> _patients = new();

        // StationID → Name aus DB (Cache!)
        private readonly Dictionary<string, (string First, string Last)> _patientCache = new();

        private int _nextPatientNr = 1;

        private readonly PatientRepository _repo = new();
        private readonly Random _rng = new();

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

            // aus App.Properties (gesetzt im ConfigDialog)
            broker = (string)(Application.Current.Properties["MQTT_BROKER"] ?? "");
            port = (int)(Application.Current.Properties["MQTT_PORT"] ?? 0);
            username = (string)(Application.Current.Properties["MQTT_USER"] ?? "");
            password = (string)(Application.Current.Properties["MQTT_PASS"] ?? "");
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(broker, port)
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

            await client.SubscribeAsync($"{username}/+/heartrate");
            await client.SubscribeAsync($"{username}/+/temperature");
            await client.SubscribeAsync($"{username}/+/bloodpressure");
            await client.SubscribeAsync($"{username}/+/resprate");
            await client.SubscribeAsync($"{username}/+/spo2");

            Console.WriteLine("[MQTT] Subscribed.");

            // 🔹 1-Hz Timer für Historie (läuft IMMER)
            var historyTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            historyTimer.Tick += (_, __) =>
            {
                foreach (var p in _patients.Values)
                {
                    p.StalePulse++;
                    p.AddSnapshot();
                }
            };

            historyTimer.Start();


        }

        private Task OnMqttMessage(MqttApplicationMessageReceivedEventArgs e)
        {

            if (e.ApplicationMessage.Retain)
                return Task.CompletedTask;

            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            Console.WriteLine($"[MQTT] Message received: Topic='{e.ApplicationMessage.Topic}', Payload='{payload}'");

            var parts = topic.Split('/');
            if (parts.Length < 3)
                return Task.CompletedTask;

            string stationId = parts[1];
            string parameter = parts[2];

            // 🔹 Station → PatientID
            // Patient-ID für Station vergeben (noch ohne Objekt-Erzeugung)
            if (!_stationToPatient.ContainsKey(stationId))
            {
                // Neue interne Patient-ID erzeugen
                string pid = $"P-{_nextPatientNr:0000}";
                _nextPatientNr++;

                _stationToPatient[stationId] = pid;

                // Monitor-ID (moid) aus stationId ableiten
                // (stationId kommt bei dir als String, DB erwartet int moid)
                if (!int.TryParse(stationId, out int moid))
                {
                    // Fallback, falls stationId keine Zahl ist
                    _patientCache[stationId] = ("Unbekannt", "Patient");
                    _demo[pid] = ("?", 0);
                }
                else
                {
                    // Patientendaten aus der DB laden
                    var dbPatient = _repo.GetPatientByMonitorId(moid);

                    if (dbPatient.HasValue)
                    {
                        // Name cachen
                        _patientCache[stationId] =
                            (dbPatient.Value.FirstName, dbPatient.Value.LastName);

                        // Alter & Geschlecht cachen
                        _demo[pid] =
                            (dbPatient.Value.Gender, dbPatient.Value.Age);
                    }
                    else
                    {
                        // Kein Patient zugeordnet
                        _patientCache[stationId] = ("Unbekannt", "Patient");
                        _demo[pid] = ("?", 0);
                    }
                }

                // Zimmer / Bett (wie bisher)
                string[] rooms = { "101", "102", "103", "104" };
                _room[pid] = rooms[_rng.Next(rooms.Length)];
                _bed[pid] = _rng.Next(1, 5);
            }

            string patientId = _stationToPatient[stationId];

            if (!_buffer.ContainsKey(stationId))
                _buffer[stationId] = new PartialVital();

            var b = _buffer[stationId];

            if (!b.Ts.HasValue)
            {
                var now = DateTime.UtcNow;
                if (now < _programStart)
                    return Task.CompletedTask;

                b.Ts = now;
            }

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

            if (!b.Complete)
                return Task.CompletedTask;

            // 🔹 VitalSample JETZT erzeugen
            if (!_patients.ContainsKey(patientId))
            {
                var (gender2, age2) = _demo[patientId];

                var (firstName, lastName) = _patientCache.TryGetValue(stationId, out var n)
                    ? n
                    : ("Unbekannt", "Patient");

                var sample = new VitalSample
                {
                    PatientId = patientId,
                    MonitorId = stationId,

                    FirstName = firstName,
                    LastName = lastName,

                    Gender = gender2,
                    Age = age2,

                    Room = _room[patientId],
                    Bed = _bed[patientId],
                    Ts = b.Ts.Value,
                    Limits = _savedThresholds.ContainsKey(patientId)
                        ? _savedThresholds[patientId]
                        : new Threshold()
                };

                _patients[patientId] = sample;
                _savedThresholds[patientId] = sample.Limits;

                Console.WriteLine($"[MQTT] Patient object created: {patientId} ({sample.FirstName} {sample.LastName}) for Monitor {stationId}");
            }

            var vitals = _patients[patientId];
            vitals.Ts = b.Ts.Value;
            vitals.Hr = (int)b.Hr.Value;
            vitals.Spo2 = (int)b.Spo2.Value;
            vitals.Rr = (int)b.Rr.Value;
            vitals.Temp = b.Temp.Value;
            vitals.Sys = (int)b.Sys.Value;
            vitals.Dia = vitals.Sys - 50;

            vitals.RecalculateEws();

            OnSample?.Invoke(vitals);

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
