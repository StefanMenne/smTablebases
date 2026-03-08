using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public struct Move
	{
		public int     PieceIndex;
		public Field   Dest;
		public int     CapturePieceIndex;
		public Piece     Prom;

		private const int PieceIndexK = 99;

		public Move( Field dest, int capturePieceIndex ) : this( PieceIndexK, dest, capturePieceIndex )
		{
		}

		public Move( int pieceIndex, Field dest, int capturePieceIndex )
		{
			PieceIndex          = pieceIndex;
			Dest                = dest;
			CapturePieceIndex   = capturePieceIndex;
			Prom                = Piece.PW;
		}

		public Move( int pieceIndex, Field dest, int capturePieceIndex, Piece prom )
		{
			PieceIndex          = pieceIndex;
			Dest                = dest;
			CapturePieceIndex   = capturePieceIndex;
			Prom                = prom;
		}

		public bool isK
		{
			get { return PieceIndex == PieceIndexK; }
		}

		public bool isProm
		{
			get { return Prom != Piece.PW; }
		}

		public bool IsCapture
		{
			get{ return CapturePieceIndex != -1; }
		}

		public Move Mirror( MirrorType m )
		{
			return new Move( PieceIndex, Dest.Mirror( m ), CapturePieceIndex, Prom );
		}

		public Move MirrorBack( MirrorType m )
		{
			return new Move( PieceIndex, Dest.MirrorBack( m ), CapturePieceIndex, Prom );
		}

		public override string ToString()
		{
			return "PieceIdx=" + PieceIndex.ToString() + " Dest=" + Dest.ToString() + " Cap=" + CapturePieceIndex.ToString() ;
		}
	}
}
