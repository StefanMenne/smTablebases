using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class MyTaskInit : MyTaskPieces
	{
		private DataChunkWrite data;


		public MyTaskInit( CalcTB calc, WkBk wkBk, Pieces pieces, bool wtm ) : base( calc, wkBk, pieces, wtm )
		{
		}


		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			data = calc.TaBasesWrite.GetDataChunk( wkBk, wtm, true, true, false );
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			IndexPos pos = new IndexPos( wkBk, Pieces, wtm );
			long count = pos.IndexCount;

#if DEBUG
			for ( long i=0 ; i<count ; i++ ) {
				pos.SetToIndex( i );
				data.SetDebug( pos, i, ResWithCount.Init.Value, "InitTB", null, VerifyResType.DontVerify );
			}
#else
			data.Fill( count, ResWithCount.Init.Value );
#endif
		}


		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			calc.TaBasesWrite.FreeDataChunk( data );
		}
	}
}
