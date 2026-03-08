using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class BitReader
	{
		private byte[] buffer;
		private int    bufferPos;
		private ulong  currentBits;
		private int    currentBitCount;    // always:    0 <= currentBitCount < 40 


		public BitReader( byte[] buffer, int startReadOffset = 0 )
		{ 
			this.buffer               = buffer;
			this.bufferPos            = startReadOffset;
			this.currentBits          = 0UL;
			this.currentBitCount     = 0;
		}

		
		public void Seek( int bitCount )
		{
			LoadBits( bitCount );
			SeekBits( bitCount );
		}


		public uint Get( int bitCount )
		{
			LoadBits( bitCount );
			uint val = (uint)(currentBits>>(currentBitCount-bitCount));
			SeekBits( bitCount );
			return val;
		}


		public uint Peek( int bitCount )
		{
			LoadBits( bitCount );
			uint val = (uint)(currentBits>>(currentBitCount-bitCount));
			return val;
		}


		private void SeekBits( int bitCount )
		{
			currentBitCount  -= bitCount;
			//currentBits &= 0xffffffffffffffffUL>>(64-currentBitsCount);      Problem: >>64 not defined
			currentBits &= 0x00ffffffffffffffUL>>(56-currentBitCount);
		}


		private void LoadBits( int count )
		{
			while ( count > currentBitCount ) {
				currentBits      = (currentBits<<8) | buffer[bufferPos++];
				currentBitCount += 8; 
			}
		}

	}
}
