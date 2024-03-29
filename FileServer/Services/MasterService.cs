﻿using FileServer.Storages;
using FileSharing.Commons;
using FileSharing.Commons.OpCodes;
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
        public TcpSocketWorker TcpSocketWorker { get; private set; }

        public MasterServer(TcpSocket tcpSocket)
        {
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
            var json = JsonConvert.SerializeObject(files);

            TcpPacket tcpPacket = new TcpPacket((byte)MasterServerFileServerOpCode.ReturnFileList);
            using (var writer = tcpPacket.GetPayloadBufferWriter())
            {
                writer.Write(json);
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
            tcpSocket.Connect();

            this.MasterServer = new MasterServer(tcpSocket);
            this.MasterServer.TcpSocketWorkerClosed += (sender, args) =>
            {
                this.Stop();
            };
            this.MasterServer.TcpSocketWorker.Run();

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
            this.isRunning = false;

            this.MasterServer.TcpSocketWorker.Close();
        }

        private async Task ExchangeOnStart()
        {
            await this.MasterServer.SendDownloadEndPoint(this.settings.FileServiceIP, this.settings.FileServicePort);
            var files = this.settings.SharedFileStorage.GetFiles();
            await this.MasterServer.SendFileList(files);
        }
    }
}
