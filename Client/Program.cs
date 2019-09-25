using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            EnterLoop();
        }

        static string GetMasterServerIP()
        {
            IPAddress ipAddress;
            string inputIP;
            do
            {
                Console.Write("Master Server IP: ");
                inputIP = Console.ReadLine();
            } while (!IPAddress.TryParse(inputIP, out ipAddress));

            return ipAddress.ToString();
        }

        static int GetMasterServerPort()
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

        static int InputIndex()
        {
            int num;
            string input;
            do
            {
                Console.WriteLine("Chỉ mục của tập tin cần tải: ");
                input = Console.ReadLine();
            } while (!int.TryParse(input, out num));

            return num;
        }

        static void EnterLoop()
        {
            string masterServerIP = GetMasterServerIP();
            int masterServerPort = GetMasterServerPort();
            var client = new Client(masterServerIP, masterServerPort);

            ConsoleKeyInfo pressedKey;
            do
            {
                Console.WriteLine("1. Lấy danh sách tập tin từ Master Server.");
                Console.WriteLine("2. In danh sách tập tin đang lưu trữ");
                Console.WriteLine("3. Tải tập tin");
                Console.WriteLine("q. Thoát");
                pressedKey = Console.ReadKey(true);

                switch (pressedKey.Key)
                {
                    case ConsoleKey.D1:
                        client.GetFileList().ContinueWith(task =>
                        {
                            task.Exception.Handle((ex) =>
                            {
                                if (ex is SocketException)
                                {
                                    var socketException = ex as SocketException;
                                    switch (socketException.NativeErrorCode)
                                    {
                                        case 10061:
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("Lỗi: Không kết nối được tới Master Server.");
                                            Console.ResetColor();
                                            return true;
                                        default:
                                            return false;
                                    }
                                }
                                return false;
                            });
                        }, TaskContinuationOptions.OnlyOnFaulted);
                        break;
                    case ConsoleKey.D2:
                        client.PrintFileList();
                        break;
                    case ConsoleKey.D3:
                        int index = InputIndex();
                        client.DownloadFile(index).ContinueWith(task => { }, TaskContinuationOptions.OnlyOnFaulted);
                        break;
                    default:
                        break;
                }
            } while (pressedKey.Key != ConsoleKey.Q);
        }
    }
}
