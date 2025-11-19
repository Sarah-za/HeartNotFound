using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace VitalDatenSim
{
    internal class VitalMqttPublisher
    {
        private string broker = "mqtt.inftech.hs-mannheim.de";
        private readonly int port = 8883;
        private readonly string username = "25pms02";
        private readonly string password = "cf0fc303";
        private readonly string clientId = Guid.NewGuid().ToString();

        private IMqttClient myClient;
        private MqttFactory myFactory;
        private bool isConnected = false;

        public bool IsConnected { get { return isConnected; } }

        public VitalMqttPublisher()
        {
            myFactory = new MqttFactory();
            myClient = myFactory.CreateMqttClient();
        }

        public void Connect()
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(broker, port)
                    .WithTlsOptions(
                        o => o.WithCertificateValidationHandler(_ => true))
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

            Publish($"25pms02/{stationId}/heartrate", heartRate.ToString("F1"));
            Publish($"25pms02/{stationId}/temperature", temperature.ToString("F1"));
            Publish($"25pms02/{stationId}/bloodpressure", bloodPressure.ToString("F1"));
            Publish($"25pms02/{stationId}/resprate", respRate.ToString("F1"));
            Publish($"25pms02/{stationId}/spo2", spo2.ToString("F1"));
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
    }
}