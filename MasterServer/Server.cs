using MasterServer.Services;
using MasterServer.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer
{

    class ServerSettings
    {
        public string FileServerServiceIP { get; set; }

        public int FileServerServicePort { get; set; }

        public string FileClientServiceIP { get; set; }

        public int FileClientServicePort { get; set; }
    }

    class Server
    {
        private FileServerMetaStorage fileServerMetaStorage;

        private FileServerService fileServerService;

        private ClientService clientService;

        public Server(ServerSettings settings)
        {
            this.fileServerMetaStorage = new FileServerMetaStorage();
            this.fileServerService = new FileServerService(new FileServerServiceSettings()
            {
                ListenIP = settings.FileServerServiceIP,
                Port = settings.FileServerServicePort,
                FileServerMetaStorage = this.fileServerMetaStorage
            });
            this.clientService = new ClientService(new ClientServiceSettings()
            {
                IP = settings.FileClientServiceIP,
                Port = settings.FileClientServicePort,
                FileServerMetaStorage = this.fileServerMetaStorage
            });
        }

        public void Start()
        {
            this.fileServerService.Start();
            this.clientService.Start();
        }

        public void Stop()
        {
            this.fileServerService.Stop();
            this.clientService.Stop();
        }
    }
}
