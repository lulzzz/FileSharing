using FileServer.Services;
using FileServer.Storages;
using FileSharing.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileServer
{
    class ServerSettings
    {
        public string MasterServerIP { get; set; }

        public int MasterServerPort { get; set; }

        public string FileServiceIP { get; set; }

        public int FileServicePort { get; set; }
    }
    class Server
    {

        private readonly SharedFileStorage sharedFileStorage;

        public MasterService MasterService { get; }

        public ClientService ClientService { get; }

        public Server(ServerSettings settings)
        {
            this.sharedFileStorage = new SharedFileStorage();

            this.MasterService = new MasterService(new MasterServiceSettings()
            {
                MasterServerIP = settings.MasterServerIP,
                MasterServerPort = settings.MasterServerPort,
                FileServiceIP = settings.FileServiceIP,
                FileServicePort = settings.FileServicePort,
                SharedFileStorage = this.sharedFileStorage
            });

            this.ClientService = new ClientService(new ClientServiceSettings()
            {
                FileServiceIP = settings.FileServiceIP,
                FileServicePort = settings.FileServicePort,
                SharedFileStorage = this.sharedFileStorage
            });
        }

        public void StartUp()
        {
            this.PrepareStorage();
            this.ScanFiles();
            this.MasterService.Start();
            this.ClientService.Start();
        }

        public void Shutdown()
        {
            this.MasterService.Stop();
            this.ClientService.Stop();
        }

        private void PrepareStorage()
        {
            if (!Directory.Exists("FileStorage"))
            {
                Directory.CreateDirectory("FileStorage");
            }
        }

        private void ScanFiles()
        {
            var directory = new DirectoryInfo("FileStorage");
            var files = directory.GetFiles();
            foreach (var file in files)
            {
                Console.Write($"Hashing \"{file.Name}\" ... ");
                byte[] hash;
                using (var sha512 = SHA512.Create())
                {
                    hash = sha512.ComputeHash(file.OpenRead());
                }
                Console.Write("Done!");

                this.sharedFileStorage.AddFile(new FileDetails()
                {
                    Name = file.Name,
                    Size = file.Length,
                    SHA512Hash = hash
                });
            }
        }
    }
}
