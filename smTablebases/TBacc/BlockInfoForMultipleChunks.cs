using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public class BlockInfoForMultipleChunks : BlockInfo
	{
		public BlockInfoChunk[] Chunks;


		public BlockInfoChunk GetChunkInfo( WkBk wkBk, bool wtm )
		{
			for ( int i=0 ; i<Chunks.Length ; i++ ) {
				if ( Chunks[i].WkBk == wkBk && Chunks[i].Wtm == wtm )
					return Chunks[i];
			}
			throw new Exception();
		}  
	}
}
