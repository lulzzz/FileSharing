using FileSharing.Sockets.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Commons.UdpPackets
{
    public class ReturnFileInfoPacket : UdpPacket
    {
        /*
         *  _____________________
         * |____OpCode: 1 byte___| <-- Header (Packet Type)
         * |   FileID: 4 bytes   | <-- Payload
         * |_____________________|
         * |                     |
         * |  SHA512: 64 bytes   |
         * |_____________________|
         * |  FileSize: 8 bytes  |
         * |_____________________|
         * |MaxBlockSize: 4 bytes|
         * |_____________________|
         * | BlockCount: 4 bytes |
         * |_____________________|
         * |                     |
         * |  FileName: String   |
         * |                     |
         * |_____________________|
         * 
         * BinaryWriter and BinaryReader support write string with a length prefix.
         * - https://docs.microsoft.com/en-us/dotnet/api/system.io.binaryreader.readstring?view=netframework-4.8
         * - https://docs.microsoft.com/en-us/dotnet/api/system.io.binarywriter.write?view=netframework-4.8#System_IO_BinaryWriter_Write_System_String_
         * 
         */

        public int FileID
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    return reader.ReadInt32();
                }
            }
        }

        public byte[] SHA512Hash
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    reader.BaseStream.Seek(4, SeekOrigin.Begin);
                    return reader.ReadBytes(64);
                }
            }
        }

        public long FileSize
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    reader.BaseStream.Seek(68, SeekOrigin.Begin);
                    return reader.ReadInt64();
                }
            }
        }

        public int MaxBlockSize
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    reader.BaseStream.Seek(76, SeekOrigin.Begin);
                    return reader.ReadInt32();
                }
            }
        }

        public int BlockCount
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    reader.BaseStream.Seek(80, SeekOrigin.Begin);
                    return reader.ReadInt32();
                }
            }
        }

        public string FileName
        {
            get
            {
                using (var reader = base.GetPayloadBufferReader())
                {
                    reader.BaseStream.Seek(84, SeekOrigin.Begin);
                    return reader.ReadString();
                }
            }
        }

        public ReturnFileInfoPacket(int fileID, byte[] sha512Hash, long fileSize, int maxBlockSize, int blockCount, string fileName) : base((byte)FileServerOpCode.ReturnFileInfo)
        {
            using (var writer = base.GetPayloadBufferWriter())
            {
                writer.Write(fileID);
                writer.Write(sha512Hash);
                writer.Write(fileSize);
                writer.Write(maxBlockSize);
                writer.Write(blockCount);
                writer.Write(fileName);
            }
        }

        public ReturnFileInfoPacket(UdpPacket udpPacket) : base(udpPacket)
        {

        }
    }
}
