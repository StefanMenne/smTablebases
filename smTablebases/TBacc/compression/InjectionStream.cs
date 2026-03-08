using System;
using System.Collections.Generic;
using System.Text;

namespace TBacc
{
    public class InjectionStream : Stream
    {
        private byte[] data;
        private int dataLength;
        private int[] posToVirtualPos = null;
        private int pos = 0;
        private int virtualPos = 0;


        public InjectionStream( byte[] data, int dataLength, int[] posToVirtualPos )
        {
            this.data = data;
            this.dataLength = dataLength;
            this.posToVirtualPos = posToVirtualPos;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;

            while ( bytesRead < count && pos < dataLength ) {
                if ( posToVirtualPos[pos] == virtualPos++ )
                    buffer[offset+bytesRead++] = data[pos++];
                else 
                    buffer[offset + bytesRead++] = 0;

            }
            return bytesRead;
        }

        public override long Length => posToVirtualPos[dataLength-1] + 1;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
