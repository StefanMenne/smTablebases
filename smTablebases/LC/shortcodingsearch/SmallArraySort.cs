using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class SmallArraySort
	{
		private int[] rankToValueIndex;//, valueIndexToRank;


		public SmallArraySort( int n )
		{
			rankToValueIndex = new int[n];
//			valueIndexToRank = new int[n];
			for ( int i=0 ; i<n ; i++ )
				rankToValueIndex[i] = /*valueIndexToRank[i] =*/ i;
		}


		public void Update( int[] values )
		{
			bool isSorted = true;

			for ( int i=0 ; i+1<rankToValueIndex.Length ; i++ )
				isSorted &= values[rankToValueIndex[i]] >= values[rankToValueIndex[i+1]];

			if ( !isSorted ) {
				for ( int i=0 ; i<rankToValueIndex.Length ; i++ ) {
					for ( int j=i+1 ; j<rankToValueIndex.Length ; j++ ) {
						if ( values[rankToValueIndex[i]] < values[rankToValueIndex[j]] ) {
							int tmp = rankToValueIndex[i];
							rankToValueIndex[i] = rankToValueIndex[j];
							rankToValueIndex[j] = tmp;
						}
					}
				}
				//for ( int i=0 ; i<rankToValueIndex.Length ; i++ )
				//	valueIndexToRank[rankToValueIndex[i]] = i;
			}
		}


		public void Update( double[] values )
		{
			bool isSorted = true;

			for ( int i=0 ; i+1<rankToValueIndex.Length ; i++ )
				isSorted &= values[rankToValueIndex[i]] >= values[rankToValueIndex[i+1]];

			if ( !isSorted ) {
				for ( int i=0 ; i<rankToValueIndex.Length ; i++ ) {
					for ( int j=i+1 ; j<rankToValueIndex.Length ; j++ ) {
						if ( values[rankToValueIndex[i]] < values[rankToValueIndex[j]] ) {
							int tmp = rankToValueIndex[i];
							rankToValueIndex[i] = rankToValueIndex[j];
							rankToValueIndex[j] = tmp;
						}
					}
				}
				//for ( int i=0 ; i<rankToValueIndex.Length ; i++ )
				//	valueIndexToRank[rankToValueIndex[i]] = i;
			}
		}


		public int RankToValueIndex( int sortedIndex )
		{
			return rankToValueIndex[sortedIndex];
		}


		//public int ValueIndexToRank( int index )
		//{
		//	return valueIndexToRank[index];
		//}

	}
}
