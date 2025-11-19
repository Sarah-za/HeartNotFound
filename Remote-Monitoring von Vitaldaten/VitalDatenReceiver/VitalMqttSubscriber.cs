using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;



namespace VitalDatenReceiver
{
    internal class VitalMqttSubscriber
    {
        private readonly string broker = "mqtt.inftech.hs-mannheim.de";
        private readonly int port = 8883;
        private string clientId = Guid.NewGuid().ToString();
        private readonly string username = "25pms02";
        private readonly string password = "cf0fc303";

        private IMqttClient myClient;
        private MqttFactory myFactory;

        private string stationId = "";
        private List<(DateTime Time, string station, string parameter, string value)> dataList = new List<(DateTime, string, string, string)>();

        public VitalMqttSubscriber()
        {
            myFactory = new MqttFactory();
            myClient = myFactory.CreateMqttClient();
        }

        public async Task Connect()
        {
            var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(broker, port)
                    .WithTlsOptions(
                        o => o.WithCertificateValidationHandler(_ => true))
                    .WithCredentials(username, password)
                    .Build();

            var connetctResult = myClient.ConnectAsync(options).GetAwaiter().GetResult();

            if (connetctResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                Console.WriteLine("Erfolgreich verbunden mit: " + broker);
                myClient.ApplicationMessageReceivedAsync += receivedMessage;

                await myClient.SubscribeAsync("25pms02/+/heartrate");
                await myClient.SubscribeAsync("25pms02/+/temperature");
                await myClient.SubscribeAsync("25pms02/+/bloodpressure");
                await myClient.SubscribeAsync("25pms02/+/resprate");
                await myClient.SubscribeAsync("25pms02/+/spo2");

                Console.WriteLine("Abonniert: 25pms02/<StationID>/(alleVitalparameter)\n");
            }
            else
            {
                Console.WriteLine($"Verbindung fehlgeschlagen: {connetctResult.ResultCode}");
            }
        }

        private async Task receivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            var parts = topic.Split('/');
            if (parts.Length >= 3)
            {
                stationId = parts[1];
                string station = parts[1];
                string parameter = parts[2];
                dataList.Add((DateTime.Now, station, parameter, payload));
                Console.WriteLine($"[{DateTime.Now:T}] Station {station,-6} {parameter,-12}: {payload}");
            }

            await Task.CompletedTask;

        }

        public async Task disconnect()
        {
            var disconnectoptions = new MqttClientDisconnectOptionsBuilder()
                                    .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                                    .Build();
            await myClient.DisconnectAsync(disconnectoptions);
            Console.WriteLine("Disconnected!");
        }

        public void printSummary()
        {
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Empfangene Vitaldaten");
            Console.WriteLine("--------------------------------");

            if (string.IsNullOrEmpty(stationId))
            {
                Console.WriteLine("Keine Station-ID erkannt.");
                return;
            }

            Console.WriteLine("Station-ID: " + stationId + "\n");

            if (dataList.Count == 0)
            {
                Console.WriteLine("Keine Daten empfangen.");
                return;
            }

            foreach (var entry in dataList)
            {
                Console.WriteLine($"{entry.Time:T} {entry.parameter,-12} {entry.value,6}");
            }

            Console.WriteLine("--------------------------------");
            Console.WriteLine($"Gesamt: {dataList.Count} Datensätze gespeichert.\n");
        }
    }
}
