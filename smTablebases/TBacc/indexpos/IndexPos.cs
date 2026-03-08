using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public enum IndexPosType
	{
		Calc       = 0x01,  
		// Defines the data order during calculation of tablebases
		// All data in the DataChunkRead and DataChunkWrite are stored in this order
		// mainly optimized for speed and not for size
		// Currently no PieceGroup reordering is done except making Pawns highest weight for EP handling
		// also no PieceGroup index reordering

		Verify     = 0x02,  
		// Default reordering to make MD5 most compatible for future changes. Only used while calculating MD5.
		// Same as Calc without making Pawns highmost
		 
		Compress   = 0x04,  
		// Includes Illegal Indices to improve compression. 

	}


	public class IndexPos
	{
		public const  int     TotalMaxMvCount = 128;

		private   long                indexCount   = 1;
		private   WkBk                wkBk;
		private   Pieces              pieces;
		private   PieceGroup[]        pieceGroup, pieceGroupWeightSorted;
		private   readonly long[]     weight,   weightSorted;
		private   bool                wtm;
		private   int                 pieceGroupCountW = 0;
		private   int                 firstPieceGrpIdxToMv, lastPieceGrpIdxToMvPlus1;
		private   PieceGroupReorder   pieceGroupReorder;
		private   IndexPosType        type;
		
		// En Passant      see EP.cs
		private   BitBrd              overlapFields    = new BitBrd();
		private   PieceGroup          pieceGroupPw=null, pieceGroupPb=null, pieceGroupPawnSntm=null;
		private   int[]               overlapFieldToEp14Index;
		private   long                isEpHeuristicMin, isEpHeuristicMaxPlus1;
		private   bool                isEpHeuristicValue;

		// EP GetIsEp for general PieceGroupReordering (PawnPieceGroups not highest weight)		
		private   int                 pieceGroupPHighSortedIndex=-1, pieceGroupPLowSortedIndex=-1;


		public IndexPos( WkBk wkBk, Pieces pieces, bool wtm ) : this( wkBk, pieces, wtm, IndexPosType.Calc, null )
		{
		}

		public IndexPos( IndexPos ip ) : this( ip.WkBk, ip.Pieces, ip.Wtm, ip.type, ip.pieceGroupReorder )
		{
		}

		public IndexPos( WkBk wkBk, Pieces pieces, bool wtm, IndexPosType type, PieceGroupReorder pgr )
		{
			this.type = type;
			if ( type == IndexPosType.Calc ) {
				pieceGroupReorder = PieceGroupReorder.Get(pieces,wtm,PieceGroupReorderType.PawnHeighestWeight);
			}
			else if ( type == IndexPosType.Compress ) {
				pieceGroupReorder = pgr;
			}
			else if ( type == IndexPosType.Verify ) {   // MD5  ; use no reordering at all 
				pieceGroupReorder = PieceGroupReorder.Get(pieces,wtm,PieceGroupReorderType.NoReordering);
			}

			int                  pieceGroupCountW = 0;
			List<PieceGroup>     pieceGroupList                = new List<PieceGroup>();
	
			PieceGroupInfo pgi = pieces.GetPieceGroupInfo();			
			pieceGroupCountW   = pgi.CountW;


			int                firstPieceOfGroupIndex = 0;
			for ( int i=0 ; i<pgi.Count ; i++ ) {
				Piece pi    = pgi.GetPiece(i);
				int count = pgi.GetPieceCount(i);
				bool isW = i<pieceGroupCountW;

				PieceGroup pg = PieceGroup.Create(pieces,wkBk,count,pi,isW,wtm,firstPieceOfGroupIndex,type);
				
				pieceGroupList.Add( pg );
				if ( pi.IsP ) {
					if ( isW )
						pieceGroupPw = pg;
					else
						pieceGroupPb = pg;
				}
				
				firstPieceOfGroupIndex += pgi.GetPieceCount(i);
			}
			this.pieceGroup           = pieceGroupList.ToArray();
			weight                  = new long[pieceGroup.Length];
			weightSorted            = new long[pieceGroup.Length];
			pieceGroupWeightSorted    = new PieceGroup[pieceGroup.Length];

			for ( int sortedIndex=0 ; sortedIndex<pieceGroup.Length ; sortedIndex++ ) {
				int origIndex = pieceGroupReorder.WeightIndexToOrigIndex( sortedIndex );				
				weight[origIndex] = weightSorted[sortedIndex] = indexCount;
				pieceGroupWeightSorted[sortedIndex] = pieceGroup[origIndex];
				indexCount *= pieceGroup[origIndex].IndexCount;
			}

			if ( pieceGroupPw==null || pieceGroupPb==null ) {
				pieceGroupPw           = pieceGroupPb = null;
				isEpHeuristicMin     = 0;              // make heuristic return always false
				isEpHeuristicMaxPlus1     = indexCount;
				isEpHeuristicValue   = false;
			}
			else {
				if ( type == IndexPosType.Compress ) {
					// IndexPosType.Calc:    PawnPieceGroups are always highermost which makes IsEP-calculation simple; therefore pieceGroupPLowSortedIndex, pieceGroupPHighSortedIndex is not needed
					// IndexPosType.Verify:  No IsEP-calculation will be performed; therefore pieceGroupPLowSortedIndex, pieceGroupPHighSortedIndex is not needed
					// IndexPosType.Final:   calc pieceGroupPLowSortedIndex, pieceGroupPHighSortedIndex for GetIsEp-calculation 
					for ( int i=0 ; i<pieceGroupWeightSorted.Length ; i++ ) {
						if ( pieceGroupWeightSorted[i]==pieceGroupPw || pieceGroupWeightSorted[i]==pieceGroupPb ) {
							if ( pieceGroupPLowSortedIndex == -1 )
								pieceGroupPLowSortedIndex = i;
							else 
								pieceGroupPHighSortedIndex = i;
						}
					}
				}

				pieceGroupPawnSntm = wtm ? pieceGroupPb : pieceGroupPw;
				PieceGroup pieceGroupStm = wtm ? pieceGroupPw : pieceGroupPb;
				overlapFieldToEp14Index = new int[64];
				for ( int i=0 ; i<overlapFieldToEp14Index.Length ; i++ )
					overlapFieldToEp14Index[i] = -1;

				for ( int i=0 ; i<14 ; i++ ) {
					Field epDblStepDst, epCapSrc;
					EP.Index14ToFields( i, wtm, out epDblStepDst, out epCapSrc );
					Field     overlap   = EP.GetOverlap( epDblStepDst, epCapSrc, wkBk );
					if ( !overlap.IsNo && pieceGroupStm.AllowedFields.Contains(epCapSrc) ) {
						overlapFieldToEp14Index[overlap.Value]  = i;
						overlapFields                          |= overlap.AsBit;
					}
				}
			}


			this.pieces             = pieces;
			this.wtm                = wtm;
			this.wkBk               = wkBk;
			this.pieceGroupCountW     = pieceGroupCountW;
			firstPieceGrpIdxToMv      = wtm ? 0 : pieceGroupCountW;
			lastPieceGrpIdxToMvPlus1  = wtm ? pieceGroupCountW : pieceGroup.Length;

			for ( int i=0 ; i<pieceGroup.Length ; i++ )
				pieceGroup[i].Init( overlapFields );
			SetToIndex( 0 );
		}


		public PieceGroup[] GetPieceGroupsWeightSorted()
		{
			return pieceGroupWeightSorted;
		}


		public PieceGroup GetPieceGroupWeightSorted( int index )
		{
			return pieceGroupWeightSorted[index];
		}


		public PieceGroupReorder PieceGroupReorder
		{
			get{ return pieceGroupReorder; }
		}

		public bool WPawnAndBPawn
		{
			get{ return pieceGroupPw!=null; }
		}


		public long IndexCount
		{
			get { return indexCount; }
		}


		public static long GetMaxIndiciesOverAllChunks( Pieces p )
		{
			return Math.Max( GetMaxIndiciesOverAllChunks(p,false), GetMaxIndiciesOverAllChunks(p,true) );
		}


		public static long GetMaxIndiciesOverAllChunks( Pieces p, bool wtm )
		{
			PieceGroupInfo pgi  = p.GetPieceGroupInfo();
			int          cntW = pgi.CountW;
			bool  ep     = p.ContainsWpawnAndBpawn;
			long  countNoEp = 1;
			
			for ( int i=0 ; i<pgi.Count ; i++ ) {
				countNoEp *= PieceGroupX.GetMaxIndicesOverAllChunks( pgi.GetPiece(i), pgi.GetPieceCount(i), wtm );
			}

			return countNoEp;
		}


		public Fields GetFields()
		{
			Fields f = new Fields();
			for ( int i=0 ; i<pieceGroup.Length ; i++ )
				f = f | (pieceGroup[i].GetFields()<<pieceGroup[i].FirstPieceIndex);
			return f;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="f">sorted and no overlap</param>
		/// <returns>true success; false unblockable check</returns>
		public bool SetSortedFields( Fields f )
		{
			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				if ( !pieceGroup[i].SetSortedFields( f ) )
					return false;
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="f">can be unsorted; overlapping not allowed</param>
		/// <returns></returns>
		public bool SetFields( Fields f )
		{
			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				if ( !pieceGroup[i].SetFields( f ) )
					return false;
			}
			return true;
		}


		private bool SetSortedFieldsExceptOnePieceGroup( Fields f, int pieceGrpIdx )
		{
			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				if ( i != pieceGrpIdx ) {
					if ( !pieceGroup[i].SetSortedFields( f ) )
						return false;
				}
			}			
			return true;
		}


		private bool SetFieldsExceptOnePieceGroup( Fields f, int pieceGrpIdx )
		{
			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				if ( i != pieceGrpIdx ) {
					if ( !pieceGroup[i].SetFields( f ) )
						return false;
				}
			}			
			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="f">Must be sorted</param>
		/// <param name="mv"></param>
		/// <param name="wtm"></param>
		public int GetBackMvNoCapDestIndex( Fields f, long[] mv, bool wtm, int pieceGrpIdx, BitBrd occFld )
		{
			if ( !SetSortedFieldsExceptOnePieceGroup( f, pieceGrpIdx ) )
				return 0;

			// calc index without the index from the piece group where moves are generated
			long    indexAdd       = GetIndexExceptOnePieceGroup( pieceGrpIdx );
			Fields  sntmPawns       = Fields.No;
			int     sntmPawnsCount  = -1;
			if ( pieceGroupPawnSntm!=null ) {
				sntmPawns = pieceGroupPawnSntm.GetFields();
				sntmPawnsCount = pieceGroupPawnSntm.PieceCount;
			}
			return pieceGroup[pieceGrpIdx].GetBackMvNoCapDestIndex( mv, occFld, indexAdd, f, sntmPawns, sntmPawnsCount, weight[pieceGrpIdx] );
		}


		/// <summary>
		/// Gets all back capture move's.
		/// </summary>
		/// <param name="fUnsorted">The fields for all pieces. Piece groups might be unsorted. The captured and the capturing piece are on the same field.</param>
		/// <param name="mv">Output of all moves</param>
		/// <param name="wtm">white to move</param>
		/// <param name="pieceIdx">Piece to move</param>
		/// <param name="pieceGroupIdx">Piece group of piece to move</param>
		/// <param name="occFld">Occupied fields</param>
		public int GetBackMvCapDestIndex( Fields fUnsorted, long[] mv, bool wtm, int pieceIdx, int pieceGroupIdx, BitBrd occFld )
		{		
			if ( !SetFieldsExceptOnePieceGroup( fUnsorted, pieceGroupIdx ) ) {
				return 0;
			}

			// calc index without the index from the piece group where moves are generated
			long indexAdd = GetIndexExceptOnePieceGroup(pieceGroupIdx);

			return pieceGroup[pieceGroupIdx].GetBackMvCapDestIndex( mv, occFld, indexAdd, fUnsorted, pieceIdx, Fields.No, -1, weight[pieceGroupIdx] );
		}


		public int GetPieceGrpIdx( int pieceIdx )
		{
			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				if ( pieceIdx < ((PieceGroup)pieceGroup[i]).LastPieceIndexPlusOne )
					return i;
			}
			throw new Exception();
		}


		public int GetPieceGrpIdx( Piece p, bool pIsW )
		{
			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				PieceGroup pg = (PieceGroup)pieceGroup[i];
				if ( pg.PieceType == p && pg.PieceIsW==pIsW )
					return i;
			}
			throw new Exception();
		}


		public Pieces Pieces
		{
			get{ return pieces; }
		}


		/// <summary>
		/// upper bound for amount of moves of one pieceGroup. No king moves included. 
		/// </summary>
		public int GetMvCountBound()
		{
			int max = 0;

			for ( int i=firstPieceGrpIdxToMv ; i<lastPieceGrpIdxToMvPlus1 ; i++ ) 
				max = Math.Max( pieceGroup[i].GetMvCountBound(), max );

			return Math.Min(max,TotalMaxMvCount);
		}


		public int Count
		{
			get{ return pieces.PieceCount; }
		}


		public int CountW
		{
			get{ return pieces.CountW; }
		}


		public int CountB
		{
			get{ return pieces.CountB; }
		}


		public WkBk WkBk
		{
			get{ return wkBk; }
		}


		public Field WK
		{
			get{ return wkBk.Wk; }
		}


		public Field BK
		{
			get{ return wkBk.Bk; }
		}


		public bool Wtm
		{
			get{ return wtm; }
		}


		public int FirstPieceGrpIdxToMv
		{
			get{ return firstPieceGrpIdxToMv; }
		}


		public int LastPieceGrpIdxToMvPlus1
		{
			get { return lastPieceGrpIdxToMvPlus1; }
		}
		

		public long GetIndex()
		{
			long index = 0;
			for ( int i=0 ; i<pieceGroup.Length ; i++ )
				index += pieceGroup[i].Index * weight[i];
			return index;
		}


		public long GetIndexExceptOnePieceGroup( int pieceGrpIdx )
		{
			long index = 0;

			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				if ( pieceGrpIdx != i ) {
					index += pieceGroup[i].Index * weight[i];
				}
			}
			return index;
		}


		public void SetToIndex( long index )
		{
			for ( int i=0 ; i<pieceGroupWeightSorted.Length ; i++ ) {
				long newIndex = index / pieceGroupWeightSorted[i].IndexCount;
				pieceGroupWeightSorted[i].Index = (int)( index - (newIndex * pieceGroupWeightSorted[i].IndexCount) );
				index = newIndex;
			}
		}



		public void ChangeIndex( int delta, ref Fields fld )
		{
			int i=0;

			int pgIndexCount = pieceGroupWeightSorted[0].IndexCount;
			delta += pieceGroupWeightSorted[0].Index;
			pieceGroupWeightSorted[0].Index = delta % pgIndexCount;
			fld = pieceGroupWeightSorted[0].ReplaceFields(fld);

			while( delta >= pgIndexCount ) {
				delta  /= pgIndexCount;
				pgIndexCount = pieceGroupWeightSorted[++i].IndexCount;
				delta += pieceGroupWeightSorted[i].Index;
				pieceGroupWeightSorted[i].Index = delta % pgIndexCount;
				fld = pieceGroupWeightSorted[i].ReplaceFields(fld);
			}
		}


		/// <summary>
		/// Increments the index.
		/// Restarts by index 0 if incremented on last index.
		/// </summary>
		/// <param name="fld"></param>
		public void IncIndex( ref Fields fld )
		{
			int pgIndexCount = pieceGroupWeightSorted[0].IndexCount;
			int currentIndex = pieceGroupWeightSorted[0].Index + 1;
			if ( currentIndex == pgIndexCount ) {
				pieceGroupWeightSorted[0].Index = 0;
				fld = pieceGroupWeightSorted[0].ReplaceFields(fld);
				int i = 1;
				while ( i<pieceGroupWeightSorted.Length && pieceGroupWeightSorted[i].Index+1 == pieceGroupWeightSorted[i].IndexCount ) {
					pieceGroupWeightSorted[i].Index = 0;
					fld = pieceGroupWeightSorted[i++].ReplaceFields(fld);
				}
				if ( i<pieceGroupWeightSorted.Length ) {
					pieceGroupWeightSorted[i].Index = pieceGroupWeightSorted[i].Index + 1;
					fld = pieceGroupWeightSorted[i].ReplaceFields(fld);
				}
			}
			else {
				pieceGroupWeightSorted[0].Index = currentIndex;
				fld = pieceGroupWeightSorted[0].ReplaceFields(fld);
			}
		}


		public bool SetToFirstWithOccField( int pieceGrpIdx, Field f )
		{
			int sortedIndex = pieceGroupReorder.OrigIndexToWeightIndex( pieceGrpIdx );

			for ( int i=0 ; i<pieceGroupWeightSorted.Length ; i++ ) {
				if ( i==sortedIndex ) {
					if ( !pieceGroupWeightSorted[i].SetToFirstWithOccField( f ) )
						return false;
				}
				else
					pieceGroupWeightSorted[i].Index = 0;
			}
			return true;
		}

		public bool NextIndexWithOccField( int pieceGroupIndex, Field f )
		{
			int sortedIndex = pieceGroupReorder.OrigIndexToWeightIndex(pieceGroupIndex);
			for ( int i=0 ; i<pieceGroupWeightSorted.Length ; i++ ) {
				if ( i==sortedIndex ) {
					if ( pieceGroupWeightSorted[i].NextIndexWithOccField( f ) )
						return true;
					else
						pieceGroupWeightSorted[i].SetToFirstWithOccField( f );
				}
				else {
					if ( pieceGroupWeightSorted[i].Index == pieceGroupWeightSorted[i].IndexCount - 1 )
						pieceGroupWeightSorted[i].Index = 0;
					else {
						pieceGroupWeightSorted[i].Index++;
						return true;
					}
				}
			}
			return false;
		}

		

		public int PieceGroupCount
		{
			get{ return pieceGroup.Length; }
		}


		public long GetWeight( int pieceGroupIndex )
		{
			return weight[pieceGroupIndex];
		}


		public PieceGroup GetPieceGroup( int index )
		{
			return pieceGroup[index];
		}


		public override string ToString()
		{
			Fields f;
			string s = pieces.ToString() + " " + wkBk.Wk.ToString();
			bool isEp = GetIsEp();
			if ( isEp ) {
				Field epDblStepDst, epCapSrc;
				f =	GetFieldsEP( out epDblStepDst, out epCapSrc );
			}
			else 
				f = GetFields(); 
			for ( int i=0 ; i<CountW ; i++ )
				s += " " + f.Get(i).ToString();
			s += " " + wkBk.Bk.ToString();
			for ( int i=CountW ; i<Count ; i++ )
				s += " " + f.Get(i).ToString();
			if ( isEp )
				s += " EP";
			return s;
		}


		#region EP


		public BitBrd EpOverlapBits
		{
			get { return overlapFields; }
		}


		/// <summary>
		/// Call first time with -1.
		/// Returns next index and true if successful and -1 and false if no more EP positions are available.
		/// </summary>
		public bool GetNextEpIndex( ref long index )
		{
			++index;
			while ( index != indexCount ) {
				if ( isEpHeuristicMin<=index && index<isEpHeuristicMaxPlus1 ) {
					if ( isEpHeuristicValue )
						return true;
					else 
						index = isEpHeuristicMaxPlus1;
				}
				else 
					CalcEpHeuristic( index );
			}
			index = -1;
			return false;
		}


		/// <summary>
		/// Precondition: Black pawn piece group has highest weight, white pawn piece group has second highest weight.
		/// </summary>
		public bool GetIsEp( long index )
		{
			if ( isEpHeuristicMin<=index && index<isEpHeuristicMaxPlus1 ) {
				return isEpHeuristicValue;
			}
			CalcEpHeuristic( index );
			return isEpHeuristicValue;
		}

		private void CalcEpHeuristic( long index )
		{
			if ( pieceGroupPLowSortedIndex == -1 ) {  // PawnPieceGroups are highmost; easy IsEp calculation
				long   weightW = weightSorted[weightSorted.Length-2];
				long   weightB = weightSorted[weightSorted.Length-1];
				int    pawnB    = (int)(index / weightB);
				int    pawnW    = (int)((index-pawnB*weightB) / weightW);
				BitBrd bb      = overlapFields & pieceGroupPw.GetFields(pawnW) & pieceGroupPb.GetFields(pawnB);
				isEpHeuristicMin    = pawnW * weightW + pawnB * weightB;
				isEpHeuristicMaxPlus1    = isEpHeuristicMin + weightW;
				isEpHeuristicValue = bb.IsNotEmpty;
			}
			else {   // Piece groups might be reordered

				// Example with overlap = [ 10, 20 }
				// 
				// (1)      +--------------+--------------+--------------+--------------+--------------+
				// index    |      x       |  14, 20      |      Y       |    3, 20     |       Z      |  IsEp=false
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMin  |      x       |  14, 20      |      Y       |    3, 20     |       0      |
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMax  |      x       |  14, 20      |      Y       |    4, 20     |       0      | 
				//          +--------------+--------------+--------------+--------------+--------------+
				// 
				// (2)      +--------------+--------------+--------------+--------------+--------------+
				// index    |      x       |  21, 22      |      Y       |      A       |       Z      |  IsEp=false
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMin  |      x       |  14, 20      |      Y       |      A       |       0      |
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMax  |     x+1      |      0       |      0       |      0       |       0      |   
				//          +--------------+--------------+--------------+--------------+--------------+
				// 
				// (3)      +--------------+--------------+--------------+--------------+--------------+
				// index    |      x       |   7, 14      |      Y       |      A       |       Z      |  IsEp=false
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMin  |      x       |   7, 14      |      Y       |      A       |       0      |
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMax  |      x       |  10, 14      |      0       |      0       |       0      |   
				//          +--------------+--------------+--------------+--------------+--------------+
				// 
				// (4)      +--------------+--------------+--------------+--------------+--------------+
				// index    |      x       |  14, 20      |      Y       |   21, 20     |       Z      |  IsEp=false
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMin  |      x       |  14, 20      |      Y       |   21, 20     |       Z      |
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMax  |      x       |  14, 20      |     Y+1      |      0       |       0      |   
				//          +--------------+--------------+--------------+--------------+--------------+
				// 
				// (5)      +--------------+--------------+--------------+--------------+--------------+
				// index    |      x       |  14, 20      |      Y       |   15, 16     |       Z      |  IsEp=false
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMin  |      x       |  14, 20      |      Y       |   15, 16     |       0      |
				//          +--------------+--------------+--------------+--------------+--------------+
				// heurMax  |      x       |  14, 20      |      Y       |    0, 20     |       0      |   
				//          +--------------+--------------+--------------+--------------+--------------+

				PieceGroup pieceGroupPLow=pieceGroupWeightSorted[pieceGroupPLowSortedIndex], pieceGroupPHigh=pieceGroupWeightSorted[pieceGroupPHighSortedIndex];
				long     weightPgLow=weightSorted[pieceGroupPLowSortedIndex], weightPgHigh=weightSorted[pieceGroupPHighSortedIndex];
				int      indexHigh = (int) ((index/weightPgHigh)%pieceGroupPHigh.IndexCount);
				int      indexLow  = (int) ((index/weightPgLow)%pieceGroupPLow.IndexCount);
				BitBrd   bbHigh    = pieceGroupPHigh.GetFields( indexHigh );
				BitBrd   bbLow     = pieceGroupPLow.GetFields(  indexLow  );

				isEpHeuristicMin    = index - ( index % weightPgLow );
				isEpHeuristicValue  = (bbLow & bbHigh & overlapFields).IsNotEmpty;

				if ( isEpHeuristicValue ) {   // (1)
					isEpHeuristicMaxPlus1 = isEpHeuristicMin + weightPgLow;
				}
				else {
					if ( (overlapFields&bbHigh).IsEmpty ) {
						int   i                         = pieceGroupPHigh.NextEpIndex( indexHigh );
						long  nextHighestPieceGroupWeight = weightPgHigh * pieceGroupPHigh.IndexCount;
						if ( i == -1 )  // (2)
							isEpHeuristicMaxPlus1 = index - ( index % nextHighestPieceGroupWeight ) + nextHighestPieceGroupWeight;
						else   // (3)
							isEpHeuristicMaxPlus1 = index - ( index % nextHighestPieceGroupWeight ) + i * weightPgHigh;
					}
					else {
						int   i                         = pieceGroupPLow.NextEpIndex( indexLow, overlapFields&bbHigh );
						long  nextHighestPieceGroupWeight = weightPgLow * pieceGroupPLow.IndexCount;
						if ( i == -1 )  // (4)
							isEpHeuristicMaxPlus1 = index - ( index % nextHighestPieceGroupWeight ) + nextHighestPieceGroupWeight;
						else   // (5)
							isEpHeuristicMaxPlus1 = index - ( index % nextHighestPieceGroupWeight ) + i * weightPgLow;
					}
				}
			}
		}


		public bool GetIsEpOld()
		{
			return GetIsEp( GetIndex() );
		}
		public bool GetIsEp()
		{
			return WPawnAndBPawn && ( pieceGroupPw.GetBitBrd() & pieceGroupPb.GetBitBrd() & overlapFields ).IsNotEmpty;
		}


		/// <summary>
		/// </summary>
		/// <param name="f">Piece Groups can be unsorted</param>
		/// <returns>true success; false unblockable check</returns>
		public bool SetFieldsEP( Fields f, Field epDblStepDst, Field epCapSrc )
		{
			Field overlapField = EP.GetOverlap( epDblStepDst, epCapSrc, wkBk );
			if ( overlapField.IsNo )        // e.G. DblStepDst=A5, CapSrc=B5, BK=A7
				return false;

			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
//				if ( !pieceGroup[i].PieceType.IsP ) {
					if ( !pieceGroup[i].SetFields( f ) )
						return false;
//				}
			}

			Fields pw = pieceGroupPw.GetFields();    // get Fields starting index 0
			Fields pb = pieceGroupPb.GetFields();    // get Fields starting index 0

			pw = pieceGroupPw.ReplaceField( pw, (wtm ? epCapSrc : epDblStepDst), overlapField );  // replace fields and keep sorted
			pb = pieceGroupPb.ReplaceField( pb, (wtm ? epDblStepDst : epCapSrc), overlapField );  // replace fields and keep sorted

			pieceGroupPw.SetFields( pw<<pieceGroupPw.FirstPieceIndex );
			pieceGroupPb.SetFields( pb<<pieceGroupPb.FirstPieceIndex );

			return true;
		}

		


		/// <summary>
		/// indices enumerated are not monotone rising
		/// 
		/// Only valid if PawnPieceGroups are highest most. This is only true during Calculation.
		/// Finally it will be reordered for maximum compression.
		/// MD5 has default PieceGroup-reordering
		/// 
		/// So calling this function is only valid for IndexPosType.Calc.
		/// </summary>
		public EpEnumerateInfo SetToFirstEpPos()
		{
			EpEnumerateInfo info = new EpEnumerateInfo();
			info.InfoB = pieceGroupPb.SetToFirstWithOccFields( overlapFields );
			info.InfoW = pieceGroupPw.SetToFirstWithOccFields( overlapFields & pieceGroupPb.GetFields().GetBitBoard(pieceGroupPb.PieceCount) );

			for ( int i=0 ; i<pieceGroupWeightSorted.Length-2 ; i++ )
				pieceGroupWeightSorted[i].Index = 0;
			return info;
		}


		/// <summary>
		/// indices enumerated are not monotone rising
		/// 
		/// Only valid if PawnPieceGroups are highest most. This is only true during Calculation.
		/// Finally it will be reordered for maximum compression.
		/// MD5 has default PieceGroup-reordering
		/// 
		/// So calling this function is only valid for IndexPosType.Calc.
		/// </summary>
		public bool NextEpPos( ref EpEnumerateInfo info )
		{
			for ( int i=0 ; i<pieceGroupWeightSorted.Length-2 ; i++ ) {
				if ( pieceGroupWeightSorted[i].Index == pieceGroupWeightSorted[i].IndexCount-1 )
					pieceGroupWeightSorted[i].Index = 0;
				else {
					pieceGroupWeightSorted[i].Index++;
					return true;
				}
			}

			if ( !pieceGroupPw.NextIndexWithOccFields( ref info.InfoW) ) {
				if ( !pieceGroupPb.NextIndexWithOccFields( ref info.InfoB) )
					return false;
				info.InfoW = pieceGroupPw.SetToFirstWithOccFields( overlapFields & pieceGroupPb.GetFields().GetBitBoard(pieceGroupPb.PieceCount) );
			}
			return true;
		}
		

		private int GetEp14Index( out Field overlapField )
		{
			if ( WPawnAndBPawn ) {
				BitBrd overlap = pieceGroupPw.GetFields().GetBitBoard( pieceGroupPw.PieceCount ) & pieceGroupPb.GetFields().GetBitBoard( pieceGroupPb.PieceCount );
				if ( overlap.IsNotEmpty ) {
					BitBrd lowestBit = overlap.LowestBit;
					if ( (lowestBit & overlapFields).IsNotEmpty ) {
						overlapField = lowestBit.LowestField;
						return overlapFieldToEp14Index[overlapField.Value];
					}
				}
			}
			overlapField = Field.No;
			return -1;
		}


		/// <summary>
		/// Precondition: Position is an EP pos
		/// If two EP-cap-moves are possible for this position two indices exists!!!
		/// If it's the second index this function will return true
		/// </summary>
		public bool IsRedundandEpPos()
		{
			Field overlapField;
			int epIndex = GetEp14Index( out overlapField );
			Field epDblStepDst, epCapSrc;
			Fields f = GetFieldsEP( out epDblStepDst, out epCapSrc );
			if ( epIndex%2==0 && epIndex!=0 && epIndex!=13 ) { 
				// with epIndex=0 and epIndex=13 no double ep-cap-moves are possible;
				// Following epIndex represents an pair which can result in the same index for the same position:
				// 1,2 ; 3,4 ; 5,6 ; 7,8 ; 9,10 ; 11,12
				
				if ( wtm ) {
					Fields fl = pieceGroupPw.GetFields();
					if ( EP.CapSrcLeftExist(epDblStepDst) && fl.Contains(EP.GetCapSrcLeft(epDblStepDst),pieceGroupPw.PieceCount) )
						return true;
				}
				else {
					Fields fl = pieceGroupPb.GetFields();
					if ( EP.CapSrcLeftExist(epDblStepDst) && fl.Contains(EP.GetCapSrcLeft(epDblStepDst),pieceGroupPb.PieceCount) )
						return true;
				}
			}
			return false;
		}
		

		/// <summary>
		/// Precondition: current pos is EP
		/// </summary>
		/// <returns>Fields</returns>
		public Fields GetFieldsEP( out Field epDblStepDst, out Field epCapSrc )
		{
			Field overlapField;
			int   ep14Index    = GetEp14Index( out overlapField );

			Fields f = new Fields();
			for ( int i=0 ; i<pieceGroup.Length ; i++ ) {
				if ( !pieceGroup[i].PieceType.IsP )
					f = f | (pieceGroup[i].GetFields() << pieceGroup[i].FirstPieceIndex);
			}

			EP.Index14ToFields( ep14Index, wtm, out epDblStepDst, out epCapSrc );

			// remove overlapping pawns given by index and replace with real non overlapping fields
			Fields pw = pieceGroupPw.GetFields();
			Fields pb = pieceGroupPb.GetFields();

			pw = pieceGroupPw.ReplaceField( pw, overlapField, (wtm ? epCapSrc : epDblStepDst) );  // replace fields and keep sorted
			pb = pieceGroupPb.ReplaceField( pb, overlapField, (wtm ? epDblStepDst : epCapSrc) );  // replace fields and keep sorted

			f = f | (pw<<pieceGroupPw.FirstPieceIndex) | (pb<<pieceGroupPb.FirstPieceIndex);

			return f;
		}


		public bool GetIsValid()
		{
			Pos pos = Pos.FromIndexPos( this );
			return GetIsValidCommon() && pos.GetIsValid( wtm );
		}


		public bool GetIsValidEpPos( CheckAndPin checkAndPinStm, CheckAndPin checkAndPinSntm )
		{
			Field epDblStepDst, epCapSrc;
			Pos pos = Pos.FromIndexPosEp( this, out epDblStepDst, out epCapSrc );
			return GetIsValidCommon() && pos.GetIsValid( wtm, epDblStepDst, checkAndPinStm, checkAndPinSntm ) && !IsRedundandEpPos();
		}


		private bool GetIsValidCommon()
		{
			if ( (type & IndexPosType.Compress) != 0 ) {
				// check states that can only occur with compression IndexPos
				for ( int i=0 ; i<PieceGroupCount ; i++ ) {
					PieceGroup pg = GetPieceGroup(i);
					if ( pg.IsPieceGroup2 || pg.PieceType.IsP ) {
						Fields f = pg.GetFields();
						if ( f.IsNo )
							return false;
						if ( pg.PieceType.IsP ) {
							for ( int j=0 ; j<pg.PieceCount ; j++ ) {
								if ( f.Get(j).IsLine0Or7 )
									return false;
							}
						}				
					}
				}
			}
			return true;	
		}


		#endregion
		

	}
}
