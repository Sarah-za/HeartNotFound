using System;
using System.Threading.Tasks;
using VitalDatenReceiver;

class Programm
{
    static async Task Main(string[] args)
    {
        VitalMqttSubscriber vr = new VitalMqttSubscriber();
        await vr.Connect();

        Console.WriteLine("--------------------------------");
        Console.WriteLine("MQTT Vitaldaten Receiver gestartet");
        Console.WriteLine("Drücke [Q], um das Programm zu beenden.\n");

        while(true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Q)
                    break;
            }
            await Task.Delay(100);
        }

        await vr.disconnect();
        vr.printSummary();

        Console.WriteLine("Drücke eine beliebige Taste zum schließen...");
        Console.ReadKey();
    }
}