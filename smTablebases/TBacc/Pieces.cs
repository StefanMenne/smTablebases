using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace TBacc
{
	// Represents the pieces for the tablebases in the order the tablebases are created
	//  KQK, KRK, KBK, KNK, KPK, KKQ, KKR, KKB, KKN, KKP, KQQK, KQRK ....
	public sealed class Pieces
	{
		public  static Pieces    KK;
		public  static int     Count = 1512;     // All 2,3,4,5,6,7 men
		public  static Pieces[]  Instances                  = new Pieces[2*Count];
		private static short[] indexWithDoubles_To_Index;

		private Piece[]            piece;
		private int                count;
		private int                countW;
		private short              index;
		private short[]            addPieceToIndex;
		private short[]            removePieceToIndex;


		static Pieces()
		{
			short index = 0;
			int count = GetTotalCountInclDoubled( Config.MaxNonKMen );
			indexWithDoubles_To_Index = new short[count];
			for ( int i=0 ; i<count ; i++ ) {
				Pieces p = Pieces.CalcPiecesFromIndex(i);
				if ( p.IsDoubled ) {
					indexWithDoubles_To_Index[i] = -1;
				}
				else {
					indexWithDoubles_To_Index[i] = index;
					Instances[index] = p;
					p.index = (short)index;
					p = p.SwitchSidesIntern();
					Instances[Count+index] = p;
					p.index = (short)(Count+index);
					index++;
				}
			}

			for ( int i=0 ; i<Instances.Length ; i++ )
				Instances[i].Init2();

			KK = Instances[0];
		}


		public static Pieces FromString( string s )
		{
			return new Pieces(s).ToIndexableInstance();
		}

		public static Pieces FromPieces( Piece[] piece )
		{ 
			return new Pieces( piece ).ToIndexableInstance();
		}


		public static Pieces FromIndex( int index )
		{
			return Instances[index];
		}


		// index to pieces
		//
		// KQBBKB    =>   n=4   =>   Choose 9 out of n+9=13 possible 6 stone pieces
		//
		// KQBBKB is to choose {2,3,6,7,8,9,10,12,13} out of {1,...,13}        1  2  3  4  5  6  7  8  9 10 11 12 13
		//              X=choosen   Q,R,B,N,P=not choosen                      Q  X  X  B  B  X  X  X  X  X  B  X  X
		private static Pieces CalcPiecesFromIndex( int index )
		{
            int n=0;
            int currentCount = 1;
            while ( index>=currentCount ) {
                index -= currentCount;
                currentCount = GetCountForNPieces( ++n );
            }

            int pieceTypeIndex = 0;    // WQ=0, WR=1, WB=2, WN=3, WP=4, BQ=5, BR=6, BB=7, BN=8, BP=9
            int pieceIndex = 1;
            Piece[] piece = new Piece[n+2];
            piece[0] = Piece.K;
            for ( int i=0 ; i<ItemsToChooseOf(n) ; i++ ) {
                if ( index<Tools.ChooseKOutOfN( ItemsToChoose-pieceTypeIndex,ItemsToChooseOf(n)-i-1) ) { // i is not choosen   // ChooseKOutOfN( 0...9, 0...9 )
                    piece[pieceIndex++] = intToPiece[pieceTypeIndex];
                }
                else {  // i is choosen 
                    index -= (int)Tools.ChooseKOutOfN( ItemsToChoose-pieceTypeIndex, ItemsToChooseOf(n)-i-1 );// ChooseKOutOfN( 0...9, 0...9 )
                    if ( ++pieceTypeIndex == 5 )
                        piece[pieceIndex++] = Piece.K;
                }
			}
			return new Pieces( piece );
		}


		private Pieces( Piece[] p )
		{
			count = countW = 0;
			Init( p );
		}


		private Pieces( Piece[] pWithoutK, int countW )
		{
			Piece[] p = new Piece[pWithoutK.Length+2];
			p[0] = Piece.K;
			p[countW+1] = Piece.K;
			for ( int i=0 ; i<pWithoutK.Length ; i++ )
				p[ (i<countW) ? (i+1) : (i+2) ] = pWithoutK[i];
			count = countW = 0;
			Init( p );
		}


		private Pieces( string p )
		{
			count = countW = 0;
			Piece[] pa = new Piece[p.Length];
			for ( int i=0 ; i<pa.Length ; i++ )
				pa[i] = Piece.FromChar( p[i] );
			Init( pa );
		}


		private void Init( Piece[] p )
		{
			piece = new Piece[ p.Length-2 ];
			for ( int i=1 ; i<p.Length ; i++ ) {
				if ( p[i].IsK )
					countW = i-1;
				else {
					piece[count++] = p[i];
				}
			}
		}
        

		private void Init2()
		{
			if ( PieceCount<Config.MaxNonKMen ) {
				addPieceToIndex = new short[10];
				for ( int i=0 ; i<addPieceToIndex.Length ; i++ ) {
					addPieceToIndex[i] = this.AddIntern( i<5, Piece.IntToPiece2[i%5] ).ToIndexableInstance().index;
				}
			}
			removePieceToIndex = new short[count];
			for ( int i=0 ; i<removePieceToIndex.Length ; i++ ) {
				removePieceToIndex[i] = this.RemovePieceIntern( i ).ToIndexableInstance().index;
			}
		}


		public int Index
		{
			get{ return index; }
		}


		private Pieces ToIndexableInstance()
		{
			if ( IsDoubled )
				return Instances[ (short)( Count + indexWithDoubles_To_Index[SwitchSidesIntern().CalcIndex()] )];
			else
			{
				short idx = (short)(indexWithDoubles_To_Index[CalcIndex()]);
				return Instances[idx];
			}
		}


		private short CalcIndex()
		{
            int index = 0;
            int n = PieceCount;
            for ( int i=0 ; i<n ; i++ )
                index += GetCountForNPieces( i );

			int pieceTypeIndex = 0;    // WQ=0, WR=1, WB=2, WN=3, WP=4, BQ=5, BR=6, BB=7, BN=8, BP=9
            int pieceIndex = 0;
            for ( int i=0 ; i<ItemsToChooseOf(n) ; i++ ) {
                if ( pieceIndex<PieceCount && GetPieceType(pieceIndex).AsInt3 + (IsW(pieceIndex) ? -1 : 4)  == pieceTypeIndex ) {    // i is not choosen
                    pieceIndex++;
                }
                else {           // i is choosen
                    index += (int)Tools.ChooseKOutOfN( ItemsToChoose-pieceTypeIndex++, ItemsToChooseOf(n)-i-1 );  // ChooseKOutOfN( 0...9, 0...9 )
                }
            }
            return (short)index;
		}


		public int GetMvCountBound( bool wtm )
		{
			int mvCount = 8; // K
			for ( int i=FirstPiece(wtm) ; i<LastPiecePlusOne(wtm) ; i++ )
				mvCount += GetPieceType( i ).MvBound;
			return mvCount;
		}


		public int FirstPiece( bool w )
		{
			return w ? 0 : CountW;
		}


		public int LastPiecePlusOne( bool w )
		{
			return w ? CountW : PieceCount;
		}


		public PieceGroupInfo GetPieceGroupInfo()
		{
			PieceGroupInfo pgi = new PieceGroupInfo();

			int cnt = 0;
			for ( int i=0 ; i<PieceCount ; i+=cnt ) {
				Piece cp = GetPieceType(i);
				cnt = 1;
				while ( i+cnt<PieceCount && i+cnt!=countW && GetPieceType(i+cnt)==cp )
					cnt++;
				pgi.Add( cp, cnt, IsW(i) );
			}
			return pgi;
		}


		public bool ContainsPawn
		{
			get{ return ( countW>0 && GetPieceType(countW-1).IsP ) || ( CountB>0 && GetPieceType(count-1).IsP ); }
		}


		public bool ContainsWpawnAndBpawn
		{
			get{ return ( countW>0 && GetPieceType(countW-1).IsP ) && ( CountB>0 && GetPieceType(count-1).IsP ); }
		}


		public List<Pieces> GetSubPieces()
		{
			List<Pieces> p = new List<Pieces>();

			// capturing
			for ( int i=0 ; i<PieceCount ; i++ ) {
				Pieces pCap = RemovePiece( i );
				if ( pCap.IsDoubled )
					pCap = pCap.SwitchSides();
				if ( !p.Contains( pCap ) )
					p.Add( pCap );
			}

			// promotion
			foreach( bool w in Tools.BoolArray ) {
				if ( ContainsPawnColor(w) ) {
					for ( int pieceInt=Piece.Q.AsInt3 ; pieceInt<=Piece.N.AsInt3 ; pieceInt++ ) {
						Pieces pProm = RemovePiece( w ? (CountW-1) : (PieceCount-1) );
						pProm = pProm.Add( w, Piece.IntToPiece[pieceInt] );
						if ( pProm.IsDoubled )
							pProm = pProm.SwitchSides();
						if ( !p.Contains( pProm ) )
							p.Add( pProm );
					}
				}
			}

			// cap and promotion
			foreach( bool w in Tools.BoolArray ) {
				if ( ContainsPawnColor(w) ) {
					for ( int i=FirstPiece(!w) ; i<LastPiecePlusOne(!w) ; i++ ) {
						Pieces pCap = RemovePiece( i );
						for ( int pieceInt=Piece.Q.AsInt3 ; pieceInt<=Piece.N.AsInt3 ; pieceInt++ ) {
							Pieces pProm = pCap;
							pProm = pProm.RemovePiece( w ? (pProm.CountW-1) : (pProm.PieceCount-1) );
							pProm = pProm.Add( w, Piece.IntToPiece[pieceInt] );
							if ( pProm.IsDoubled )
								pProm = pProm.SwitchSides();
							if ( !p.Contains( pProm ) )
								p.Add( pProm );
						}
					}
				}
			}

			return p;
		}


		public bool ContainsPawnColor( bool w )
		{
			if ( w )
				return countW>0 && GetPieceType(countW-1).IsP;
			else
				return CountB>0 && GetPieceType(count-1).IsP;
		}


		public int GetPawnCount( bool w )
		{
			int count = 0;
			for ( int i=FirstPiece(w) ; i<LastPiecePlusOne(w) ; i++ )
				if ( GetPieceType(i).IsP )
					count++;
			return count;
		}


		public int IncPieceIndexSkipSamePieceType( int pieceIndex )
		{
			while ( pieceIndex+1!=countW && pieceIndex+1!=count && GetPieceType(pieceIndex)==GetPieceType(pieceIndex+1) )
				pieceIndex++;
			return pieceIndex+1;
		}


        public bool IsDoubled
        {
            get {
				for ( int i=0 ; i<Math.Max(CountW,CountB) ; i++ ) {
					if ( i<CountW && i<CountB ) {
						if ( GetPieceType(i).IsPW && GetPieceType(CountW+i).IsPB )
							continue;
						else if ( GetPieceType(i).AsInt2 == GetPieceType(CountW+i).AsInt2 )
							continue;
						else 
							return GetPieceType(i).AsInt2 > GetPieceType(CountW+i).AsInt2;
					}
					else
						return i<CountB;
				}
				return false;
            }
        }


		/// <summary>
		/// Piece Count without K's. Returns e.g. 1 for KQK
		/// </summary>
		public int PieceCount
		{
			get{ return piece.Length; }	
		}


		public int CountW
		{
			get{ return countW; }
		}


		public int CountB
		{
			get{ return PieceCount - CountW; }
		}


		public bool IsW( int idx )
		{
			return idx<CountW;
		}


		public Piece GetPieceType( int index )
		{
			return piece[index];
		}


		public Pieces Add( bool w, Piece p )
		{
			return Instances[addPieceToIndex[ (w?-1:4) + p.AsInt3 ]];
		}


		private Pieces AddIntern( bool w, Piece p )
		{
			int index = GetIndexToAdd( w, p );
			Piece[] pi = new Piece[PieceCount+1];
			pi[index] = p;
			for ( int i=0 ; i<PieceCount ; i++ )
				pi[ (i<index) ? i : (i+1) ] = GetPieceType(i);
			return new Pieces( pi, w ? (countW+1) : countW );
		}


		public Pieces RemovePiece( int index )
		{
			return Instances[removePieceToIndex[index]];
		}


		private Pieces RemovePieceIntern( int index )
		{
			Piece[] p = new Piece[piece.Length-1];
			for ( int i=0 ; i<p.Length ; i++ )
				p[i] = piece[(i<index)?i:(i+1)];
			return new Pieces( p, ((index<countW)?(countW-1):countW) );
		}


		public Pieces SwitchSides()
		{
			return Instances[((index<Count)?(index+Count):(index-Count))];
		}


		private Pieces SwitchSidesIntern()
		{
			Piece[] p = new Piece[PieceCount];

			for ( int i=0 ; i<countW ; i++ )
				p[CountB+i] = GetPieceType(i).SwitchPawn;
			for ( int i=0 ; i<CountB ; i++ )
				p[i] = GetPieceType(countW+i).SwitchPawn;

			return new Pieces( p, CountB );
		}


		public int GetIndexToAdd( bool w, Piece p )
		{
			int i = FirstPiece(w);
			while( i<LastPiecePlusOne(w)&&p.AsInt2>GetPieceType(i).AsInt2  )
				i++;
			return i;
		}


		public int GetPieceCount( bool wtm )
		{
			return wtm ? CountW : CountB;
		}


		public int GetPieceCount( int index )
		{
			int   count   = 1;
			Piece   pieceType = GetPieceType( index );
			while ( ++index!=countW && index!=this.count && pieceType==GetPieceType(index) )
				count++;
			return count;
		}



		public static bool operator==( Pieces pieces1, Pieces pieces2 )
		{
			if ( object.ReferenceEquals( pieces1, pieces2 ) )
				return true;
			if ( object.Equals(pieces1,null) || object.Equals(pieces2,null) || pieces1.count!=pieces2.count || pieces1.countW!=pieces2.countW )
				return false;
			for ( int i=0 ; i<pieces1.piece.Length ; i++ ) {
				if ( pieces1.piece[i] != pieces2.piece[i] )
					return false;
			}
			return true;
		}


		public static bool operator!=( Pieces pieces1, Pieces pieces2 )
		{
			return !(pieces1==pieces2);
		}


		public override int GetHashCode()
		{
			int bits = (count<<3) | countW;
			for ( int i=0 ; i<count ; i++ )
				bits = (bits<<4) | piece[i].AsInt2;
			return bits;
		}


		public override bool Equals(object obj)
		{
			if ( obj==null || !(obj is Pieces) )
				return false;
			else
				return this==((Pieces)obj);
		}


		public override string ToString()
		{
			string s = "K";
			for ( int i=0 ; i<PieceCount ; i++ )
				s += GetPieceType(i).AsCharacter;
			return s.Insert( countW+1, "K" );
		}


		private static Piece[] intToPiece = new Piece[] { Piece.Q, Piece.R, Piece.B, Piece.N, Piece.PW, Piece.Q, Piece.R, Piece.B, Piece.N, Piece.PB };


        /// <summary>
        /// Number of X in above example
        /// </summary>
        private static int ItemsToChoose
        {
            get{ return /*2*Piece.Count-3*/9; }
        }

        /// <summary>
        /// Total number of items to choose of. 14 in above example.
        /// </summary>
        private static int ItemsToChooseOf( int n )
        {
            return ItemsToChoose+n;
        }


        /// <summary>
        /// Count of possible Pieces for n stones without K. E.g. n=1 returns 10 (KQK,KRK,KBK,KNK, KPK, KKQ,KKR,KKB,KKN, KKP)
        /// </summary>
        private static int GetCountForNPieces( int n )
        {
            return (int)Tools.ChooseKOutOfN( ItemsToChoose, ItemsToChooseOf(n) );// ChooseKOutOfN( 0...9, 0...9 )
        }


		public static int GetTotalCountInclDoubled( int nofNonKMenMax )
		{
			int count = 0;
			for ( int i=0 ; i<=nofNonKMenMax ; i++ )
				count += GetCountForNPieces( i );
			return count;
		}


		public bool GetIsSymmetric()
		{
			// Disabled; makes problems
			return false;


			//if ( CountW != CountB )
			//	return false;
			//for ( int i=0 ; i<CountW ; i++ ) {
			//	if ( GetPieceType(i).AsInt2 != GetPieceType(countW+i).AsInt2 )
			//		return false;
			//}
			//return true;
		}
	}
}
