using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;


namespace smTablebases
{
	/// <summary>
	///
	///                   Q(1)  Q(2)    Q(n)                                                            Q'
	///
	///                    |     |       |                                                              |
	///                    |     |       |                                                              |   Pawn double step
	///                    |     |       |                                                              |
	///                   \|/   \|/     \|/                                                            \|/
	///
	///               P=Pos without EP option                                       P'= Pos P with additional EP option
	///
	///                    |     |       |                                                     /        |
	///                    |     |       |         --------------------------------------------         |   Cap move using EP option
	///                    |     |       |        /      P(1) ... P(k)                                  |
	///                   \|/   \|/     \|/     |_                                                     \|/
	///
	///                 P(1)   P(2) ...  P(k)                                                         P(k+1)
	///
	///
	///  1. P and P' have different indices
	///  2. Positions with EP option will never be mate by definition
	///  3. Positions with EP option are illegal if:
	///                            - Overlapping pieces
	///                            - EP option cannot be used e.g. check
	///                            - double step of pawn was not possible e.g. other piece is on origin or between src and dst
	///  4. InitEp_Cap function will:
	///                            - mark illegal EP positions
	///                            - Set move counter to 1; (move counter is not used due EP positions will get their results by transferring from non EP positions)
	///                            - P' is rated using already finished databases for P(k+1)
	///
	/// </summary>
	public static class EPcalc
	{
		public static bool EpCapAndInit( CalcTB calc )
		{
			//
			//                                 piecesSrc                                    piecesDst
			//             Dbl step pawn         wtm               cap                       !wtm
			//          ---------------------->   P      -----------------------------------> P'
			//

			Pieces piecesSrc = Settings.PiecesSrc;
			if ( !piecesSrc.ContainsWpawnAndBpawn )
				return false;

			Message.Text( piecesSrc.ToString() + " EP Cap" );
			Progress.Max = 2 * WkBk.GetCount(true).Index;

			foreach ( bool wtm in Tools.BoolArray ) {
				Pieces piecesDst = piecesSrc.RemovePiece( wtm ? (piecesSrc.PieceCount-1) : (piecesSrc.CountW-1) );
				bool sideSwitchNeeded = TaBasesWrite.IsDoubleTaBa( piecesDst );
				if (sideSwitchNeeded) {
#if DEBUG
					if ( wtm )
						throw new Exception();
#endif
					piecesDst = piecesDst.SwitchSides();
				}

				for ( WkBk wkBk = WkBk.First(calc.Pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
					CheckAndPin cpStm = new CheckAndPin( wkBk, calc.Pieces, wtm ), cpSntm = new CheckAndPin( wkBk, calc.Pieces, !wtm );
					Progress.Value = (wtm ? 0 : WkBk.GetCount(true).Index) + wkBk.Index;
					WkBk wkBkDst = sideSwitchNeeded ? (wkBk.Reverse().Mirror(MirrorType.MirrorOnHorizontal)) : wkBk;
					// if sideSwitchNeeded then wtm=false
					MirrorType mirrorForSideSwitch = MirrorType.MirrorOnHorizontal | MirrorNormalize.WkBkToMirror(wkBk.Bk.Mirror(MirrorType.MirrorOnHorizontal), wkBk.Wk.Mirror(MirrorType.MirrorOnHorizontal), piecesDst);
					TaBaRead    taBaReadDst                = calc.TaBasesRead.GetTaBa( piecesDst );
					IndexPos    indexPosPawnBesidePawnEp     = new IndexPos( wkBk, piecesSrc, wtm );
					IndexPos    indexPosPawnBesidePawnNoEp   = new IndexPos( wkBk, piecesSrc, wtm );
					bool        wtmAfterCap                = !wtm^sideSwitchNeeded;
					IndexPos    indexPosAfterCap           = new IndexPos( wkBkDst, piecesDst, wtmAfterCap );
					DataChunkWrite dataSrc                    = calc.TaBasesWrite.GetDataChunk( wkBk, wtm, true, false );
					DataChunkRead  dataDst                    = calc.TaBasesRead.GetDataChunk( piecesDst, wkBkDst, !wtm^sideSwitchNeeded );
					calc.TaBasesRead.LoadDataChunkSingleThreaded( dataDst );
					long count    = indexPosPawnBesidePawnEp.IndexCount;
					Pos  pos      = new Pos();



					for ( long i=0 ; i<count&&!Calc.Abort ; i++ ) {
#if DEBUG
						ResWithCount resSrc = new ResWithCount( dataSrc.GetDebug(i,"EPcalc",null) );
#else
						ResWithCount resSrc = new ResWithCount( dataSrc.Get(i) );
#endif

						if ( !indexPosPawnBesidePawnEp.GetIsEp(i) )
							continue;

						indexPosPawnBesidePawnEp.SetToIndex(i);

						Field epDblStepDst, epCapSrc;
						pos = Pos.FromIndexPosEp( indexPosPawnBesidePawnEp, out epDblStepDst, out epCapSrc );
						Fields fPawnBesidePawn = pos.Fields;


						if ( pos.GetIsValid(wtm,epDblStepDst,cpStm,cpSntm) && !indexPosPawnBesidePawnEp.IsRedundandEpPos() ) {
#if DEBUG
							if ( Config.DebugGeneral && !indexPosPawnBesidePawnNoEp.SetSortedFields(fPawnBesidePawn) )  // check weather pos without ep option is also indexable
								throw new Exception();
#endif

							Field epCapDest         = EP.GetCapDst(epDblStepDst);
							Field epDlbStepSrcField = EP.GetDblStepSrc(epDblStepDst);
							foreach ( bool firstSecondPawn in Tools.BoolArray ) {
								Field dummy1, dummy2;
								pos = Pos.FromIndexPosEp( indexPosPawnBesidePawnEp, out dummy1, out dummy2 );
								Field capSrc;
								if ( firstSecondPawn )
									capSrc = epCapSrc;
								else {
									if ( !EP.GetSecondCapSrcAvailable(epDblStepDst) )
										continue;
									capSrc = EP.GetSecondCapSrc(epDblStepDst,epCapSrc);
								}
								int capSrcPawnIndex = pos.FToPieceIndex(capSrc);
								if ( capSrcPawnIndex == -1 || pos.IsW(capSrcPawnIndex)!=wtm || !pos.GetPieceType(capSrcPawnIndex).IsP )
									continue;

								int pawnDblStepSntmIndex = pos.FToPieceIndex( epDblStepDst );
								pos.RemovePiece( pawnDblStepSntmIndex );
								int pawnStmIndex = pos.FToPieceIndex( capSrc );
								pos.SetPiecePos( pawnStmIndex, epCapDest );
								Fields f = pos.Fields;
								if ( sideSwitchNeeded ) {
									f = f.SwitchSides( piecesDst.CountW, piecesDst.CountB );
									f = f.Mirror( mirrorForSideSwitch );
								}

								if ( !indexPosAfterCap.SetFields( f ) )
									continue;
								Res resDst = dataDst.Get( indexPosAfterCap.GetIndex() );

								if ( !resDst.IsIllegalPos )
									resSrc = resSrc.Combine( resDst.HalfMoveAwayFromMate );
							}
						}
						else
							resSrc = ResWithCount.IllegalPos;
#if DEBUG
						dataSrc.SetDebug( indexPosPawnBesidePawnEp, i, resSrc.Value, "EpCap ", indexPosAfterCap, VerifyResType.VerifyAlways );
#else
						dataSrc.Set( i, resSrc.Value );
#endif
					}


					calc.TaBasesWrite.FreeDataChunk( dataSrc );
					calc.TaBasesRead.FreeDataChunk( dataDst );
				}
			}
			return true;
		}



		private static bool IsOverlappingEpPos( Pos pos )
		{
			BitBrd bb = pos.WK.AsBit | pos.BK.AsBit;
			for ( int i=0 ; i<pos.Count ; i++ ) {
				if ( (bb|pos.GetPiecePos(i).AsBit) == bb )
					return true;
				bb = bb | pos.GetPiecePos(i).AsBit;
			}
			return false;
		}


	}
}
