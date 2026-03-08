using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBacc
{
	/// <summary>
	/// Stores up to 7 Pieces and a counter with values in [0,15]
	/// Furthermore it stores CountW and Count with 0...7
	/// </summary>
	public struct PieceGroupInfo
	{
		private Int64 val; // always 4 Bit


		public Piece GetPiece( int index )
		{
			return Piece.IntToPiece2[ (val>>(index<<3))&15 ];
		}


		public int GetPieceCount( int index )
		{
			return (int) ((val>>(4+(index<<3)))&15);			
		}


		public void SetPiece( int index, Piece piece )
		{
			int firtBit = (index<<3);
			val &= ~(15L<<firtBit);
			val |= ((long)piece.AsInt2)<<firtBit;
		}


		public void SetPieceCount( int index, int count )
		{
			int firtBit = 4+(index<<3);
			val &= ~(15L<<firtBit);
			val |= ((long)count)<<firtBit;
		}


		public int Count
		{
			get {
				return (int)(val>>60);
			}
			set {
				val = (val & 0xfffffffffffffffL) | (((Int64)value)<<60);
			}
		}


		public int CountW
		{
			get {
				return (int)((val>>56)&15);
			}
			set {
				val = (val & 0x70ffffffffffffffL) | (((Int64)value)<<56);
			}
		}


		public void Add( Piece piece, int count )
		{
			SetPiece( Count, piece );
			SetPieceCount( Count++, count ); 
		}


		public void Add( Piece piece, int count, bool isW )
		{
			SetPiece( Count, piece );
			SetPieceCount( Count++, count ); 
			if ( isW )
				CountW++;
		}


	}
}
