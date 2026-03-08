using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class MyTaskCapK : MyTaskCapOrProm
	{
		private DataChunkWrite    dataSrc;
		private DataChunkRead     dataDst;


		public MyTaskCapK( CalcTB calc, WkBk wkBkSrc, Pieces piecesSrc, bool wtm, int capIndex, Field kDestWithoutMirror ) : base( calc, wkBkSrc, piecesSrc, wtm )
		{
			this.firstCapIndex      = capIndex;
			piecesDst                 = piecesSrc.RemovePiece( capIndex );                   // remove captured piece

			Field wkNew = wtm ? kDestWithoutMirror : wkBkSrc.Wk;
			Field bkNew = wtm ? wkBkSrc.Bk : kDestWithoutMirror;

			this.wkBkDst               = new WkBk( wkNew, bkNew, piecesDst );
			this.kDestWithoutMirror    = kDestWithoutMirror;
			this.mirror                = MirrorNormalize.WkBkToMirror( wkNew, bkNew, piecesDst );

			Init();
		}

		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			dataSrc              = calc.TaBasesWrite.GetDataChunk( wkBkSrc, wtm, true, false );
			dataDst              = calc.TaBasesRead.GetDataChunk( piecesDst, wkBkDst, wtmDst );
		}

		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			calc.TaBasesWrite.FreeDataChunk(dataSrc);
			calc.TaBasesRead.FreeDataChunk(dataDst);
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			Do( PiecesSrc, Wtm, wkBkSrc, wkBkDst, piecesDst, firstCapIndex, kDestWithoutMirror, mirror, sideSwitchNeeded, threadIndex );
		}

		public void Do( Pieces piecesSrc, bool wtm, WkBk wkBkSrc, WkBk wkBkDst, Pieces piecesDst, int firstCapIndex, Field kDestWithoutMirror, MirrorType mirror, bool sideSwitchNeeded, int threadIndex )
		{
			IndexPos      indexPosSrc          = new IndexPos( wkBkSrc, piecesSrc, wtm );
			IndexPos      indexPosDst          = new IndexPos( wkBkDst, piecesDst, !wtm^sideSwitchNeeded );
			long          count                = indexPosSrc.IndexCount;
			int           pieceGrpIdx            = indexPosSrc.GetPieceGrpIdx( firstCapIndex );
			LoadDataChunk( calc.TaBasesRead, dataDst, threadIndex );

			if ( !indexPosSrc.SetToFirstWithOccField( pieceGrpIdx, kDestWithoutMirror ) )
				return;               // cap not possible; e.g. pawn cannot stand on first line

			do {
				long index = indexPosSrc.GetIndex();

				if ( !indexPosSrc.GetIsEp(index) ) {
#if DEBUG
					ResWithCount resWithCountSrc = new ResWithCount( dataSrc.GetDebug(index,"CapK",null) );
#else
					ResWithCount resWithCountSrc = new ResWithCount( dataSrc.Get(index) );
#endif

					Res          resSrc          = resWithCountSrc.Res;
					Fields f = indexPosSrc.GetFields();
					int capIndex = firstCapIndex;
					while ( f.Get(capIndex) != kDestWithoutMirror )
						capIndex++;

					f = f.Remove( capIndex );
					if ( sideSwitchNeeded )
						f = f.SwitchSides( indexPosDst.CountW, indexPosDst.CountB );
					f = f.Mirror( mirror );
					if ( indexPosDst.SetFields( f ) ) {
						Res res = dataDst.Get( indexPosDst.GetIndex() );
						if ( !res.IsIllegalPos ) {
							res = res.HalfMoveAwayFromMate;
							if ( res < resSrc ) {
#if DEBUG
								dataSrc.SetDebug( indexPosSrc, index, new ResWithCount(res).Value, "CapK ", indexPosDst, VerifyResType.DontVerify );
#else
								dataSrc.Set( index, new ResWithCount(res).Value );
#endif
							}
						}
					}
				}
			} while ( indexPosSrc.NextIndexWithOccField( pieceGrpIdx, kDestWithoutMirror ) );



		}
	}
}
