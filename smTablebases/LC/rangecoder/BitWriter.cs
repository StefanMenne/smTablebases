using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class BitWriter
	{
		private byte[] buffer;
		private int    bufferPos;
		private ulong  currentBits;
		private int    addBitsIndex;
		private int    firstByte;


		public BitWriter( byte[] buffer, int startWriteOffset = 0 )
		{ 
			this.firstByte            = startWriteOffset;
			this.buffer               = buffer;
			this.bufferPos            = startWriteOffset;
			this.currentBits          = 0;
			this.addBitsIndex         = 64;
		}


		/// <summary>
		/// Add up to 32 Bits
		/// </summary>
		public void AddBits( uint value, int countBits )
		{
			addBitsIndex      -= countBits;
			currentBits       |= ((ulong)value) << addBitsIndex;

			while ( addBitsIndex <= 56 ) {
				buffer[bufferPos++]    = (byte)(currentBits>>56);
				addBitsIndex          += 8;
				currentBits          <<= 8;
			}
		}


		public int CurrentBytePos
		{
			get {  return bufferPos; }
		}

		/// <summary>
		/// returns number of written bytes (including up to 7 padding bits)
		/// </summary>
		public int Close()
		{
			if ( addBitsIndex != 64 )
				buffer[bufferPos++] = (byte)(currentBits>>56);
			return bufferPos - firstByte;
		}
	}
}
