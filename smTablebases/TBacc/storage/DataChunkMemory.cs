using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public class DataChunkMemory
	{
#if DEBUG
		public          int             Index;
#endif
		private         DataChunk       dataChunk      = null;
		private         int             usingCount     = 0;
		public          long            LastJoin       = -1;     // increasing value to identify the oldest entry to be reused

		public bool DataChunkJoined
		{
			get{ return dataChunk != null; }
		}

		public DataChunk DataChunk
		{
			get{ return dataChunk; }
			set{ dataChunk = value; }
		}

		public int UsingCount
		{
			get{ return usingCount; }
			set{ usingCount = value; }
		}


		public override string ToString()
		{
			string s = "LastJoin: " + LastJoin.ToString() + "   ";
			if ( DataChunkJoined ) {
				if ( usingCount==0 ) 
					s += "Unused: " + dataChunk.ToString();
				else
					s += "In use: " + dataChunk.ToString();
			}
			else
				s += "Empty";
			return s;
		}
	}
}
