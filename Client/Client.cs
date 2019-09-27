using FileSharing.Client.Clients;
using FileSharing.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Client
    {
        private string masterServerIP;
        private int masterServerPort;

        private List<FileDetailsWithDownloadEndPoint> fileList = new List<FileDetailsWithDownloadEndPoint>();

        public Client(string masterServerIP, int masterServerPort)
        {
            this.masterServerIP = masterServerIP;
            this.masterServerPort = masterServerPort;
        }

        public async Task GetFileList()
        {
            var masterServiceClient = new MasterServiceClient(new MasterServiceClientSettings()
            {
                MasterServerIP = this.masterServerIP,
                MasterServerPort = this.masterServerPort
            });

            masterServiceClient.Start();
            // Always set callback at first.
            masterServiceClient.FileListReceived += (sender, args) =>
            {
                this.fileList.Clear();
                this.fileList.AddRange(args.FileList);
                Console.Beep();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Thông báo: Đã nhận được danh sách tập tin.");
                Console.ResetColor();

                masterServiceClient.Stop();
            };
            await masterServiceClient.MasterServer.RequestFileList();
        }

        public async Task DownloadFile(int index)
        {
            FileDetailsWithDownloadEndPoint fileDetails;
            try
            {
                fileDetails = this.fileList[index - 1];
            }
            catch (IndexOutOfRangeException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cảnh báo: Chỉ mục của tập tin vượt khoảng hợp lệ.");
                Console.ResetColor();

                return;
            }

            var fileServiceClient = new FileServiceClient(new FileServiceClientSettings()
            {
                FileName = fileDetails.Name,
                FileServerIP = fileDetails.DownloadIP,
                FileServerPort = fileDetails.DownloadPort
            });

            try
            {
                await fileServiceClient.Download();
            } catch (UnknownFileException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Thông báo: Tập tin {fileDetails.Name} không tồn tại trên File Server.");
                Console.ResetColor();
                return;
            } catch (CorruptedFileException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Thông báo: Tập tin {fileDetails.Name} không toàn vẹn trong quá trình truyền tải.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Thông báo: Đã tải xong tập tin {fileDetails.Name}.");
            Console.ResetColor();
        }

        public void PrintFileList()
        {
            int index = 1;
            Console.WriteLine($"{"Index",5}|{"File Name",32}");
            foreach (var file in this.fileList)
            {
                Console.WriteLine($"{index++,5}|{file.Name,32}");
            }
        }
    }
}
