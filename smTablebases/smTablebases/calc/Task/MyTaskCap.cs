using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class MyTaskCap : MyTaskCapOrProm
	{
		private DataChunkWrite    dataSrc;
		private DataChunkRead     dataDst;


		public MyTaskCap( CalcTB calc, WkBk wkBkSrc, Pieces piecesSrc, bool wtm, int capIndex  ) : base( calc, wkBkSrc, piecesSrc, wtm )
		{
			this.firstCapIndex      = capIndex;
			piecesDst                 = piecesSrc.RemovePiece( capIndex );     // remove piece

			wkBkDst                 = new WkBk( wkBkSrc.Wk, wkBkSrc.Bk, piecesDst );
			mirror                  = MirrorNormalize.WkBkToMirror( wkBkSrc.Wk, wkBkSrc.Bk, piecesDst );
			kDestWithoutMirror      = wkBkSrc.K(wtm);

			Init();
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			IndexPos      indexPosSrc          = new IndexPos( wkBkSrc, PiecesSrc, wtm);
			IndexPos      indexPosDst          = new IndexPos( wkBkDst, piecesDst, wtmDst );
			long          count                = indexPosDst.IndexCount;
			long[]        mv                   = new long[indexPosSrc.GetMvCountBound()];
			LoadDataChunk( calc.TaBasesRead, dataDst, threadIndex );


			indexPosDst.SetToIndex( 0 );
			Fields fields = indexPosDst.GetFields();
			long lastIndex = 0;

			for ( long i=0 ; i<count&&!Calc.Abort ; i++ ) {
				if ( !indexPosDst.GetIsEp(i) ) {
					Res resDst = dataDst.Get(i);

					if ( !resDst.IsIllegalPos ) {
						resDst = resDst.HalfMoveAwayFromMate;
						indexPosDst.ChangeIndex( (int)(i-lastIndex), ref fields );
						lastIndex = i;

						for ( int mvPieceIdxSrc=PiecesSrc.FirstPiece(wtm) ; mvPieceIdxSrc<PiecesSrc.LastPiecePlusOne(wtm) ; mvPieceIdxSrc++ ) {
							Fields flds = fields;

							flds = flds.MirrorBack( mirror );    // side switch or promotion to non Pawn pos(WkBk from 1806 to 462) might cause mirror
							if ( sideSwitchNeeded ) {
								flds = flds.SwitchSides( indexPosDst.CountB, indexPosDst.CountW );
							}
							BitBrd bb = wkBkSrc.Wk.AsBit|wkBkSrc.Bk.AsBit|flds.GetBitBoard(indexPosDst.Count);
							flds = flds.Insert( firstCapIndex, flds.Get( ((firstCapIndex<mvPieceIdxSrc)?mvPieceIdxSrc-1:mvPieceIdxSrc ) ) );   // Set on current=dst position of moving piece
							int pieceGrpIdxSrc = indexPosSrc.GetPieceGrpIdx( mvPieceIdxSrc );
							int mvCount = indexPosSrc.GetBackMvCapDestIndex( flds, mv, wtm, mvPieceIdxSrc, pieceGrpIdxSrc, bb );

							for ( int j=0 ; j<mvCount ; j++ ) {
								long indexSrc = mv[j];
#if DEBUG
								ResWithCount resWithCountSrc = new ResWithCount( dataSrc.GetDebug(indexSrc,"TaskCap",null) );
#else
								ResWithCount resWithCountSrc = new ResWithCount( dataSrc.Get(indexSrc) );
#endif

								Res          resSrc          = resWithCountSrc.Res;
								if ( resDst < resSrc ) {
#if DEBUG
									dataSrc.SetDebug( indexPosSrc, indexSrc, new ResWithCount(resDst).Value, "Cap", indexPosDst, VerifyResType.VerifyAlways );
#else
									dataSrc.Set( indexSrc, new ResWithCount(resDst).Value );
#endif
								}
							}
						}
					}
				}
			}
		}


		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			dataSrc              = calc.TaBasesWrite.GetDataChunk( wkBkSrc, wtm, true, false );
			dataDst              = calc.TaBasesRead.GetDataChunk( piecesDst, wkBkDst, !wtm^sideSwitchNeeded );
		}


		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			calc.TaBasesWrite.FreeDataChunk(dataSrc);
			calc.TaBasesRead.FreeDataChunk(dataDst);
		}



		public static void DoFinalize()
		{
		}




	}
}
