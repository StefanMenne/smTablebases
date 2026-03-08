using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using TBacc;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace smTablebases
{
/// <summary>
	///  Bit31                        Bit0
	///       32 Bit
	///  |-------------------------------|
	///
	///  +-------------------------------+    ---
	///  |    Res 0        |     Res 1   |     |
	///  +-------------------------------+     |
	///  |   |    Res 2       |  Res3    |     |   alignedBlockSizeInUints
	///  +-------------------------------+     |
	///  |       |    Res 4       |      |     |
	///  +-------------------------------+     |
	///  |  Res 5    |    Res 6       |  |     |
	///  +-------------------------------+    ---
	///  |  Res 7        |    Res 8      |
	///  +-------------------------------+
	///  | | Res 9         |    Res 10   |
	///  +-------------------------------+
	///  |     | Res 11          |       |
	///  +-------------------------------+
	///  |  Res 12 | Res 13          |   |
	///  +-------------------------------+
	///  |   Res 14    | Res 15          |
	///  +-------------------------------+
	///
	/// </summary>
	public sealed class DataChunkWrite : DataChunk
	{
		public     const int        ArrayItemSizeInBytesData           = sizeof(int);
		private    uint[]           data;
		public     const int        ArrayItemSizeInBytesFastBit        = sizeof(long);
		private    long[]           fastBit;
		private    int              bitsPerEntry;
		private    int              bitMask                            = 0;     // (1<<bitsPerEntry)-1
		private    ResCountConvert  resCountConvert                    = null;
		private    long             byteOffset;
		private    int              alignedBlockSizeInUints;         // number of uints when no overlapping occurs; see example above



		public DataChunkWrite( Pieces pieces, bool wtm, WkBk wkbk, long firstIndex, long indexCount, int bitsPerEntry, int chunkIndex ) : base( wtm, wkbk, pieces, firstIndex, indexCount, chunkIndex )
		{
			this.bitsPerEntry               = bitsPerEntry;
			this.bitMask                    = (1<<bitsPerEntry)-1;
			this.alignedBlockSizeInUints    = bitsPerEntry / (int)Tools.GCD( 32, bitsPerEntry );
		}

		public static long IndexCountToByteCount( long index, bool useFastBit, int bitsPerEntry )
		{
			if ( useFastBit )
				return IndexCountToByteCountData(index,bitsPerEntry) + IndexCountToByteCountFastBit(index);
			else
				return IndexCountToByteCountData(index,bitsPerEntry);
		}

		public static int IndexCountToByteCountData( long indexCount, int bitsPerEntry )
		{
			long   bits         = ((long)indexCount) * ((long)bitsPerEntry);
			int    integers     = (int)((bits+31)/32);
			return integers * 4;
		}

		public static long IndexCountToByteCountFastBit( long index )
		{
			return ((index+63) / 64) * 8;
		}


		public void Init(long byteOffset)
		{
			this.byteOffset = byteOffset;
		}


		public override void Join( DataChunkMemory mem, long counterToIdentifyOldestEntry, bool fixedJoin )
		{
			base.Join(mem,counterToIdentifyOldestEntry,fixedJoin);
			DataChunkMemoryWrite   dcm = (DataChunkMemoryWrite)mem;
			data                       = dcm.Data;
			fastBit                    = dcm.FastBit;
#if DEBUG
			ZeroPaddingBits();    // for debug purpose only; temp files is always unique !
#endif
		}

		public override void Unjoin()
		{
			base.Unjoin();
			data                      = null;
			fastBit                   = null;
		}

		public override bool IsWriteDataChunk
		{
			get { return true; }
		}

		public ResCountConvert ResCountConvert
		{
			get{ return resCountConvert; }
			set{ resCountConvert = value; }
		}

		public long ByteOffset
		{
			get { return byteOffset; }
		}

		public long GetByteCount()
		{
			return ByteCountData + ByteCountFastBits;
		}

		private int ByteCountData
		{
			get{
				return IndexCountToByteCountData( indexCount, bitsPerEntry );
			}
		}

		private int ByteCountFastBits
		{
			get{ return ( (((int)indexCount) + 63) / 64 ) * 8; }
		}


		public int Get( long index )
		{
			long  firstBit                        = index * bitsPerEntry;
			int   dataArrayIndex                  = (int)(firstBit>>5);
			//int   firstBitInFirstArrayEntry     = ((int)firstBit)&31;
			long  lastBitPlus1                    = firstBit + bitsPerEntry;
			int   dataArrayIndex2                 = (int)((lastBitPlus1-1)>>5);
			int   lastBitPlus1InSecondArrayEntry  = ((int)lastBitPlus1)&31;
			int   val = (int) ( ( (data[dataArrayIndex]<<lastBitPlus1InSecondArrayEntry) | (data[dataArrayIndex2]>>(32-lastBitPlus1InSecondArrayEntry)) ) & bitMask );
			return resCountConvert.IndexToValue( val );
		}


		public int GetResWithCountIndex( long index )
		{
			long  firstBit                        = index * bitsPerEntry;
			int   dataArrayIndex                  = (int)(firstBit>>5);
			//int   firstBitInFirstArrayEntry     = ((int)firstBit)&31;
			long  lastBitPlus1                    = firstBit + bitsPerEntry;
			int   dataArrayIndex2                 = (int)((lastBitPlus1-1)>>5);
			int   lastBitPlus1InSecondArrayEntry  = ((int)lastBitPlus1)&31;
			int   val = (int) ( ( (data[dataArrayIndex]<<lastBitPlus1InSecondArrayEntry) | (data[dataArrayIndex2]>>(32-lastBitPlus1InSecondArrayEntry)) ) & bitMask );
			return val;
		}


		public int Get( long index, ResCountConvert resCountConvert )
		{
			long  firstBit                        = index * bitsPerEntry;
			int   dataArrayIndex                  = (int)(firstBit>>5);
			//int   firstBitInFirstArrayEntry     = ((int)firstBit)&31;
			long  lastBitPlus1                    = firstBit + bitsPerEntry;
			int   dataArrayIndex2                 = (int)((lastBitPlus1-1)>>5);
			int   lastBitPlus1InSecondArrayEntry  = ((int)lastBitPlus1)&31;
			int   val = (int) ( ( (data[dataArrayIndex]<<lastBitPlus1InSecondArrayEntry) | (data[dataArrayIndex2]>>(32-lastBitPlus1InSecondArrayEntry)) ) & bitMask );
			return resCountConvert.IndexToValue( val );
		}


		public void Set( long index, int val )
		{
#if DEBUG
			if ( Config.DebugGeneral && !((DataChunkMemoryWrite)dataChunkMemory).WriteData )
				throw new Exception();
#endif


			int value = resCountConvert.ValueToIndexAdd( val );

			int   firstBit                        = ((int)index) * bitsPerEntry;
			int   dataArrayIndex                  = (int)(firstBit>>5);
			int   firstBitInFirstArrayEntry       = ((int)firstBit)&31;
			int   lastBitPlus1                    = firstBit + bitsPerEntry;
			int   dataArrayIndex2                 = (int)((lastBitPlus1-1)>>5);
			//int   lastBitPlus1InSecondArrayEntry  = ((int)lastBitPlus1)&31;

			// get current value of the two array entries inside one ulong
			ulong v                               = ((ulong)data[dataArrayIndex])<<32 | ((ulong)data[dataArrayIndex2]);

			// remove bits for new value; if entry fits in one array-entry only high 32 bits are changed
			v &= ~(((ulong)bitMask)<<(64-firstBitInFirstArrayEntry-bitsPerEntry));

			// write new value into v
			v |= ((ulong)value)<<(64-firstBitInFirstArrayEntry-bitsPerEntry);

			// write back data; write second array entry first because only high 32 bit of v are valid if the entry fits in 32bit
			data[dataArrayIndex2] = (uint)v;
			data[dataArrayIndex]  = (uint)(v>>32);
		}


		public void SetResWithCountIndex( long index, int value )
		{
#if DEBUG
			if ( Config.DebugGeneral ) {
				if ( !((DataChunkMemoryWrite)dataChunkMemory).WriteData )
					throw new Exception();
				if ( index>=indexCount )
					throw new Exception();
			}
#endif
			int   firstBit                        = ((int)index) * bitsPerEntry;
			int   dataArrayIndex                  = (int)(firstBit>>5);
			int   firstBitInFirstArrayEntry       = ((int)firstBit)&31;
			int   lastBitPlus1                    = firstBit + bitsPerEntry;
			int   dataArrayIndex2                 = (int)((lastBitPlus1-1)>>5);
			//int   lastBitPlus1InSecondArrayEntry  = ((int)lastBitPlus1)&31;

			// get current value of the two array entries inside one ulong
			ulong v                               = ((ulong)data[dataArrayIndex])<<32 | ((ulong)data[dataArrayIndex2]);

			// remove bits for new value; if entry fits in one array-entry only higher 32 bits are changed
			v &= ~(((ulong)bitMask)<<(64-firstBitInFirstArrayEntry-bitsPerEntry));

			// write new value into v
			v |= ((ulong)value)<<(64-firstBitInFirstArrayEntry-bitsPerEntry);

			// write back data; write second array entry first because only high 32 bit of v are valid if the entry fits in 32bit
			data[dataArrayIndex2] = (uint)v;
			data[dataArrayIndex]  = (uint)(v>>32);
		}


		/// <summary>
		/// Precondition: firstIndex must be aligned
		/// </summary>
		public void Fill( long count, int val )
		{
			if ( count<alignedBlockSizeInUints ) {      // to few indices; use normal Set only
				for ( int i=0 ; i<count ; i++ )
					Set( i, val );
				return;
			}

			for ( int i=0 ; i<alignedBlockSizeInUints ; i++ )    // set first alignedBlock using normal set
				Set( i, val );
			int indicesPerAlignedBlock = 32 * alignedBlockSizeInUints / bitsPerEntry;
			int indexOffset = indicesPerAlignedBlock;
			int uintOffset  = alignedBlockSizeInUints;

			while ( indexOffset + indicesPerAlignedBlock <= count ) {
				for ( int i=0 ; i<alignedBlockSizeInUints ; i++ )            // copy block
					data[uintOffset++] = data[i];
				indexOffset += indicesPerAlignedBlock;
			}

			for ( int i=indexOffset ; i<count ; i++ )            // last incomplete block; use normal Set
				Set( i, val );
		}


		/// <summary>
		/// Not necessary. Done to make temp files unique for debug purpose.
		/// </summary>
		public void ZeroPaddingBits()
		{
			long  firstBit                        = indexCount * bitsPerEntry;
			int   dataArrayIndex                  = (int)(firstBit>>5);
			int   firstBitInArrayEntry            = ((int)firstBit)&31;

			if ( firstBitInArrayEntry != 0 )
				data[dataArrayIndex] &= ~(0xffffffffu >> firstBitInArrayEntry);


			// same for fast bits
			dataArrayIndex                        = (int)(indexCount>>6);
			firstBitInArrayEntry                  = ((int)indexCount)&63;

			if ( firstBitInArrayEntry != 0 )
				fastBit[dataArrayIndex] &= (long)~(0xfffffffffffffffful >> firstBitInArrayEntry);
		}


#if DEBUG
		public int GetDebug( long index, string dbgText, object param )
		{
			int result = Get( index );
			if ( Debug.TrackPosition )
				GetDebugCommon( index, dbgText, param, result );
			return result;
		}


		public void SetDebug( IndexPos indexPos, long index, int value, string dbgText, object param, VerifyResType verify )
		{
			if ( Debug.TrackPosition )
				SetDebugCommon( indexPos, index, value, dbgText, param, verify );
			Set( index, value );
		}


		public int SA_GetNextIndexWithWrite_DEBUG( long index, string dbgText, object param )
		{
			int resultIndex = SA_GetNextIndexWithWrite();
			if ( Debug.TrackPosition )
				GetDebugCommon( index, dbgText, param, resCountConvert.IndexToValue( resultIndex ) );
			return resultIndex;
		}


		public void SA_SetCurrentIndex_DEBUG( IndexPos indexPos, long index, int resIndex, string dbgText, object param  )
		{
			if ( Debug.TrackPosition )
				SetDebugCommon( indexPos, index, resCountConvert.IndexToValue(resIndex), dbgText, param, VerifyResType.VerifyAlways );
			SA_SetCurrentIndex( resIndex );
		}


		private void GetDebugCommon( long index, string dbgText, object param, int result )
		{
			if ( index==Debug.TrackIndex && WkBk==Debug.TrackWkBk && Wtm==Debug.TrackWtm ) {
				if ( param is IndexPos )
					dbgText += " " + ((IndexPos)param).ToString();
				else if ( param is ResWithCount )
					dbgText += " " + ((ResWithCount)param).ToString();
				Debug.GetResult( result, dbgText );
			}
		}


		private void SetDebugCommon( IndexPos indexPos, long index, int value, string dbgText, object param, VerifyResType verify )
		{
			if ( index==Debug.TrackIndex && WkBk==Debug.TrackWkBk && Wtm==Debug.TrackWtm ) {
				if ( param is IndexPos )
					dbgText += " " + ((IndexPos)param).ToString();
				else if ( param is ResWithCount )
					dbgText += " " + ((ResWithCount)param).ToString();
				Debug.SetResult( value, dbgText );
			}
		}
#endif


		public void Load( Storage storage )
		{
			storage.Load( byteOffset, data, ByteCountData/ArrayItemSizeInBytesData );
			storage.Load( byteOffset + ByteCountData, fastBit, ByteCountFastBits/ArrayItemSizeInBytesFastBit );
		}

		public void Save( Storage storage, bool saveData, bool saveFastBit )
		{
			if ( saveData )
				storage.Save( byteOffset, data, ByteCountData/ArrayItemSizeInBytesData );
			if ( saveFastBit )
				storage.Save( byteOffset + ByteCountData, fastBit, ByteCountFastBits/ArrayItemSizeInBytesFastBit );
		}

		public FastBits GetFastBits( long count )
		{
#if DEBUG
			return new FastBits( fastBit, count, this, TrackIndex );
#else
			return new FastBits( fastBit, count, this );
#endif
		}

		public FastBits GetFastBits()
		{
#if DEBUG
			return new FastBits( fastBit, indexCount, this, TrackIndex );
#else
			return new FastBits( fastBit, indexCount, this );
#endif
		}

		#region Sequential Access

		private int   sequentialAccessIndex;
		private int   sequentialAccessNextBit;
		private ulong sequentialAccessBuffer;


		//
		//                                           sequentialAccessIndex
		//               |-------------------------------------------------------------------|
		//
		//  +--------------------------------------------------------------------------------+
		//  |            data[i]                        |       data[i+1]                    |
		//  +--------------------------------------------------------------------------------+
		//
		//               |---------------|
		//              next Value To Read
		//
		public void SA_SetToFirst()
		{
			sequentialAccessIndex    = 0;
			sequentialAccessNextBit  = 64;
			sequentialAccessBuffer   = ((ulong)data[0])<<32;
			if ( data.Length > 1 )
				sequentialAccessBuffer |= data[1];
		}

		public int SA_GetNextWithWrite()
		{
			sequentialAccessNextBit -= bitsPerEntry;
			if ( sequentialAccessNextBit < 0 ) {
				data[sequentialAccessIndex++] = (uint)(sequentialAccessBuffer >> 32);
				sequentialAccessBuffer <<= 32;
				sequentialAccessNextBit += 32;
				sequentialAccessBuffer |= data[sequentialAccessIndex+1];
			}
			int index = ((int)(sequentialAccessBuffer>>(sequentialAccessNextBit))) & bitMask;
			return resCountConvert.IndexToValue( index );
		}

		public int SA_GetNextIndexWithWrite()
		{
			sequentialAccessNextBit -= bitsPerEntry;
			if ( sequentialAccessNextBit < 0 ) {
				data[sequentialAccessIndex++] = (uint)(sequentialAccessBuffer >> 32);
				sequentialAccessBuffer <<= 32;
				sequentialAccessNextBit += 32;
				sequentialAccessBuffer  |= data[sequentialAccessIndex+1];
			}
			int index = ((int)(sequentialAccessBuffer>>(sequentialAccessNextBit))) & bitMask;
			return index;
		}


		public int SA_GetNextWithWrite( ResCountConvert resCountConvert )
		{
			sequentialAccessNextBit -= bitsPerEntry;
			if ( sequentialAccessNextBit < 0 ) {
				data[sequentialAccessIndex++] = (uint)(sequentialAccessBuffer >> 32);
				sequentialAccessBuffer <<= 32;
				sequentialAccessNextBit += 32;
				sequentialAccessBuffer  |= data[sequentialAccessIndex+1];
			}
			int index = ((int)(sequentialAccessBuffer>>(sequentialAccessNextBit))) & bitMask;
			return resCountConvert.IndexToValue( index );
		}


		public void SA_SetCurrent( int value )
		{
			int index = resCountConvert.ValueToIndexAdd( value );
			sequentialAccessBuffer &= ~((ulong)bitMask<<(sequentialAccessNextBit));
			sequentialAccessBuffer |= ((ulong)index)<<(sequentialAccessNextBit);
		}


		public void SA_SetCurrentIndex( int index )
		{
			sequentialAccessBuffer &= ~((ulong)bitMask << (sequentialAccessNextBit));
			sequentialAccessBuffer |= ((ulong)index) << (sequentialAccessNextBit);
		}


		public void SA_Finish()
		{
			data[sequentialAccessIndex++] = (uint)(sequentialAccessBuffer>>32);
			if ( sequentialAccessNextBit < 32 ) {
				data[sequentialAccessIndex] = (uint)(sequentialAccessBuffer&(0xffffffff));
			}

		}

		#endregion

	}
}
