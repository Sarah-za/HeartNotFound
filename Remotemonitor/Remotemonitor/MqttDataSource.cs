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
        private readonly Dictionary<string, HashSet<int>> _occupiedBedsByRoom = new();

        // StationID → Buffer
        private readonly Dictionary<string, PartialVital> _buffer = new();

        // PatientID → Thresholds
        private readonly Dictionary<string, Threshold> _savedThresholds = new();

        // PatientID → VitalSample
        private readonly Dictionary<string, VitalSample> _stationSamples = new();

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
                foreach (var p in _stationSamples.Values)
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
                // Default: falls keine DB-Zuordnung existiert
                string patientIdFromDbOrFallback = $"P-{_nextPatientNr:0000}";

                // Monitor-ID (moid) aus stationId ableiten
                if (!int.TryParse(stationId, out int moid))
                {
                    // Fallback, falls stationId keine Zahl ist
                    _patientCache[stationId] = ("Unbekannt", "Patient");
                    _demo[patientIdFromDbOrFallback] = ("?", 0);
                }
                else
                {
                    // Patientendaten aus der DB laden (inkl. pid)
                    var dbPatient = _repo.GetPatientByMonitorId(moid);

                    if (dbPatient.HasValue)
                    {
                        // PatientId aus DB: int pid -> in Format "P-0001"
                        patientIdFromDbOrFallback = $"P-{dbPatient.Value.Pid:0000}";

                        // Name 
                        _patientCache[stationId] =
                            (dbPatient.Value.FirstName, dbPatient.Value.LastName);

                        // Alter & Geschlecht 
                        _demo[patientIdFromDbOrFallback] =
                            (dbPatient.Value.Gender, dbPatient.Value.Age);
                    }
                    else
                    {
                        _patientCache[stationId] = ("Unbekannt", "Patient");
                        _demo[patientIdFromDbOrFallback] = ("?", 0);
                    }
                }


                if (!_demo.ContainsKey(patientIdFromDbOrFallback) ||
                    (patientIdFromDbOrFallback.StartsWith("P-") && patientIdFromDbOrFallback == $"P-{_nextPatientNr:0000}"))
                {
                    
                    _nextPatientNr++;
                }

                // Mapping setzen
                _stationToPatient[stationId] = patientIdFromDbOrFallback;

                // Zimmer/Bett vergeben (pro PatientId)
                AssignUniqueRoomBed(patientIdFromDbOrFallback);

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

            // ✅ 1Hz: Jetzt prüfen, ob an dieser Station noch derselbe Patient hängt
            string currentPatientIdFromDb = EnsureCurrentPatientForStation(stationId);

            if (!_stationToPatient.TryGetValue(stationId, out var mappedPatientId))
            {
                mappedPatientId = currentPatientIdFromDb;
                _stationToPatient[stationId] = mappedPatientId;
            }

            bool patientChanged = mappedPatientId != currentPatientIdFromDb;

            if (patientChanged)
            {
                // alten Patienten "abmelden": Bett/Room freigeben (optional aber sinnvoll)
                ReleaseRoomBed(mappedPatientId);

                // neuen Patienten setzen
                _stationToPatient[stationId] = currentPatientIdFromDb;

                // DB-Daten neu laden (Name/Alter/Geschlecht)
                string first = "Unbekannt";
                string last = "Patient";
                string gender = "?";
                int age = 0;

                if (int.TryParse(stationId, out int moid2))
                {
                    var dbP = _repo.GetPatientByMonitorId(moid2);
                    if (dbP.HasValue)
                    {
                        first = dbP.Value.FirstName;
                        last = dbP.Value.LastName;
                        gender = dbP.Value.Gender;
                        age = dbP.Value.Age;
                    }
                }

                // Room/Bed für neuen Patienten
                if (!_room.ContainsKey(currentPatientIdFromDb))
                    AssignUniqueRoomBed(currentPatientIdFromDb);

                // Thresholds für neuen Patienten
                var limits = _savedThresholds.ContainsKey(currentPatientIdFromDb)
                    ? _savedThresholds[currentPatientIdFromDb]
                    : new Threshold();

                // Wenn es schon ein Sample-Objekt für die Station gibt: RESET (HistoryWindow wird dadurch "neu")
                if (_stationSamples.TryGetValue(stationId, out var existing))
                {
                    existing.ResetForNewPatient(
                        currentPatientIdFromDb,
                        first, last,
                        gender, age,
                        _room[currentPatientIdFromDb],
                        _bed[currentPatientIdFromDb],
                        limits);

                    _savedThresholds[currentPatientIdFromDb] = existing.Limits;
                }

                // wenn es noch keins gibt, wird es unten normal erzeugt
            }


            // 🔹 VitalSample JETZT erzeugen
            patientId = _stationToPatient[stationId];

            if (!_stationSamples.ContainsKey(stationId))
            {
                // Name/Demo aus Cache oder DB (du kannst hier deinen bisherigen Cache-Code verwenden)
                string firstName = "Unbekannt";
                string lastName = "Patient";
                string gender2 = "?";
                int age2 = 0;

                if (int.TryParse(stationId, out int moid))
                {
                    var dbPatient = _repo.GetPatientByMonitorId(moid);
                    if (dbPatient.HasValue)
                    {
                        firstName = dbPatient.Value.FirstName;
                        lastName = dbPatient.Value.LastName;
                        gender2 = dbPatient.Value.Gender;
                        age2 = dbPatient.Value.Age;
                    }
                }

                if (!_room.ContainsKey(patientId))
                    AssignUniqueRoomBed(patientId);

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

                _stationSamples[stationId] = sample;
                _savedThresholds[patientId] = sample.Limits;
            }

            var vitals = _stationSamples[stationId];

            // 🔹 Vitalwerte übernehmen (double → int sauber runden)
            vitals.Ts = b.Ts!.Value;

            vitals.Hr = (int)Math.Round(b.Hr!.Value);
            vitals.Sys = (int)Math.Round(b.Sys!.Value);
            vitals.Dia = Math.Max(0, vitals.Sys - 50);
            vitals.Rr = (int)Math.Round(b.Rr!.Value);
            vitals.Spo2 = (int)Math.Round(b.Spo2!.Value);

            // Temperatur bleibt double
            vitals.Temp = b.Temp!.Value;

            vitals.StalePulse = 0;

            // Buffer für nächste Sekunde zurücksetzen
            _buffer[stationId] = new PartialVital();

            // UI informieren
            OnSample?.Invoke(vitals);

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (client != null && client.IsConnected)
                await client.DisconnectAsync();
        }

        private void AssignUniqueRoomBed(string patientId)
        {
            string[] rooms = { "101", "102", "103", "104" };
            const int bedsPerRoom = 4;

            var roomOrder = rooms.OrderBy(_ => _rng.Next()).ToArray();

            foreach (var room in roomOrder)
            {
                if (!_occupiedBedsByRoom.TryGetValue(room, out var occ))
                {
                    occ = new HashSet<int>();
                    _occupiedBedsByRoom[room] = occ;
                }

                // freie Betten in diesem Room sammeln
                var freeBeds = Enumerable.Range(1, bedsPerRoom).Where(b => !occ.Contains(b)).ToList();
                if (freeBeds.Count == 0)
                    continue;

                // zufällig eines der freien Betten wählen
                int bed = freeBeds[_rng.Next(freeBeds.Count)];

                // speichern
                _room[patientId] = room;
                _bed[patientId] = bed;

                // als belegt markieren
                occ.Add(bed);

                return;
            }

            _room[patientId] = "FULL";
            _bed[patientId] = 0;
        }

        private void ReleaseRoomBed(string patientId)
        {
            if (_room.TryGetValue(patientId, out var room) && _bed.TryGetValue(patientId, out var bed))
            {
                if (_occupiedBedsByRoom.TryGetValue(room, out var occ))
                    occ.Remove(bed);
            }

            _room.Remove(patientId);
            _bed.Remove(patientId);
        }

        private string EnsureCurrentPatientForStation(string stationId)
        {
            // Fallback
            string fallbackPid = $"P-{_nextPatientNr:0000}";

            if (!int.TryParse(stationId, out int moid))
                return fallbackPid;

            var dbPatient = _repo.GetPatientByMonitorId(moid);
            if (!dbPatient.HasValue)
                return fallbackPid;

            return $"P-{dbPatient.Value.Pid:0000}";
        }

    }
}
