using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public class PieceGroupX : PieceGroup
	{
		private Values64[] indexToPieceIndices;


		public PieceGroupX( Pieces pieces, WkBk wkBk, int pieceCount, Piece pieceType, bool pieceIsW, bool wtm, int firstPieceIndex, IndexPosType type ) : base( pieces, wkBk, pieceCount, pieceType, pieceIsW, wtm, firstPieceIndex, type )
		{
			indexToPieceIndices = KchooseNindex.GetIndexToValuesTable( pieceCount );
		}


		public override Fields GetFields()
		{
			Values64 val = IndexToPieceIndices( index );
			Fields f = new Fields();
			for ( int i=0 ; i<pieceCount ; i++ )
				f = f.SetNew( i, new Field( indexToField[val.Get(i)] ) );
			return f;
		}


		public override Fields ReplaceField( Fields f, Field toReplace, Field newField )
		{
			Fields fout          = new Fields();
			int    cnt           = 0;
			int    indexNewField = fieldToIndex[newField.Value];

			for ( int i=0 ; i<pieceCount ; i++ ) {
				Field curr = f.Get(i);
				if ( curr == toReplace )
					continue;
				if ( indexNewField < fieldToIndex[curr.Value] ) {
					fout = fout.SetNew( cnt++, newField );
					indexNewField = 255;
				}
				fout = fout.SetNew( cnt++, curr );
			}

			if ( indexNewField != 255 )	  // new Field not yet added; add as last
				fout = fout.SetNew( cnt++, newField );
				
			return fout;
		}


		public override bool SetToFirstWithOccField( Field field )
		{
			int pieceIdx = fieldToIndex[field.Value];
			if ( pieceIdx == 255 )
				return false;
			index = (pieceIdx<pieceCount) ? 0 : (int)Tools.ChooseKOutOfN( pieceCount, pieceIdx ); // ChooseKOutOfN( 0...7, 0...62 )
			return true;
		}


		public override bool NextIndexWithOccField( Field field )
		{
			int pieceIdx = fieldToIndex[field.Value];
			while ( ++index!=indexCount ) {
				int indexOf = IndexToPieceIndices(index).IndexOf(pieceIdx, pieceCount);
				if ( indexOf!=-1 )
					return true;
			}
			return false;
		}



		public override NextIndexWithOccFieldsInfo SetToFirstWithOccFields( BitBrd fields )
		{
			NextIndexWithOccFieldsInfo info = new NextIndexWithOccFieldsInfo();
			int       allOccIdxCnt    = 0;
			Values64  allOccIdx       = new Values64();
			BitBrd    occFieldsBitBrd = fields;

			while ( fields.IsNotEmpty ) {
				Field  f         = fields.LowestField;
				int    i         = fieldToIndex[f.Value];
				allOccIdx.Set( allOccIdxCnt++, i );
				fields=fields.XorField(f);
			}

			info.AllOccIdx    = allOccIdx;
			info.AllOccIdxCnt = allOccIdxCnt;

			info.OccIdxToIdx = new int[allOccIdxCnt];
			info.OthIdxToIdx = new int[IndexCountOnePiece-allOccIdxCnt];
			int j=0, k=0;
			for ( int i=0 ; i<IndexCountOnePiece ; i++ ) {
				if ( occFieldsBitBrd.Contains(new Field(indexToField[i])) )
					info.OccIdxToIdx[j++] = i;
				else
					info.OthIdxToIdx[k++] = i;
			}

			NextIndexWithOccFields(ref info);
			return info;
		}

		public override bool NextIndexWithOccFields( ref NextIndexWithOccFieldsInfo info )
		{
			if ( ++info.OtherIdx >= info.OtherIdxCnt ) {
				info.OtherIdx = 0;
				if ( ++info.OccIdx >= info.OccIdxCnt ) {
					info.OccIdx = 0;
					if ( info.CurOccCnt++ == Math.Min( info.AllOccIdxCnt, pieceCount ) )
						return false;
					info.OccIdxCnt   = (int)Tools.ChooseKOutOfN( info.CurOccCnt, info.AllOccIdxCnt );
					info.OtherIdxCnt = (int)Tools.ChooseKOutOfN( pieceCount-info.CurOccCnt, IndexCountOnePiece-info.AllOccIdxCnt );
					info.indexToPieceIndicesOcc   = KchooseNindex.GetIndexToValuesTable( info.CurOccCnt );
					info.indexToPieceIndicesOther = KchooseNindex.GetIndexToValuesTable( pieceCount-info.CurOccCnt );

				}
			}

			Values64 indicesOcc = info.indexToPieceIndicesOcc[info.OccIdx];
			Values64 indicesOth = info.indexToPieceIndicesOther[info.OtherIdx];

			Bits indicesBits = new Bits();
			for ( int i=0 ; i<info.CurOccCnt ; i++ )
				indicesBits |= Bits.SingleBitSet(info.OccIdxToIdx[indicesOcc.Get(i)]);
			for ( int i=0 ; i<pieceCount-info.CurOccCnt ; i++ )
				indicesBits |= Bits.SingleBitSet(info.OthIdxToIdx[indicesOth.Get(i)]);

			int k=0;
			index = 0;
			while( indicesBits.IsNotEmpty ) {
				Bits lowestBit = indicesBits.LowestBit;
				int i = lowestBit.OneBitValue;
				index += (int)Tools.ChooseKOutOfN( ++k, i ); // Parameter range: ChooseKOutOfN( 1...7, 0...62 )		
				indicesBits &= ~lowestBit;		
			}

			return true;
		}



		/// <summary>
		/// Supports PieceGroupIndices reordering
		/// </summary>
		public override int NextEpIndex( int index )
		{
			while( ++index<indexCount ) {
				Values64 val = IndexToPieceIndices(index);
				for ( int i=0 ; i<pieceCount ; i++ ) {
					if ( ( (1UL<<val.Get(i)) & overlapIndices ) != 0UL )
						return index;
				}
			}
				
			return -1;
		}
		public override int NextEpIndex( int index, BitBrd overlapSubset )
		{
			while( ++index<indexCount ) {
				Values64 val = IndexToPieceIndices(index);
				for ( int i=0 ; i<pieceCount ; i++ ) {
					if ( ( (1UL<<val.Get(i)) & overlapIndices ) != 0UL && ((new Field(indexToField[val.Get(i)]).AsBit)&overlapSubset).IsNotEmpty )
						return index;
				}
			}
				
			return -1;
		}


		public override Fields ReplaceFields( Fields f )
		{
			Values64 val = IndexToPieceIndices(index);
			for ( int i=0 ; i<pieceCount ; i++ )
				f = f.SetNew( i+firstPieceIndex, new Field( indexToField[val.Get(i)] ) );
			return f;
		}


		public override bool SetSortedFields( Fields f )
		{
			int[] indices = new int[pieceCount];
			f = f >> firstPieceIndex;

			for ( int i=0 ; i<indices.Length ; i++ ) {
				if ( (indices[i]=fieldToIndex[f.Get(i).Value]) == 255 )
					return false;	
			}

			index=0;
			for ( int i=0 ; i<indices.Length ; i++ )
				index += (int)Tools.ChooseKOutOfN( i+1, indices[i] ); // ChooseKOutOfN( 0...7, 0...62 )
			return true;
		}



		public override bool SetFields( Fields f )
		{
			int[] indices = new int[pieceCount];
			f = f >> firstPieceIndex;

			for ( int i=0 ; i<indices.Length ; i++ ) {
				if ( (indices[i]=fieldToIndex[f.Get(i).Value]) == 255 )
					return false;	
			}


			// sort indices now
			if ( pieceCount == 2 ) {
				if ( indices[0] > indices[1] ) {
					int tmp = indices[0];
					indices[0] = indices[1];
					indices[1] = tmp;
				}
			}
			else {
				for ( int i=0 ; i<indices.Length ; i++ ) {
					for ( int j=i+1 ; j<indices.Length ; j++ ) {
						if ( indices[i] > indices[j] ) {
							int tmp = indices[i];
							indices[i] = indices[j];
							indices[j] = tmp;
						}
					}
				}
			}
#if DEBUG
			if ( Config.DebugGeneral ) { 
				for ( int i=0 ; i<indices.Length-1 ; i++ ) {
					if ( indices[i] > indices[i+1] )
						throw new Exception();
				}
			}
#endif

			index=0;
			for ( int i=0 ; i<indices.Length ; i++ )
				index += (int)Tools.ChooseKOutOfN( i+1, indices[i] ); // Parameter range: ChooseKOutOfN( 1...7, 0...62 )
			return true;
		}


		public override BitBrd GetBitBrd()
		{
			ulong bb = 0UL;
			Values64 val = IndexToPieceIndices( index );
			for ( int i=0 ; i<pieceCount ; i++ )
				bb |= 1UL<<indexToField[val.Get(i)];
			return new BitBrd( bb );
		}


		public override BitBrd GetFields( int index )
		{
			BitBrd bb = new BitBrd();
			Values64 val = IndexToPieceIndices( index );
			for ( int i=0 ; i<pieceCount ; i++ )
				bb |= new Field(indexToField[val.Get(i)]).AsBit;
			return bb;
		}


		public override int GetBackMvCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, int pieceIdx, Fields sntmPawns, int sntmPawnsCount, long weight )
		{
			f = f>>firstPieceIndex;
			pieceIdx -= FirstPieceIndex;
			KchooseNindex kChooseNindex = null;
			int[] pieceIdxPos = new int[pieceCount];
			int idxInvalid = FieldsToIndices( f, pieceIdxPos );
			if ( idxInvalid != -1 && idxInvalid != pieceIdx  ) {
				return 0;
			}

			Field src = f.Get( pieceIdx );
			Fields fs = f;
			fs = fs.Remove( pieceIdx );

			kChooseNindex = new KchooseNindex( indexToField.Length, pieceCount, pieceIdxPos, pieceIdx );

			BitBrd mvBits = GetCapBits( src, sntmPawns, sntmPawnsCount, occFld );
			int count = 0;
			BitsToIndices( mv, ref count, mvBits, indexAdd, fs, kChooseNindex, weight );
			return count;
		}


		public override int GetBackMvNoCapDestIndex( long[] mv, BitBrd occFld, long indexAdd, Fields f, Fields sntmPawns, int sntmPawnsCount, long weight )
		{
			int count = 0;
			int[]          pieceIdxPos                   = new int[pieceCount];
			f = f>>firstPieceIndex;
			int idxInvalid = FieldsToIndices( f, pieceIdxPos );
			if ( idxInvalid == -2 )  // two pieces are on an illegal field(non blocking check); no legal positions can be generated
				return 0;
			else if ( idxInvalid == -1 ) { // no piece on illegal field
				KchooseNindex kChooseNindex = new KchooseNindex( indexToField.Length, pieceCount, pieceIdxPos, 0 );
				Field  src      = f.Get( 0 );
				Fields fs       = f;
				fs = fs.RemoveFirst();
				GetMoves( mv, ref count, src, sntmPawns, sntmPawnsCount, occFld, indexAdd, fs, kChooseNindex, weight );

				for ( int pieceIdx = 1 ; pieceIdx < pieceCount ; pieceIdx++ ) {
					src      = f.Get(pieceIdx);
					fs       = f;
					fs = fs.Remove( pieceIdx );
					kChooseNindex.ReplaceChoosenValue( pieceIdxPos[pieceIdx-1], pieceIdxPos[pieceIdx], pieceIdx );
					GetMoves( mv, ref count, src, sntmPawns, sntmPawnsCount, occFld, indexAdd, fs, kChooseNindex, weight );
				}	
			} 
			else { // one piece on an illegal field; this piece has to move
				int    pieceIdx = idxInvalid;
				Field  src      = f.Get(pieceIdx);
				Fields fs       = f;
				fs = fs.Remove( pieceIdx );
				KchooseNindex kChooseNindex = new KchooseNindex( indexToField.Length, pieceCount, pieceIdxPos, pieceIdx );
				GetMoves( mv, ref count, src, sntmPawns, sntmPawnsCount, occFld, indexAdd, fs, kChooseNindex, weight );
			}



			return count;
		}


		private void GetMoves( long[] mv, ref int count, Field src, Fields sntmPawns, int sntmPawnsCount, BitBrd occFld, long indexAdd, Fields fs, KchooseNindex kChooseNindex, long weight )
		{
			BitBrd mvBits = GetMvBits( src, sntmPawns, sntmPawnsCount, occFld );
			BitsToIndices( mv, ref count, mvBits, indexAdd, fs, kChooseNindex, weight );
		}

		private void BitsToIndices( long[] mv, ref int count, BitBrd mvBits, long indexAdd, Fields fs, KchooseNindex kChooseNindex, long weight )
		{
			int countPiecesBefore = 0;

			mvBits = mvBits & allowedFields;
			while ( mvBits.IsNotEmpty ) {
				Field  mvDst     = mvBits.LowestField;

				while ( countPiecesBefore<pieceCount-1 && fs.Get(countPiecesBefore)<mvDst )
					countPiecesBefore++;

				int dstIdx = fieldToIndex[mvDst.Value];
				mv[count++] = indexAdd + weight*( kChooseNindex.GetIndexForPeekValue(countPiecesBefore,dstIdx) );
				mvBits = mvBits.XorField(mvDst);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="indices"></param>
		/// <returns>illegal index, -1 no illegal index; -2 more than one illegal index</returns>
		private int FieldsToIndices( Fields f, int[] indices )
		{
			int idxInvalid = -1;
			for ( int i=0 ; i<pieceCount; i++) {
				int idx = fieldToIndex[f.Get(i).Value];
				indices[i] = idx;
				if ( idx == 255 ) {
					if ( idxInvalid == -1 )
						idxInvalid = i;
					else
						return -2;
				}
			}
			return idxInvalid;
		}


		private Bits FieldsToIndices( BitBrd fields )
		{
			Bits bits = new Bits();
			while ( fields.IsNotEmpty ) {
				Field  lowestField  = fields.LowestField;
				int    idx          = fieldToIndex[lowestField.Value];
				bits |= Bits.SingleBitSet( idx );
				fields=fields.XorField(lowestField);
			}
			return bits;
		}


		private Values64 IndexToPieceIndices( long index )
		{
			return indexToPieceIndices[index];
#if false
			// Calculation instead of large array.
			Values64 v64 = new Values64();

			int b = IndexCountOnePiece-1;    
			for ( int i=pieceCount ; i>=1 ; i-- ) {
				int a = i-1;       
				while ( a != b ) {
					int c = (a+b+1)>>1;

					if ( index >= Tools.ChooseKOutOfN(i,c) )
						a = c;
					else
						b = c - 1;
				}
				index -= Tools.ChooseKOutOfN(i,a);
				v64.Set( i-1, a );
			}

			return v64;
#endif
		}

		public static long GetMaxIndicesOverAllChunks( Piece p, int pieceCnt, bool wtm )
		{
			return Tools.ChooseKOutOfN( pieceCnt, 64 );
		}



	}
}
