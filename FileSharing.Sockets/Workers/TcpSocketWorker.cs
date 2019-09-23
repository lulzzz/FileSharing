using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSharing.Sockets.Workers
{
    public delegate TcpPacket TcpPacketHandler(TcpPacket tcpPacket);

    public class TcpSocketWorker
    {
        /**
         * 1 Worker per 1 particular TCP connection (1 client).
         * 
         * 1. Parsing incoming stream of bytes to TCP Packet (a message at application level).
         * 2. Processing the received packet.
         * 3. Return a packet to the client if needed.
         * 
         * Request -> [Response]: Optional
         * 
         * 
         * Working as sequential mode. Don't need to work at concurrent mode.
         * 
         */

        private TcpSocket tcpSocket;

        private TcpPacketHandler tcpPacketHandler;

        private CancellationTokenSource tokenSource;

        const int MAXIMUM_BUFFER_SIZE = 4096; // bytes.

        public bool Active { get; private set; }

        private bool isClosed;

        public TcpSocketWorker(TcpSocket tcpSocket, TcpPacketHandler tcpPacketHandler)
        {
            this.tcpSocket = tcpSocket;
            this.tcpPacketHandler = tcpPacketHandler;
            this.Active = false;
            this.isClosed = false;
        }

        public void Close()
        {
            if (this.isClosed)
                return;

            if (tokenSource != null)
                this.tokenSource.Cancel();
            this.tcpSocket.Close();

            this.Active = false;
            this.isClosed = true;

            this.Closed?.Invoke(this, EventArgs.Empty);
        }

        public void Run()
        {
            if (this.Active)
                return;

            if (this.isClosed)
                throw new TcpSocketIsClosedException();

            this.Active = true;
            this.tokenSource = new CancellationTokenSource();

            this.ReceiveLoop(this.tokenSource.Token).ContinueWith(task =>
            {
                this.Close();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            byte[] receiveBuffer = new byte[MAXIMUM_BUFFER_SIZE];
            List<byte> backlog = new List<byte>();
            var token = this.tokenSource.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                // The heart of the worker.
                await this.ReceiveData(receiveBuffer, backlog, token);
                await this.ProcessReceivedData(backlog, token);
            }
        }

        private async Task ReceiveData(byte[] buffer, List<byte> backlog, CancellationToken token)
        {
            var byteCount = await tcpSocket.ReceiveAsync(buffer, 0, MAXIMUM_BUFFER_SIZE);
            if (byteCount == 0) // The remote has shutdown the socket.
            {
                throw new TcpSocketIsClosedException();
            }
            byte[] data = new byte[byteCount];
            Array.Copy(buffer, 0, data, 0, byteCount);
            backlog.AddRange(data);
        }

        private async Task ProcessReceivedData(List<byte> backlog, CancellationToken token)
        {
            while (backlog.Count >= TcpPacket.HeaderLength)
            {
                byte[] headerBytes = backlog.GetRange(0, TcpPacket.HeaderLength).ToArray();
                int payloadLength = BitConverter.ToInt32(headerBytes, 1);
                int packetLength = payloadLength + TcpPacket.HeaderLength;

                if (backlog.Count < packetLength)
                    break;

                byte packetType = headerBytes[0];
                byte[] payloadBytes = backlog.GetRange(TcpPacket.HeaderLength, payloadLength).ToArray();
                backlog.RemoveRange(0, packetLength);

                var tcpPacket = new TcpPacket(packetType, payloadBytes);
                TcpPacket returnPacket = this.tcpPacketHandler?.Invoke(tcpPacket);
                if (returnPacket != null)
                {
                    await this.SendPacket(returnPacket);
                }
            }
        }

        public async Task SendPacket(TcpPacket tcpPacket)
        {
            byte[] data = tcpPacket.GetBytes();
            var bytesSent = await this.tcpSocket.SendAsync(data, 0, data.Length);
        }

        public bool IsConnectionAlive()
        {
            return this.tcpSocket.IsConnected();
        }

        public event EventHandler Closed;
    }
}
