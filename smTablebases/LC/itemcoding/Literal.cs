using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class Literal
	{
		private byte[] data;
		private int    bytesPerItem;
		private int    literalPosBits, lastByteHighBits;


		public Literal( byte[] data, int bytesPerItem, int literalPosBits, int lastByteHighBits )
		{
			this.data             = data;
			this.bytesPerItem     = bytesPerItem;
			this.literalPosBits   = literalPosBits;
			this.lastByteHighBits = lastByteHighBits;
		}


		public int GetContextIndex( int dataPos )
		{
			if ( dataPos == 0 )
				return 0;
			else
				return (dataPos&((1<<literalPosBits)-1)) | (((int)data[dataPos-1])>>(8-lastByteHighBits)<<literalPosBits);
		}


		public int ContextCount
		{
			get { return (1<<(literalPosBits+lastByteHighBits)); }
		}


		public int Bits
		{
			get { return (bytesPerItem==2) ? 16 : 8; }
		}

		

		public void WriteToData( int dataPos, int literal )
		{
			if ( Bits == 16 ) {
				data[2*dataPos+0] = (byte)(literal>>8);
				data[2*dataPos+1] = (byte)(literal&255);				
			}
			else {
				data[bytesPerItem*dataPos] = (byte)literal;
			}
		}

		
		public int GetFromData( int dataPos )
		{
			if ( Bits == 16 ) {
				return (((int)data[2*dataPos+0])<<8) | ((int)data[2*dataPos+1]);
			}
			else {
				return data[bytesPerItem*dataPos];
			}
		}
	}
}
