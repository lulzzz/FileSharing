using FileSharing.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Sockets
{
    public static partial class SocketExtension
    {
        public static async Task<Result> ConnectTap(
            this Socket socket,
            string remoteIpAddress,
            int port,
            int timeoutMs)
        {
            try
            {
                var connectTask = Task.Factory.FromAsync(
                        socket.BeginConnect,
                        socket.EndConnect,
                        remoteIpAddress,
                        port,
                        null
                    );
                if (connectTask == await Task.WhenAny(connectTask, Task.Delay(timeoutMs)).ConfigureAwait(false))
                {
                    await connectTask.ConfigureAwait(false);
                }
                else
                {
                    throw new TimeoutException();
                }
            }
            catch (SocketException ex)
            {
                return Result.Fail($"{ex.Message} ({ex.GetType()})");
            }
            catch (TimeoutException ex)
            {
                return Result.Fail($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok();
        }

        public static async Task<Result<Socket>> AcceptTap(this Socket listenSocket)
        {
            Socket workSocket;
            try
            {
                var acceptTask = Task<Socket>.Factory.FromAsync(listenSocket.BeginAccept, listenSocket.EndAccept, null);
                workSocket = await acceptTask.ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok(workSocket);
        }

        public static async Task<Result<int>> ReceiveTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags, int timeoutMs)
        {
            int bytesReceived;
            try
            {
                var asyncResult = socket.BeginReceive(buffer, offset, size, socketFlags, null, null);
                var receiveTask = Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndReceive(asyncResult));

                if (receiveTask == await Task.WhenAny(receiveTask, Task.Delay(timeoutMs)))
                {
                    bytesReceived = await receiveTask.ConfigureAwait(false);
                }
                else
                {
                    throw new TimeoutException();
                }
            }
            catch (SocketException ex)
            {
                return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
            }
            catch (TimeoutException ex)
            {
                return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok(bytesReceived);
        }

        public static async Task<Result<int>> ReceiveTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            int byteReceived;
            try
            {
                var asyncResult = socket.BeginReceive(buffer, offset, size, socketFlags, null, null);
                byteReceived = await Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndReceive(asyncResult));
            }
            catch (SocketException ex)
            {
                return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok(byteReceived);
        }

        public static async Task<Result<int>> SendTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags, int timeoutMs)
        {
            int byteSent;
            try
            {
                var asyncResult = socket.BeginSend(buffer, offset, size, socketFlags, null, null);
                var sendBytesTask = Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndSend(asyncResult));

                if (sendBytesTask != await Task.WhenAny(sendBytesTask, Task.Delay(timeoutMs)))
                {
                    throw new TimeoutException();
                }

                byteSent = await sendBytesTask;
            }
            catch (SocketException ex)
            {
                return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
            }
            catch (TimeoutException ex)
            {
                return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok(byteSent);
        }

        public static async Task<Result<int>> SendTap(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            int byteSent;
            try
            {
                var asyncResult = socket.BeginSend(buffer, offset, size, socketFlags, null, null);
                byteSent = await Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndSend(asyncResult));
            }
            catch (SocketException ex)
            {
                return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok(byteSent);
        }
    }
}
