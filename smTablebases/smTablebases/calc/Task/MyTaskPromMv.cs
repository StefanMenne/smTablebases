using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class MyTaskPromMv : MyTaskProm
	{

		public MyTaskPromMv( CalcTB calc, WkBk wkBkSrc, Pieces piecesSrc, bool wtm, Piece prom ) : base( calc, wkBkSrc, piecesSrc, wtm )
		{
			PromPiece                      = prom;
			addPawnIndex                 = wtm?(piecesSrc.CountW-1):(piecesSrc.PieceCount-1);
			piecesDst                      = piecesSrc.RemovePiece( addPawnIndex );     // remove pawn
			promotedPieceIdxFirst          = promPieceIndex       = piecesDst.GetIndexToAdd( wtm, prom );               // index where the promoted piece is Q,R,B,N
			piecesDst                      = piecesDst.Add( wtm, prom );         // insert new piece
			promotedPieceIdxLastPlusOne    = promotedPieceIdxFirst + piecesDst.GetPieceCount( promotedPieceIdxFirst );
			wkBkDst                      = new WkBk( wkBkSrc.Wk, wkBkSrc.Bk, piecesDst );
			mirror                       = MirrorNormalize.WkBkToMirror( wkBkSrc.Wk, wkBkSrc.Bk, piecesDst );
			kDestWithoutMirror           = wkBkSrc.K(wtm);

			Init();
		}


		/// <summary>
		///
		///   Example:
		///
		///
		///   .......QR       piecesSrc:     KQRKP
		///   ........p       wtm:           false
		///   .........       wkBkSrc:       (a4,a2)
		///   .........       capIndex:      0
		///   K........
		///   .........
		///   k........
		///   .........
		///
		///     ||    Remove captured piece
		///     \/
		///
		///   ........R       KRKP
		///   ........p       addPawnIdx:     1          // needed for backward steps to add pawn (KRK->KRKP); valid after removing of captured piece
		///   .........
		///   .........
		///   K........
		///   .........       promPieceIdx:    1          // where to add promoted piece (Q; KRK->KRKQ)
		///   k........       promPieceLastIdxPlusOne: 2  // if black already had a Q it would be 3. When generating back moves both could be captured
		///   .........
		///
		///     ||    Promotion
		///     \/
		///
		///    ......qR
		///    ........
		///    ........
		///    ........
		///    K.......
		///    ........
		///    k.......
		///    ........
		///
		///     ||    switch sides
		///     \/
		///
		///    ......Qr
		///    ........
		///    ........
		///    ........
		///    k.......
		///    ........
		///    K.......
		///    ........
		///
		///     ||    mirror
		///     \/
		///
		///    .......r        piecesDst: KQKR
		///    .......Q        wkBkDst: (b1,d1)
		///    ........
		///    ........
		///    ........
		///    ........
		///    ........
		///    .K.k....
		///
		///
		///
		///
		///
		///   promPieceIndex:
		///
		/// </summary>
		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			List<int>         mv                   = new List<int>();
			IndexPos          indexPosSrc          = new IndexPos( wkBkSrc, PiecesSrc, wtm);
			IndexPos          indexPosDst          = new IndexPos( wkBkDst, piecesDst, wtmDst );
			long              count                = indexPosDst.IndexCount;   // ep pos not necessary due last move was promotion
			BitBrd            pawnPromFlds          = BitBrd.PawnPromFields( wtm ).Mirror( mirror );
			int               promPieceGroupIdx      = indexPosDst.GetPieceGrpIdx( PromPiece, !indexPosDst.Wtm );
			LoadDataChunk( calc.TaBasesRead, dataDst, threadIndex );


			OccFldsIndexEnumerator indexEnum = new OccFldsIndexEnumerator( indexPosDst, promPieceGroupIdx, pawnPromFlds );
			long i = indexEnum.Index;

			do {
				if ( !indexPosDst.GetIsEp(i) ) {
					Res resDst = dataDst.Get(i);
					if ( !resDst.IsIllegalPos ) {
						resDst = resDst.HalfMoveAwayFromMate;
						Fields flds = indexPosDst.GetFields();
						flds = flds.MirrorBack( mirror );    // side switch or promotion to non Pawn pos(WkBk from 1806 to 462) might cause mirror
						if ( sideSwitchNeeded )
							flds = flds.SwitchSides( indexPosDst.CountB, indexPosDst.CountW );
						BitBrd occFld = flds.GetBitBoard( piecesDst.PieceCount );

						for ( int promotedPieceIdx=promotedPieceIdxFirst ; promotedPieceIdx<promotedPieceIdxLastPlusOne ; promotedPieceIdx++ ) {
							Field  dst = flds.Get( promotedPieceIdx );

							if ( dst.IsPawnPromLine(wtm) ) {
								Fields flds2 = flds;
								flds2 = flds2.Remove( promotedPieceIdx );          // remove promoted piece (Q,R,B or N)

								Field pawnSrc = dst.PawnBackMv(wtm);
								if ( (pawnSrc.AsBit & occFld).IsNotEmpty )
									continue;

								flds2 = flds2.Insert( addPawnIndex, pawnSrc );          // insert pawn
								if ( indexPosSrc.SetFields( flds2 ) ) {
									long indexSrc = indexPosSrc.GetIndex();
#if DEBUG
									ResWithCount resWithCountSrc = new ResWithCount( dataSrc.GetDebug(indexSrc,"PromMv",null) );
#else
									ResWithCount resWithCountSrc = new ResWithCount( dataSrc.Get(indexSrc) );
#endif
									Res          resSrc          = resWithCountSrc.Res;
									if ( resSrc > resDst ) {
										ResWithCount resWithCountSrcNew = new ResWithCount( resDst );
#if DEBUG
										dataSrc.SetDebug( indexPosSrc, indexSrc, resWithCountSrcNew.Value, "PromMv", null, VerifyResType.VerifyFinals );
#else
										dataSrc.Set( indexSrc, resWithCountSrcNew.Value );
#endif
									}
								}
							}
						}
					}
				}
			} while( (i = indexEnum.Next())!=-1 && !Calc.Abort );
		}
	}
}
