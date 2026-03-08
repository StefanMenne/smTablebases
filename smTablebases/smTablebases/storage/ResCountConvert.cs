using System;
using TBacc;


namespace smTablebases
{
	public sealed class ResCountConvert
	{
		private  int[]              indexToValue;
		private  int[]              valueToIndex;
		private  int[]              usedAndUnusedIndices;         // list with indices already in use and after a bound the unused indices
		private  int                countUsedRespFirstUnused;     // the bound to split used indices on beginning and unused indices on tail
		private  int                currentlyUsedIndexBound;      // is not up to date; is only maximum before last index decrementation; use CurrentlyUsedIndexBound to get real Bound
		private  int                valueBitCount;
		private  int                maxIndexBitCount;


		/// <summary>
		/// thread safety: only called single threaded
		/// </summary>
		public ResCountConvert( Storage storage, long offset )
		{
			valueBitCount    = storage.LoadInt( offset );
			offset += 4;
			maxIndexBitCount = storage.LoadInt( offset );
			offset += 4;

			int maxValue = 1<<valueBitCount;
			int maxIndex = 1<<maxIndexBitCount;
			indexToValue = new int[maxIndex];
			valueToIndex = new int[maxValue];

			storage.Load( offset , indexToValue, indexToValue.Length );
			offset += 4 * indexToValue.Length;
			storage.Load( offset,  valueToIndex, valueToIndex.Length );
			offset += 4 * valueToIndex.Length;

			usedAndUnusedIndices = new int[indexToValue.Length];
			countUsedRespFirstUnused = storage.LoadInt( offset );
			offset += 4;
			currentlyUsedIndexBound = storage.LoadInt( offset );
			offset += 4;
			storage.Load( offset, usedAndUnusedIndices, usedAndUnusedIndices.Length );
		}


		/// <summary>
		/// thread safety: only called single threaded
		/// </summary>
		public ResCountConvert( int valueBitCount, int maxIndexBitCount )
		{
			int maxValue = 1<<valueBitCount;
			int maxIndex = 1<<maxIndexBitCount;

			indexToValue = new int[maxIndex];
			valueToIndex = new int[maxValue];

			for ( int i=0 ; i<valueToIndex.Length ; i++ )
				valueToIndex[i] = -1;

			this.valueBitCount    = valueBitCount;
			this.maxIndexBitCount = maxIndexBitCount;

			countUsedRespFirstUnused = 0;
			maxIndex = -1;
			usedAndUnusedIndices = new int[indexToValue.Length];
			for ( int i=0 ; i<usedAndUnusedIndices.Length ; i++ )
				usedAndUnusedIndices[i] = i;
		}


		/// <summary>
		/// memory of orig will be reused
		/// indexInfo[index] >= 0     :   change value of the index
		/// indexInfo[index] == -1    :   do nothing
		/// indexInfo[index] == -2    :   remove index
		///
		/// thread safety: only called single threaded
		/// </summary>
		public ResCountConvert( ResCountConvert orig, int[] indexInfo )
		{
			this.indexToValue     = orig.indexToValue;
			this.valueToIndex     = orig.valueToIndex;
			this.valueBitCount    = orig.valueBitCount;
			this.maxIndexBitCount = orig.maxIndexBitCount;


			// change values
			// Example:
			//
			// Old:    5 (index) -----indexToValue--------> 100 (value) -----valueToIndex----------> 5 (index)
			//
			// New:    5 (index) -----indexToValue(*1)----> 200 (value) -----valueToIndex(*2)------> 5 (index)
			//                                              100 (value) -----valueToIndex(*3)------> -1
			//
			for ( int i=0 ; i<indexInfo.Length ; i++ ) {
				if ( indexInfo[i] >= 0 ) {
					valueToIndex[indexToValue[i]] = -1;            // (*3)
					valueToIndex[indexInfo[i]]    = i;             // (*2)
					indexToValue[i]               = indexInfo[i];  // (*1)
				}
			}


			// remove indices
			//
			// +-------------------+
			// |   used            |  <-  usedIndexPtr start position  (move down)
			// |                   |
			// |                   |
			// |                   |
			// |                   |
			// |                   |
			// |                   |  <-   unusedIndexPtr start position   (move up)
			// +-------------------+
			// |   unused          |
			// |                   |
			// |                   |
			// |                   |
			// |                   |
			// |                   |
			// +-------------------+
			this.usedAndUnusedIndices = orig.usedAndUnusedIndices;
			this.currentlyUsedIndexBound             = orig.CurrentlyUsedIndexBound;
			int usedIndexPtr      = 0;
			int unusedIndexPtr    = orig.countUsedRespFirstUnused - 1;
			while ( usedIndexPtr <= unusedIndexPtr ) {
				int usedIndex   = usedAndUnusedIndices[usedIndexPtr];
				int unusedIndex = usedAndUnusedIndices[unusedIndexPtr];
				if ( indexInfo[usedIndex] == -1 )        // still to be used
					usedIndexPtr++;
				else if ( indexInfo[unusedIndex] != -1 ) { // new unused found
					valueToIndex[indexToValue[unusedIndex]] = -1;
					indexToValue[unusedIndex]               = -1;
					unusedIndexPtr--;
				}
				else {    // usedIndexPtr points to unused; unusedIndexPtr points to used ; swap them
					int tmp                              = usedAndUnusedIndices[usedIndexPtr];
					usedAndUnusedIndices[usedIndexPtr]   = usedAndUnusedIndices[unusedIndexPtr];
					usedAndUnusedIndices[unusedIndexPtr] = tmp;
				}
			}
			this.countUsedRespFirstUnused = usedIndexPtr;
		}


		/// <summary>
		/// thread safety: only called single threaded
		/// </summary>
		public void Save( Storage storage, long offset )
		{
			storage.SaveInt( offset, valueBitCount );
			offset += 4;
			storage.SaveInt( offset, maxIndexBitCount );
			offset += 4;
			storage.Save( offset, indexToValue, indexToValue.Length );
			offset += 4 * indexToValue.Length;
			storage.Save( offset, valueToIndex, valueToIndex.Length );
			offset += 4 * valueToIndex.Length;

			storage.SaveInt( offset, countUsedRespFirstUnused );
			offset += 4;
			storage.SaveInt( offset, currentlyUsedIndexBound );
			offset += 4;
			storage.Save( offset, usedAndUnusedIndices, usedAndUnusedIndices.Length );
		}


		/// <summary>
		/// thread safety: will be called from different threads
		/// </summary>
		public int ValueToIndexAdd( int value )
		{
			if ( valueToIndex[value] == -1 ) {
				// Now we have to add a new value. But thread safe.
				lock( this ) {
					// check again if another thread has added the value in the meanwhile
					if ( valueToIndex[value] == -1 ) {
						int index = usedAndUnusedIndices[countUsedRespFirstUnused++];
						indexToValue[index]      = value;
						valueToIndex[value]      = index;
					}
				}
			}

			return valueToIndex[value];
		}


		public int Count
		{
			get{ return countUsedRespFirstUnused; }
		}


		public void Get( int number, out int index, out int value )
		{
			index = usedAndUnusedIndices[number];
			value = IndexToValue( index );
		}


		public int CurrentlyUsedBitCountForIndices
		{
			get{ return Tools.Log2ForAnyNumber( CurrentlyUsedIndexBound )+1; }
		}


		/// <summary>
		/// returns a maximum bound for the used indices
		///
		/// CurrentlyUsedIndexBound+1 >= Count   always
		/// CurrentlyUsedIndexBound+1 == Count   if only adding values
		/// CurrentlyUsedIndexBound+1 >  Count   possible if removing unused indices and reusing them later
		/// </summary>
		public int CurrentlyUsedIndexBound
		{
			get{ return Math.Max( currentlyUsedIndexBound, countUsedRespFirstUnused ); }
		}


		/// <summary>
		/// The maximal useable index
		/// </summary>
		public int MaxIndex
		{
			get{ return indexToValue.Length; }
		}



		public int ValueBitCount
		{
			get{ return valueBitCount; }
		}


		public int MaxIndexBitCount
		{
			get { return maxIndexBitCount; }
		}


		public int IndexToValue( int index )
		{
			return indexToValue[index];
		}


		public int ValueToIndex( int value )
		{
			return valueToIndex[value];
		}


		public int ReadWriteByteCount
		{
			get { return 2*4 + 4*usedAndUnusedIndices.Length + 2*4 + 4*indexToValue.Length + 4*valueToIndex.Length; }
		}


		public string GetCountString()
		{
			return countUsedRespFirstUnused.ToString().PadLeft(4) + "/" + indexToValue.Length.ToString();
		}


	}
}
