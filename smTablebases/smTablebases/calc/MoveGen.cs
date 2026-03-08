using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using smTablebases;
using TBacc;

namespace smTablebases
{
	public static class MoveGen
	{
		private static BitBrd[] coveredMv  = new BitBrd[5];
		private static BitBrd[] coveredCap = new BitBrd[5];

		public static void CalcMv( List<Move> moves, Pos pos, bool wtm, Field epCapDst )
		{
			moves.Clear();
			CheckAndPin checkAndInfoIllegal = new CheckAndPin( pos.WkBk, pos.Pieces, !wtm );
			checkAndInfoIllegal.Create( pos.Pieces, pos.Fields );
			if ( checkAndInfoIllegal.IsCheck )
				return;

			CheckAndPin checkAndInfo = new CheckAndPin( pos.WkBk, pos.Pieces, wtm );
			checkAndInfo.Create( pos.Pieces, pos.Fields );
			bool checkOrPinnedPiece = checkAndInfo.CheckPinBits.IsNotEmpty;

			// king moves
			Field k = wtm ? pos.WK : pos.BK;
			for ( int j=0 ; j<Piece.K.Delta.Length ; j++ ) {
				if ( !Piece.IsMvToOutside(k,Piece.K.DeltaX[j],Piece.K.DeltaY[j])  ) {
					Field fNew = k + Piece.K.Delta[j];
					int capPieceIndex = pos.FToPieceIndex( fNew );
					bool validMv = !Field.IsDist0or1( fNew, (wtm?pos.BK:pos.WK) );

					if ( capPieceIndex==-1 )
						validMv &= !MoveCheck.IsCheck( pos.Pieces, pos.Fields, wtm?fNew:pos.WK, wtm?pos.BK:fNew , wtm );
					else {
						validMv &= !( (wtm && capPieceIndex<pos.CountW) || (!wtm && capPieceIndex>=pos.CountW) );
						validMv &= !MoveCheck.IsCheckCaptured( pos.Pieces, pos.Fields, wtm?fNew:pos.WK, wtm?pos.BK:fNew , wtm, capPieceIndex );
					}

					if ( validMv )
						moves.Add( new Move( fNew, capPieceIndex ) );
				}
			}


			// other moves
			for ( int i=pos.FirstPiece(wtm) ; i<pos.LastPiecePlusOne(wtm) ; i++ ) {
				Piece     p = pos.GetPieceType(i);
				Field   f = pos.GetPiecePos(i);

				for ( int j=0 ; j<p.Delta.Length ; j++ ) {
					int   capPieceIndex = -1;
					Field fNew        = f;

					while( !Piece.IsMvToOutside(fNew,p.DeltaX[j],p.DeltaY[j]) && capPieceIndex==-1 ) {
						fNew += p.Delta[j];
						if ( fNew==pos.WK || fNew==pos.BK )
							break;
						capPieceIndex       = pos.FToPieceIndex(fNew);


						if ( p.IsP ) {
							if ( !epCapDst.IsNo && p.CapMove[j] && fNew==epCapDst ) {
								capPieceIndex = pos.FToPieceIndex( EP.GetDblStepDst(epCapDst) );        // ep
							}
							else if ( p.CapMove[j] != (capPieceIndex!=-1) )
								break;
							if ( p.PawnTwoFieldMv[j] ) { // check weather double pawn move is possible
								if ( !f.IsPawnGrndLine(wtm) )  // pawn can only move 2 fields from ground line
									break;
								Field fOneStep = f;
								fOneStep += (p.Delta[j]/2);
								if ( !pos.IsFieldEmpty(fOneStep) )
									break;
							}
						}

						if ( capPieceIndex==-1 || pos.IsW(capPieceIndex)!=wtm ) {
							bool validMv = true;
							Fields fields = pos.Fields;
							fields = fields.SetNew( i, fNew );

							if ( checkOrPinnedPiece ) {
								if ( capPieceIndex==-1 )
									validMv &= !MoveCheck.IsCheck( pos.Pieces, fields, pos.WK, pos.BK, wtm );
								else
									validMv &= !MoveCheck.IsCheckCaptured( pos.Pieces, fields, pos.WK, pos.BK, wtm, capPieceIndex );
							}

							if ( validMv ) {
								if ( p.IsP && f.IsPawnGrndLine(!wtm) ) {
									moves.Add( new Move( i, fNew, capPieceIndex, Piece.Q ) );
									moves.Add( new Move( i, fNew, capPieceIndex, Piece.R ) );
									moves.Add( new Move( i, fNew, capPieceIndex, Piece.B ) );
									moves.Add( new Move( i, fNew, capPieceIndex, Piece.N ) );
								}
								else {
									moves.Add( new Move( i, fNew, capPieceIndex ) );
								}
							}

						}
						if ( p.IsSingleStep )
							break;
					}
				}
			}
		}
	}
}
