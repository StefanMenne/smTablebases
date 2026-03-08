using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class OccFldsIndexEnumerator
	{
		private PieceGroup    pieceGroup;
		private PieceGroup[]  pieceGroups;
		private int           pieceGroupIndex;
		private int[]         indexToNextIndex;
		private int           firstIndex          = -1;
		private IndexPos      indexPos;
		private long          indexOut            = -1;


		public OccFldsIndexEnumerator( IndexPos indexPos, int pieceGroupIndex, BitBrd occFields )
		{
			this.indexPos         = indexPos;
			pieceGroups           = indexPos.GetPieceGroupsWeightSorted();
			this.pieceGroupIndex  = indexPos.PieceGroupReorder.OrigIndexToWeightIndex( pieceGroupIndex );
			pieceGroup            = pieceGroups[this.pieceGroupIndex];
			indexToNextIndex    = new int[pieceGroup.IndexCount];

			int lastIndex       = -1;
			for ( int i=0 ; i<pieceGroup.IndexCount ; i++ ) {
				BitBrd bb = pieceGroup.GetFields( i );
				if ( (bb&occFields).IsNotEmpty ) {
					if ( firstIndex == -1 )
						firstIndex = i;
					else
						indexToNextIndex[lastIndex] = i;
					lastIndex = i;
				}
			}
			if ( lastIndex != -1 ) {
				indexToNextIndex[lastIndex] = -1;
				for ( int i=0 ; i<pieceGroups.Length ; i++ )
					pieceGroups[i].Index = (i==this.pieceGroupIndex) ? firstIndex : 0 ;
				indexOut = indexPos.GetIndex();
			}
		}


		public long Next()
		{
			for ( int i=0 ; i<pieceGroups.Length ; i++ ) {
				if ( i==pieceGroupIndex ) {
					int newIndex = indexToNextIndex[pieceGroup.Index];
					if ( newIndex == -1 )
						pieceGroup.Index = firstIndex;
					else {
						pieceGroup.Index = newIndex;
						return indexOut = indexPos.GetIndex();
					}
				}
				else {
					if ( pieceGroups[i].Index == pieceGroups[i].IndexCount-1 )
						pieceGroups[i].Index = 0;
					else {
						pieceGroups[i].Index++;
						return indexOut = indexPos.GetIndex();
					}
				}
			}
			return indexOut = -1;
		}


		public long Index
		{
			get{ return indexOut; }
		}
	}
}
