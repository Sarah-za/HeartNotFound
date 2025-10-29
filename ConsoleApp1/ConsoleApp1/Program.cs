using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World");
            Greet g = new Greet();
            g.Mannheim();
            g.Heidelberg();

            Console.ReadLine();
        }
    }
}