using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Sockets
{
    public class TcpSocketIsClosedException : Exception
    {
        public TcpSocketIsClosedException()
        {

        }

        public TcpSocketIsClosedException(string message) : base(message)
        {

        }

        public TcpSocketIsClosedException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }

    public class TcpSocket
    {
        private Socket workSocket;
        private EndPoint ipEndPoint;

        private bool cleanedUp;

        public TcpSocket(string ip, int port)
        {
            IPAddress ipAddress;
            IPAddress.TryParse(ip, out ipAddress);
            this.ipEndPoint = new IPEndPoint(
                 address: ipAddress ?? IPAddress.IPv6Loopback,
                 port: port
             );

            this.workSocket = new Socket(
                addressFamily: this.ipEndPoint.AddressFamily,
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp
            );
        }

        public TcpSocket(Socket socket)
        {
            this.workSocket = socket;
        }

        public void Connect()
        {
            if (this.workSocket.Connected)
                return;

            if (this.cleanedUp)
            {
                throw new TcpSocketIsClosedException();
            }

            this.workSocket.Connect(this.ipEndPoint);
        }

        public async Task ConnectAsync()
        {
            if (this.workSocket.Connected)
                return;

            await this.workSocket.ConnectTap(this.ipEndPoint, 10000);
        }

        public void Disconnect()
        {
            if (!this.workSocket.Connected)
                return;

            this.workSocket.Shutdown(SocketShutdown.Both);
            this.workSocket.Disconnect(true);
        }

        public async Task DisconnectAsync()
        {
            if (this.workSocket.Connected)
                return;

            this.workSocket.Shutdown(SocketShutdown.Both);
            await this.workSocket.DisconnectTap(true);
        }

        public void Close()
        {
            if (this.cleanedUp)
                return;

            try
            {
                this.workSocket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                this.workSocket.Close();
                this.cleanedUp = true;
            }
        }

        public void Close(int timeout)
        {
            if (this.cleanedUp)
                return;

            try
            {
                this.workSocket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                this.workSocket.Close(timeout);
                this.cleanedUp = true;
            }
        }

        public bool IsConnected()
        {
            if (!this.workSocket.Connected)
                return false;

            bool oldBlockingState = this.workSocket.Blocking;
            bool isConnected = false;
            try
            {
                // Sending 0 bytes.
                byte[] testBuffer = new byte[1];
                this.workSocket.Blocking = false; // The Blocking property has no effect on asynchronous methods.
                this.workSocket.Send(testBuffer, 0, 0);
                isConnected = true;
            }
            catch (SocketException ex)
            {
                if (ex.NativeErrorCode.Equals(10035))
                {
                    isConnected = true;
                }
            }
            finally
            {
                this.workSocket.Blocking = oldBlockingState;
            }

            return isConnected;
        }

        public int GetVailable()
        {
            return this.workSocket.Available;
        }

        public async Task<int> SendAsync(byte[] data, int offset, int size)
        {
            return await this.workSocket.SendTap(data, offset, size, SocketFlags.None);
        }

        public async Task<int> ReceiveAsync(byte[] data, int offset, int size)
        {
            return await this.workSocket.ReceiveTap(data, offset, size, SocketFlags.None);
        }
    }
}
