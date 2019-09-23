using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace FileSharing.Sockets
{
    public delegate void TcpConnectionAccept(TcpSocket tcpSocket);

    public class TcpSocketListener : IDisposable
    {
        private Socket listenerSocket;

        private EndPoint localEndPoint;

        private CancellationTokenSource tokenSource;

        private TcpConnectionAccept tcpConnectionAccept;

        public bool IsListening { get; private set; }

        public TcpSocketListener(string ip, int port)
        {
            IPAddress ipAddress;
            IPAddress.TryParse(ip, out ipAddress);
            this.localEndPoint = new IPEndPoint(address: ipAddress ?? IPAddress.IPv6Loopback, port);
            this.listenerSocket = new Socket(
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp
            );

            this.listenerSocket = new Socket(
                addressFamily: this.localEndPoint.AddressFamily,
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp
            );

            this.IsListening = false;
        }

        public void Start(TcpConnectionAccept tcpConnectionAccept)
        {
            if (this.IsListening)
                return;

            this.listenerSocket.Bind(this.localEndPoint);
            this.listenerSocket.Listen(256);
            this.IsListening = true;
            this.tokenSource = new CancellationTokenSource();

            this.tcpConnectionAccept = tcpConnectionAccept;
            this.AcceptLoop(tokenSource.Token).ContinueWith((task) =>
            {
                this.IsListening = false;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop()
        {
            if (!this.IsListening)
                return;

            this.IsListening = false;
            this.tokenSource.Cancel();
            this.listenerSocket.Close();

            this.listenerSocket = new Socket(
                addressFamily: this.localEndPoint.AddressFamily,
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp
            );
        }

        private async Task AcceptLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var workSocket = await this.listenerSocket.AcceptTap();
                this.tcpConnectionAccept?.Invoke(new TcpSocket(workSocket));
            }
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                this.listenerSocket.Shutdown(SocketShutdown.Both);
                this.listenerSocket.Close();
                this.listenerSocket = null;
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
