using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	// Hides mirroring from user
	public sealed class UserPos
	{
		private static object lockTBaccess = new object();
		private Pos        pos;
		private MirrorType mirror;
		private bool       wtm;
		private Field      epCapDst;


		public UserPos( Field wk, Field bk, Pieces pieces, Fields fields, bool wtm )
		{
			mirror = MirrorNormalize.WkBkToMirror( wk, bk, pieces );
			pos = new Pos( new WkBk( wk, bk, pieces ), pieces, fields.Mirror(mirror) );
			this.wtm = wtm;
			epCapDst = Field.No;
		}


		public UserPos( Field wk, Field bk, Pieces pieces, Fields fields, bool wtm, Field epCapDst )
		{
			mirror = MirrorNormalize.WkBkToMirror( wk, bk, pieces );
			pos = new Pos( new WkBk( wk, bk, pieces ), pieces, fields.Mirror(mirror) );
			this.wtm = wtm;
			this.epCapDst = epCapDst.IsNo ? Field.No : epCapDst.Mirror(mirror);
		}


		public UserPos( Pieces pieces, Field[] fields, bool wtm, Field epCapDst )
		{
			Init( pieces, fields, wtm, epCapDst );
		}


		public UserPos( UserPos p )
		{
			mirror = p.mirror;
			wtm    = p.wtm;
			pos    = new Pos( p.Pos.WkBk, p.Pos.Pieces, p.Pos.Fields );
			epCapDst = p.epCapDst;
		}


		public UserPos( string s )
		{
			string[] sp = s.Split( ' ' );
			Pieces pieces = Pieces.FromString( sp[0] );
			Field[] f = new Field[pieces.PieceCount+2];
			for ( int i=0 ; i<f.Length ; i++ )
				f[i] = new Field( sp[i+1] );
			bool wtm = sp[f.Length+1]=="wtm";
			Init( pieces, f, wtm, Field.No );
		}


		private void Init( Pieces pieces, Field[] fields, bool wtm, Field epCapDst )
		{
			Field wk = fields[0];
			Field bk = fields[pieces.CountW+1];
			mirror = MirrorNormalize.WkBkToMirror( wk, bk, pieces );
			Fields f = new Fields();
			int idx = 0;
			for ( int i=0 ; i<fields.Length ; i++ )
				if ( i!=0 && i != pieces.CountW+1 )
					f = f.SetNew( idx++, fields[i] );
			pos = new Pos( new WkBk( wk, bk, pieces ), pieces, f.Mirror(mirror) );
			this.epCapDst = epCapDst.IsNo ? Field.No : (epCapDst.Mirror(mirror));
			this.wtm = wtm;
		}


		public bool Wtm
		{
			get { return wtm; }
		}

		public Pos Pos
		{
			get{ return pos; }
		}

		public Pieces Pieces
		{
			get { return pos.Pieces; }
		}

		public Fields Fields
		{
			get { return pos.Fields.MirrorBack(mirror); }
		}


		public bool IsIllegal
		{
			get{ return WkBk.IsIllegal; }
		}

		public List<MoveInfo> GetMoves()
		{
			List<MoveInfo> mvInfo = new List<MoveInfo>();
			if ( !IsIllegal ) {
				List<Move> mv = new List<Move>();
				MoveGen.CalcMv( mv, pos, wtm, epCapDst );
				for ( int i=0 ; i<mv.Count ; i++ ) {
					mv[i] = mv[i].MirrorBack( mirror );
					mvInfo.Add( GetMoveInfo( mv[i] ) );
				}
			}
			return mvInfo;
		}


		public Field GetPiecePos( int index )
		{
			return pos.GetPiecePos( index ).MirrorBack(mirror);
		}

		public void SetPiecePos( int index, Field f )
		{
			pos.SetPiecePos( index, f.Mirror(mirror) );
		}

		public Piece GetPieceType( int index )
		{
			return pos.GetPieceType( index );
		}

		public bool IsW( int index )
		{
			return pos.IsW( index );
		}

		public int Count
		{
			get{ return pos.Count; }
		}

		public Field Wk
		{
			get{ return pos.WkBk.Wk.MirrorBack( mirror ); }
		}

		public Field Bk
		{
			get{ return pos.WkBk.Bk.MirrorBack( mirror ); }
		}

		public WkBk WkBk
		{
			get { return pos.WkBk;  }
		}

		public Field EpCapDst
		{
			get{ return epCapDst.IsNo ? Field.No : epCapDst.MirrorBack( mirror ); }
		}



		public void SetWkBk( Field wk, Field bk )
		{
			MirrorType oldMir = mirror;
			mirror            = MirrorNormalize.WkBkToMirror( wk, bk, pos.Pieces );
			pos.WkBk          = new WkBk( wk, bk, pos.Pieces );
			pos.Fields        = pos.Fields.MirrorBack( oldMir ).Mirror( mirror );
		}

		public IndexPos ToIndexPos( out long index )
		{
			if ( epCapDst.IsNo ) {
				IndexPos ip = new IndexPos( pos.WkBk, pos.Pieces, wtm );
				ip.SetFields( pos.Fields );
				index = ip.GetIndex();
				return ip;
			}
			else {
				IndexPos ip = new IndexPos( pos.WkBk, pos.Pieces, wtm );
				long count = ip.IndexCount;
				Fields f = Fields;
				for ( index=0 ; index<count ; index++ ) {
					ip.SetToIndex( index );
					Field epDblStepDst, epCapSrc;
					if ( ip.GetIsEp(index) && Fields.Compare( ip.GetFieldsEP( out epDblStepDst, out epCapSrc ), f, pos.Pieces.PieceCount ) == 0 ) {
						return ip;
					}
				}
				return null;
			}
		}

		public Res GetResult()
		{
			lock ( lockTBaccess ) {
				if ( WkBk.IsIllegal )
					return Res.IllegalPos;
				else
					return TBaccess.GetResult( Pieces, Wk, Bk, wtm, Fields, epCapDst );
			}
		}


		private MoveInfo GetMoveInfo( Move move )
		{
			Piece   p   = move.isK ? Piece.K : GetPieceType( move.PieceIndex );
			Field src = move.isK ? (wtm?Wk:Bk) : GetPiecePos( move.PieceIndex ) ;
			string moveString = p.AsCharacter + src.ToString() + (move.IsCapture ? "x" : "-") + move.Dest.ToString();

			if ( move.isProm )
				moveString += "(" + move.Prom.AsCharacter + ")";


			UserPos newPos = DoMove( move );
			return new MoveInfo( src, move.Dest, newPos, moveString, Res.IllegalPos );
		}

		private UserPos DoMove( Move move )
		{
			UserPos newPos = new UserPos( this );
			newPos.epCapDst = Field.No;
			if ( move.isK ) {
				Field wkNew = wtm?move.Dest:Wk;
				Field bkNew = wtm?Bk:move.Dest;
				newPos.SetWkBk( wkNew, bkNew );
			}
			else {
				Field src = newPos.GetPiecePos(move.PieceIndex);
				newPos.SetPiecePos( move.PieceIndex, move.Dest );
				if ( newPos.GetPieceType(move.PieceIndex).IsP && src.IsPawnGrndLine(wtm) && move.Dest.IsPawnFourthLine(wtm) ) {
					Field[] ep = EP.GetEp(newPos.Pieces, newPos.Fields, !wtm);
					if ( ep!=null ) {
						for ( int i=0 ; i<ep.Length; i++ ) {
							if ( ep[i] ==  EP.GetCapDst(move.Dest) )
								newPos.epCapDst = ep[i];
						}
					}
				}
			}


			if ( move.IsCapture )
				newPos.RemovePiece( move.CapturePieceIndex );

			if ( move.isProm ) {
				newPos.RemovePiece( move.PieceIndex );     // remove pawn
				newPos.AddPiece( wtm, move.Prom, move.Dest );
			}

			newPos.wtm = !wtm;
			return newPos;
		}

		private void RemovePiece( int index )
		{
			Field wk = Wk;
			Field bk = Wk;
			pos.RemovePiece( index );
			mirror = MirrorNormalize.WkBkToMirror( wk, bk, Pieces );
		}

		private void AddPiece( bool w, Piece piece, Field f )
		{
			Field wk = Wk;
			Field bk = Wk;
			pos.AddPiece( piece, w, f.Mirror(mirror) );
			mirror = MirrorNormalize.WkBkToMirror( wk, bk, Pieces );
		}

		public override string ToString()
		{
			string s = Pieces.ToString() + " " + Wk.ToString();
			for ( int i=0 ; i<pos.CountW ; i++ )
				s += " " + Fields.Get(i).ToString();
			s += " " + Bk.ToString();
			for ( int i=pos.CountW ; i<Count ; i++ )
				s += " " + Fields.Get(i).ToString();
			if ( !epCapDst.IsNo )
				s += " EP=" + epCapDst.ToString();
			return s + (wtm?" wtm": " btm");
		}


		public static bool operator ==( UserPos pos1, UserPos pos2 )
		{
			if ( object.Equals(pos1,null) || object.Equals(pos2,null) )
				return object.Equals(pos1, null) && object.Equals(pos2, null);
			else
				return Pos.CompareUnsorted(pos1.Pos,pos2.Pos) && pos1.mirror == pos2.mirror && pos1.wtm==pos2.wtm && pos1.epCapDst==pos2.epCapDst;
		}


		public static bool operator !=( UserPos pos1, UserPos pos2)
		{
			return !(pos1==pos2);
		}


		public override int GetHashCode()
		{
			return pos.GetHashCode();
		}


		public override bool Equals(object obj)
		{
			throw new Exception();
		}



	}
}
