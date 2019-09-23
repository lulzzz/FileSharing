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
                return base.GetPayloadBufferReader().ReadByte();
            }
            set
            {
                var writer = base.GetPayloadBufferWriter();
                writer.Seek(0, SeekOrigin.Begin);
                writer.Write(value);
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
