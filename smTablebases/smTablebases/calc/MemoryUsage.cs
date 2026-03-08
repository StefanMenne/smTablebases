using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class MemoryUsage
	{
		public static void Show()
		{
			long[] maxMemoryUsage = new long[Config.MaxNonKMen+1];

			Message.Line( "Amount of memory(RAM or HD if RAM is not sufficient) needed during calculation." );

			for ( int i=1 ; i<Pieces.Count ; i++ ) {
				Pieces p = Pieces.FromIndex( i );
				long memUsage = GetMinMemoryUsage( p );
				maxMemoryUsage[p.PieceCount] = Math.Max( maxMemoryUsage[p.PieceCount], memUsage );
				Message.Line( p.ToString().PadRight(10) + "  Bits: " + TbInfoFileList.Get(p).GetBitsPerResWtm() + "/" + TbInfoFileList.Get(p).GetBitsPerResBtm() + "    " + Tools.LongToKiloMegaGiga(memUsage) + "B");
			}

			Message.Line( "" );
			for ( int i=0 ; i<maxMemoryUsage.Length ; i++ ) {
				Message.Line( "Max " + (i+2).ToString() + "-men     " + Tools.LongToKiloMegaGiga(maxMemoryUsage[i]) + "B" );
			}

		}


		private static long GetMinMemoryUsage( Pieces p )
		{
			long bytes = 0;
			if ( p.Index == 0 )
				return 0;
			foreach ( bool wtm in Tools.BoolArray ) {
				for ( WkBk wkbk=WkBk.First(p) ; wkbk<wkbk.Count ; wkbk++ ) {
					IndexPos ip = new IndexPos( wkbk, p, wtm );
					bytes += DataChunkWrite.IndexCountToByteCount( ip.IndexCount, false, TbInfoFileList.Get(p).GetBitsPerRes(wtm) );
				}
			}
			return bytes;
		}
	}
}
