using System;

namespace AnagramServer2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Init();

            HttpServer server = new HttpServer("http://localhost:5050/");
            server.Start();

            Console.WriteLine("Unesite 'exit' za bezbedno gašenje servera.");

            while (true)
            {
                string unos = Console.ReadLine();
                if (unos?.Trim().ToLower() == "exit")
                {
                    break;
                }
            }

            Console.WriteLine("Pokrećem proceduru za gašenje...");
            server.Stop();
            Console.WriteLine("Server je uspešno ugašen.");
        }
    }
}