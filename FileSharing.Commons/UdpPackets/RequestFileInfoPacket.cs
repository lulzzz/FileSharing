using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons.UdpPackets
{
    public class RequestFileInfoPacket : UdpPacket
    {

        public string FileName
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    return reader.ReadString();
                }
            }
            set
            {
                using (var writer = base.GetPayloadBufferWriter())
                {
                    writer.BaseStream.SetLength(0L);
                    writer.Write(value);
                }
            }
        }

        public RequestFileInfoPacket(string fileName) : base((byte)FileServerOpCode.RequestFileInfo)
        {
            using (var writer = base.GetPayloadBufferWriter())
            {
                writer.Write(fileName);
            }
        }

        public RequestFileInfoPacket(UdpPacket udpPacket) : base(udpPacket)
        {

        }
    }
}
