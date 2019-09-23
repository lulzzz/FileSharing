using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons.UdpPackets
{
    public class RequestBlockPacket : UdpPacket
    {
        public int FileID
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    return reader.ReadInt32();
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

        public int BlockNumber
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    reader.BaseStream.Seek(4, SeekOrigin.Begin);
                    return reader.ReadInt32();
                }
            }
            set
            {
                using (var writer = base.GetPayloadBufferWriter())
                {
                    writer.Seek(4, SeekOrigin.Begin);
                    writer.Write(value);
                }
            }
        }

        public RequestBlockPacket(int fileID, int blockNumber) : base((byte)FileServerOpCode.RequestBlock)
        {
            using (var writer = base.GetPayloadBufferWriter())
            {
                writer.Write(fileID);
                writer.Write(blockNumber);
            }
        }

        public RequestBlockPacket(UdpPacket udpPacket) : base(udpPacket)
        {

        }
    }
}
