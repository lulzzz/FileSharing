using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons.UdpPackets
{
    public class ReturnBlockPacket : UdpPacket
    {
        public int FileID {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    return reader.ReadInt32();
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
        }

        public byte[] BlockData
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    reader.BaseStream.Seek(8, SeekOrigin.Begin);
                    var remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
                    return reader.ReadBytes((int)remainingBytes);
                }
            }
        }

        public ReturnBlockPacket(int fileID, int blockNumber, byte[] blockData) : base((byte)FileServerOpCode.ReturnBlock)
        {
            using (var writer = base.GetPayloadBufferWriter())
            {
                writer.Write(fileID);
                writer.Write(blockNumber);
                writer.Write(blockData);
            }
        }

        public ReturnBlockPacket(UdpPacket udpPacket) : base(udpPacket)
        {

        }
    }
}
