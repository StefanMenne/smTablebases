using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBacc
{
	// only used to improve compression;     PieceGroupX is used also for PieceGroups with 2 pieces
	public class PieceGroup2 : PieceGroup
	{
		public PieceGroup2( Pieces pieces, WkBk wkBk, int pieceCount, Piece pieceType, bool pieceIsW, bool wtm, int firstPieceIndex, IndexPosType type ) : base( pieces, wkBk, pieceCount, pieceType, pieceIsW, wtm, firstPieceIndex, type )
		{
			indexCount = 64 * 64;
		}
		
		 
		public override Fields GetFields()
		{
			int a = index&63, b = index>>6;
			return new Fields( new Field(a), new Field(b), a>=b );
		}


		public override bool SetFields( Fields f )
		{
			throw new NotImplementedException();
		}


		public override Fields ReplaceField( Fields f, Field toReplace, Field newField )
		{
			Field a = new Field(index&63), b = new Field(index>>6);
			if ( a==toReplace )
				return b<newField ? new Fields( b, newField ) : new Fields( newField, b );
			else
				return a<newField ? new Fields( a, newField ) : new Fields( newField, a );
		}


		public override bool SetToFirstWithOccField( Field field )
		{
			throw new NotImplementedException();
		}


		public override bool NextIndexWithOccField( Field field )
		{
			throw new NotImplementedException();
		}


		public override NextIndexWithOccFieldsInfo SetToFirstWithOccFields( BitBrd fields )
		{
			throw new NotImplementedException();
		}


		public override bool NextIndexWithOccFields( ref NextIndexWithOccFieldsInfo info )
		{
			throw new NotImplementedException();
		}


		public override int NextEpIndex( int index )
		{
			while( ++index<indexCount ) {
				int f1 = index&63, f2 = index>>6;
				if ( ( ((1UL<<f1)|(1UL<<f2)) & overlapIndices ) != 0UL )
					return index;
			}
			return -1;
		}
		public override int NextEpIndex( int index, BitBrd overlapSubset )
		{
			while( ++index<indexCount ) {
				int f1 = index&63, f2 = index>>6;
				if ( ( ((1UL<<f1)|(1UL<<f2)) & overlapSubset ) != 0UL )
					return index;
			}
			return -1;
		}


		public override Fields ReplaceFields( Fields f )
		{
			int a = index&63, b = index>>6;
			f = f.SetNew( firstPieceIndex,   new Field( a ) );
			return f.SetNew( firstPieceIndex+1, new Field( b ) );
		}


		public override bool SetSortedFields( Fields f )
		{
			throw new NotImplementedException();
		}


		public override BitBrd GetBitBrd()
		{
			int a = index>>6, b = index&63;
			return new Field(a).AsBit | new Field(b).AsBit; 
		}


		public override BitBrd GetFields( int index )
		{
			int a = index>>6, b = index&63;
			return new Field(a).AsBit | new Field(b).AsBit; 
		}


		public override int GetBackMvCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, int pieceIdx, Fields sntmPawns, int sntmPawnsCount, long weight )
		{
			throw new NotImplementedException();
		}


		public override int GetBackMvNoCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, Fields sntmPawns, int sntmPawnsCount, long weight )
		{
			throw new NotImplementedException();
		}


		public override bool IsPieceGroup2
		{
			get{ return true; }
		}
	}
}
