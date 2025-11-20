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
        private readonly string username = "pms02";
        private readonly string password = "cf0fc303";

        private readonly IMqttClient client;
        private readonly MqttFactory factory = new MqttFactory();

        public event Action<VitalSample>? OnSample;

        //Patienten Mapping wie Mock!

        private readonly string[] _patients =
            Enumerable.Range(1, 16).Select(i => $"P-{i:0000}").ToArray();

        private int _assignedPatients = 0;

        private readonly Dictionary<string, string> _stationToPatient = new();
        private readonly Dictionary<string, (string Gender, int Age)> _demo = new();
        private readonly Dictionary<string, string> _room = new();
        private readonly Dictionary<string, int> _bed = new();

        private readonly Random _rng = new();

        private readonly Dictionary<string, PartialVital> _buffer = new();

        private class PartialVital
        {
            public DateTime? Timestamp;
            public double? Hr;
            public double? Temp;
            public double? BpSys;
            public double? Rr;
            public double? Spo2;

            public bool IsComplete =>
                Hr.HasValue && Temp.HasValue && BpSys.HasValue && Rr.HasValue && Spo2.HasValue;
        }

        public MqttDataSource()
        {
            client = factory.CreateMqttClient();
            InitPatientRooms();
        }

        private void InitPatientRooms()
        {
            string[] rooms =
            {
                "101","101","101","101","101",
                "102","102","102",
                "103","103","103","103",
                "104","104","104","104",
            };

            var bedCounter = new Dictionary<string, int>();

            for (int i = 0; i < _patients.Length; i++)
            {
                string p = _patients[i];

                string gender = _rng.NextDouble() < 0.5 ? "m" : "w";
                int age = _rng.Next(18, 90);
                _demo[p] = (gender, age);

                string room = rooms[i];
                if (!bedCounter.ContainsKey(room)) bedCounter[room] = 0;
                bedCounter[room]++;

                _room[p] = room;
                _bed[p] = bedCounter[room];
            }
        }

        public async Task StartAsync(CancellationToken ct)
        {
            // var options = new MqttClientOptionsBuilder()
            //   .WithTcpServer(broker, port)
            //  .WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true))
            // .WithCredentials(username, password)
            //.Build();

            var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(broker, port)
                    .WithTlsOptions(
                        o => o.WithCertificateValidationHandler(_ => true))
                    .WithCredentials(username, password)
                    .Build();

            try
            {
                var result = await client.ConnectAsync(options, ct);

                if (result.ResultCode != MqttClientConnectResultCode.Success)
                {
                    Console.WriteLine($"[MQTT] Connection failed: {result.ResultCode}");
                    return;
                }

                Console.WriteLine("[MQTT] Connected successfully.");

                client.ApplicationMessageReceivedAsync += OnMqttMessage;

                await client.SubscribeAsync("25pms/+/heartrate");
                await client.SubscribeAsync("25pms/+/temperature");
                await client.SubscribeAsync("25pms/+/bloodpressure");
                await client.SubscribeAsync("25pms/+/resprate");
                await client.SubscribeAsync("25pms/+/spo2");

                Console.WriteLine("[MQTT] Subscribed to vital data topics.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            


        }

        private Task OnMqttMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            var parts = topic.Split('/');
            if (parts.Length < 3) return Task.CompletedTask;

            string station = parts[1];
            string parameter = parts[2];

            if (!_stationToPatient.ContainsKey(station))
            {
                if(_assignedPatients >= _patients.Length)
                    return Task.CompletedTask;

                _stationToPatient[station] = _patients[_assignedPatients++];
                Console.WriteLine($"Sation {station} -> Patient {_stationToPatient[station]}");
            }

            string pid = _stationToPatient[station];

            if (!_buffer.ContainsKey(station))
                _buffer[station] = new PartialVital();

            var p = _buffer[station];

            if (!p.Timestamp.HasValue)
                p.Timestamp = DateTime.UtcNow;

            try
            {
                switch (parameter)
                {
                    case "heartrate": p.Hr = double.Parse(payload); break;
                    case "temperature:": p.Temp = double.Parse(payload); break;
                    case "bloodpressure": p.BpSys = double.Parse(payload); break;
                    case "resprate": p.Rr = double.Parse(payload); break;
                    case "spo2": p.Spo2 = double.Parse(payload); break;
                }
            }

            catch
            {
                Console.WriteLine($"[MQTT] Parse error on payload '{payload}'");
            }

            if (!p.IsComplete)
                return Task.CompletedTask;

            var (gender, age) = _demo[pid];

            
            var sample = new VitalSample
             {
                PatientId = pid,
                MonitorId = $"MON-{station}",

                Gender = gender,
                Age = age,

                Room = _room[pid],
                Bed = _bed[pid],

                Ts = p.Timestamp.Value,

                Hr = (int)p.Hr.Value,
                Temp = p.Temp.Value,
                Sys = (int)p.BpSys.Value,
                Dia = (int)(p.BpSys.Value - 60),
                Rr = (int)p.Rr.Value,
                Spo2 = (int)p.Spo2.Value
             };

             OnSample?.Invoke(sample);

             Console.WriteLine($"[MQTT] Sample emitted for patient {sample.PatientId}");

             // Buffer für diese Station leeren
             _buffer[station] = new PartialVital();

            return Task.CompletedTask;
            
        }

        public async ValueTask DisposeAsync()
        {
            if (client != null && client.IsConnected)
                await client.DisconnectAsync();
        }

    }
}
