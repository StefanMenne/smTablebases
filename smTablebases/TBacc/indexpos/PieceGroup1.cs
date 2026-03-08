using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public class PieceGroup1 : PieceGroup
	{
		public PieceGroup1( Pieces pieces, WkBk wkBk, Piece pieceType, bool pieceIsW, bool wtm, int firstPieceIndex, IndexPosType type ) : base( pieces, wkBk, 1, pieceType, pieceIsW, wtm, firstPieceIndex, type )
		{
		}


		public override Fields GetFields()
		{
			Fields f = new Fields();
			f = f.SetNew( 0, new Field( indexToField[index] ) );
			return f;
		}


		public override bool SetToFirstWithOccField( Field field )
		{
			index = fieldToIndex[field.Value];
			return index != 255;
		}


		public override bool NextIndexWithOccField ( Field field )
		{
			return false;
		}

		
		public override NextIndexWithOccFieldsInfo SetToFirstWithOccFields( BitBrd fields )
		{
			Field f = (fields&allowedFields).LowestField;   // (new BitBrd(0)).LowestBit=0; (new BitBrd(0)).Field=Field.No
			index = fieldToIndex[f.Value&63];    // & 63 will change Field.No=-1 to 63 
			return new NextIndexWithOccFieldsInfo(){BitsOcc=((f==Field.No)?Bits.All:(new Bits(fields.Value)))};
		}

		public override bool NextIndexWithOccFields( ref NextIndexWithOccFieldsInfo info )
		{
			BitBrd fields = new BitBrd( info.BitsOcc.Value );
			BitBrd curFld                     = (new Field(indexToField[index])).AsBit;   // 00000100000
			BitBrd allFldsBeforeCurAndItself  = (curFld-1)|curFld;                      // 00000111111
			fields = fields & allowedFields & ~allFldsBeforeCurAndItself;               // remove already passed back fields
			if ( fields.IsEmpty )
				return false;
			Field fNew = fields.LowestField;     // (new BitBrd(0)).LowestBit=0; (new BitBrd(0)).Field=Field.No
			index = fieldToIndex[fNew.Value&63];       // & 63 will change Field.No=-1 to 63 
			return fNew != Field.No;
		}

		

		/// <summary>
		/// Does not support PieceGroupIndex-Reordering
		/// </summary>
		public override int NextEpIndex( int index, BitBrd overlapSubset )
		{
			do {
				index=NextEpIndex(index);
			} while ( index!=-1 && ((new Field(indexToField[index])).AsBit & overlapSubset).IsEmpty );
			return index;
		}


		public override int NextEpIndex( int index )
		{
			ulong  curIdxBit                  = 1UL<<index;                                 // 00000100000
			ulong  allIdxBeforeCurAndItself   = (curIdxBit-1)|curIdxBit;                    // 00000111111
			ulong  indices                    = overlapIndices & ~allIdxBeforeCurAndItself; // 01010000000  allowed=01010001101
			
			if ( indices==0UL )
				return -1;
			else {
				ulong lowestIndex       = indices & ((ulong)(-((long)indices)));
				int   lowestBitIndex    = Tools.Log2( lowestIndex );
				return lowestBitIndex;
			}
		}


		public override Fields ReplaceFields( Fields f )
		{
			return f.SetNew( firstPieceIndex, new Field(indexToField[index]) );
		}


		public override bool SetSortedFields( Fields f )
		{
			index = fieldToIndex[f.Get(firstPieceIndex).Value];
			return index!=255;
		}


		public override bool SetFields( Fields f )
		{
			index = fieldToIndex[f.Get(firstPieceIndex).Value];
			return index != 255;
		}


		public override int GetBackMvCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, int pieceIdx, Fields sntmPawns, int sntmPawnsCount, long weight )
		{
			Field src = f.Get(firstPieceIndex);
			BitBrd mvBits = GetCapBits( src, sntmPawns, sntmPawnsCount, occFld );
			return BitsToIndices( mv, mvBits, indexAdd, weight );	
		}


		public override int GetBackMvNoCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, Fields sntmPawns, int sntmPawnsCount, long weight )
		{
			Field src = f.Get(firstPieceIndex);
			BitBrd mvBits = GetMvBits( src, sntmPawns, sntmPawnsCount, occFld );
			return BitsToIndices( mv, mvBits, indexAdd, weight );		
		}


		public override BitBrd GetBitBrd()
		{
			return new BitBrd(1UL<<indexToField[index]);
		}


		public override BitBrd GetFields( int index )
		{
			return new Field( indexToField[index] ).AsBit;
		}


		private int BitsToIndices( long[] mv, BitBrd mvBits, long indexAdd, long weight )
		{
			int count = 0;
			mvBits = mvBits & allowedFields;
			while ( mvBits.IsNotEmpty ) {
				Field  mvDst     = mvBits.LowestField;
				int    dstIdx    = fieldToIndex[mvDst.Value];
				mv[count++] = indexAdd + weight*dstIdx;
				mvBits = mvBits.XorField(mvDst);
			}
			return count;
		}


		public override Fields ReplaceField( Fields f, Field toReplace, Field newField )
		{
			f = f.SetNew(0, newField);
			return f;
		}


	}
}
