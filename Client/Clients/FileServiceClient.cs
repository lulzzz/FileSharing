using FileSharing.Commons.OpCodes;
using FileSharing.Commons.UdpPackets;
using FileSharing.Sockets;
using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FileSharing.Client.Clients
{
    class FileServiceClientSettings
    {
        public string FileName { get; set; }

        public string FileServerIP { get; set; }

        public int FileServerPort { get; set; }
    }

    class UnknownFileException : Exception
    {
        public UnknownFileException()
        {

        }

        public UnknownFileException(string message) : base(message)
        {

        }

        public UnknownFileException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }

    class CorruptedFileException : Exception
    {
        public CorruptedFileException()
        {

        }

        public CorruptedFileException(string message) : base(message)
        {

        }

        public CorruptedFileException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }

    class FileServiceClient
    {
        private FileServiceClientSettings settings;

        private Socket udpSocket;
        private EndPoint fileServerEndPoint;

        private bool inProgress = false;

        private CancellationTokenSource tokenSource;

        public FileServiceClient(FileServiceClientSettings settings)
        {
            this.settings = settings;

            IPAddress fileServerIP;
            IPAddress.TryParse(this.settings.FileServerIP, out fileServerIP);
            this.fileServerEndPoint = new IPEndPoint(fileServerIP, this.settings.FileServerPort);
        }

        private async Task<ReturnFileInfoPacket> RequestFileInfo(CancellationToken cancellationToken)
        {
            var requestFileInfoPacket = new RequestFileInfoPacket(this.settings.FileName);

            UdpPacket udpPacket;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                await this.SendPacket(requestFileInfoPacket);
                try
                {
                    byte[] buffer = new byte[UdpPacket.MaxUDPSize];
                    int receivedBytes = await this.udpSocket.ReceiveAsync(buffer, 0, UdpPacket.MaxUDPSize, SocketFlags.None, 3000);
                    byte[] data = new byte[receivedBytes];
                    Array.Copy(buffer, 0, data, 0, receivedBytes);
                    udpPacket = new UdpPacket(data);
                }
                catch (TimeoutException)
                {
                    this.udpSocket.Close();
                    this.udpSocket = new Socket(this.fileServerEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    udpPacket = null;
                }
            } while (udpPacket?.PacketType != (byte)FileServerOpCode.ReturnFileInfo);
            return new ReturnFileInfoPacket(udpPacket);
        }

        private async Task SendPacket(UdpPacket udpPacket)
        {
            byte[] bytes = udpPacket.GetBytes();
            await this.udpSocket.SendToAsync(bytes, 0, bytes.Length, SocketFlags.None, this.fileServerEndPoint);
        }

        public async Task Download()
        {
            if (this.inProgress)
                return;
            this.inProgress = true;

            this.udpSocket = new Socket(this.fileServerEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            this.tokenSource = new CancellationTokenSource();

            var returnFileInfoPacket = await this.RequestFileInfo(this.tokenSource.Token);
            if (returnFileInfoPacket.FileID == -1)
            {
                this.inProgress = false;
                throw new UnknownFileException();
            }

            await this.RequestFileData(
                returnFileInfoPacket.FileName,
                returnFileInfoPacket.FileID,
                returnFileInfoPacket.FileSize,
                returnFileInfoPacket.MaxBlockSize,
                returnFileInfoPacket.BlockCount,
                returnFileInfoPacket.SHA512Hash,
                this.tokenSource.Token
            );

            await Task.Delay(3000);

            await Task.Run(() =>
            {
                byte[] sha512Hash = returnFileInfoPacket.SHA512Hash;
                using (var sha512 = SHA512.Create())
                {
                    using (var file = File.OpenRead(returnFileInfoPacket.FileName))
                    {
                        byte[] hash = sha512.ComputeHash(file);

                        if (!sha512Hash.SequenceEqual(hash))
                        {
                            throw new CorruptedFileException();
                        }
                    }
                }
            });
        }

        private async Task ReceiveLoop(BufferBlock<byte[]> incommingData, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[UdpPacket.MaxUDPSize];
            while (!cancellationToken.IsCancellationRequested)
            {
                var receivedBytes = await udpSocket.ReceiveAsync(buffer, 0, buffer.Length, SocketFlags.None);
                byte[] data = new byte[receivedBytes];
                Array.Copy(buffer, 0, data, 0, receivedBytes);
                incommingData.Post(data);
            }
        }

        private async Task SaveBlocks(int fileID, string fileName, int maxBlockSize, BufferBlock<byte[]> incommingData, HashSet<int> downloadedBlocks, CancellationToken cancellationToken)
        {
            using (FileStream file = File.OpenWrite(fileName))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    byte[] packetData;
                    packetData = await incommingData.ReceiveAsync(cancellationToken);

                    var udpPacket = new UdpPacket(packetData);

                    if (udpPacket.PacketType != (byte)FileServerOpCode.ReturnBlock)
                        continue;

                    ReturnBlockPacket returnBlockPacket = new ReturnBlockPacket(udpPacket);
                    if (returnBlockPacket.FileID != fileID)
                        continue;

                    downloadedBlocks.Add(returnBlockPacket.BlockNumber);

                    byte[] blockData = returnBlockPacket.BlockData;
                    file.Seek((returnBlockPacket.BlockNumber - 1) * maxBlockSize, SeekOrigin.Begin);
                    await file.WriteAsync(blockData, 0, blockData.Length, cancellationToken);
                }
            }
        }

        private async Task RequestFileBlock(int fileID, int blockNumber)
        {
            var requestFileBlock = new RequestBlockPacket(fileID, blockNumber);
            await this.SendPacket(requestFileBlock);
        }

        private async Task RequestFileData(string fileName, int fileID, long size, int maxBlockSize, int blockCount, byte[] sha512Hash, CancellationToken cancellationToken)
        {
            BufferBlock<byte[]> incommingData = new BufferBlock<byte[]>();
            _ = this.ReceiveLoop(incommingData, cancellationToken).ContinueWith(task =>
            {
                //
            }, TaskContinuationOptions.OnlyOnFaulted);

            HashSet<int> downloadedBlocks = new HashSet<int>();
            _ = this.SaveBlocks(fileID, fileName, maxBlockSize, incommingData, downloadedBlocks, cancellationToken).ContinueWith(task =>
            {

            }, TaskContinuationOptions.OnlyOnFaulted);

            do
            {
                for (int blockNumber = 1; blockNumber <= blockCount; ++blockNumber)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (downloadedBlocks.Contains(blockNumber))
                        continue;

                    await this.RequestFileBlock(fileID, blockNumber);
                }

                await Task.Delay(3000, cancellationToken);
            } while (downloadedBlocks.Count != blockCount);

            incommingData.Complete();
        }

        public void Cancel()
        {
            if (!this.inProgress)
                return;

            this.tokenSource.Cancel();
            this.udpSocket.Close();

            this.inProgress = false;
        }
    }
}
