using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	/// <summary>
	/// Calculates the index for a given choosing of k values out of n.
	/// Example: n=60    k=4     Choosen values: 11, 22, 33, 44
	/// => Index = (1,11) + (2,22) + (3,33) + (4,44)
	/// 
	/// usage: 
	/// KchooseNindex kci = new KchooseNindex( 60 );
	/// kci.AddHighestChoosedValue( 11 );
	/// kci.AddHighestChoosedValue( 22 );
	/// kci.AddHighestChoosedValue( 33 );
	/// kci.AddHighestChoosedValue( 44 );
	/// kci.ReplaceChoosenValue( 33, 34, 2 );    // new value must be >22 and <44
	/// 
	/// Example: Fields= 12, 15, 33, 44, 55   pieceCount=5   pieceIdx = 2    => fs= 12, 15, 44, 55
	/// {k,n} := number of possibilities to choose k ou of n
	/// For Mv 33-> 13 we have to sum     {1,12} + {2,13} + {3,15} + {4,44} + {5,55}
	/// 
	/// sumBitValues[0]  =  {2,12} + {3,15} + {4,44} + {5,55}
	/// sumBitValues[1]  =  {1,12} + {3,15} + {4,44} + {5,55}
	/// sumBitValues[2]  =  {1,12} + {2,15} + {4,44} + {5,55}
	/// sumBitValues[3]  =  {1,12} + {2,15} + {3,44} + {5,55}
	/// sumBitValues[4]  =  {1,12} + {2,15} + {3,44} + {4,55}
	///
	///
	/// 
	/// 
	/// Memory Consumption:
	/// Men    NonKMen           Memory in Bytes               Memory sum in bytes
	///  2        0                      8                                  8
	///  3        1                    496                                504
	///  4        2                 15.128                             15.632
	///  5        3                302.560                            318.192
	///  6        4              4.462.760                          4.780.952 
	///  7        5             51.768.016                         56.548.968           around 57 MB
	/// 
	/// </summary>
	public sealed class KchooseNindex
	{
		private        int          n;
		private        int          k;
		private        long[]       sumBitValues;
		private const  int          maxN                  = 64;    // 64 necessary when "compressAllIndices" otherwise 62 = 64 - 2(K) is sufficient
		private static Values64[][] indexToPieceIndices   = new Values64[6][];

		public KchooseNindex( int n, int maxK )
		{
			this.n       = n;
			sumBitValues = new long[maxK];
		}

		public KchooseNindex( int n, int maxK, int[] chooseValue, int ignoreIndexChooseValue )
		{
			this.n       = n;
			sumBitValues = new long[maxK];
			
			for ( int i=0 ; i<chooseValue.Length ; i++ ) {
				if ( i!=ignoreIndexChooseValue ) 
					AddHighestChoosenValue( chooseValue[i] );
			}
		}




		public void AddHighestChoosenValue( int val )
		{
			long tmp = Tools.ChooseKOutOfN( ++k, val );  // ChooseKOutOfN( 0...7, 0...62 )
			sumBitValues[k] = sumBitValues[k-1] + tmp;
			tmp = (tmp*(val-k))/(k+1);
			for ( int i=0 ; i<k ; i++ )
				sumBitValues[i] += tmp;
		}

		public void ReplaceChoosenValue( int oldValue, int newValue, int oldAndNewSortedPosition )
		{
			// we update only the existing values for sumBitValues
			// Example:  Fields= 12, 15, 33, 44, 55   pieceCount=5   pieceIdx = 3  sumBitValuesOldPieceIdx = 2  => fs= 12, 15, 33, 55 
			// Wa have to update:   
			//   sumBitValues[0]   += (4,33) - (4,44)
			//   sumBitValues[1]   += (4,33) - (4,44)
			//   sumBitValues[2]   += (4,33) - (4,44)
			//   sumBitValues[3]   += (3,33) - (3,44)
			//   sumBitValues[4]   += (3,33) - (3,44)		
			long tmpSum      = Tools.ChooseKOutOfN( oldAndNewSortedPosition+1, oldValue ) - Tools.ChooseKOutOfN( oldAndNewSortedPosition+1, newValue ) ;// ChooseKOutOfN( 0...7, 0...62 )
			for ( int i=0 ; i<oldAndNewSortedPosition ; i++ )
				sumBitValues[i] += tmpSum;
			tmpSum           = Tools.ChooseKOutOfN( oldAndNewSortedPosition, oldValue ) - Tools.ChooseKOutOfN( oldAndNewSortedPosition, newValue ) ;// ChooseKOutOfN( 0...7, 0...62 )
			for ( int i=oldAndNewSortedPosition ; i<=k ; i++ )
				sumBitValues[i] += tmpSum;
		}

		public long GetIndexForPeekValue( int index, int value )
		{
			return sumBitValues[index] + Tools.ChooseKOutOfN(index+1,value);// ChooseKOutOfN( 0...7, 0...62 )
		}

		public static Values64[] GetIndexToValuesTable( int k )
		{
			if ( indexToPieceIndices[k] == null )
				CalcIndexToPieceIndicesTables( k );
			return indexToPieceIndices[k];
		}


		private static void CalcIndexToPieceIndicesTables( int kk )
		{		
			for ( int k=0 ; k<=kk ; k++ ) {
				if ( indexToPieceIndices[k] == null ) {
					int        indexCount   = (int)Tools.ChooseKOutOfN( k, maxN ); // ChooseKOutOfN( 0...5, 0...62 )
					Values64[] values       = new Values64[indexCount];
					Values64   currentV64   = new Values64();
			
					for ( int i=0 ; i<k ; i++ )
						currentV64.Set( i, i );
			
					for ( int i=0 ; i<indexCount ; i++ ) {
						values[i] = currentV64;

						// increment currentV64
						for ( int j=0 ; j<k ; j++ ) {
							if ( currentV64.Get(j)+1 < ((j==k-1)?maxN:currentV64.Get(j+1)) ) {
								currentV64.Set( j, currentV64.Get(j)+1 );
								break;
							}
							else if ( j!=k-1 ) {
								currentV64.Set( j, j );
							}
						}
					}
					indexToPieceIndices[k] = values;
				}
			}
		}
	}
}
