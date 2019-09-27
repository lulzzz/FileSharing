using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            EnterLoop();
        }

        static string GetFileServerServiceListenIP()
        {
            IPAddress ipAddress;
            string inputIP;
            do
            {
                Console.Write("File Server Service Listen IP: ");
                inputIP = Console.ReadLine();
            } while (!IPAddress.TryParse(inputIP, out ipAddress));

            return inputIP;
        }

        static int GetFileServerServicePort()
        {
            int port;
            string inputPort;
            do
            {
                Console.Write("File Server Service Listen Port: ");
                inputPort = Console.ReadLine();
            } while (!int.TryParse(inputPort, out port));

            return port;
        }

        static string GetFileClientServiceListenIP()
        {
            IPAddress ipAddress;
            string inputIP;
            do
            {
                Console.Write("File Client Service Listen IP: ");
                inputIP = Console.ReadLine();
            } while (!IPAddress.TryParse(inputIP, out ipAddress));

            return inputIP;
        }


        static int GetFileClientServicePort()
        {
            int port;
            string inputPort;
            do
            {
                Console.Write("File Client Service Listen Port: ");
                inputPort = Console.ReadLine();
            } while (!int.TryParse(inputPort, out port));

            return port;
        }

        static void EnterLoop()
        {
            string fileServerServiceIP = GetFileServerServiceListenIP();
            int fileServerServicePort = GetFileServerServicePort();
            string fileClientServiceIP = GetFileClientServiceListenIP();
            int fileClientServicePort = GetFileClientServicePort();

            //string fileServerServiceIP = "127.0.0.1";
            //int fileServerServicePort = 8000;
            //string fileClientServiceIP = "127.0.0.1";
            //int fileClientServicePort = 7000;

            var server = new Server(new ServerSettings()
            {
                FileServerServiceIP = fileServerServiceIP,
                FileServerServicePort = fileServerServicePort,
                FileClientServiceIP = fileClientServiceIP,
                FileClientServicePort = fileClientServicePort
            });
            server.Start();

            ConsoleKeyInfo pressedKey;
            do
            {
                Console.WriteLine("Press 'q' to exit.");
                pressedKey = Console.ReadKey(true);
            } while (pressedKey.Key != ConsoleKey.Q);

            server.Stop();
        }
    }
}
