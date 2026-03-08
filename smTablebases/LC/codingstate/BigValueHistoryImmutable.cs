using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class BigValueHistoryImmutable : BigValueHistoryBase
	{
		// Example:
		// Value   Occurence        
		// 19550    115  
		//  2733     76
		//    17     34
		// 20999     12
		//   466      1
		//   ...    ...
		//
		//  oldestAndNewestQueueIndex = 7
		//
		//     ArrayEntry                  age    (0=newest  59=oldest)
		// oldestAndNewestQueue[0]          6
		// oldestAndNewestQueue[1]          5
		// ...                            ... 
		// oldestAndNewestQueue[6]          0
		// -----------------------------------
		// oldestAndNewestQueue[7]         59
		// oldestAndNewestQueue[8]         58
		// oldestAndNewestQueue[9]         57
		// -----------------------------------
		// otherQueue[0]                   56
		// otherQueue[1]                   55
		// ...                            ...
		// otherQueue[49]                   7
		//
		private BigValueHistoryImmutablePool pool;
		private int[]                oldestAndNewestQueue;
		private int[]                otherQueue;
		private int                  oldestAndNewestQueueIndex;
		private int                  countInstances;


		private BigValueHistoryImmutable Create( BigValueHistoryImmutable hist, int valueToAdd )
		{
			BigValueHistoryImmutable source;
			if ( hist == null ) {
				source = this;
			}
			else {
				source               = hist;
				countInstances = 1;
				pool                 = hist.pool;
				countDifferentValues  = hist.countDifferentValues;
				firstOccurenceOneIndex         = hist.firstOccurenceOneIndex;
				sumOccurenceTwoOrHigher              = hist.sumOccurenceTwoOrHigher;
				values               = pool.GetIntArray();
				occurence            = pool.GetIntArray();
				oldestAndNewestQueue = pool.GetIntArrayForOldestNewestQueue();
				Array.Copy( hist.values,               values,               countDifferentValues         );
				Array.Copy( hist.occurence,            occurence,            countDifferentValues         );
				Array.Copy( hist.oldestAndNewestQueue, oldestAndNewestQueue, oldestAndNewestQueue.Length );
				oldestAndNewestQueueIndex = hist.oldestAndNewestQueueIndex;
			}

			int valueToRemove = oldestAndNewestQueue[oldestAndNewestQueueIndex];
			oldestAndNewestQueue[oldestAndNewestQueueIndex++] = valueToAdd;

			if ( oldestAndNewestQueueIndex == oldestAndNewestQueue.Length ) {  // no more space; otherQueue to update
				oldestAndNewestQueueIndex = 0;
				otherQueue = pool.GetIntArray();
				otherQueue[otherQueue.Length-1] = 1;    // using count = 1

				Array.Copy( oldestAndNewestQueue, 0, otherQueue, WindowSize-2*oldestAndNewestQueue.Length, oldestAndNewestQueue.Length );
				Array.Copy( source.otherQueue, oldestAndNewestQueue.Length, otherQueue, 0, WindowSize-2*oldestAndNewestQueue.Length );
				Array.Copy( source.otherQueue, 0, oldestAndNewestQueue, 0, oldestAndNewestQueue.Length );
			}
			else if ( hist != null ) { // simple case; just reuse old instance of otherQueue
				otherQueue = hist.otherQueue;
				otherQueue[otherQueue.Length-1]++;    // use last entry for usingCount
			}

			if ( valueToAdd != valueToRemove ) { 
				int indexToRemove = -1, indexToAdd = -1;
				for ( int i=0 ; i<countDifferentValues ; i++ ) {
					if ( values[i] == valueToRemove ) {
						indexToRemove = i;
						if ( indexToAdd!=-1 )
							break;
					}
					else if ( values[i] == valueToAdd ) {
						indexToAdd = i;
						if ( valueToRemove==-1 || indexToRemove!=-1 )
							break;
					}
				}
				UpdateOccurence( valueToAdd, indexToAdd, valueToRemove, indexToRemove, pool.MinOccurence );
			}

			return this;
		}


		private BigValueHistoryImmutable Create( BigValueHistoryImmutablePool pool, int minOccurence, InitValues initValues )
		{
			this.pool                 = pool;
			values                    = pool.GetIntArray();
			occurence                 = pool.GetIntArray();
			oldestAndNewestQueue      = pool.GetIntArrayForOldestNewestQueue();
			otherQueue                = pool.GetIntArray();
			oldestAndNewestQueueIndex = 0;

			for ( int i=0 ; i<oldestAndNewestQueue.Length ; i++ )
				oldestAndNewestQueue[i] = initValues.IndexToValue(i);
			for ( int i=0 ; i<otherQueue.Length-oldestAndNewestQueue.Length ; i++ )
				otherQueue[i] = initValues.IndexToValue( i + oldestAndNewestQueue.Length );		
			otherQueue[otherQueue.Length-1] = 1;    // using count = 1
			Init( pool.WindowSize, initValues, minOccurence );
			countInstances = 1;
			return this;
		}


		public static BigValueHistoryImmutable CreateEmpty( BigValueHistoryImmutablePool pool, int minOccurence, InitValues initValues )
		{
			return pool.GetInstance().Create( pool, minOccurence, initValues );
		}


		public BigValueHistoryImmutable AddValue( int value, bool reuseInstance )
		{
			if ( reuseInstance && countInstances==1 && 
			     (oldestAndNewestQueueIndex+1)!=oldestAndNewestQueue.Length )  // for simplicity don't reuse in this case 
			{
				Create( null, value );
				return this;
			}
			else {
				BigValueHistoryImmutable inst = pool.GetInstance();
				inst.Create( this, value );
				if ( reuseInstance )
					Dispose();
				return inst;
			}
		}


		public int WindowSize
		{
			get { return pool.WindowSize; }
		}


		public BigValueHistoryImmutable Clone()
		{
			if ( countInstances++<=0 )
				throw new ObjectDisposedException(null);
			return this;
		}



		public void Dispose()
		{
			if ( --countInstances == 0 ) {
				pool.ReuseIntArray( values );
				pool.ReuseIntArray( occurence );
				pool.ReuseIntArrayForOldestNewestQueue( oldestAndNewestQueue );
				if ( --otherQueue[otherQueue.Length-1] == 0 )
					pool.ReuseIntArray( otherQueue );
				pool.ReuseInstance( this );
			}
			else if ( countInstances < 0 )
				throw new ObjectDisposedException(null);
		}

	}
}
