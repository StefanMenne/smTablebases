using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class MoveCounter
	{
		// unchanged data after constructor
		private WkBk        wkBk;
		private Field       kStm, kSntm;
		private Pieces        pieces;
		private Fields      fields;
		private bool        wtm;
		private Fields      lastPosFieldsWithoutFirst             = new Fields( Field.No, Field.No );    // optimization assuming first piece pos is changing often and others not
		private BitBrd      kPossibleMvFields;
		private int         firstSntmIndexWoPiece0;
		private int         lastSntmIndexPlus1WoPiece0;
		private int         firstStmIndex;
		private int         lastStmIndexPlus1;
		private BitBrd[]    field_TO_innerLineBits;
		private BitBrd[]    field_TO_lineBitsTillBorder;
		private BitBrd      wtmAllBits1_btmAllBits0;


		// data changed after UpdatePos
		private BitBrd    occFields;
		private BitBrd    occFieldsWithoutKStm;
		private BitBrd    occFieldsStm;
		private BitBrd    occFieldsSntm;


		// dataWithoutFirstPiece
		private BitBrd     woPiece0_occFields;
		private BitBrd     woPiece0_occFieldsW;
		private BitBrd     woPiece0_occFieldsB;
		private BitBrd     woPiece0_occFieldsStm;
		private BitBrd     woPiece0_occFieldsSntm;
		private bool       woPiece0_OverlappingPieces;
		private BitBrd     woPiece0_kSntmPossibleThreat           = new BitBrd();
		private BitBrd     woPiece0_fieldsToPreventCheckKSntm;      // no check => All bits 1; single check => fields to prevent check are 1; double check => all bits 0
		private BitBrd     woPiece0_fieldsToPreventCheckKStm;
		private BitBrd     woPiece0_pinFieldsKStm;                // contains all bits from all pinnings; 1's: K to checkPiece without K and without check piece
		private BitBrd[]   woPiece0_coveredFieldsSntm_WithoutKStmblocking;
		private int        woPiece0_checkCntKStm;

		// data for first piece
		private Piece      piece0Type;
		private Field      f0;
		private BitBrd     f0BitBrd;
		private BitBrd     piece0_Covered;


		public MoveCounter( Pieces pieces, WkBk wkBk, bool wtm )
		{
			this.wkBk    = wkBk;
			this.pieces  = pieces;
			this.wtm     = wtm;
			kStm         = wtm ? wkBk.Wk : wkBk.Bk;
			kSntm        = wtm ? wkBk.Bk : wkBk.Wk;
			piece0Type = pieces.GetPieceType( 0 );
			field_TO_innerLineBits = BitBrd.Get_field_field_TO_innerLineBitsArray( kStm );
			field_TO_lineBitsTillBorder = BitBrd.Get_field_field_TO_lineBitsTillBorderArray( kStm );
			wtmAllBits1_btmAllBits0 = wtm ? (~BitBrd.Empty) : BitBrd.Empty;

			for ( int i=Math.Max(1,pieces.FirstPiece(wtm)) ; i<pieces.LastPiecePlusOne(wtm) ; i++ ) {
				Piece p = pieces.GetPieceType(i);

				// the direction is switched due that we calculate checks from k to piece
				woPiece0_kSntmPossibleThreat = woPiece0_kSntmPossibleThreat | p.GetCapBackBitsInclProm( kSntm );
			}

			woPiece0_coveredFieldsSntm_WithoutKStmblocking   = new BitBrd[ wtm ? (pieces.PieceCount) : (pieces.CountW) ];
			lastPosFieldsWithoutFirst           = Fields.Last;
			kPossibleMvFields                   = MoveCheck.KPosOtherKPos_To_MvFields(kStm,kSntm);
			firstSntmIndexWoPiece0                = Math.Max(1,pieces.FirstPiece(!wtm));
			lastSntmIndexPlus1WoPiece0            = pieces.LastPiecePlusOne(!wtm);
			if ( pieces.PieceCount == 1 ) {
				PrecalculationWithoutFirstPiece( pieces, new Fields(Field.A1), wtm );
			}
			firstStmIndex = pieces.FirstPiece(wtm);
			lastStmIndexPlus1 = pieces.LastPiecePlusOne(wtm);
		}


		/// <summary>
		///
		/// </summary>
		/// <returns>true if valid; false if illegal pos</returns>
		public bool UpdatePos( Fields f )
		{
			this.fields               = f;
			Fields fieldsWithoutFirst = f;
			fieldsWithoutFirst = fieldsWithoutFirst.RemoveFirst();

			if ( Fields.Compare(fieldsWithoutFirst,lastPosFieldsWithoutFirst,pieces.PieceCount-1) != 0 ) {
				// pos has not only changed in first position
				PrecalculationWithoutFirstPiece( pieces, f, wtm );
				lastPosFieldsWithoutFirst = fieldsWithoutFirst;
			}

			if ( woPiece0_OverlappingPieces )	                       // overlapping in pieces without piece 0
				return false;

			f0          = f.First;                                 //  is always white piece
			f0BitBrd    = f0.AsBit;
			if ( (f0BitBrd&woPiece0_occFields).IsNotEmpty )          // overlapping with piece 0
				return false;

			occFields   = woPiece0_occFields  | f0BitBrd;


			// sntm is in check     =>    illegal
			if ( (woPiece0_fieldsToPreventCheckKSntm & f0BitBrd).IsEmpty )   // see comment of woPiece0_FieldsToPreventCheck
				return false;                // last piece does not prevent check


			// remains to check weather new piece gives check
			if ( wtm ) {     // f0 piece is white
				BitBrd f0CoveringFields = piece0Type.GetCapBits( f0 );

				if ( ( f0CoveringFields & kSntm.AsBit ).IsNotEmpty ) { // f0 gives check or check is blocked
					if ( (MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween(f0,kSntm)&occFields).IsEmpty )  // is unblocked check
						return false;
				}
			}

			return true;
		}


		/// <returns>-1 is mate, 0 stalemate</returns>
		public int CalcMvCount( bool capMvsPossible )
		{
			BitBrd  fieldsToPreventCheckKStm;
			BitBrd  pinFieldsKstm;

			int checkCnt = GetCheckInfo( out fieldsToPreventCheckKStm, out pinFieldsKstm );

			if ( checkCnt > 0 ) {
				int count = GetKMvBits().BitCount;
				if ( checkCnt < 2 ) 				// only king moves are possible if double check
					count += GetMvCountCheck(pinFieldsKstm,fieldsToPreventCheckKStm);
				if ( count==0 && !capMvsPossible )
					return -1;
				else
					return count;
			}
			else {  // no check maybe stalemate
				return GetKMvBits().BitCount + GetMvCountNoCheck(pinFieldsKstm);
			}
		}


		private int GetCheckInfo( out BitBrd fieldsToPreventCheckKStm, out BitBrd pinFieldsKstm )
		{
			occFieldsStm         = woPiece0_occFieldsStm  | (f0BitBrd &  wtmAllBits1_btmAllBits0);
			occFieldsSntm        = woPiece0_occFieldsSntm | (f0BitBrd & ~wtmAllBits1_btmAllBits0);
			occFieldsWithoutKStm = occFields & ~kStm.AsBit;


			//
			// Calculate checkCnt; fieldsToPreventCheckKstm; pinFieldsKstm using the data without piece0
			//
			int checkCnt = woPiece0_checkCntKStm;
			fieldsToPreventCheckKStm = woPiece0_fieldsToPreventCheckKStm;
			pinFieldsKstm = woPiece0_pinFieldsKStm;


			if ( (fieldsToPreventCheckKStm&f0).IsNotEmpty )  {
				BitBrd lineKstmToBorderInclPiece0 = MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween(kStm,f0)|MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehind(kStm,f0);
				checkCnt--;
				if ( wtm )
					pinFieldsKstm |= fieldsToPreventCheckKStm&lineKstmToBorderInclPiece0;
				fieldsToPreventCheckKStm &= ~lineKstmToBorderInclPiece0;
			}
			else if ( (pinFieldsKstm&f0).IsNotEmpty ) {
				BitBrd lineKstmToBorderInclPiece0 = MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween(kStm,f0) | MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehind(kStm,f0);
				pinFieldsKstm &= ~lineKstmToBorderInclPiece0;
			}

			if ( !wtm ) {   // piece0 can perform check
				piece0_Covered = piece0Type.GetCapBits( f0 );
				if ( ( piece0_Covered & kStm ).IsNotEmpty ) {
					BitBrd fieldsToPreventCheckKStmPiece0 = MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween( f0, kStm );
					BitBrd blockCheckBits               = fieldsToPreventCheckKStmPiece0 & occFields;

					if ( blockCheckBits.IsEmpty ) {
						checkCnt++;
						fieldsToPreventCheckKStm |= fieldsToPreventCheckKStmPiece0;
					}
					else if ( blockCheckBits.BitCount == 1 ) {   // exactly one piece is blocking
						if ( (blockCheckBits&occFieldsStm).IsNotEmpty ) {  // the blocking piece is stm => it is pinned
							pinFieldsKstm |= MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween( f0, kStm );
						}
					}
				}
			}

			return checkCnt;
		}


		private BitBrd GetKMvBits()
		{
			return kPossibleMvFields & (~GetSntmCoveredFields()) & (~occFields);     // all possible move destinations fields
		}


		private BitBrd GetSntmCoveredFields()
		{
			BitBrd covered = wtm ? (new BitBrd()) : Piece.RemoveBlockingMvBits(f0, piece0Type.GetCapBits(f0), occFieldsWithoutKStm );
			for ( int i = firstSntmIndexWoPiece0; i < lastSntmIndexPlus1WoPiece0; i++) {
				BitBrd coveredCurrent = woPiece0_coveredFieldsSntm_WithoutKStmblocking[i];
				if ( ( coveredCurrent & f0BitBrd ).IsNotEmpty )
					coveredCurrent &= MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehindInverse( fields.Get(i), f0 );
				covered |= coveredCurrent;
			}
			return covered;

		}


		/// <summary>
		/// Precondition: Position is check but not double check
		/// </summary>
		/// <returns>count possible moves without capturing moves and without king moves.</returns>
		private int GetMvCountCheck( BitBrd pinFieldsKstm, BitBrd fieldsToPreventCheckKStm )
		{
			int count = 0;
			for ( int i=firstStmIndex; i<lastStmIndexPlus1 ; i++ ) {
				Field piecePos = fields.Get(i);

				if ( (pinFieldsKstm&piecePos).IsEmpty ) {     // pinned pieces cannot prevent any check; so they can be just ignored
					Piece pt = pieces.GetPieceType( i );
					count += (fieldsToPreventCheckKStm & Piece.RemoveBlockingMvBits(piecePos,pt.GetMvBits(piecePos),occFields)).BitCount;
				}
			}
			return count;
		}


		private int GetMvCountNoCheck( BitBrd pinFieldsKstm )
		{
			int count = 0;
			for ( int i=firstStmIndex ; i<lastStmIndexPlus1 ; i++ ) {
				Piece     pt     = pieces.GetPieceType( i );
				Field   piecePos = fields.Get(i);
				BitBrd  mvBits = Piece.RemoveBlockingMvBits( piecePos, pt.GetMvBits(piecePos), occFields );

				if (  ( pinFieldsKstm & piecePos ).IsEmpty ) {  // unpinned
					count += mvBits.BitCount;
				}
				else { // pinned piece cannot leave the line between king and check giving piece
					BitBrd mvInsidePinFlds = field_TO_lineBitsTillBorder[piecePos.Value] & pinFieldsKstm;
					count += (mvBits&mvInsidePinFlds).BitCount;
				}
			}
			return count;
		}


		private void PrecalculationWithoutFirstPiece( Pieces pieces, Fields f, bool wtm )
		{
			// overlapping   =>   illegal
			woPiece0_occFields        = wkBk.Wk.AsBit | wkBk.Bk.AsBit;
			woPiece0_occFieldsW       = wkBk.Wk.AsBit;
			woPiece0_occFieldsB       = wkBk.Bk.AsBit;
			woPiece0_OverlappingPieces  = false;

			for ( int i=1 ; i<pieces.PieceCount ; i++ ) {
				BitBrd bitNew  = f.Get(i).AsBit;
				BitBrd bitsNew = woPiece0_occFields | bitNew;
				if ( woPiece0_occFields == bitsNew )
					woPiece0_OverlappingPieces = true;
				if ( i<pieces.CountW )
					woPiece0_occFieldsW |= bitNew;
				else
					woPiece0_occFieldsB |= bitNew;

				woPiece0_occFields = bitsNew;
			}

			woPiece0_occFieldsStm  = wtm ? woPiece0_occFieldsW : woPiece0_occFieldsB;
			woPiece0_occFieldsSntm = wtm ? woPiece0_occFieldsB : woPiece0_occFieldsW;
			BitBrd woPiece0_occFieldsWithoutKSntm = woPiece0_occFields & ~kSntm.AsBit;
			BitBrd woPiece0_occFieldsWithoutKStm = woPiece0_occFields & ~kStm.AsBit;


			// sntm is checked => illegal
			bool neccessaryConditionForIllPos = (woPiece0_kSntmPossibleThreat&woPiece0_occFieldsStm).IsNotEmpty;
			int woPiece0_SntmCheckCnt       = 0;
			woPiece0_fieldsToPreventCheckKSntm   = ~BitBrd.Empty;

			if ( neccessaryConditionForIllPos ) {
				for ( int i=Math.Max(1,pieces.FirstPiece(wtm)) ; i<pieces.LastPiecePlusOne(wtm) ; i++ ) {
					Piece pt = pieces.GetPieceType( i );
					Field piecePos = f.Get(i);
					BitBrd coveredCurrent = Piece.RemoveBlockingMvBits(piecePos,pt.GetCapBits(piecePos),woPiece0_occFieldsWithoutKSntm) & ~(woPiece0_occFieldsStm);
					if ((coveredCurrent & kSntm.AsBit).IsNotEmpty) {
						if ( ++woPiece0_SntmCheckCnt == 1 )
							woPiece0_fieldsToPreventCheckKSntm = MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween( piecePos, kSntm );
						else
							woPiece0_fieldsToPreventCheckKSntm = BitBrd.Empty;
					}
				}
			}



			//
			// - Calculate covered fields from sntm pieces  ... for calculating king move count
			// - Calculate check count kStm
			// - Calculate bits to block one of the checks
			// - Calculate pin Bits
			//
			woPiece0_checkCntKStm = 0;
			woPiece0_fieldsToPreventCheckKStm = BitBrd.Empty;
			woPiece0_pinFieldsKStm          = BitBrd.Empty;
			for ( int i=Math.Max(1,pieces.FirstPiece(!wtm)) ; i<pieces.LastPiecePlusOne(!wtm) ; i++ ) {
				Field  piecePos  = f.Get(i);
				woPiece0_coveredFieldsSntm_WithoutKStmblocking[i] = Piece.RemoveBlockingMvBits(piecePos, pieces.GetPieceType(i).GetCapBits(piecePos), woPiece0_occFieldsWithoutKStm);

				BitBrd coveredCurrentSntmPieceWithRemovedBlockingsSntm = Piece.RemoveBlockingMvBits( piecePos, pieces.GetPieceType(i).GetCapBits(piecePos), woPiece0_occFieldsSntm );

				if ( ( coveredCurrentSntmPieceWithRemovedBlockingsSntm & kStm ).IsNotEmpty ) {
					BitBrd stmCheckBlockingPieces = MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween( piecePos, kStm ) & woPiece0_occFieldsStm;

					if ( stmCheckBlockingPieces.IsEmpty ) {	  // no stm blocking pieces => check
						woPiece0_checkCntKStm++;
						woPiece0_fieldsToPreventCheckKStm |= MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween( kStm, piecePos );
					}
					else if ( stmCheckBlockingPieces.BitCount == 1 ) {
						woPiece0_pinFieldsKStm |= MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween( kStm, piecePos );
					}
				}
			}
		}
	}
}
