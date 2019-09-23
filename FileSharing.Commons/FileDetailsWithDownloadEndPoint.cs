using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons
{
    public class FileDetailsWithDownloadEndPoint : FileDetails
    {
        public string DownloadIP { get; set; }

        public int DownloadPort { get; set; }
    }
}
