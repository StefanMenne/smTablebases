using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBacc
{
	public class IndexEnumerator
	{
		private int[]      pgSrcWeightIndexToPgIndex;                // pgIndexSrc = pgIndexDst
		private int[][]    srcIdxToDstIdx;                           // src index to dst index inside same piece group; sorted by src weight
		private IndexPos   indexPosSrc, indexPosDst;
		private int        dstIsInvalidBits                   = 0;

		// first loop optimization
		private int        pgi0;                 
		private PieceGroup   pgSrc0,pgDst0;
		private int        pgSrc0IndexCount;
		private int[]      srcIdxToDstIdx0;
		private long       pgDst0Weight;


		// for validity check
		private Piece               pieceTypeSrc0;
		private Field             kStm, kSntm;
		private BitBrd            blocked, check, occ, mvBits0;
		private readonly BitBrd   valid0;
		private BitBrd            allowed;   // generally allowed fields without regarding other pieces;
		                                     // allowed fields for piece group 0 without fields that would give a check
		private BitBrd            valid;
		private BitBrd[]          checkFields, blockedArray, checkArray, occArray;             

		// It applies:   validInfoIndexSrcFirst   <=   currentIndexSrcPg0   <   validInfoIndexSrcNext
		// validInfoIndexSrcFirst points to the first pos where one piece out of PG(lowest weight) is on A1 
		// validInfoIndexSrcNext  points to the next pos where the same holds
		private int     validInfoIndexSrcFirst=0, validInfoIndexSrcNext=int.MaxValue;   // values for case pieceGroupSrc0.PieceCount = 1

		private Fields     fields                       = new Fields();
		private bool       keepFieldsUpToDate           = false;
		private long       overallIndexSrcWithoutPg0    = 0;  
		private long       overallIndexDstWithoutPg0    = 0;  

		private bool       endReached                   = false;


		public IndexEnumerator( IndexPos src, IndexPos dst=null, bool keepFieldsUpToDate=true )
		{
			indexPosSrc                           = src;
			pgSrcWeightIndexToPgIndex             = new int[src.PieceGroupCount];
			this.keepFieldsUpToDate               = keepFieldsUpToDate;
			checkFields                             = new BitBrd[src.PieceGroupCount];
			kStm                                  = indexPosSrc.Wtm ? indexPosSrc.WkBk.Wk : indexPosSrc.WkBk.Bk;
			kSntm                                 = indexPosSrc.Wtm ? indexPosSrc.WkBk.Bk : indexPosSrc.WkBk.Wk;
			blockedArray                          = new BitBrd[src.PieceGroupCount];
			checkArray                              = new BitBrd[src.PieceGroupCount];
			occArray                              = new BitBrd[src.PieceGroupCount];
			blockedArray[src.PieceGroupCount-1]     = MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehindInverse( kSntm, kStm );
			checkArray[src.PieceGroupCount-1]         = BitBrd.Empty;
			occArray[src.PieceGroupCount-1]         =	indexPosSrc.WkBk.Bits;

			for ( int i=0 ; i<src.PieceGroupCount ; i++ ) {
				int origIndex                          = src.PieceGroupReorder.WeightIndexToOrigIndex(i);
				pgSrcWeightIndexToPgIndex[i]           = origIndex;
				PieceGroup pg                          = indexPosSrc.GetPieceGroup(origIndex);
				checkFields[i]                         = (indexPosSrc.Wtm==pg.PieceIsW) ? (pg.PieceType.GetCapBackBitsInclProm( kSntm )) : BitBrd.Empty;
			}
			pgi0                   = pgSrcWeightIndexToPgIndex[0];
			pgSrc0                 = indexPosSrc.GetPieceGroup(pgi0);
			pgSrc0IndexCount       = pgSrc0.IndexCount;
			pieceTypeSrc0            = pgSrc0.PieceType;
			mvBits0                = (indexPosSrc.Wtm==pgSrc0.PieceIsW) ? (pieceTypeSrc0.GetCapBackBitsInclProm( kSntm )) : BitBrd.Empty;
			allowed                = ~mvBits0;
			valid0                 = pieceTypeSrc0.IsP ? (~BitBrd.Line1AndLine8) : BitBrd.All;

			if ( dst != null ) {
				indexPosDst                           = dst;
				srcIdxToDstIdx                        = new int[src.PieceGroupCount][];

				for ( int i=0 ; i<src.PieceGroupCount ; i++ ) {
					// Get the same PieceGroup in Src and Dst and calculate the index-change.
					// Start with lowest weight in Src.
					int origIndex                          = src.PieceGroupReorder.WeightIndexToOrigIndex(i);
					pgSrcWeightIndexToPgIndex[i]           = origIndex;
					srcIdxToDstIdx[i]                      = new int[src.GetPieceGroup(origIndex).IndexCount];
					for ( int j=0 ; j<srcIdxToDstIdx[i].Length ; j++ ) {
						src.GetPieceGroup(origIndex).Index = j;
						Fields flds = src.GetPieceGroup(origIndex).GetFields();
						if ( !flds.IsNo/*only for compression with PieceGroup2*/ && dst.GetPieceGroup(origIndex).SetFields( flds<<src.GetPieceGroup(origIndex).FirstPieceIndex ) )
							srcIdxToDstIdx[i][j] = dst.GetPieceGroup(origIndex).Index;
						else
							srcIdxToDstIdx[i][j] = -1;
					}
				}
				pgDst0             = indexPosDst.GetPieceGroup(pgi0);
				srcIdxToDstIdx0    = srcIdxToDstIdx[0];
				pgDst0Weight       = indexPosDst.GetWeight(pgi0);
			}

			Reset();
		}


		public bool Reset( long offset=0, bool setToNextValid=false )
		{
			endReached = false;
			for ( int i=0 ; i<pgSrcWeightIndexToPgIndex.Length ; i++ )
				SetPieceGroupIndex(i,pgSrcWeightIndexToPgIndex[i],0);
			AddToSrcIndex( offset );
			if ( setToNextValid && !GetIsValid() )
				return NextValid();
			return true;
		}


		public Fields Fields
		{
			get { 
#if DEBUG
				if ( !keepFieldsUpToDate )
					throw new Exception();
#endif
				return fields; 
			}
		}


		public bool EndReached
		{
			get { return endReached; }
		} 


		public bool IndexPosDstValid
		{
			get { return dstIsInvalidBits == 0; }
		}

		
		public void AddToSrcIndex( long delta )
		{
			for ( int i=0 ; i<pgSrcWeightIndexToPgIndex.Length&&delta!=0 ; i++ ) {
				int pgIndex = pgSrcWeightIndexToPgIndex[i];
				delta += indexPosSrc.GetPieceGroup(pgIndex).Index;
				int newIndex = (int) (delta % indexPosSrc.GetPieceGroup(pgIndex).IndexCount);
				SetPieceGroupIndex( i, pgIndex, newIndex );
				delta = delta / indexPosSrc.GetPieceGroup(pgIndex).IndexCount;
			} 
			if ( delta == 0 ) {
				overallIndexSrcWithoutPg0 = indexPosSrc.GetIndex() - pgSrc0.Index;
				UpdateIsValidInfo( pgSrcWeightIndexToPgIndex.Length-1 );
				if ( keepFieldsUpToDate )
					fields = fields.SetNew( pgSrc0.FirstPieceIndex, new Field( pgSrc0.IndexToField( pgSrc0.Index-validInfoIndexSrcFirst ) ) );
				if ( indexPosDst!=null )
					overallIndexDstWithoutPg0 = indexPosDst.GetIndexExceptOnePieceGroup(pgi0);
			}
			else
				endReached = true;
		}


		public void IncSrcIndex()
		{
			int newIndex = pgSrc0.Index+1;
			if ( newIndex < pgSrc0IndexCount ) {
				
				
				// speed optimization
				// this block equals: SetPieceGroupIndex( 0, pgi0, newIndex );
				pgSrc0.Index    = newIndex;
				if ( newIndex == validInfoIndexSrcNext )
					UpdateIsValidInfo( 0 );   
				if ( keepFieldsUpToDate )
					fields = fields.SetNew( pgSrc0.FirstPieceIndex, new Field( pgSrc0.IndexToField( newIndex-validInfoIndexSrcFirst ) ) );

				
				if ( indexPosDst != null ) {
					int newIndexDst = srcIdxToDstIdx0[newIndex];
					pgDst0.Index    = newIndexDst;
					dstIsInvalidBits &= (unchecked((int)0xbfffffff));   
					dstIsInvalidBits |= (newIndexDst&0x40000000);   
				}					
			}
			else { 
				if ( NextInSecondPieceGroup() ) {
					if ( keepFieldsUpToDate )
						fields = fields.SetNew( pgSrc0.FirstPieceIndex, new Field( pgSrc0.IndexToField( pgSrc0.Index-validInfoIndexSrcFirst ) ) );
				}
				else
					endReached = true;
			}
		}


		public bool NextValid()
		{
			BitBrd val = valid  & (BitBrd.AllWithoutA1<<pgSrc0.IndexToField(pgSrc0.Index-validInfoIndexSrcFirst));

			while( val == 0 ) {	
				if ( validInfoIndexSrcNext < pgSrc0IndexCount ) {
					pgSrc0.Index    = validInfoIndexSrcNext;
					UpdateIsValidInfo( 0 );
				}
				else {
					if ( !NextInSecondPieceGroup() ) {
						endReached = true;
						return false;		
					}
				}
				val = valid  &  ((BitBrd.All)<<pgSrc0.IndexToField(pgSrc0.Index-validInfoIndexSrcFirst));
			}
				
				
			Field f = val.LowestField;
			if ( keepFieldsUpToDate )
				fields = fields.SetNew( pgSrc0.FirstPieceIndex, f );
			int newIndex = validInfoIndexSrcFirst + pgSrc0.FieldToIndex( f.Value );

			pgSrc0.Index    = newIndex;
			if ( newIndex >= validInfoIndexSrcNext )
				throw new Exception();
			if ( indexPosDst != null ) {
				int newIndexDst = srcIdxToDstIdx0[newIndex];
				pgDst0.Index    = newIndexDst;
				dstIsInvalidBits &= (unchecked((int)0xbfffffff));   
				dstIsInvalidBits |= (newIndexDst&0x40000000);   
			}
			return true;		
		}


		 // same as indexPosSrc.GetIndex()
		public long IndexSrc
		{
			get{ return overallIndexSrcWithoutPg0 + pgSrc0.Index; }
		}


		 // same as indexPosDst.GetIndex()
		public long IndexDst
		{
			get{ return overallIndexDstWithoutPg0 + pgDst0.Index * pgDst0Weight; }
		}


		public bool GetIsValid()
		{
			return valid.Contains( pgSrc0.IndexToField(pgSrc0.Index-validInfoIndexSrcFirst) );
		}


		/// <summary>
		/// Increments the index at least until minIndex but no more than minIndex+64.
		/// So it holds    minIndex &lt;= IndexSrc &lt minIndex+64
		/// The amount of valid positions that were skipped are returned.
		/// returns false if the end is reached.
		/// </summary>
		public int CountValid( long minIndex = long.MaxValue )
		{
			int count = 0;
			while ( IndexSrc < minIndex ) {
				count += valid.BitCount;
				if ( validInfoIndexSrcNext < pgSrc0IndexCount ) {
					pgSrc0.Index    = validInfoIndexSrcNext;
					UpdateIsValidInfo( 0 );
				 }
				else {
					if ( !NextInSecondPieceGroup() ) {
						endReached = true;
						return count;
					}
				}
			} 
			return count;
		}


		private bool NextInSecondPieceGroup()
		{
			overallIndexSrcWithoutPg0 += pgSrc0.IndexCount;
			SetPieceGroupIndex( 0, pgi0, 0 );
			for ( int i=1 ; i<pgSrcWeightIndexToPgIndex.Length ; i++ ) {
				int  pgIndex  = pgSrcWeightIndexToPgIndex[i];
				int newIndex = indexPosSrc.GetPieceGroup(pgIndex).Index+1;
				if ( newIndex < indexPosSrc.GetPieceGroup(pgIndex).IndexCount ) {
					SetPieceGroupIndex( i, pgIndex, newIndex );
					UpdateIsValidInfo( i );
					if ( indexPosDst!=null )
						overallIndexDstWithoutPg0 = indexPosDst.GetIndexExceptOnePieceGroup(pgi0);
					return true;
				}
				else {
					SetPieceGroupIndex( i, pgIndex, 0 );
				}
			}
			return false;
		}


		private void SetPieceGroupIndex( int pgWeightedIndex, int pgi, int index )
		{
			indexPosSrc.GetPieceGroup(pgi).Index = index;
			if ( indexPosDst != null ) {
				index = srcIdxToDstIdx[pgWeightedIndex][index];
				indexPosDst.GetPieceGroup( pgi ).Index = index;

				dstIsInvalidBits &= ( (unchecked((int)0xbfffffff))>>pgWeightedIndex );   // delete the first 2+pgi bits from left
				dstIsInvalidBits |= (index&0x40000000) >> pgWeightedIndex;               // set the first 2+pgi bits from left if index is negative (more exact is -1)
				// similar code but with branch
				//if ( index == -1 )
				//	dstIsInvalidBits |= (1<<pgi);
				//else
				//	dstIsInvalidBits &= ~(1<<pgi);

			}
		}


		private void UpdateIsValidInfo( int highestPieceGroupToUpdate )
		{
			blocked            = blockedArray[highestPieceGroupToUpdate];
			check                = checkArray[highestPieceGroupToUpdate];
			occ                = occArray[highestPieceGroupToUpdate];

			for ( int i=highestPieceGroupToUpdate ; i>=1 ; i-- ) {
				int      pgIndex = pgSrcWeightIndexToPgIndex[i];
				PieceGroup pg      = indexPosSrc.GetPieceGroup(pgIndex);
				Fields   fl      = pg.GetFields();

				if ( fl.IsNo ) {
					occ = BitBrd.All;
					break;
				}

				if ( keepFieldsUpToDate )
					fields = fields.Overwirte( pg.FirstPieceIndex, pg.PieceCount, fl );

				BitBrd newFields = BitBrd.Empty;
				BitBrd checkBits   = checkFields[i];
				for ( int j=0 ; j<pg.PieceCount ; j++ ) {
					Field  f = fl.Get(j);
					BitBrd b = f.AsBit;
					newFields |= b;
					if ( ( b & checkBits ).IsNotEmpty )    // check
						check |= b;
					blocked &= MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehindInverse( kSntm, f ) | b;
				}
				if ( (occ & newFields).IsNotEmpty || ( pg.PieceType.IsP && (newFields&BitBrd.Line1AndLine8).IsNotEmpty ) ) {
					occ = BitBrd.All;
				}
				occ |= newFields;

				blockedArray[i-1] = blocked;
				checkArray[i-1]     = check;
				occArray[i-1]     = occ;
			}


			BitBrd validFields = BitBrd.All;
			if ( pgSrc0.PieceCount >= 2 ) {
				Fields fl = pgSrc0.GetFields();
				if ( keepFieldsUpToDate )
					fields = fields.Overwirte( pgSrc0.FirstPieceIndex, pgSrc0.PieceCount, fl );

				BitBrd newFields = BitBrd.Empty;
				for ( int j=1 ; j<pgSrc0.PieceCount ; j++ ) {
					Field  f = fl.Get(j);
					BitBrd b = f.AsBit;
					newFields |= b;
					if ( ( b & mvBits0 ).IsNotEmpty )    // check
						check |= b;
					blocked &= MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehindInverse( kSntm, f ) | b;
				}

				if ( (occ & newFields).IsNotEmpty || ( pieceTypeSrc0.IsP && (newFields&BitBrd.Line1AndLine8).IsNotEmpty ) ) {
					occ = BitBrd.All;
				}
				occ |= newFields;

				// set validInfoIndexSrcFirst and validInfoIndexSrcNext ; not needed if pgSrc0 is PieceGroup1  
				validInfoIndexSrcFirst = pgSrc0.Index - pgSrc0.FieldToIndex(fl.Get(0).Value);
				if ( pgSrc0.IsPieceGroup2 ) 
					validInfoIndexSrcNext  = 64 + validInfoIndexSrcFirst;
				else 
					validInfoIndexSrcNext  = pgSrc0.FieldToIndex(fl.Get(1).Value) + validInfoIndexSrcFirst;
				validFields = new BitBrd( (1UL<<fl.Get(1).Value)-1 );     // Binary:  0000...01...1    to ensure fld0 < fld1
			}

			BitBrd c = check & blocked;
			if ( c.IsEmpty ) {  // no check
				valid = allowed | (~blocked);
			}
			else {
				BitBrd lowestBit = c.LowestBit;
				if ( c == lowestBit ) {  // check
					BitBrd checkBlockingFields = MoveCheck.PiecePosAnotherPiecePos_To_FieldsBetween( c.LowestField, kSntm );
					valid = checkBlockingFields & allowed;
				}
				else {   // double check
					valid = BitBrd.Empty;
				}
			}
			valid &= valid0 & validFields & ~occ;
		}
	}
}
