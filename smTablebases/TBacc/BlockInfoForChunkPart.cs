using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public class BlockInfoForChunkPart : BlockInfo
	{
		public WkBk WkBk;
		public bool Wtm;
		public long FirstDataChunkIndex;
		public long DataChunkIndexCount;
	}
}
