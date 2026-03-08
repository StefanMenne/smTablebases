using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public sealed class DataChunkMemoryRead : DataChunkMemory, IComparable<DataChunkMemoryRead>
	{
		private byte[] arrayXBitMemory  = null;


		public DataChunkMemoryRead( long countBytes )
		{
			arrayXBitMemory = new byte[countBytes];
		}	

		
		public byte[] Memory
		{
			get{ return arrayXBitMemory; }
		}


		public int CompareTo( DataChunkMemoryRead other )
		{
			if ( UsingCount == other.UsingCount )    // compare first by using count; unused first;
				return LastJoin.CompareTo( other.LastJoin );
			else
				return UsingCount.CompareTo( other.UsingCount );
		}

	}

}
