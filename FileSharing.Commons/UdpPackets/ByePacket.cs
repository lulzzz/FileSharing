using FileSharing.Commons.OpCodes;
using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons.UdpPackets
{
    public class ByePacket : UdpPacket
    {
        public ByePacket(): base((byte) FileServerOpCode.Bye)
        {

        }

        public ByePacket(UdpPacket udpPacket): base (udpPacket)
        {

        }
    }
}
