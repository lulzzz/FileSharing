using FileSharing.Commons;
using FileSharing.Commons.OpCodes;
using FileSharing.Sockets;
using FileSharing.Sockets.Packets;
using FileSharing.Sockets.Workers;
using MasterServer.Storages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterServer.Services
{
    class FileServerServiceSettings
    {
        public string ListenIP { get; set; }

        public int Port { get; set; }

        public FileServerMetaStorage FileServerMetaStorage { get; set; }
    }

    class FileServerClosedEventArgs : EventArgs
    {
        public int FileServerId { get; set; }
    }

    class FileServerState
    {
        public FileServerState(int id)
        {
            this.ID = id;
        }

        public int ID { get; }
    }

    delegate TcpPacket FileServerTcpPacketHandler(TcpPacket tcpPacket, FileServerState state);

    class FileServer
    {
        public int ID { get; private set; }

        public TcpSocketWorker TcpSocketWorker { get; }

        private readonly FileServerState fileServerState;

        private Dictionary<byte, FileServerTcpPacketHandler> fileServerPacketProcessor;

        public FileServer(int id, TcpSocket tcpSocket, Dictionary<byte, FileServerTcpPacketHandler> fileServerPacketProcessor)
        {
            this.ID = id;
            this.fileServerPacketProcessor = fileServerPacketProcessor;
            this.fileServerState = new FileServerState(id);
            this.TcpSocketWorker = new TcpSocketWorker(tcpSocket, this.HandleTcpPacket);
            this.TcpSocketWorker.Closed += TcpSocketWorker_Closed;
        }

        private TcpPacket HandleTcpPacket(TcpPacket tcpPacket)
        {
            FileServerTcpPacketHandler handler;
            if (this.fileServerPacketProcessor.TryGetValue(tcpPacket.PacketType, out handler))
            {
                return handler?.Invoke(tcpPacket, this.fileServerState);
            }

            return null;
        }

        private void TcpSocketWorker_Closed(object sender, EventArgs e)
        {
            this.FileServerClosed?.Invoke(this, new FileServerClosedEventArgs() { FileServerId = this.ID });
        }

        public event EventHandler<FileServerClosedEventArgs> FileServerClosed;
    }

    class FileServerService
    {
        private FileServerServiceSettings settings;

        private TcpSocketListener tcpSocketListener;

        private Dictionary<int, FileServer> fileServers;

        private readonly Dictionary<byte, FileServerTcpPacketHandler> fileServerPacketProcessor = new Dictionary<byte, FileServerTcpPacketHandler>();

        private int fileServerIdIncrement = 0;

        private CancellationTokenSource tokenSource;

        private bool isRunning = false;

        public FileServerService(FileServerServiceSettings settings)
        {
            this.settings = settings;
            this.InitializeTcpPacketProcessor();
            this.tcpSocketListener = new TcpSocketListener(this.settings.ListenIP, this.settings.Port);
            this.fileServers = new Dictionary<int, FileServer>();
        }

        private void AddNewFileServer(TcpSocket tcpSocket)
        {
            int id = Interlocked.Increment(ref this.fileServerIdIncrement);
            // TODO: fileServerId recycle.

            var fileServer = new FileServer(id, tcpSocket, this.fileServerPacketProcessor);
            fileServer.FileServerClosed += this.OnFileServerTcpSocketWorkerClosed;

            fileServer.TcpSocketWorker.Run();
            this.fileServers.Add(id, fileServer);
        }

        public void Start()
        {
            if (isRunning)
                return;

            this.tcpSocketListener.Start(this.AddNewFileServer);

            this.tokenSource = new CancellationTokenSource();
            this.RunRemoveOfflineFileServerLoop(this.tokenSource.Token).ContinueWith((task) =>
            {
                if (!this.tokenSource.IsCancellationRequested)
                {
                    this.Stop();
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

            this.isRunning = true;
        }

        public void Stop()
        {
            if (!this.isRunning)
                return;
            this.isRunning = false;

            this.tokenSource.Cancel();

            this.tcpSocketListener.Stop();
            foreach (var item in this.fileServers)
            {
                var fileServer = item.Value;
                fileServer.TcpSocketWorker.Close();
            }
            this.fileServers.Clear();

            this.settings.FileServerMetaStorage.Clear();
        }

        private async Task RunRemoveOfflineFileServerLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //var removedFileServerIds = new List<int>();
                //foreach (var item in this.fileServers)
                //{
                //    var fileServerId = item.Key;
                //    var fileServer = item.Value;

                //    if (fileServer.TcpSocketWorker.IsConnectionAlive())
                //        continue;

                //    removedFileServerIds.Add(fileServerId);
                //}

                //foreach (var removedFileServerId in removedFileServerIds)
                //{
                //    FileServer removedFileServer;
                //    if (this.fileServers.TryGetValue(removedFileServerId, out removedFileServer))
                //    {
                //        removedFileServer.TcpSocketWorker.Close();
                //        this.fileServers.Remove(removedFileServerId);
                //        this.settings.SharedFilesStorage.RemoveFileList(removedFileServerId);
                //    }
                //}

                await Task.Delay(3000, cancellationToken);
            }
        }

        private void OnFileServerTcpSocketWorkerClosed(object sender, FileServerClosedEventArgs args)
        {
            this.fileServers.Remove(args.FileServerId);
            this.settings.FileServerMetaStorage.RemoveFileServer(args.FileServerId);
        }

        private void InitializeTcpPacketProcessor()
        {
            this.fileServerPacketProcessor.Add((byte)MasterServerFileServerOpCode.Hello, null);
            this.fileServerPacketProcessor.Add((byte)MasterServerFileServerOpCode.KeepAlive, null);
            this.fileServerPacketProcessor.Add((byte)MasterServerFileServerOpCode.ReturnFileList, this.HandleReturnFileList);
            this.fileServerPacketProcessor.Add((byte)MasterServerFileServerOpCode.ReturnDownloadEndPoint, this.HandleReturnDownloadEndPoint);
        }

        private TcpPacket HandleReturnFileList(TcpPacket tcpPacket, FileServerState state)
        {
            using (var reader = tcpPacket.GetPayloadBufferReader())
            {
                var json = reader.ReadString();
                var files = JsonConvert.DeserializeObject<List<FileDetails>>(json);
                this.settings.FileServerMetaStorage.StoreFileList(state.ID, files);
            }

            return null;
        }

        private TcpPacket HandleReturnDownloadEndPoint(TcpPacket tcpPacket, FileServerState state)
        {
            using (var reader = tcpPacket.GetPayloadBufferReader())
            {
                var ip = reader.ReadString();
                var port = reader.ReadInt32();
                this.settings.FileServerMetaStorage.StorageDownloadEndPoint(state.ID, ip, port);
            }

            return null;
        }
    }
}
