class Programm
{
    static void Main(string[] args)
    {
        MQTTclient mc = new MQTTclient();

        mc.connect();

        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("Simple Example for MQTTnet library to be used in .NET 8.0 and higher ");
        Console.WriteLine("");
        Console.WriteLine("Select mode: ");
        Console.WriteLine("(s) Subscribe mode: receive message ");
        Console.WriteLine("(p) Publish mode: send message ");
        ConsoleKey k;
        do
        {
            k = Console.ReadKey(true).Key;
        } while (k != ConsoleKey.S && k != ConsoleKey.P);

        string topic = "25pms02/test";
        if (k == ConsoleKey.S)
        {
            Console.WriteLine("listen to data on topic: " + topic);
            mc.subscribe(topic);
        }

        if (k == ConsoleKey.P)
        {
            Console.WriteLine("publishing data on topic: " + topic);
            Console.WriteLine("press enter to stop...");
            string message = "";
            do
            {
                Console.Write("data: ");
                message = Console.ReadLine();
                if (!message.Equals("")) mc.publish(topic, message);
            } while (!message.Equals(""));
        }

        Console.WriteLine("Press any key to disconnect...");
        Console.ReadKey();
        mc.disconnect();

    }

}