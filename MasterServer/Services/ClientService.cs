using FileSharing.Commons;
using FileSharing.Commons.OpCodes;
using FileSharing.Sockets;
using FileSharing.Sockets.Packets;
using FileSharing.Sockets.Workers;
using MasterServer.Storages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterServer.Services
{
    class ClientServiceSettings
    {
        public string IP { get; set; }

        public int Port { get; set; }

        public FileServerMetaStorage FileServerMetaStorage { get; set; }
    }

    class ClientClosedArgs : EventArgs
    {
        public int ClientID { get; set; }
    }

    class ClientState
    {
        public ClientState(int id)
        {
            this.ID = id;
        }

        public int ID { get; }
    }

    delegate TcpPacket ClientTcpPacketHandler(TcpPacket tcpPacket, ClientState state);

    class Client
    {
        public int ID { get; }

        public TcpSocketWorker TcpSocketWorker { get; }

        private Dictionary<byte, ClientTcpPacketHandler> clientPacketProcessor;

        private ClientState clientState;

        public Client(int id, TcpSocket tcpSocket, Dictionary<byte, ClientTcpPacketHandler> clientPacketProcessor)
        {
            this.ID = id;
            this.clientPacketProcessor = clientPacketProcessor;
            this.clientState = new ClientState(id);
            this.TcpSocketWorker = new TcpSocketWorker(tcpSocket, this.HandleTcpPacket);
            this.TcpSocketWorker.Closed += TcpSocketWorker_Closed;
        }

        private TcpPacket HandleTcpPacket(TcpPacket tcpPacket)
        {
            ClientTcpPacketHandler handler;
            if (this.clientPacketProcessor.TryGetValue(tcpPacket.PacketType, out handler))
            {
                return handler?.Invoke(tcpPacket, this.clientState);
            }

            return null;
        }

        private void TcpSocketWorker_Closed(object sender, EventArgs e)
        {
            this.ClientClosed?.Invoke(this, new ClientClosedArgs()
            {
                ClientID = this.ID
            });
        }

        public event EventHandler<ClientClosedArgs> ClientClosed;
    }

    class ClientService
    {
        private ClientServiceSettings settings;

        private TcpSocketListener tcpSocketListener;

        private Dictionary<int, Client> clients = new Dictionary<int, Client>();

        private readonly Dictionary<byte, ClientTcpPacketHandler> tcpPacketProcessor = new Dictionary<byte, ClientTcpPacketHandler>();

        private int incrementID = 0;

        private bool isRunning = false;

        public ClientService(ClientServiceSettings settings)
        {
            this.settings = settings;
            this.InitializeTcpPacketProcessor();
            this.tcpSocketListener = new TcpSocketListener(this.settings.IP, this.settings.Port);
        }

        private void AddNewFileClient(TcpSocket tcpSocket)
        {
            int id = Interlocked.Increment(ref this.incrementID);
            var client = new Client(id, tcpSocket, this.tcpPacketProcessor);
            client.ClientClosed += Client_ClientClosed;
            client.TcpSocketWorker.Run();
            this.clients.Add(id, client);
        }

        private void Client_ClientClosed(object sender, ClientClosedArgs e)
        {
            this.clients.Remove(e.ClientID);
        }

        public void Start()
        {
            if (this.isRunning)
                return;

            this.tcpSocketListener.Start(this.AddNewFileClient);
            this.isRunning = true;
        }

        public void Stop()
        {
            if (!this.isRunning)
                return;
            this.isRunning = false;

            this.tcpSocketListener.Stop();
            foreach (var item in this.clients)
            {
                var client = item.Value;
                client.TcpSocketWorker.Close();
            }
            this.clients.Clear();
        }


        private void InitializeTcpPacketProcessor()
        {
            this.tcpPacketProcessor.Add((byte)MasterServerOpCode.RequestFileList, this.HandleRequestFileList);
        }

        private TcpPacket HandleRequestFileList(TcpPacket tcpPacket, ClientState state)
        {
            var returnPacket = new TcpPacket((byte)MasterServerOpCode.ReturnFileList);
            using (var writer = returnPacket.GetPayloadBufferWriter())
            {
                var fileList = BuildListFileWithDownloadEndPoint();
                var jsonString = JsonConvert.SerializeObject(fileList);
                writer.Write(jsonString);
            }
            return returnPacket;
        }

        private List<FileDetailsWithDownloadEndPoint> BuildListFileWithDownloadEndPoint()
        {
            var result = new List<FileDetailsWithDownloadEndPoint>();
            var fileServers = this.settings.FileServerMetaStorage.GetFileServers();
            foreach (var fileServer in fileServers)
            {
                foreach (var file in fileServer.FileList)
                {
                    result.Add(new FileDetailsWithDownloadEndPoint()
                    {
                        Name = file.Name,
                        Size = file.Size,
                        SHA512Hash = file.SHA512Hash,
                        DownloadIP = fileServer.FileServiceIP,
                        DownloadPort = fileServer.FileServicePort
                    });
                }
            }
            return result;
        }
    }
}
