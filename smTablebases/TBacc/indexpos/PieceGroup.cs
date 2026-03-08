using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public abstract class PieceGroup
	{
		/// <summary>
		/// To understand index to piece calculation see following table
		///  index       piecePosIndex      bits
		///    0          0, 1           110000000
		///    1          0, 2           101000000
		///    2          1, 2           011000000
		///    3          0, 3           100100000
		///    4          1, 3           010100000
		///    5          2, 3           001100000
		///    6          0, 4           100010000
		///    7          1, 4           010010000
		///    8          2, 4           001010000
		///    9          3, 4           000110000
		///    
		///  e.g. for index = 8 
		///  
		///  the first index where bit nr. 4 is set is 6 and this can be calced by choosing 2 out of 4
		///  the first index where bit nr. 2 is set in the remaining left part is calculated by choosing 1 out of 2
		///  
		/// </summary>

		protected           int        index;
		protected  readonly int        pieceCount;
		protected  readonly byte[]     indexToField;
		protected  readonly byte[]     fieldToIndex;
		protected           int        indexCount;
		protected  readonly int        firstPieceIndex;
		protected  readonly BitBrd     allowedFields     = new BitBrd();
		protected  readonly Piece      pieceType;
		protected  readonly bool       pieceIsW;
		protected           ulong      overlapIndices    = 0;    // overlapIndices for P
		private    readonly bool       wtm;



		public static PieceGroup Create( Pieces pieces, WkBk wkBk, int pieceCount, Piece pType, bool pieceIsW, bool wtm, int firstPieceIndex, IndexPosType type )
		{
			if ( pieceCount == 1 )
				return new PieceGroup1( pieces, wkBk, pType, pieceIsW, wtm, firstPieceIndex, type );
			else if ( pieceCount==2 && type==IndexPosType.Compress )
				return new PieceGroup2( pieces, wkBk, pieceCount, pType, pieceIsW, wtm, firstPieceIndex, type );
			else 
				return new PieceGroupX( pieces, wkBk, pieceCount, pType, pieceIsW, wtm, firstPieceIndex, type );
		}
		

		protected PieceGroup( Pieces pieces, WkBk wkBk, int pieceCount, Piece pType, bool pieceIsW, bool wtm, int firstPieceIndex, IndexPosType type )
		{
			this.pieceCount                 = pieceCount;
			this.pieceType                = pType;
			this.pieceIsW                   = pieceIsW;
			this.wtm                      = wtm;
			this.firstPieceIndex            = firstPieceIndex;

			if ( type == IndexPosType.Calc )
				PieceGroupIndexTables.Get( pieces, wkBk, pType, pieceIsW, wtm, out indexToField, out fieldToIndex );			
			else if ( type == IndexPosType.Verify )
				PieceGroupIndexTables.Get64( pieces, wkBk, pType, pieceIsW, wtm, out indexToField, out fieldToIndex );
			else if ( type == IndexPosType.Compress )
				PieceGroupIndexTables.Get64( pieces, wkBk, pType, pieceIsW, wtm, out indexToField, out fieldToIndex );
			else
				throw new Exception();

			indexCount = (int)Tools.ChooseKOutOfN( pieceCount, IndexCountOnePiece );// ChooseKOutOfN( 0...5, 0...62 )
			
			ulong tmp = 0;
			for ( int i=0 ; i<indexToField.Length ; i++ )
				tmp |= (1UL<<indexToField[i]);
			allowedFields = new BitBrd( tmp );
		}


		public void Init( BitBrd overlapFields )
		{
			if ( pieceType.IsP ) {
				while ( overlapFields.IsNotEmpty ) {
					Field f = overlapFields.LowestField;
					overlapIndices |= 1UL<<(fieldToIndex[f.Value]);
					overlapFields &= ~f.AsBit;
				}
			}
		}


		public bool PieceIsW
		{
			get{ return pieceIsW; }
		}

		public int Index
		{
			get{ return index; }
			set{ index = value; }
		}

		public int IndexCount
		{
			get{ return indexCount; }
		}

		public int IndexCountOnePiece
		{
			get { return indexToField.Length; }
		}


		public Piece PieceType
		{
			get{ return pieceType; }
		}


		public int FirstPieceIndex
		{
			get { return firstPieceIndex; }
		}


		public int GetMvCountBound()
		{
			return pieceCount * pieceType.MvBound;
		}


		public int PieceCount
		{
			get{ return pieceCount; }
		}


		public int LastPieceIndexPlusOne
		{
			get{ return firstPieceIndex+pieceCount; }
		}


		public BitBrd AllowedFields
		{
			get{ return allowedFields; }
		}


		
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="f">overlapping not allowed</param>
		/// <returns>true success; false unblockable check</returns>
		public abstract bool SetFields(Fields f);


		/// <returns>true success; false unblockable check</returns>
		public abstract bool SetSortedFields(Fields f);


		public abstract Fields GetFields();


		public abstract BitBrd GetBitBrd();
		public abstract BitBrd GetFields( int index );


		public abstract int GetBackMvNoCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, Fields sntmPawns, int sntmPawnsCount, long weight);


		public abstract int GetBackMvCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, int pieceIdx, Fields sntmPawns, int sntmPawnsCount, long weight );


		/// <summary>
		/// Only used for cap. The "field/fields" must be occupied that it can be captured
		/// Does not support PieceGroupIndex-Reordering.
		/// </summary>
		public abstract bool SetToFirstWithOccField( Field field );
		public abstract bool NextIndexWithOccField( Field field );
//		public abstract NextIndexWithOccFieldsInfoSequential SetToFirstWithOccFieldsSequential( BitBrd fields );
//		public abstract bool NextIndexWithOccFieldsSequential( NextIndexWithOccFieldsInfoSequential info );
		public abstract NextIndexWithOccFieldsInfo SetToFirstWithOccFields( BitBrd fields );
		public abstract bool NextIndexWithOccFields( ref NextIndexWithOccFieldsInfo info );
		

		/// <summary>
		/// Used for GetIsEp. Don't changes the state of the pieceGroup.
		/// returns the next index > currentIndex which holds the overlapping criteria. Pass -1 for first index.
		/// returns -1 if no more indices are available.
		/// 
		/// overlapSubset specifies subset of overlapFields which to enumerate
		/// 
		/// Supports PieceGroupIndexReordering
		/// </summary>
		public abstract int NextEpIndex( int currentIndex );
		public abstract int NextEpIndex( int currentIndex, BitBrd overlapSubset );


		/// <summary>
		/// Replace one Field for a given set of Fields. 
		/// Precondition: In fields are sorted, toReplace must exist in f, newField must be valid in PieceGroup
		/// Output fields are sorted.
		/// </summary>
		public abstract Fields ReplaceField( Fields f, Field toReplace, Field newField );

		public abstract Fields ReplaceFields( Fields f);


		public virtual bool IsPieceGroup2
		{
			get { return false; }
		}


		public override string ToString()
		{
			return GetFields().ToString( pieceCount );
		}


		public int IndexToField( int index )
		{
			return indexToField[index];
		}
		public int FieldToIndex( int index )
		{
			return fieldToIndex[index];
		}

		/// <summary>
		/// Get all possible moves for one piece of this PieceGroup.
		/// </summary>
		protected BitBrd GetMvBits( Field src, Fields sntmPawns, int sntmPawnsCount, BitBrd occFld )
		{
			BitBrd mvBits = pieceType.GetMvBackBits(src);
			if ( pieceType.IsP && TBacc.EP.IsEpPos(src,pieceIsW,sntmPawns,sntmPawnsCount) )   // this function is only called for non ep pos; therefore if a sntm pawn stands on some ep-field the double step pawn back move has to be filtered
				mvBits = mvBits ^ (src + (pieceIsW ? -16 : 16) ).AsBit;   // remove double step

			return Piece.RemoveBlockingMvBits( src, mvBits, occFld );
		}


		/// <summary>
		/// Get all possible cap moves for one piece of this PieceGroup.
		/// </summary>
		protected BitBrd GetCapBits( Field src, Fields sntmPawns, int sntmPawnsCount, BitBrd occFld )
		{
			return Piece.RemoveBlockingMvBits( src, pieceType.GetCapBackBits(src), occFld );
		}
	}
}