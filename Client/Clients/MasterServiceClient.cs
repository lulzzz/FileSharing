using FileSharing.Commons;
using FileSharing.Commons.OpCodes;
using FileSharing.Sockets;
using FileSharing.Sockets.Packets;
using FileSharing.Sockets.Workers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Client.Clients
{
    class MasterServiceClientSettings
    {
        public string MasterServerIP { get; set; }

        public int MasterServerPort { get; set; }
    }

    class MasterServerState
    {

    }

    class FileListReceivedArgs : EventArgs
    {
        public List<FileDetailsWithDownloadEndPoint> FileList { get; set; }
    }

    delegate TcpPacket MasterServerTcpPacketHandler(TcpPacket tcpPacket, MasterServerState state);

    class MasterServer
    {
        public TcpSocketWorker TcpSocketWorker { get; }

        private Dictionary<byte, MasterServerTcpPacketHandler> packetProcessor;

        public MasterServer(TcpSocket tcpSocket, Dictionary<byte, MasterServerTcpPacketHandler> packetProcessor)
        {
            this.packetProcessor = packetProcessor;
            this.TcpSocketWorker = new TcpSocketWorker(tcpSocket, HandleTcpPacket);
            this.TcpSocketWorkerClosed += MasterServer_TcpSocketWorkerClosed;
        }

        private TcpPacket HandleTcpPacket(TcpPacket tcpPacket)
        {
            MasterServerTcpPacketHandler handler;
            if (this.packetProcessor.TryGetValue(tcpPacket.PacketType, out handler))
            {
                return handler?.Invoke(tcpPacket, new MasterServerState());
            }

            return null;
        }

        private void MasterServer_TcpSocketWorkerClosed(object sender, EventArgs e)
        {
            this.TcpSocketWorkerClosed?.Invoke(this, EventArgs.Empty);
        }

        public async Task RequestFileList()
        {
            using (var tcpPacket = new TcpPacket((byte)MasterServerOpCode.RequestFileList))
            {
                await this.TcpSocketWorker.SendPacket(tcpPacket);
            }
        }

        public event EventHandler TcpSocketWorkerClosed;
    }

    class MasterServiceClient
    {
        private MasterServiceClientSettings settings;

        private bool isConnecting = false;

        public MasterServer MasterServer { get; private set; }

        private readonly Dictionary<byte, MasterServerTcpPacketHandler> masterServerPacketProcessor = new Dictionary<byte, MasterServerTcpPacketHandler>();

        public MasterServiceClient(MasterServiceClientSettings settings)
        {
            this.settings = settings;
            this.InitializePacketProcessor();
        }

        public void Start()
        {
            if (this.isConnecting)
                return;

            TcpSocket tcpSocket = new TcpSocket(settings.MasterServerIP, settings.MasterServerPort);
            tcpSocket.Connect();
            this.MasterServer = new MasterServer(tcpSocket, this.masterServerPacketProcessor);
            this.MasterServer.TcpSocketWorker.Run();

            this.isConnecting = true;
        }

        public void Stop()
        {
            if (!this.isConnecting)
                return;
            this.isConnecting = false;

            this.MasterServer.TcpSocketWorker.Close();
        }

        private void InitializePacketProcessor()
        {
            this.masterServerPacketProcessor.Add((byte)MasterServerOpCode.ReturnFileList, this.HandleReturnFileList);
        }

        private TcpPacket HandleReturnFileList(TcpPacket tcpPacket, MasterServerState state)
        {
            using (var reader = tcpPacket.GetPayloadBufferReader())
            {
                var json = reader.ReadString();
                var fileList = JsonConvert.DeserializeObject<List<FileDetailsWithDownloadEndPoint>>(json);

                this.FileListReceived?.Invoke(this, new FileListReceivedArgs()
                {
                    FileList = fileList
                });
            }

            return null;
        }

        public event EventHandler<FileListReceivedArgs> FileListReceived;
    }
}
