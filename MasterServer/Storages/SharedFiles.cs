using FileSharing.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer.Storages
{
    class SharedFiles
    {
        private Dictionary<int, List<FileDetails>> files = new Dictionary<int, List<FileDetails>>();

        public SharedFiles()
        {

        }

        public void StoreFileList(int fileServerId, List<FileDetails> newFiles)
        {
            lock (this.files)
            {
                this.files[fileServerId] = newFiles;
            }
        }

        public void RemoveFileList(int fileServerId)
        {
            this.files.Remove(fileServerId);
        }

        public void RemoveFileList()
        {
            this.files.Clear();
        }

        public List<FileDetails> GetFileList(int fileServerId)
        {
            List<FileDetails> targetFiles;
            if (this.files.TryGetValue(fileServerId, out targetFiles))
            {
                return targetFiles;
            }

            throw new KeyNotFoundException();
        }

        public List<FileDetails> GetFileList()
        {
            lock (this.files)
            {
                return this.files
                    .SelectMany(pair => pair.Value)
                    .ToList();
            }
        }
    }
}
