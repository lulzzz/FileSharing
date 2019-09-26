using FileSharing.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer.Storages
{
    [Serializable]
    class FileServerMeta
    {
        public List<FileDetails> FileList { get; set; }

        public string FileServiceIP { get; set; } = IPAddress.IPv6Loopback.ToString();

        public int FileServicePort { get; set; } = 0;
    }

    class FileServerMetaStorage
    {
        private Dictionary<int, FileServerMeta> fileServers = new Dictionary<int, FileServerMeta>();

        public FileServerMetaStorage()
        {

        }

        public void StoreFileList(int fileServerId, List<FileDetails> newFiles)
        {
            lock (this.fileServers)
            {
                FileServerMeta metaData;
                if (this.fileServers.TryGetValue(fileServerId, out metaData))
                {
                    metaData.FileList = newFiles;
                }
                else
                {
                    this.fileServers[fileServerId] = new FileServerMeta() { FileList = newFiles };
                }
            }
        }

        public void StorageDownloadEndPoint(int fileServerId, string downloadIP, int downloadPort)
        {
            lock (this.fileServers)
            {
                FileServerMeta metaData;
                if (this.fileServers.TryGetValue(fileServerId, out metaData))
                {
                    metaData.FileServiceIP = downloadIP;
                    metaData.FileServicePort = downloadPort;
                }
                else
                {
                    this.fileServers[fileServerId] = new FileServerMeta() {
                        FileServiceIP = downloadIP,
                        FileServicePort = downloadPort
                    };
                }
            }
        }

        public void RemoveFileServer(int fileServerId)
        {
            this.fileServers.Remove(fileServerId);
        }

        public void Clear()
        {
            this.fileServers.Clear();
        }

        public FileServerMeta GetFileServer(int fileServerId)
        {
            FileServerMeta fileServer;
            if (this.fileServers.TryGetValue(fileServerId, out fileServer))
            {
                return fileServer;
            }

            throw new KeyNotFoundException();
        }

        public List<FileServerMeta> GetFileServers()
        {
            return new List<FileServerMeta>(this.fileServers.Values);
        }
    }
}
