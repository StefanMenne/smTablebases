using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public abstract class Piece
	{
		public const int Count = 5;    // KQRBN
		
		public static Piece K  = new K();
		public static Piece Q  = new Q();
		public static Piece R  = new R();
		public static Piece B  = new B();
		public static Piece N  = new N();
		public static Piece PW = new PW();
		public static Piece PB = new PB();

		public static Piece[] IntToPiece  = new Piece[] { K, Q, R, B, N, PW, K, Q, R, B, N, PB };
		public int GetAsInt( bool w )
		{
			return ( w ? 0 : 6 ) + AsInt3;
		} 

		public static Piece[] IntToPiece2 = new Piece[] { Q, R, B, N, PW, PB };
		public abstract int AsInt2 { get; }    //   K=exc Q=0 R=1 B=2 N=3 PW=4 PB=5

		public abstract int AsInt3 { get; }    //   K=0   Q=1 R=2 B=3 N=4 PW=5 PB=5        

		public   readonly int[]      Delta;
		public   readonly int[]      DeltaX;
		public   readonly int[]      DeltaY;
		public   readonly Field[][]  MvBack;
		public   readonly byte[][]   MvBackSkip;
		public   readonly bool[]     CapMove;
		public   readonly bool[]     PawnTwoFieldMv;
		private  BitBrd[]   piecePos_to_mvBits, piecePos_to_mvBitsInclProm, piecePos_to_mvBackBits, piecePos_to_capBits, piecePos_to_capBackBits, piecePos_to_capBackBitsInclProm;
		public   ulong               MvDeltaBits;
		//private  BitBrd[]   piecePosOtherPiecePos_to_coveredFields;
		//private  BitBrd[]   piecePosOtherPiecePos_to_coveredFieldsExcluded;

		protected Piece(){}

		protected Piece( int[] delta, int[] deltaX, int[] deltaY, bool[] capMv, bool[] pawnTwoFieldMv )
		{
			Delta           = delta;
			DeltaX          = deltaX;
			DeltaY          = deltaY;
			CapMove         = capMv;
			PawnTwoFieldMv   = pawnTwoFieldMv;
			MvBack          = new Field[64][];
			MvBackSkip      = new byte[64][];

			piecePos_to_mvBits            = new BitBrd[64];
			if ( IsP ) {
				piecePos_to_mvBackBits          = new BitBrd[64];
				piecePos_to_capBits             = new BitBrd[64];
				piecePos_to_capBackBits         = new BitBrd[64];
				piecePos_to_mvBitsInclProm      = new BitBrd[64];
				piecePos_to_capBackBitsInclProm = new BitBrd[64];
				P p = (P)this;
				for ( Field pf=Field.A1 ; pf<=Field.H8 ; pf++ ) {
					BitBrd cap    = 0UL,   capBack = 0UL, capBackInclProm = 0UL;
					
					if ( pf.X > 0 )
						capBackInclProm |= (new Field( pf.X-1, pf.Y+(p.IsPW?-1:1) )).AsBit;
					if ( pf.X<7 ) 
						capBackInclProm |= (new Field( pf.X+1, pf.Y+(p.IsPW?-1:1) )).AsBit;

					piecePos_to_capBackBitsInclProm[pf.Value] = capBackInclProm;

					if ( pf<Field.A2 || pf>Field.H7 || pf.IsPawnGrndLine(p.IsPW) ) {
						MvBack[pf.Value] = new Field[0];
						MvBackSkip[pf.Value] = new byte[0];
					}
					else if ( pf.IsPawnFourthLine(p.IsPW) ){
						MvBack[pf.Value] = new Field[2] { pf-delta[0], pf-delta[1] };
						MvBackSkip[pf.Value] = new byte[2] { 2, 1 };
					}
					else {
						MvBack[pf.Value] = new Field[1] { pf-delta[0] };
						MvBackSkip[pf.Value] = new byte[1] { 1 };
					}

					if ( pf<Field.A2 || pf>Field.H7 )
						continue;

					BitBrd mv     = (new Field(pf.X, pf.Y + (p.IsPW ? 1 : -1))).AsBit;
					BitBrd mvBack = (new Field(pf.X, pf.Y + (p.IsPW ? -1 : 1))).AsBit;
					
					if ( pf.X > 0 ) {
						cap     |= (new Field( pf.X-1, pf.Y+(p.IsPW?1:-1) )).AsBit;
						capBack |= (new Field( pf.X-1, pf.Y+(p.IsPW?-1:1) )).AsBit;
					}
					if ( pf.X<7 ) {
						cap     |= (new Field( pf.X+1, pf.Y+(p.IsPW?1:-1) )).AsBit;
						capBack |= (new Field( pf.X+1, pf.Y+(p.IsPW?-1:1) )).AsBit;
					}
					if ( pf.IsPawnGrndLine(p.IsPW) )
						mv     |= new Field( pf.X, pf.Y + ( p.IsPW ?  2 : -2 ) ).AsBit;
					if ( pf.IsPawnFourthLine(p.IsPW) )
						mvBack |= new Field( pf.X, pf.Y + ( p.IsPW ? -2 :  2 ) ).AsBit;
					if ( !pf.IsPawnGrndLine(!IsPW) ) // no promotion mv'S
						piecePos_to_mvBits[pf.Value] = mv;

					piecePos_to_mvBitsInclProm[pf.Value]      = mv;
					piecePos_to_mvBackBits[pf.Value]          = mvBack;
					piecePos_to_capBits[pf.Value]             = cap;
					piecePos_to_capBackBits[pf.Value]         = capBack;
				}
			}
			else {
				piecePos_to_mvBackBits = piecePos_to_capBits = piecePos_to_capBackBits = piecePos_to_mvBitsInclProm = piecePos_to_capBackBitsInclProm = piecePos_to_mvBits;
				for ( Field pf=Field.A1 ; pf<=Field.H8 ; pf++ ) {
					List<Field> mvBackList = new List<Field>();
					List<byte>  mvBackSkipList = new List<byte>();
					BitBrd mv = 0UL;
					for ( int dir=0 ; dir<Delta.Length ; dir++ ) {
						Field f = pf;
						while ( !IsMvToOutside(f,DeltaX[dir],DeltaY[dir]) ) {
							f += Delta[dir];
							mvBackList.Add( f );
							mv |= f.AsBit;
							if ( IsN )
								break;
						}
						while ( mvBackSkipList.Count<mvBackList.Count )
							mvBackSkipList.Add( (byte)(mvBackList.Count-mvBackSkipList.Count) );
					}
					piecePos_to_mvBits[pf.Value] = mv;
					MvBack[pf.Value] = mvBackList.ToArray();
					MvBackSkip[pf.Value] = mvBackSkipList.ToArray();
				}
			}
		}

		public virtual   bool IsK           { get{ return false; } }
		public virtual   bool IsQ           { get{ return false; } }
		public virtual   bool IsR           { get{ return false; } }
		public virtual   bool IsB           { get{ return false; } }
		public virtual   bool IsN           { get{ return false; } }
		public virtual   bool IsP           { get{ return false; } }
		public virtual   bool IsPW          { get{ return false; } }
		public virtual   bool IsPB          { get{ return false; } }
		public abstract  char AsCharacter   { get; }
		public virtual   bool IsSingleStep  { get { return false; } }
		public virtual   bool MvHorVert     { get { return false; } }
		public virtual   bool MvDiag        { get { return false; } }
		public abstract  int  MvBound       { get; }


		public static Piece FromChar( char c )
		{
			for ( int i=0 ; i<IntToPiece.Length ; i++ )
				if ( IntToPiece[i].AsCharacter == c )
					return IntToPiece[i];
			throw new Exception();
		}

		public Piece SwitchPawn
		{
			get{
				if ( IsPW )
					return PB;
				else if ( IsPB )
					return PW;
				else
					return this;
			}
		}

		public override string ToString()
		{
			return AsCharacter.ToString();
		}


		protected void Init()
		{

		}

		/// <summary>
		/// All possible moves. For Pawn: No promotion. No Cap.
		/// </summary>
		public BitBrd GetMvBits( Field f )
		{
			return piecePos_to_mvBits[f.Value];
		}

		/// <summary>
		/// All possible moves. For Pawn: Incl. promotion. No Cap.
		/// </summary>
		public BitBrd GetMvBitsInclProm(Field f)
		{
			return piecePos_to_mvBitsInclProm[f.Value];
		}


		public BitBrd GetMvBackBits( Field f )
		{
			return piecePos_to_mvBackBits[f.Value];
		}


		/// <summary>
		/// All fields the piece could capture opponent pieces. It is the same as move bits for non Pawns.
		/// </summary>
		public BitBrd GetCapBits( Field f )
		{
			return piecePos_to_capBits[f.Value];
		}

		public BitBrd GetCapBackBits( Field f )
		{
			return piecePos_to_capBackBits[f.Value];
		}

		public BitBrd GetCapBackBitsInclProm( Field f )
		{
			return piecePos_to_capBackBitsInclProm[f.Value];
		}
		


		public static bool IsMvToOutside( Field f, int dx, int dy )
		{
			int xNew = f.X + dx;
			int yNew = f.Y + dy;
			return xNew<0 || xNew>=8 || yNew<0 || yNew>=8;
		}


		public static BitBrd RemoveBlockingMvBits( Field src, BitBrd mvBits, BitBrd occFld )
		{
			BitBrd blockingFlds = mvBits & occFld;

			while ( blockingFlds.IsNotEmpty ) {
				BitBrd   blockingFldBitBrd = blockingFlds.LowestBit;
				Field    blockingFld       = blockingFldBitBrd.LowestField;
				mvBits = mvBits & MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehindInverse( src, blockingFld );
				blockingFlds = mvBits & occFld;
			}
			return mvBits;
		}

	}
}
