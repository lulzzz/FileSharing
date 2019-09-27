using FileServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FileServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            EnterLoop();
        }

        static string InputMasterServerIP()
        {
            IPAddress ipAddress;
            string inputIP;
            do
            {
                Console.Write("Master Server IP: ");
                inputIP = Console.ReadLine();
            } while (!IPAddress.TryParse(inputIP, out ipAddress));

            return inputIP;
        }

        static int InputMasterServerPort()
        {
            int port;
            string inputPort;
            do
            {
                Console.Write("Master Server Port: ");
                inputPort = Console.ReadLine();
            } while (!int.TryParse(inputPort, out port));

            return port;
        }

        static string InputFileServiceIP()
        {
            IPAddress ipAddress;
            string inputIP;
            do
            {
                Console.Write("File Server Listen IP: ");
                inputIP = Console.ReadLine();
            } while (!IPAddress.TryParse(inputIP, out ipAddress));

            return inputIP;
        }

        static int InputFileServicePort()
        {
            int port;
            string inputPort;
            do
            {
                Console.Write("File Server Listen Port: ");
                inputPort = Console.ReadLine();
            } while (!int.TryParse(inputPort, out port));

            return port;
        }

        static void EnterLoop()
        {
            string masterServerIP = InputMasterServerIP();
            int masterServerPort = InputMasterServerPort();
            string fileServiceIP = InputFileServiceIP();
            int fileServicePort = InputFileServicePort();

            //string masterServerIP = "127.0.0.1";
            //int masterServerPort = 8000;
            //string fileServiceIP = "127.0.0.1";
            //int fileServicePort = 9000;

            var server = new Server(new ServerSettings()
            {
                MasterServerIP = masterServerIP,
                MasterServerPort = masterServerPort,
                FileServiceIP = fileServiceIP,
                FileServicePort = fileServicePort
            });

            server.StartUp();

            ConsoleKeyInfo pressedKey;
            do
            {
                Console.Write("Press 'q' to exit.");
                pressedKey = Console.ReadKey(true);
            } while (pressedKey.Key != ConsoleKey.Q);

            server.Shutdown();
        }
    }
}
