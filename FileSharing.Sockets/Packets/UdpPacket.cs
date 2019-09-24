using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharing.Sockets.Packets
{
    public class UdpPacket : IDisposable
    {
        public const int MaxUDPSize = 65_000;

        /*
         * "UDP Packet" at application level.
         * 
         * Below is UDP Datagram's data.
         *  ____________________
         * |___OpCode: 1 byte___|
         * |                    |
         * |                    |
         * |       Payload      |
         * |                    |
         * |        ...         |
         * |____________________|
         * 
         * C# Socket receives only one UDP Datagram at a time.
         * 
         */

        public const int PacketTypeSize = sizeof(byte);

        public byte PacketType { get; set; }

        public MemoryStream PayloadBuffer { get; }

        public int PacketLength
        {
            get => PacketTypeSize + (int)PayloadBuffer.Length;
        }

        public int PayloadLength
        {
            get => (int)this.PayloadBuffer.Length;
        }

        public UdpPacket(byte packetType)
        {
            this.PacketType = packetType;
            this.PayloadBuffer = new MemoryStream();
        }

        public UdpPacket(byte[] packetBytes)
        {
            this.PacketType = packetBytes[0];

            this.PayloadBuffer = new MemoryStream();
            this.PayloadBuffer.Write(buffer: packetBytes, offset: 1, count: packetBytes.Length - 1);            
        }

        public UdpPacket(byte packetType, byte[] payload)
        {
            this.PacketType = packetType;

            this.PayloadBuffer = new MemoryStream();
            this.PayloadBuffer.Write(payload, 0, payload.Length);
        }

        public UdpPacket(UdpPacket udpPacket)
        {
            this.PacketType = udpPacket.PacketType;
            udpPacket.PayloadBuffer.WriteTo(this.PayloadBuffer);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[PacketLength];

            bytes[0] = this.PacketType;
            this.PayloadBuffer.ToArray().CopyTo(array: bytes, index: 1);

            return bytes;
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
