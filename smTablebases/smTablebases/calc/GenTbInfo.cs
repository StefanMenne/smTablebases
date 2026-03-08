using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class GenTbInfo
	{
		public static void Gen( CalcTB calc, int threadIndex )
		{
			Pieces pieces = calc.Pieces;
			long[] resCount        = new long[Res.MaxValue+1];
			long[] resMoveCount = new long[Res.MaxValue+1];
			Progress.Max = WkBk.GetCount( pieces ).Index;

			foreach( bool wtm in Tools.BoolArray ) {
				for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
					Progress.Value = wkBk.Index;
					IndexPos pos = new IndexPos( wkBk, pieces, wtm );
					DataChunkWrite data = (DataChunkWrite)calc.TaBasesWrite.GetDataChunk( wkBk, wtm, false, false );
					long count = pos.IndexCount;

					for ( long i=0 ; i<count ; i++ ) {
						ResWithCount res = new ResWithCount( data.Get(i) );
						if ( res.IsUnknown )
							resMoveCount[res.Res.Value]++;
						else
							resCount[res.Res.Value]++;
					}
					calc.TaBasesWrite.FreeDataChunk( data );
				}
			}
			for ( int resInt=0 ; resInt<=Res.MaxValue ; resInt++ ) {
				if ( resCount[resInt] != 0 || resMoveCount[resInt] != 0 ) {
					Message.Line( string.Format("{0,6}", new Res(resInt).AsInt )  + " " + string.Format("{0,15:#,###,###,###,###,##0}",resCount[resInt]) + "   " + string.Format("{0,15:#,###,###,###,###,##0}",resMoveCount[resInt]) );
				}
			}

		}
	}
}
