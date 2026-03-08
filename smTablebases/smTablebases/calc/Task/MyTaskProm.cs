using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public abstract class MyTaskProm : MyTaskCapOrProm
	{
		// Example for backward processing:
		//
		// 1. Dst:                   KQK
		// 2. Mirror:                KQK
		// 3. Side switch:           KKQ
		// 4. Remove promoted piece: KK
		// 5. Insert pawn:           KKP
		// 6. Insert captured piece: KQKP
		// 7. Src:                   KQKP
		//

		protected   int     addPawnIndex                 = -1;
		protected   int     promotedPieceIdxFirst        = -1;
		protected   int     promotedPieceIdxLastPlusOne  = -1;

		protected DataChunkWrite    dataSrc;
		protected DataChunkRead     dataDst;


		public MyTaskProm( CalcTB calc, WkBk wkBkSrc, Pieces piecesSrc, bool wtm  ) : base( calc, wkBkSrc, piecesSrc, wtm )
		{
		}



		public int AddPawnIndex
		{
			get{ return addPawnIndex; }
		}

		public int PromotedPieceIdxFirst
		{
			get{ return promotedPieceIdxFirst; }
		}

		public int PromotedPieceIdxLastPlusOne
		{
			get{ return promotedPieceIdxLastPlusOne; }
		}

		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			dataSrc = calc.TaBasesWrite.GetDataChunk( wkBkSrc, wtm, true, false );
			dataDst = calc.TaBasesRead.GetDataChunk( piecesDst, wkBkDst, !wtm^sideSwitchNeeded );
		}


		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			calc.TaBasesWrite.FreeDataChunk( dataSrc );
			calc.TaBasesRead.FreeDataChunk( dataDst );
		}










	}
}
