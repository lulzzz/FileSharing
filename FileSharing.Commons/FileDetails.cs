using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons
{
    public class FileDetails
    {
        public string Name { get; set; }

        public long Size { get; set; }

        public byte[] SHA512Hash { get; set; } // 512 bits, 64 bytes.
    }

}
