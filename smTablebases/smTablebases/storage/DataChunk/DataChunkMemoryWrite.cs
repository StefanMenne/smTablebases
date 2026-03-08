using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using TBacc;


namespace smTablebases
{
	public sealed class DataChunkMemoryWrite : DataChunkMemory
	{
		private         bool        writeData             = false;
		private         bool        writeFastBit          = false;
		private         uint[]      data                  = null;
		private         long[]      fastBit               = null;

		public DataChunkMemoryWrite( int countItems1, int bitCount1, int countItems2, int bitCount2 )
		{
			int intCount1 = DataChunkWrite.IndexCountToByteCountData(countItems1,bitCount1)/4;
			int intCount2 = DataChunkWrite.IndexCountToByteCountData(countItems2,bitCount2)/4;
			data         = new uint[Math.Max(intCount1,intCount2)];
			int longCount1 = (int)(DataChunkWrite.IndexCountToByteCountFastBit(countItems1)/8);
			int longCount2 = (int)(DataChunkWrite.IndexCountToByteCountFastBit(countItems2)/8);
			fastBit = new long[Math.Max(longCount1,longCount2)];
		}	
		
		public bool WriteData
		{
			get{ return writeData; }
			set{ writeData = value; }
		}

		public bool WritePotentialNew
		{
			get { return writeFastBit; }
			set { writeFastBit = value; }
		}


		public uint[] Data
		{
			get{ return data; }
		}

		public long[] FastBit
		{
			get{ return fastBit; }
		}

		public bool UseFastBit
		{
			get{ return fastBit != null; }
		}

	}

}
