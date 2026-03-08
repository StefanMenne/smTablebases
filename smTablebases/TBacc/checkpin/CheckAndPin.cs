using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
    public sealed class CheckAndPin
    {
		private Lines         lines, linesInitValue;
        private Field         kStm, kSntm;            // king side to move; king side not to move
        private bool          wtm;
        private int           checkCounter = 0;
		private int           firstIndexStm, lastIndexPlusOneStm, firstIndexSntm, lastIndexPlusOneSntm;
		private BitBrd        checkPinBits;     // see kStm_lineIdx_dir_TO_PinnedAndCheck
		private BitBrd        piecesSntm;

		private int[]         pieceType_pSntm_TO_AddPieceInfoIndex;
		private int[]         pStm_TO_AddPieceInfoIndex;
		private BitBrd[]      lineIdx_dir_TO_PinnedAndCheck;    // see kStm_lineIdx_dir_TO_PinnedAndCheck



		// Returns all fields between kstm and check giving piece and all fields between kstm and pinning piece.
		// Check giving piece field is included. Pinning piece field is not included. Kstm not included.
		// 
		// Example: WTM  KQKRR
		//  . . . . . . . .       0 0 0 0 0 0 0 0
		//  . . . . . . . .       0 0 0 0 0 0 0 0
		//  . . . . . . . .       0 0 0 0 0 0 0 0
		//  . r . K . Q . r   =>  0 1 1 0 1 1 1 0
		//  . . . . . . . .       0 0 0 0 0 0 0 0
		//  . . . . . . . .       0 0 0 0 0 0 0 0 
		//  . . . . . . . .       0 0 0 0 0 0 0 0
		//  . . . . . . . .       0 0 0 0 0 0 0 0
		private static BitBrd[][] kStm_lineIdx_dir_TO_PinnedAndCheck = new BitBrd[64][];


		private static int[]    lineIdx_addPieceInfoIdx_TO_lineIdx         = new int[Line.Count*AddPieceInfo.Count];
		private static int[][]  kStm_pieceType_pSntm_TO_AddPieceInfoIndex  = new int[64][];
		private static int[][]  kStm_pStm_TO_AddPieceInfoIndex             = new int[64][];



		public CheckAndPin( WkBk wkbk, Pieces pieces, bool wtm )
		{
			this.kStm    = wtm ? wkbk.Wk : wkbk.Bk;
			this.kSntm   = wtm ? wkbk.Bk : wkbk.Wk;
			this.wtm     = wtm;
			pieceType_pSntm_TO_AddPieceInfoIndex   = kStm_pieceType_pSntm_TO_AddPieceInfoIndex[kStm.Value];
			pStm_TO_AddPieceInfoIndex              = kStm_pStm_TO_AddPieceInfoIndex[kStm.Value];
			lineIdx_dir_TO_PinnedAndCheck          = kStm_lineIdx_dir_TO_PinnedAndCheck[kStm.Value];
			firstIndexStm                          = wtm ? 0 : pieces.CountW;
			firstIndexSntm                         = wtm ? pieces.CountW : 0;
			lastIndexPlusOneStm                    = wtm ? pieces.CountW : pieces.PieceCount;
			lastIndexPlusOneSntm                   = wtm ? pieces.PieceCount : pieces.CountW;
			
			Dir  dir = Dir.Get( kStm, kSntm );
			if ( !dir.IsNo ) {
				Line l   = new Line();
				l.AddSntmBlck( dir.GetPos(kSntm) - dir.GetPos(kStm) );
				linesInitValue.Set( dir.Value, l.Index );
			}
		}


		static CheckAndPin()
        {
			// calc kStm_pieceType_pSntm_TO_AddPieceInfoIndex
			for ( Field kStm=Field.A1 ; kStm<Field.Count ; kStm++ ) {
				kStm_pieceType_pSntm_TO_AddPieceInfoIndex[kStm.Value] = new int[64*Piece.IntToPiece2.Length];
				kStm_pStm_TO_AddPieceInfoIndex[kStm.Value] = new int[64];

				for ( Field f = Field.A1 ; f<Field.Count ; f++ ) {
					Dir             lo    = Dir.Get( kStm, f );
					int             dist  = lo.GetPos(f) - lo.GetPos(kStm);
					
					AddPieceInfo api;
					if ( lo.IsNo || dist==0 ) 
						api = AddPieceInfo.Empty;
					else 
						api = AddPieceInfo.Get( dist, AddPieceType.Stm );

					kStm_pStm_TO_AddPieceInfoIndex[kStm.Value][f.Value] = (api.Index<<3) | lo.Value; 

					for ( int pieceTypeInt=0 ; pieceTypeInt<Piece.IntToPiece2.Length ; pieceTypeInt++ ) {
						Piece pieceType = Piece.IntToPiece2[pieceTypeInt];
						bool isCheck = (pieceType.GetCapBits(f) & kStm.AsBit).IsNotEmpty;

						if ( pieceType.IsN && isCheck )
							api = AddPieceInfo.NCheck;
						else if ( lo == Dir.No || dist==0 )
							api = AddPieceInfo.Empty;
						else
							api = AddPieceInfo.Get( dist, ( isCheck ? AddPieceType.SntmCheck : AddPieceType.SntmBlck ) ); 
						kStm_pieceType_pSntm_TO_AddPieceInfoIndex[kStm.Value][(pieceTypeInt<<6)|f.Value] = ((api.Index)<<3) | lo.Value;
					}
				}
			}

			// calc kStm_LineIdx_dir_TO_PinnedAndCheck
			for ( Field kStm=Field.A1 ; kStm<Field.Count ; kStm++ ) {
				kStm_lineIdx_dir_TO_PinnedAndCheck[kStm.Value] = new BitBrd[4*Line.Count];
				for ( Dir dir=Dir.First ; dir<Dir.Count ; dir++ ) {
					for ( int i=0 ; i<Line.Count ; i++ ) {
						BitBrd bb = new BitBrd();
						Line l = Line.Get( i );
						if ( l.IsCheckRight || l.IsPinRight ) 
							bb |= BitBrd.GetLine( kStm, kStm+(dir.Delta*l.Right), false, l.IsCheckRight );
						if ( l.IsCheckLeft || l.IsPinLeft ) 
							bb |= BitBrd.GetLine( kStm, kStm-(dir.Delta*l.Left), false, l.IsCheckLeft );
						kStm_lineIdx_dir_TO_PinnedAndCheck[kStm.Value][(i<<2)|dir.Value] = bb;
					}
				}
			}

			// calc lineIdx_addPieceInfoIdx_TO_lineIdx
			for ( int lineIdx=0 ; lineIdx<Line.Count ; lineIdx++ ) {
				for ( int addPieceInfoIdx=0 ; addPieceInfoIdx<AddPieceInfo.Count ; addPieceInfoIdx++ ) {
					Line       line       = Line.Get( lineIdx );
					AddPieceInfo addPieceInfo = AddPieceInfo.Get( addPieceInfoIdx );
					if ( addPieceInfo.Type == AddPieceType.Stm )
						line.AddStm( addPieceInfo.DistToKstm );
					else if ( addPieceInfo.Type == AddPieceType.SntmBlck )
						line.AddSntmBlck( addPieceInfo.DistToKstm );
					else if ( addPieceInfo.Type == AddPieceType.SntmCheck )
						line.AddSntmCheck( addPieceInfo.DistToKstm );
					lineIdx_addPieceInfoIdx_TO_lineIdx[lineIdx*AddPieceInfo.Count+addPieceInfoIdx] = line.Index;
				}
			}

        }

        public void Create( Pieces pieces, Fields fields )
        {
            checkCounter = 0;
			lines      = linesInitValue;

			piecesSntm = new BitBrd();
            for ( int i=firstIndexSntm ; i<lastIndexPlusOneSntm; i++ ) {  // add sntm pieces
                Field f = fields.Get(i);
                Piece   p = pieces.GetPieceType(i);
				piecesSntm |= f;
				int api = pieceType_pSntm_TO_AddPieceInfoIndex[(p.AsInt2<<6)|f.Value];
				lines.Set( api&7, lineIdx_addPieceInfoIdx_TO_lineIdx[lines.Get(api&7) * AddPieceInfo.Count + (api>>3) ] );
			}

            for ( int i=firstIndexStm ; i<lastIndexPlusOneStm; i++ ) {  // add stm pieces
                Field f = fields.Get(i);
				int api = pStm_TO_AddPieceInfoIndex[f.Value];
				lines.Set( api&7, lineIdx_addPieceInfoIdx_TO_lineIdx[ lines.Get(api&7)*AddPieceInfo.Count + (api>>3) ] );
			}

			for ( int i=0 ; i<5 ; i++ ) {
				Line l = Line.Get( lines.Get(i) );
				if ( l.IsDblCheck )
					checkCounter += 2;
				else if ( l.IsCheck )
					checkCounter++;
			}
			
			checkPinBits = new BitBrd();
			for ( int i=0 ; i<4 ; i++ ) 
				checkPinBits |= lineIdx_dir_TO_PinnedAndCheck[ (lines.Get(i)<<2) | i ];
		}

		public BitBrd CheckPinBits
		{
			get{ return checkPinBits; }
		}

        public bool IsCheck
        {
            get { return checkCounter >= 1; }
        }

        public bool IsDblCheck
        {
            get { return checkCounter >= 2; }
        }

		/// <summary>
		/// Only valid if check but not double check
		/// </summary>
		public Field FieldOfCheckGivingPiece
		{
			get { return (checkPinBits & piecesSntm).LowestField; }
		}

		public bool IsPin( Field pinnedPieceField )
		{
			return checkPinBits.Contains(pinnedPieceField);
		}
	}
}
