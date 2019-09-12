using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Sockets
{
    public static partial class SocketExtensions
    {
        public static async Task ConnectTap(this Socket socket, string remoteIpAddress, int port, int timeoutMs)
        {
            var connectTask = Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, remoteIpAddress, port, null);
            if (connectTask == await Task.WhenAny(connectTask, Task.Delay(timeoutMs)).ConfigureAwait(false))
            {
                await connectTask.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public static async Task ConnectTap(this Socket socket, EndPoint endPoint, int timeoutMs)
        {
            var connectTask = Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, endPoint, null);
            if (connectTask == await Task.WhenAny(connectTask, Task.Delay(timeoutMs)).ConfigureAwait(false))
            {
                await connectTask.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException();
            }
        }


        public static async Task DisconnectTap(this Socket socket, bool reuseSocket)
        {
            var disconnectTask = Task.Factory.FromAsync(socket.BeginDisconnect, socket.EndDisconnect, reuseSocket, null);
            await disconnectTask;
        }


        public static async Task<Socket> AcceptTap(this Socket listenSocket)
        {
            Socket workSocket;

            var acceptTask = Task<Socket>.Factory.FromAsync(listenSocket.BeginAccept, listenSocket.EndAccept, null);
            workSocket = await acceptTask.ConfigureAwait(false);

            return workSocket;
        }

        public static async Task<int> ReceiveTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags, int timeoutMs)
        {
            int bytesReceived;
            var asyncResult = socket.BeginReceive(buffer, offset, size, socketFlags, null, null);
            var receiveTask = Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndReceive(asyncResult));

            if (receiveTask != await Task.WhenAny(receiveTask, Task.Delay(timeoutMs)))
            {
                throw new TimeoutException();
            }

            bytesReceived = await receiveTask.ConfigureAwait(false);
            return bytesReceived;
        }

        public static async Task<int> ReceiveTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            int byteReceived;
            var receiveTask = Task<int>.Factory.FromAsync((callback, state) => socket.BeginReceive(buffer, offset, size, socketFlags, callback, state), socket.EndReceive, null);
            byteReceived = await receiveTask.ConfigureAwait(false);
            return byteReceived;
        }

        public static async Task<int> SendTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags, int timeoutMs)
        {
            int byteSent;

            var asyncResult = socket.BeginSend(buffer, offset, size, socketFlags, null, null);
            var sendBytesTask = Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndSend(asyncResult));
            if (sendBytesTask != await Task.WhenAny(sendBytesTask, Task.Delay(timeoutMs)))
            {
                throw new TimeoutException();
            }

            byteSent = await sendBytesTask;
            return byteSent;
        }

        public static async Task<int> SendTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            int byteSent;
            try
            {
                var asyncResult = socket.BeginSend(buffer, offset, size, socketFlags, null, null);
                byteSent = await Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndSend(asyncResult));
            }
            catch (SocketException ex)
            {
                throw ex;
            }

            return byteSent;
        }
    }
}
