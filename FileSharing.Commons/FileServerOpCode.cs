using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons
{
    public enum FileServerOpCode : byte
    {
        Ack,
        Bye,
        RequestFileInfo,
        ReturnFileInfo,
        RequestBlock,
        ReturnBlock,
    }
}
