using FileServer.Storages;
using FileSharing.Commons;
using FileSharing.Sockets;
using FileSharing.Sockets.Packets;
using FileSharing.Sockets.Workers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileServer.Services
{
    class MasterServer
    {
        private TcpSocket tcpSocket;
        public TcpSocketWorker TcpSocketWorker { get; private set; }

        public MasterServer(TcpSocket tcpSocket)
        {
            this.tcpSocket = tcpSocket;
            this.TcpSocketWorker = new TcpSocketWorker(tcpSocket, this.HandleTcpPacket);

            this.TcpSocketWorker.Closed += (sender, args) =>
            {
                this.TcpSocketWorkerClosed?.Invoke(this, EventArgs.Empty);
            };
        }

        private TcpPacket HandleTcpPacket(TcpPacket tcpPacket)
        {
            return null;
        }

        public async Task SendDownloadEndPoint(string downloadIP, int downloadPort)
        {
            var tcpPacket = new TcpPacket((byte)MasterServerFileServerOpCode.ReturnDownloadEndPoint);
            using (var writer = tcpPacket.GetPayloadBufferWriter())
            {
                writer.Write(downloadIP);
                writer.Write(downloadPort);
            }

            await this.TcpSocketWorker.SendPacket(tcpPacket);
        }

        public async Task SendFileList(List<FileDetails> files)
        {
            var jsonString = JsonConvert.SerializeObject(files);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            TcpPacket tcpPacket = new TcpPacket((byte)MasterServerFileServerOpCode.ReturnFileList);
            using (var writer = tcpPacket.GetPayloadBufferWriter())
            {
                writer.Write(jsonBytes, 0, jsonBytes.Length);
            }

            await this.TcpSocketWorker.SendPacket(tcpPacket);
        }

        public event EventHandler TcpSocketWorkerClosed;
    }

    class MasterServiceSettings
    {
        public string MasterServerIP { get; set; }

        public int MasterServerPort { get; set; }

        public string FileServiceIP { get; set; }

        public int FileServicePort { get; set; }

        public SharedFileStorage SharedFileStorage { get; set; }
    }

    class MasterService
    {
        private MasterServiceSettings settings;

        public MasterServer MasterServer { get; private set; }

        private bool isRunning = false;

        public MasterService(MasterServiceSettings settings)
        {
            this.settings = settings;
        }

        public void Start()
        {
            if (this.isRunning)
                return;

            TcpSocket tcpSocket = new TcpSocket(settings.MasterServerIP, settings.MasterServerPort);
            try
            {
                tcpSocket.Connect();
            }
            catch (SocketException ex)
            {
                switch (ex.NativeErrorCode)
                {
                    case 10061:
                        Console.Error.WriteLine($"Socket error: {ex.Message}");
                        break;
                    default:
                        Console.Error.WriteLine($"Socket error {ex.NativeErrorCode}");
                        break;
                }

                return;
            }

            this.MasterServer = new MasterServer(tcpSocket);
            this.MasterServer.TcpSocketWorkerClosed += (sender, args) =>
            {
                this.Stop();
            };

            this.isRunning = true;

            this.ExchangeOnStart().ContinueWith(task =>
            {
                this.Stop();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop()
        {
            if (!this.isRunning)
                return;

            this.MasterServer.TcpSocketWorker.Close();
            this.isRunning = false;
        }

        private async Task ExchangeOnStart()
        {
            await this.MasterServer.SendDownloadEndPoint(this.settings.FileServiceIP, this.settings.FileServicePort);
            var files = this.settings.SharedFileStorage.GetFiles();
            await this.MasterServer.SendFileList(files);
        }
    }
}
