using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public struct WkBk
	{
		private int                         index;
		private bool                        pawn;

		private static WkBkInfo[]           toInfoPawn                        = new WkBkInfo[1806];
		private static WkBkInfo[]           toInfoNoPawn                      = new WkBkInfo[462];
		private static int[]                wkBkToWkBkIndexNoPawn             = new int[64*64];
		private static int[]                wkBkToWkBkIndexPawn               = new int[64*64];
		private static int[]                wkBkIndexToWkBk                   = new int[1806];
		
		private static int[]                wkBkIndexToNewWkBkIndexMirListWtm = new int[1806*9];  
		// e.g. wkBkIndexToNewWkBkIndexMirListWtm[5*9+3] =  34*16 + 2   wkbkIdx=5,WhiteMv=3 => wkBkIdx=34,mirror=2
		
		private static int[]                wkBkIndexToNewWkBkIndexMirListBtm = new int[1806*9];  
		// e.g. wkBkIndexToNewWkBkIndexMirListBtm[5*9+3] =  34*16 + 2   wkbkIdx=5,WhiteMv=3 => wkBkIdx=34,mirror=2

		private static WkBk[]               reverseNoPawn                      = new WkBk[462];
		private static WkBk[]               reversePawn                        = new WkBk[1806];


		public WkBk( int index, bool pawn )
		{
			this.index = index;
			this.pawn = pawn;
		}

		public WkBk( Field wk, Field bk, Pieces pieces ) : this(wk, bk, pieces.ContainsPawn)
		{
		}

		public WkBk( Field wk, Field bk, bool pawn )
		{
			if (pawn)
				index = wkBkToWkBkIndexPawn[bk.Value * 64 + wk.Value];
			else
				index = wkBkToWkBkIndexNoPawn[bk.Value * 64 + wk.Value];
			this.pawn = pawn;
		}

		
		public WkBk( int index, Pieces pieces )
		{
			this.index = index;
			this.pawn = pieces.ContainsPawn;
		}

		static WkBk()
		{
			for ( int i=0 ; i<wkBkToWkBkIndexNoPawn.Length ; i++ )
				wkBkToWkBkIndexNoPawn[i] = -1;			
			for ( int i=0 ; i<wkBkToWkBkIndexPawn.Length ; i++ )
				wkBkToWkBkIndexPawn[i] = -1;			
			int index = 0;

			// without pawn
			for ( Field wk=Field.A1 ; wk<Field.Count ; wk++ ) {
				for ( Field bk=Field.A1 ; bk<Field.Count ; bk++ ) {
					if ( !Field.IsDist0or1(wk,bk) ) {
						MirrorType m = MirrorNormalize.WkBkToMirror( wk, bk, false );
						Fields f = new Fields( wk, bk );
						f = f.Mirror( m );
						Field wkNew = f.Get(0), bkNew = f.Get(1);
						if ( wkBkToWkBkIndexNoPawn[bkNew.Value*64+wkNew.Value] == -1 ) {
							wkBkToWkBkIndexNoPawn[bkNew.Value*64+wkNew.Value]       = index;
							if ( (m&(MirrorType.MirrorOnDiagonal|MirrorType.MirrorOnVertical)) == MirrorType.None )
								wkBkToWkBkIndexPawn[bkNew.Value*64+wkNew.Value]     = index;
							wkBkIndexToWkBk[index++]                               = bkNew.Value*64+wkNew.Value;
						}
						wkBkToWkBkIndexNoPawn[bk.Value*64+wk.Value] = wkBkToWkBkIndexNoPawn[bkNew.Value*64+wkNew.Value];
					}
				}
			}

#if DEBUG
			if ( index != WkBk.First(false).Count.index )
				throw new Exception();
#endif

			// with pawn
			for ( Field wk=Field.A1 ; wk<Field.Count ; wk++ ) {
				for ( Field bk=Field.A1 ; bk<Field.Count ; bk++ ) {
					if ( !Field.IsDist0or1(wk,bk) ) {
						MirrorType m  = MirrorNormalize.WkBkToMirror( wk, bk, true  );
						Fields f = new Fields( wk, bk );
						f = f.Mirror( m );
						Field wkNew = f.Get(0), bkNew = f.Get(1);
						if ( wkBkToWkBkIndexPawn[bkNew.Value*64+wkNew.Value] == -1 ) {
							wkBkToWkBkIndexPawn[bkNew.Value*64+wkNew.Value]       = index;
							wkBkIndexToWkBk[index++]                          = bkNew.Value*64+wkNew.Value;
						}
						wkBkToWkBkIndexPawn[bk.Value*64+wk.Value] = wkBkToWkBkIndexPawn[bkNew.Value*64+wkNew.Value];
					}
				}
			}

#if DEBUG
			if ( index != WkBk.First(true).Count.index )
				throw new Exception();
#endif

			foreach ( bool pawn in Tools.BoolArray ) {
				for ( WkBk wkbk=WkBk.First(pawn) ; wkbk<wkbk.Count ; wkbk++ ) {
					List<Field> wMList = new List<Field>(), bMList = new List<Field>();

					for ( Field kDst=Field.A1 ; kDst<Field.Count ; kDst++ ) {
						if ( kDst!=wkbk.Wk && Field.IsDist0or1(kDst,wkbk.Wk) && !Field.IsDist0or1(kDst,wkbk.Bk) )   // wk move to kDst possible
							wMList.Add( kDst );
						if ( kDst!=wkbk.Bk && Field.IsDist0or1(kDst,wkbk.Bk) && !Field.IsDist0or1(kDst,wkbk.Wk) )   // bk move to kDst possible
							bMList.Add( kDst );
					}

					if ( pawn )
						toInfoPawn[wkbk.Index] = new WkBkInfo( wMList.ToArray(), bMList.ToArray() );
					else 
						toInfoNoPawn[wkbk.Index] = new WkBkInfo( wMList.ToArray(), bMList.ToArray() );
				}

				List<WkBkMvInfo>[] arr = new List<WkBkMvInfo>[WkBk.GetCount(pawn).Index];
				for ( int i=0 ; i<arr.Length ; i++ )
					arr[i] = new List<WkBkMvInfo>();

				for ( WkBk wkbk=WkBk.First(pawn) ; wkbk<wkbk.Count ; wkbk++ ) {
					for ( int i=0 ; i<wkbk.Info.CountW ; i++ ) {
						WkBkMvInfo mvInfo = new WkBkMvInfo( wkbk, true, wkbk.Info.GetMv(true,i) );
						arr[mvInfo.WkBkDst.Index].Add( mvInfo );
					}
				}
				for ( WkBk wkbk=WkBk.First(pawn) ; wkbk<wkbk.Count ; wkbk++ ) {
					wkbk.Info.DstToWmvInfo = arr[wkbk.Index].ToArray();
					arr[wkbk.Index].Clear();
				}

				for ( WkBk wkbk=WkBk.First(pawn) ; wkbk<wkbk.Count ; wkbk++ ) {
					for ( int i=0 ; i<wkbk.Info.CountB ; i++ ) {
						WkBkMvInfo mvInfo = new WkBkMvInfo( wkbk, false, wkbk.Info.GetMv(false,i) );
						arr[mvInfo.WkBkDst.Index].Add( mvInfo );
					}
				}
				for ( WkBk wkbk=WkBk.First(pawn) ; wkbk<wkbk.Count ; wkbk++ )
					wkbk.Info.DstToBmvInfo = arr[wkbk.Index].ToArray();
			}


			for ( WkBk wkBk=WkBk.First(false) ; wkBk<wkBk.Count ; wkBk++ )
				reverseNoPawn[wkBk.Index] = new WkBk( wkBk.Bk, wkBk.Wk, false );
			for ( WkBk wkBk=WkBk.First(true) ; wkBk<wkBk.Count ; wkBk++ )
				reversePawn[wkBk.Index] = new WkBk( wkBk.Bk, wkBk.Wk, true );
		}

		public bool IsIllegal
		{
			get{ return index==-1; }
		}

		public BitBrd Bits
		{
			get{ return Wk.AsBit | Bk.AsBit; }
		}

		public static WkBk First( Pieces p )
		{
			return new WkBk( 0, p );
		}

		public static WkBk First( bool pawn )
		{
			return new WkBk( 0, pawn );
		}

		public static WkBk GetCount( Pieces pieces )
		{
			return First(pieces).Count;
		}

		public static WkBk GetCount( bool pawn )
		{
			return First(pawn).Count;
		}
		
		public WkBk Count
		{
			get{
				return new WkBk( pawn ? 1806 : 462, true );
			}
		}

		public int Index
		{
			get{ return index; }
		}

		public bool Pawn
		{
			get{ return pawn; }
		}

		public WkBk Reverse()
		{
			if ( pawn )
				return reversePawn[index];
			else
				return reverseNoPawn[index];
		}

		public WkBk Mirror( MirrorType mt )
		{
			return new WkBk( Wk.Mirror(mt), Bk.Mirror(mt), pawn );
		}

		public WkBk SameWkBkWithPawn
		{
			get { return new WkBk(index, true); }
		}

		public WkBkInfo Info
		{
			get{ 
				if ( pawn )
					return toInfoPawn[index];
				else
					return toInfoNoPawn[index];
			}
		}

		public Field K( bool w )
		{
			return w ? Wk : Bk;
		}

		public Field Wk
		{
			get{ return new Field( WkBkInts%64 ); }
			set{
				if ( pawn )
					index = wkBkToWkBkIndexPawn[ Bk.Value * 64 + value.Value ];
				else
					index = wkBkToWkBkIndexNoPawn[ Bk.Value * 64 + value.Value ];
			}
		}

		public Field Bk
		{
			get{ return new Field( WkBkInts/64 ); }
			set{
				if ( pawn )
					index = wkBkToWkBkIndexPawn[ value.Value * 64 + Wk.Value ];
				else
					index = wkBkToWkBkIndexNoPawn[ value.Value * 64 + Wk.Value ];
			}
		}

		private int WkBkInts
		{
			get{ return wkBkIndexToWkBk[index]; }
		}
        

		public static WkBk operator ++( WkBk a )
		{
			a.index++;
			return a;
		}
        
		public static bool operator ==( WkBk a, WkBk b )
		{
			return a.index == b.index;
		}

		public static bool operator !=( WkBk a, WkBk b )
		{
			return a.index != b.index;
		}

		public static bool operator >( WkBk a, WkBk b )
		{
			return a.index > b.index;
		}

		public static bool operator <( WkBk a, WkBk b )
		{
			return a.index < b.index;
		}


		public static WkBk operator +( WkBk a, int b )
		{
			a.index = a.Index + b;
			return a;
		}


		public static bool operator <=( WkBk a, WkBk b )
		{
			return a.index <= b.index;
		}

		public static bool operator >=( WkBk a, WkBk b )
		{
			return a.index >= b.index;
		}

		public static int WkBkIndexMvToWkBkIndexAndMirrorWtm( int wkBkIndexSrc, int mvIndex, out MirrorType m )
		{
			int val = wkBkIndexToNewWkBkIndexMirListWtm[wkBkIndexSrc*9+mvIndex];
			m= MirrorType.None;
			if ( val == -1 )
				return -1;
			m = (MirrorType)(val&0xf);
			return val >> 4;
		}

		public static int WkBkIndexMvToWkBkIndexAndMirrorBtm( int wkBkIndexSrc, int mvIndex, out MirrorType m )
		{
			int val = wkBkIndexToNewWkBkIndexMirListBtm[wkBkIndexSrc*9+mvIndex];
			m= MirrorType.None;
			if ( val == -1 )
				return -1;
			m = (MirrorType)(val&0xf);
			return val >> 4;
		}

		public override string ToString()
		{
			return "(" + Wk.ToString() + "," + Bk.ToString() + ") idx=" + index.ToString() + "/" + Count.Index.ToString();
		}

		public override int GetHashCode()
		{
			return index;
		}

		public override bool Equals(object obj)
		{
			throw new Exception();
		}

        public int CompareTo( WkBk other )
		{
            return index.CompareTo( other.index );
        }

#if DEBUG
		public bool Is( Field wk, Field bk )
		{
			return Wk == wk && Bk == bk;
		}
#endif
    }
}
