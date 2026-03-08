using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public class BlockInfoChunk
	{
		public bool   Wtm;
		public WkBk   WkBk;
		public int    ByteOffset;

		public override string ToString()
		{
			return WkBk.ToString() + (Wtm?" wtm ":" btm ") + "ByteOffset=" + ByteOffset.ToString();
		}
	}
}
