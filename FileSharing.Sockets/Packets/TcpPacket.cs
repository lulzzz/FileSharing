using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Sockets.Packets
{
    public class TcpPacket : IDisposable
    {
        /*
         * "TCP Packet" at application level.
         * 
         * Below are bytes will be written to TCP stream at transport layer.
         *  ____________________
         * |___OpCode: 1 byte___|
         * |                    |
         * |   PayloadLength    | Header: 5 bytes
         * |       4 bytes      |
         * |____________________|________________
         * |                    |
         * |                    |
         * |       Payload      |
         * |                    |
         * |        ...         |
         * |____________________|
         * 
         *  Header = OpCode + PayloadLength = 5 bytes
         *      OpCode          [0, 255]
         *      PayloadLength   [0, Int32.MAX]
         *  
         *  Payload: byte[]
         */

        public const int HeaderLength = sizeof(byte) + sizeof(int); // 5 bytes

        public byte PacketType { get; set; }

        public int PayloadLength
        {
            get
            {
                return (int)this.PayloadBuffer.Length;
            }
        }

        public MemoryStream PayloadBuffer { get; }

        public int PacketLength
        {
            get
            {
                return HeaderLength + this.PayloadLength;
            }
        }

        public TcpPacket(byte packetType)
        {
            this.PacketType = packetType;
            this.PayloadBuffer = new MemoryStream();
        }

        public TcpPacket(byte[] packetBytes)
        {
            this.PayloadBuffer = new MemoryStream();

            using (var reader = new BinaryReader(new MemoryStream(packetBytes), Encoding.UTF8, false))
            {
                this.PacketType = reader.ReadByte();
                var payloadLength = reader.ReadInt32();
                var payloadBytes = reader.ReadBytes(payloadLength);
                this.PayloadBuffer.Write(payloadBytes, 0, payloadLength);
            }
        }

        public TcpPacket(byte packetType, byte[] payload)
        {
            this.PacketType = packetType;
            this.PayloadBuffer = new MemoryStream();
            PayloadBuffer.Write(payload, 0, payload.Length);
        }

        public TcpPacket(TcpPacket tcpPacket)
        {
            this.PacketType = tcpPacket.PacketType;
            this.PayloadBuffer = new MemoryStream();
            tcpPacket.PayloadBuffer.WriteTo(this.PayloadBuffer);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[this.PacketLength];
            using (var writer = new BinaryWriter(output: new MemoryStream(bytes), encoding: Encoding.UTF8, leaveOpen: false))
            {
                writer.Write(this.PacketType);
                writer.Write(this.PayloadLength);
                writer.Write(this.PayloadBuffer.ToArray());
            }

            return bytes;
        }

        public byte[] GetPayloadBytes()
        {
            return this.PayloadBuffer.ToArray();
        }

        public BinaryReader GetPayloadBufferReader()
        {
            this.PayloadBuffer.Seek(0, SeekOrigin.Begin);
            return new BinaryReader(this.PayloadBuffer, Encoding.UTF8, true);
        }

        public BinaryWriter GetPayloadBufferWriter()
        {
            this.PayloadBuffer.Seek(this.PayloadBuffer.Length, SeekOrigin.Begin);
            return new BinaryWriter(this.PayloadBuffer, Encoding.UTF8, true);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                this.PayloadBuffer.Close();
                this.PayloadBuffer.Dispose();
            }

            this.disposed = true;
        }
    }
}
