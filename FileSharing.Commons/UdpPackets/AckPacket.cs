using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons.UdpPackets
{
    public class AckPacket : UdpPacket
    {
        public byte ForPacket
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    return reader.ReadByte();
                }
            }
            set
            {
                using (var writer = base.GetPayloadBufferWriter())
                {
                    writer.Write(value);
                }
            }
        }

        public AckPacket(byte forPacket) : base((byte)FileServerOpCode.Ack, new byte[] { forPacket })
        {

        }

        public AckPacket(UdpPacket udpPacket) : base(udpPacket)
        {

        }
    }
}
