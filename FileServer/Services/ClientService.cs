using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using FileSharing.Commons;
using FileSharing.Commons.UdpPackets;
using FileSharing.Sockets;
using FileSharing.Sockets.Packets;
using FileSharing.Commons.DataStructures;
using FileServer.Storages;
using System.IO;

namespace FileServer.Services
{

    class ClientServiceSettings
    {
        public string FileServiceIP { get; set; }

        public int FileServicePort { get; set; }

        public SharedFileStorage SharedFileStorage { get; set; }
    }

    class ClientService
    {
        public const int MaxFileBlockSize = 8192; // 8 kbytes.

        private ClientServiceSettings settings;

        private Socket udpSocket;

        private EndPoint localEndPoint;

        private bool isRunning;

        private QueueAsync<(byte[] data, EndPoint client)> incommingData;
        private QueueAsync<(byte[] data, EndPoint client)> outgoingData;

        private CancellationTokenSource tokenSource;

        public ClientService(ClientServiceSettings settings)
        {
            this.settings = settings;

            IPAddress localIPAddress;
            IPAddress.TryParse(this.settings.FileServiceIP, out localIPAddress);
            this.localEndPoint = new IPEndPoint(localIPAddress ?? IPAddress.IPv6Loopback, this.settings.FileServicePort);
            this.udpSocket = new Socket(this.localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Start()
        {
            if (this.isRunning)
                return;

            this.udpSocket.Bind(this.localEndPoint);
            if (this.udpSocket.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork)
            {
                this.udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            }
            else
            {
                this.udpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
            }

            this.tokenSource = new CancellationTokenSource();

            this.ReceiveLoop(this.tokenSource.Token).ContinueWith(task => { }, TaskContinuationOptions.OnlyOnFaulted);
            this.ProcessLoop(this.tokenSource.Token).ContinueWith(task => { }, TaskContinuationOptions.OnlyOnFaulted);
            this.SendLoop(this.tokenSource.Token).ContinueWith(task => { }, TaskContinuationOptions.OnlyOnFaulted);

            this.isRunning = true;
        }

        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[UdpPacket.MaxUDPSize];
            while (!cancellationToken.IsCancellationRequested)
            {
                var receiveResult = await this.udpSocket.ReceiveFromAsync(buffer, 0, UdpPacket.MaxUDPSize, SocketFlags.None);
                byte[] data = new byte[receiveResult.ReceivedBytes];
                this.incommingData.Add((data: data, client: receiveResult.RemoteEndPoint));
            }
        }

        private async Task ProcessLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (data, endPoint) = await this.incommingData.Take();
                var udpPacket = new UdpPacket(data);

                switch (udpPacket.PacketType)
                {
                    case (byte)FileServerOpCode.RequestFileInfo:
                        {
                            var packet = new RequestFileInfoPacket(udpPacket);
                            byte[] returnBytes = this.CreateReturnFileInfoPacket(packet).GetBytes();
                            this.outgoingData.Add((returnBytes, endPoint));
                        }
                        break;
                    case (byte)FileServerOpCode.RequestBlock:
                        {
                            var packet = new RequestBlockPacket(udpPacket);
                            var returnPacket = await CreateReturnBlockPacket(packet);
                            byte[] returnBytes = returnPacket.GetBytes();
                            this.outgoingData.Add((returnBytes, endPoint));
                        }
                        break;
                    default:
                        break;
                }

            }
        }

        private UdpPacket CreateReturnFileInfoPacket(RequestFileInfoPacket requestFileInfoPacket)
        {
            var requestedFileName = requestFileInfoPacket.FileName;
            var fileDetails = this.settings.SharedFileStorage.GetFile(requestedFileName);
            if (fileDetails != null)
            {
                int fileID = this.settings.SharedFileStorage.GetID(fileDetails.Name);
                int blockCount = (int)Math.Ceiling((double)(fileDetails.Size / MaxFileBlockSize));
                return new ReturnFileInfoPacket(fileID, fileDetails.SHA512Hash, fileDetails.Size, MaxFileBlockSize, blockCount, fileDetails.Name);
            }
            else
            {
                return new ReturnFileInfoPacket(-1, new byte[64], -1, MaxFileBlockSize, -1, "");
            }
        }

        private async Task<UdpPacket> CreateReturnBlockPacket(RequestBlockPacket requestBlockPacket)
        {
            var fileDetails = this.settings.SharedFileStorage.GetFile(requestBlockPacket.FileID);
            if (fileDetails != null)
            {
                using (var fileStream = File.OpenRead("FileStorage"))
                {
                    fileStream.Seek((requestBlockPacket.BlockNumber - 1) * MaxFileBlockSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[MaxFileBlockSize];
                    int readBytes = await fileStream.ReadAsync(buffer, 0, MaxFileBlockSize);

                    byte[] fileBlockBytes = new byte[readBytes];
                    Array.Copy(buffer, 0, fileBlockBytes, 0, readBytes);

                    return new ReturnBlockPacket(requestBlockPacket.FileID, requestBlockPacket.BlockNumber, fileBlockBytes);
                }
            }
            else
            {
                return new ReturnBlockPacket(-1, -1, new byte[0]);
            }
        }

        private async Task SendLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (data, endPoint) = await this.outgoingData.Take();
                await this.udpSocket.SendToAsync(data, 0, data.Length, SocketFlags.None, endPoint);
            }
        }

        public void Stop()
        {
            if (!this.isRunning)
                return;

            this.tokenSource.Cancel();
            this.udpSocket.Close();
            this.incommingData.Clear();
            this.outgoingData.Clear();
            this.udpSocket = new Socket(this.localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            this.isRunning = false;
        }
    }
}
