using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	/// <summary>
	/// Example (assume this step; please refer to internals.md):
	///
	///   wtm   btm             wtm  btm
	///    -1   -1              -1    -1          is mate
 	///   ---------           ------------
	///     1    1     ---->     1     1          win in 1
	///    -2   -2             |-2|   -2
	///     2                  | 2|  | 2|
	///                              |-3|
	///
	///  All positions which are:
	///             - win and have value >=2    and moveCnt is by definition 0
	///             - lose and have value = -2  and moveCnt is 0
	///
	///  will be set to FastBit = true
	///
	/// </summary>

	public sealed class FastBits
	{
#if DEBUG
		private long            lastIndex        = -1;
		private long            trackIndex       = -1;
#endif
		private DataChunkWrite  dataChunkWrite;
		private long[]          data;
		private long            dataCount;
		private int             currentDataIndex = -1;
		private long            currentBits      = 0L;

#if DEBUG
		public FastBits( long[] data, long countPos, DataChunkWrite dataChunkWrite, int trackIndex )
		{
			this.trackIndex        = trackIndex;
#else
		public FastBits( long[] data, long countPos, DataChunkWrite dataChunkWrite )
		{
#endif
			this.dataChunkWrite    = dataChunkWrite;
			this.data              = data;
			dataCount              = (countPos+63) / 64;
		}

		public bool Get( long index )
		{
			return ((data[index/64]>>((int)(index%64)))&1) == 1;
		}

		public void Set( long index )
		{
#if DEBUG
			if ( !((DataChunkMemoryWrite)dataChunkWrite.DataChunkMemory).WritePotentialNew )
				throw new Exception();
			if ( index == trackIndex )
				Debug.SetOrUnsetFastBit( true );
#endif
			data[index/64] |= 1L<<((int)(index%64));
		}

		public void SetValue( long index, bool value )
		{
			if ( value )
				Set( index );
			else
				Unset( index );
		}

		public void Unset( long index )
		{
#if DEBUG
			if ( index == trackIndex )
				Debug.SetOrUnsetFastBit( false );
#endif
			data[index/64] &= ~(1L<<((int)(index%64)));
		}


		public int GetNext()
		{
			while ( currentBits == 0L ) {
				if ( ++currentDataIndex == dataCount ) {
#if DEBUG
					if ( Debug.TrackIndex!=-1 && dataChunkWrite.WkBk==Debug.TrackWkBk && dataChunkWrite.Wtm==Debug.TrackWtm && lastIndex<Debug.TrackIndex )
						Debug.IsPotentialNewPosBits( false );
#endif
					return -1;
				}
				currentBits = data[currentDataIndex];
			}
			long lowestBit         = currentBits & (-currentBits);
			int  lowestBitIndex    = Tools.Log2( (ulong)lowestBit );
			currentBits           &= ~lowestBit;
			int  currentIndex     = 64*currentDataIndex + lowestBitIndex;

#if DEBUG
			if ( Debug.TrackIndex != -1 && dataChunkWrite.WkBk == Debug.TrackWkBk && dataChunkWrite.Wtm == Debug.TrackWtm && lastIndex<Debug.TrackIndex && Debug.TrackIndex <= currentIndex)
				Debug.IsPotentialNewPosBits( Debug.TrackIndex==currentIndex );
			lastIndex = currentIndex;
#endif
			return currentIndex;
		}


		public void Clear()
		{
			for ( long i=0 ; i<dataCount ; i++ )
				data[i] = 0L;
		}

	}
}
