using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public struct Pos
	{
		private   Fields              fields;
		private   Pieces              pieces;
		private   WkBk                wkBk;


		public Pos( Pos pos )
		{
			fields = pos.fields;
			pieces = pos.pieces;
			wkBk = pos.wkBk;
		}


		public Pos( WkBk wkBk, Pieces pieces, Fields fields )
		{
			this.wkBk   = wkBk;
			this.pieces = pieces;
			this.fields = fields;
		}


		public Fields Fields
		{
			get{ return fields; }
			set{ fields = value; }
		}


		public Pieces Pieces
		{
			get{ return pieces; }
			set { pieces = value; }
		}


		public int Count
		{
			get{ return pieces.PieceCount; }
		}


		public bool IsW( int idx )
		{
			return pieces.IsW( idx );
		}


		public int CountW
		{
			get{ return pieces.CountW; }
		}


		public int CountB
		{
			get{ return pieces.CountB; }
		}


		public Field WK
		{
			get{ return wkBk.Wk; }
		}


		public Field BK
		{
			get{ return wkBk.Bk; }
		}


		public void RemovePiece( int index )
		{
			pieces = pieces.RemovePiece( index );
			fields = fields.Remove( index );
			wkBk = new WkBk( wkBk.Wk, wkBk.Bk, pieces );
		}


		public void AddPiece( Piece p, bool w, Field f )
		{
			int indexToAddPromPiece = Pieces.GetIndexToAdd( w, p );
			pieces = pieces.Add( w, p );
			fields = fields.Insert( indexToAddPromPiece, f );
			wkBk = new WkBk( wkBk.Wk, wkBk.Bk, pieces );
		}


		public bool GetIsValid( bool wtm )
		{
			Fields f = Fields;
			bool valid = PiecesSeperate() && !MoveCheck.IsCheck( pieces, f, WK, BK, !wtm );
			return valid;
		}


		/// <summary>
		/// Precondition: EP position
		/// </summary>
		/// <param name="wtm"></param>
		/// <param name="epDblStepDst"></param>
		/// <param name="cpStm"></param>
		/// <param name="cpSntm"></param>
		/// <returns></returns>
		public bool GetIsValid( bool wtm, Field epDblStepDst, CheckAndPin cpStm, CheckAndPin cpSntm )
		{
			Field epDblStepSrc = EP.GetDblStepSrc(epDblStepDst);
			Field epCapDst     = EP.GetCapDst(epDblStepDst);
			Fields f = Fields;
			if ( !PiecesSeperate() )
				return false;
			

			Field kStm  = wtm ? WK : BK;
			Field kSntm = wtm ? BK : WK;

			if ( !IsFieldEmpty( epCapDst ) || !IsFieldEmpty( epDblStepSrc ) )
				return false; // illegal: double step pawn move wasn't possible because fields are occupied

			cpSntm.Create( Pieces, Fields );
			if (cpSntm.IsCheck)
				return false;

			cpStm.Create( Pieces, Fields );

			// check position before double step of pawn
			Pos p = this;
			int epPawnDblStpDstIndex = FToPieceIndex( epDblStepDst );
			p.SetPiecePos( epPawnDblStpDstIndex, epDblStepSrc );
			cpStm.Create( p.Pieces, p.Fields );
			if ( cpStm.IsCheck )
				return false;

			return true;
		}

		public bool PiecesSeperate()
		{
			ulong bit = wkBk.Wk.AsBit.Value | wkBk.Bk.AsBit.Value;
			for ( int i=0; i<Count ; i++ ) {
				ulong bitNew = bit | GetPiecePos(i).AsBit.Value;
				if ( bit == bitNew )
					return false;
				bit = bitNew;
			}
			return true;
		}


		public Field GetPiecePos(int index)
		{
			return fields.Get(index);
		}


		public void SetPiecePos( int index, Field f )
		{
			fields = fields.SetNew( index, f );
		}


		public Piece GetPieceType( int index )
		{
			return pieces.GetPieceType( index );
		}


		public WkBk WkBk
		{
			get{ return wkBk; }
			set{ wkBk = value; }
		}


		public int FToPieceIndex( Field field )
		{
			for ( int i=0 ; i<Count ; i++ ) {
				if ( GetPiecePos(i) == field )
					return i;
			}
			return -1;
		}


		public bool IsFieldEmpty( Field field )
		{
			if ( WK==field || BK==field )
				return false;
			for ( int i=0 ; i<Count ; i++ ) {
				if ( GetPiecePos(i) == field )
					return false;
			}
			return true;
		}


		public BitBrd OccFldBits
		{
			get{ 
				BitBrd bits = wkBk.Wk.AsBit | wkBk.Bk.AsBit;;
				for ( int i=0 ; i<Count ; i++ )
					bits |= GetPiecePos(i).AsBit;
				return bits;
			}
		}

		public int FirstPiece( bool w )
		{
			return w ? 0 : CountW;
		}

		public int LastPiecePlusOne( bool w )
		{
			return w ? CountW : Count;
		}


  
		public static Pos FromIndexPos( IndexPos indexPos )
		{
			return new Pos( indexPos.WkBk, indexPos.Pieces, indexPos.GetFields() );
		}


		public static Pos FromIndexPosEp( IndexPos indexPos, out Field epDblStepDst, out Field epCapSrc )
		{
			return new Pos( indexPos.WkBk, indexPos.Pieces, indexPos.GetFieldsEP( out epDblStepDst, out epCapSrc ) );
		}

		#region operators and overridden methods
		
		public static bool operator==( Pos pos1, Pos pos2 )
		{
			return pos1.Pieces==pos2.Pieces && pos1.WkBk==pos2.WkBk && Fields.Compare( pos1.Fields, pos2.Fields, pos1.Count )==0;
		}

		public static bool operator!=( Pos pos1, Pos pos2 )
		{
			return !(pos1.Pieces==pos2.Pieces && pos1.WkBk==pos2.WkBk && Fields.Compare( pos1.Fields, pos2.Fields, pos1.Count )==0 );
		}

		public static bool CompareUnsorted( Pos pos1, Pos pos2 )
		{
			return pos1.Pieces == pos2.Pieces && pos1.WkBk == pos2.WkBk && Fields.CompareUnsorted(pos1.Fields, pos2.Fields, pos1.CountW) && Fields.CompareUnsorted(pos1.Fields.Remove(0,pos1.CountW), pos2.Fields.Remove(0,pos2.CountW), pos1.CountB);
		}



		public override int GetHashCode()
		{
			return fields.Bits.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			throw new Exception();
		}
		

		public string ToString( bool wtm )
		{
			return ToString() + (wtm ? " wtm" : " btm");
		}	

		public override string ToString()
		{
			string s = pieces.ToString() + " " + wkBk.Wk.ToString();
			for (int i = 0; i < CountW; i++)
				s += " " + fields.Get(i).ToString();
			s += " " + wkBk.Bk.ToString();
			for (int i = CountW; i < Count; i++)
				s += " " + fields.Get(i).ToString();
			return s;
		}

		#endregion
		


	}
}
