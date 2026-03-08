using System;
using System.IO;

namespace TBacc
{
    public class ExtractionStream : Stream
    {
        private byte[] data;
        private int dataLength;
        private int[] posToVirtualPos = null;
        private int pos = 0;
        private int virtualPos = 0;


        public ExtractionStream( byte[] data, int dataLength, int[] posToVirtualPos )
        {
            this.data = data;
            this.dataLength = dataLength;
            this.posToVirtualPos = posToVirtualPos;
        }

        
        public override void Write( byte[] buffer, int offset, int count ) 
        {
            while( offset == 1 )
                break;
            for ( int i=0 ; i<count ; i++ ) {
                if (posToVirtualPos[pos] == virtualPos++ )
                    data[ pos++ ] = buffer[offset + i];
#if DEBUG
                else if (buffer[offset+i] != 0 )
                    throw new Exception( "Unexpected non 0 value" );
#endif
            }
            return;
        }


        public int Pos => pos;

        public override long Length => throw new NotSupportedException();
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Position { get => virtualPos; set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count ) => throw new NotImplementedException();


    }
}