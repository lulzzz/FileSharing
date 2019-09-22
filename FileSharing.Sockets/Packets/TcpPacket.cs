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
         *  ____________________
         * |___OpCode: 1 byte___|
         * |                    |
         * |   PayloadLength    |
         * |       4 bytes      |
         * |____________________|
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

        public const int HeaderLength = sizeof(byte) + sizeof(int);

        private byte type;

        private MemoryStream buffer;

        private int length;

        public byte PacketType
        {
            get => this.type;
            set
            {
                this.type = value;
                long currentPossition = this.buffer.Position;

                this.buffer.Position = 4;
                this.buffer.WriteByte(this.type);

                this.buffer.Position = currentPossition;                
            }
        }

        public int Length
        {
            get
            {
                this.UpdatePacketLengthField();
                return this.length;
            }
            private set => this.length = value;
        }

        public TcpPacket(byte type, int payloadLength = 0)
        {
            this.type = type;
            int packetLength = HeaderLength + payloadLength;

            this.buffer = new MemoryStream(packetLength);
            this.buffer.Write(BitConverter.GetBytes(payloadLength), 0, 4);
            this.buffer.WriteByte(type);

            this.length = packetLength;
        }

        public byte[] GetBytes()
        {
            this.UpdatePacketLengthField();
            return this.buffer.ToArray();
        }

        public BinaryWriter GetWriter()
        {
            return new BinaryWriter(this.buffer, Encoding.UTF8, true);
        }

        private void UpdatePacketLengthField()
        {
            if (this.length == (int)this.buffer.Position)
                return;

            this.length = (int)this.buffer.Position;

            // Update content-length.
            int packetLength = this.length - HeaderLength;
            this.buffer.Seek(0, SeekOrigin.Begin);
            this.buffer.Write(BitConverter.GetBytes(this.length), 0, 4);
            this.buffer.Seek(this.length, SeekOrigin.Begin);
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
                this.length = -1;
                this.buffer.Close();
                this.buffer.Dispose();
            }

            this.disposed = true;
        }
    }
}
