using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;

public class MQTTclient
{
    private string broker = "mqtt.inftech.hs-mannheim.de";
    private int port = 8883;
    private string clientId = Guid.NewGuid().ToString();
    private string username = "25pms02";
    private string password = "cf0fc303";

    private MqttFactory myFactory;
    private IMqttClient myClient;
 
    public MQTTclient()
    {
        // Create a MQTT client factory
        myFactory = new MqttFactory();

        // Create a MQTT client instance
        myClient = myFactory.CreateMqttClient();
    }

    public async Task subscribe(string topic)
    {
        await myClient.SubscribeAsync(topic);
        Console.WriteLine("Subscribed to: " + topic);

        // to unsubscribe a topic use following: 
        // await mqttClient.UnsubscribeAsync(topic);
    }

    public async Task publish(string topic, string m)
    {
        var message = new MqttApplicationMessageBuilder()
         .WithTopic(topic)
         .WithPayload(m)
         .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
         .WithRetainFlag()
         .Build();
        await myClient.PublishAsync(message);
     
    }

    private async Task receivedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
        //return Task.CompletedTask;
    }

    public async Task connect()
    {
        // Create MQTT client options
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port) // MQTT broker address and port
            .WithTlsOptions(
                  o => o.WithCertificateValidationHandler(
                      // The used public broker sometimes has invalid certificates. This sample accepts all
                      // certificates. This should not be used in live environments.
                      _ => true))
            .WithCredentials(username, password) // Set username and password     
            .Build();

        // Connect to MQTT broker
        var connectResult = await myClient.ConnectAsync(options);

        if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
        {
            Console.WriteLine("Connected successfully to: " + broker);

            // define EventHandler to execute when new message is received
            myClient.ApplicationMessageReceivedAsync += receivedMessage;
        }
        else
        {
            Console.WriteLine($"Failed to connect to MQTT broker: {connectResult.ResultCode}");
        }
    }

    public async Task disconnect()
    {
        // This will send the DISCONNECT packet. Calling _Dispose_ without DisconnectAsync the
        // connection is closed in a "not clean" way. See MQTT specification for more details.
        var disconnectoptions = new MqttClientDisconnectOptionsBuilder()
                                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                                .Build();
        await myClient.DisconnectAsync(disconnectoptions);
        Console.WriteLine("Disconnected!");
    }

}
