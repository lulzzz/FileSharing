using FileSharing.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileServer.Storages
{
    public class SharedFileStorage
    {
        private Dictionary<string, FileDetails> m_files = new Dictionary<string, FileDetails>();

        private Dictionary<string, int> m_file_id = new Dictionary<string, int>();

        private Dictionary<int, string> m_id_file = new Dictionary<int, string>();

        private int incrementID = 0;

        public SharedFileStorage()
        {

        }

        public void AddFile(FileDetails fileDetails)
        {
            this.m_files[fileDetails.Name] = fileDetails;
            this.m_file_id[fileDetails.Name] = this.incrementID;
            this.m_id_file[this.incrementID] = fileDetails.Name;

            this.incrementID++;
        }

        public void RemoveFile(string fileName)
        {
            int id = this.m_file_id[fileName];

            this.m_files.Remove(fileName);            
            this.m_file_id.Remove(fileName);
            this.m_id_file.Remove(id);
        }

        public int GetID(string fileName)
        {
            if (this.m_file_id.ContainsKey(fileName))
            {
                return this.m_file_id[fileName];
            }

            return -1;
        }

        public FileDetails GetFile(string fileName)
        {
            if (this.m_files.ContainsKey(fileName))
            {
                return this.m_files[fileName];
            }

            return null;
        }

        public FileDetails GetFile(int id)
        {
            if (this.m_id_file.ContainsKey(id))
            {
                return this.GetFile(this.m_id_file[id]);
            }

            return null;
        }

        public List<FileDetails> GetFiles()
        {
            return new List<FileDetails>(this.m_files.Values);
        }

        public void Clear()
        {
            this.m_files.Clear();
            this.m_file_id.Clear();
            this.m_id_file.Clear();
        }
    }
}
