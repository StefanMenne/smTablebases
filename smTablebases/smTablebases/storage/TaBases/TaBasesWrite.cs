using System;
using System.Collections.Generic;
using TBacc;


namespace smTablebases
{
	public class TaBasesWrite
	{
		public  bool                        AllChunksInMemory           = false;
		private Pieces                      pieces                      = null;
		private DataChunkMemoryWrite[]      dataChunksMemoryWrite       = null;
		private int                         fixedDataChunkCount         = 0;
		private TaBaWrite                   taBaWork                    = null;
		private static long                 readChunkCounter            = 0;
		private long                        maxBytesPerDataChunk        = -1;
		private int[]                       wkBkWtm_To_ChunkIndex       = null;
		private int                         nextChunkIndexToReuse;
		private int                         emptyChunkCount             = 0;

		public static bool IsDoubleTaBa( Pieces pieces )
		{
			return pieces.IsDoubled;
		}

		public TaBasesWrite( CalcTB calc, Pieces p, bool continueOldCalculation )
		{
			long bytesAllocated = 0;
			int index = p.Index;
			taBaWork = new TaBaWrite( p, continueOldCalculation ? TaBaOpenType.OpenForWrite : TaBaOpenType.CreateNewForWrite, calc.ThreadCount );
			pieces = p;
			readChunkCounter = 0;

			long maxIndicesPerChunkWtm = taBaWork.MaxIndexCountPerChunkWtm;
			long maxIndicesPerChunkBtm = taBaWork.MaxIndexCountPerChunkBtm;

			int pIndex = p.Index;
			wkBkWtm_To_ChunkIndex = new int[2*WkBk.GetCount(p).Index];
			for ( int i=0 ; i<wkBkWtm_To_ChunkIndex.Length ; i++ ) {
				wkBkWtm_To_ChunkIndex[i] = -1;
			}

			long maxBytesPerDataChunkWtm      = DataChunkWrite.IndexCountToByteCount( maxIndicesPerChunkWtm, true, taBaWork.BitsPerEntryWtm );
			long maxBytesPerDataChunkBtm      = DataChunkWrite.IndexCountToByteCount( maxIndicesPerChunkBtm, true, taBaWork.BitsPerEntryBtm );
			maxBytesPerDataChunk = Math.Max( maxBytesPerDataChunkWtm, maxBytesPerDataChunkBtm );
			int maxUsefulDataChunkCount       = 2*WkBk.GetCount(p).Index;
			int minForOptimalSpeedChunkCount  = (10 * calc.ThreadCount);

			long  userSpecifiedMemoryUsage    = 1024L*1024L*(long)Settings.MemoryMb;


			// chunk count limit due to memory limits
			int variableDataChunkCountLimitedByMemory = (int)Math.Min((userSpecifiedMemoryUsage/ maxBytesPerDataChunk), maxUsefulDataChunkCount);

			taBaWork.Init();

			// Extend by algorithm needed minimum; memory limit does not matter
			variableDataChunkCountLimitedByMemory     = Math.Max( variableDataChunkCountLimitedByMemory, 2*calc.ThreadCount );

			// calc amount of reused MemoryDataChunks
			int variableDataChunkCount = Math.Min( Math.Max(20,minForOptimalSpeedChunkCount), variableDataChunkCountLimitedByMemory );

			// to minimize padding inside the reused DataChunks make the smallest Chunks fixed
			long[] neededChunkBytes = new long[maxUsefulDataChunkCount];
			for ( int i=0 ; i<neededChunkBytes.Length ; i++ )
				neededChunkBytes[i] = (((DataChunkWrite)taBaWork.GetDataChunk( i )).GetByteCount() << 16) | ((uint)i);
			Array.Sort<long>( neededChunkBytes );

			// now fill remaining memory with fixed MemoryDataChunks which are directly Joined to the DataChunks
 			// they will not be unjoined during hole calculation
			long remainingMemory = userSpecifiedMemoryUsage - (variableDataChunkCount*maxBytesPerDataChunk);
			long remainingMemoryStartForProgress = remainingMemory;
			long memoryFilledForProgress = 0;
			Progress.Max = 1000;
			List<DataChunkMemoryWrite> list =  new List<DataChunkMemoryWrite>();
			while ( remainingMemory>0 && list.Count<maxUsefulDataChunkCount ) {
				int dataChunkIndex       = (int)(neededChunkBytes[list.Count] & 65535);
				DataChunkWrite       dc  = (DataChunkWrite)taBaWork.GetDataChunk( dataChunkIndex );
				DataChunkMemoryWrite dcm = new DataChunkMemoryWrite( (int)dc.IndexCount, (int)taBaWork.GetBitsPerEntry(dc.Wtm), 0, 0 );  // fixed chunks can be allocated with exact needed space
#if DEBUG
				if ( Settings.VerifyJoinUnjoin )
					dcm.Index = list.Count;
#endif
				dcm.WriteData = dcm.WritePotentialNew = true;
				dc.Join( dcm, readChunkCounter, true );
				if ( continueOldCalculation )
					ReadDataChunk( dcm, pieces );
				wkBkWtm_To_ChunkIndex[ dc.WkBk.Index + (dc.Wtm ? 0 : dc.WkBk.Count.Index) ] = list.Count;
				dcm.UsingCount = 0;
				list.Add( dcm );
				memoryFilledForProgress += dc.GetByteCount();

				Progress.Value = Math.Max( 1000*list.Count/maxUsefulDataChunkCount, memoryFilledForProgress/(remainingMemoryStartForProgress/1000) );
				remainingMemory -= dc.GetByteCount();
				bytesAllocated += dc.GetByteCount();
			}
			fixedDataChunkCount = nextChunkIndexToReuse = list.Count;
			AllChunksInMemory = list.Count==maxUsefulDataChunkCount;

			// reduce variableDataChunkCount if maximum useful is reached
			variableDataChunkCount = emptyChunkCount = Math.Min( variableDataChunkCount, maxUsefulDataChunkCount-fixedDataChunkCount );

			// create reusable memory datachunks
			for ( int i=0 ; i<variableDataChunkCount ; i++ ) {
				DataChunkMemoryWrite dcm = new DataChunkMemoryWrite( (int)maxIndicesPerChunkWtm, taBaWork.BitsPerEntryWtm, (int)maxIndicesPerChunkBtm, taBaWork.BitsPerEntryBtm ) ;
#if DEBUG
				if ( Settings.VerifyJoinUnjoin )
					dcm.Index = list.Count;
#endif
				list.Add( dcm );
			}

			bytesAllocated += maxBytesPerDataChunk * variableDataChunkCount;

			dataChunksMemoryWrite = list.ToArray();
			Message.Line( "   Max Bytes per Chunk              = " + BytesPerDataChunk.ToString("###,###,###,##0") + "     Sum = " + Tools.LongToKiloMegaGiga(bytesAllocated) + "B" );
			Message.Line( "   Allocated Chunks                 = " + dataChunksMemoryWrite.Length.ToString("###,##0") + "/"+maxUsefulDataChunkCount.ToString("###,##0")  );
			Message.Line( "   Minimum for current Thread count = " + (2*calc.ThreadCount).ToString("###,##0") );
			Message.Line( "   Minimum for Optimal Speed        = " + minForOptimalSpeedChunkCount.ToString("###,##0") );
			Progress.Reset();
		}



		public int DataChunkCount
		{
			get{ return dataChunksMemoryWrite.Length; }
		}

		public long BytesPerDataChunk
		{
			get{ return maxBytesPerDataChunk; }
		}

		public TaBaWrite TaBaWrite
		{
			get{ return taBaWork; }
		}



		/// <summary>
		/// You could call also the simple GetDataChunk function several times.
		/// But this might cause e.G. the first call to use the chunk the second call wants tu use.
		/// This would lead to unnecessary overhead.
		/// </summary>
		public void GetDataChunks( out DataChunkWrite data1, WkBk wkbk1, bool wtm1, bool forWriteData1, bool forWritePotentialNew1, out DataChunkWrite data2, WkBk wkbk2, bool wtm2, bool forWriteData2, bool forWritePotentialNew2, int threadIndex )
		{
			lock ( this ) {
				if ( emptyChunkCount!=0 ) {
					data1 = GetDataChunk( wkbk1, wtm1, forWriteData1, forWritePotentialNew1 );
					data2 = GetDataChunk( wkbk2, wtm2, forWriteData2, forWritePotentialNew2 );
				}
				else {
					int chunkIndex1 = wkBkWtm_To_ChunkIndex[wkbk1.Index + (wtm1 ? 0 : wkbk1.Count.Index)];
					int chunkIndex2 = wkBkWtm_To_ChunkIndex[wkbk2.Index + (wtm2 ? 0 : wkbk2.Count.Index)];
					data1 = null;
					data2 = null;

					if ( chunkIndex1!=-1 ) {
						data1 = (DataChunkWrite)dataChunksMemoryWrite[chunkIndex1].DataChunk;
						GiveDataChunk( chunkIndex1, forWriteData1, forWritePotentialNew1 );
					}
					if ( chunkIndex2!=-1 ) {
						data2 = (DataChunkWrite)dataChunksMemoryWrite[chunkIndex2].DataChunk;
						GiveDataChunk( chunkIndex2, forWriteData2, forWritePotentialNew2 );
					}
					if ( chunkIndex1==-1 )
						data1 = GetDataChunk( wkbk1, wtm1, forWriteData1, forWritePotentialNew1 );
					if ( chunkIndex2==-1 )
						data2 = GetDataChunk( wkbk2, wtm2, forWriteData2, forWritePotentialNew2 );
				}
			}
		}

		/// <summary>
		/// You could call also the simple GetDataChunk function several times.
		/// But this might cause e.G. the first call to use the chunk the second call wants tu use.
		/// This would lead to unnecessary overhead.
		/// </summary>
		public void GetDataChunks( GetDataChunkInfo[] info, int threadIndex )
		{
			for ( int i=0 ; i<info.Length ; i++ ) {
				int chunkIndex = wkBkWtm_To_ChunkIndex[info[i].WkBk.Index + (info[i].Wtm ? 0 : info[i].WkBk.Count.Index)];
				if ( chunkIndex!=-1 ) {
					info[i].DataChunk = (DataChunkWrite)dataChunksMemoryWrite[chunkIndex].DataChunk;
					GiveDataChunk( chunkIndex, info[i].ForWriteData, info[i].ForWriteFastBits );
				}
			}
			for ( int i=0 ; i<info.Length ; i++ ) {
				if ( info[i].DataChunk == null )
					info[i].DataChunk = GetDataChunk( info[i].WkBk, info[i].Wtm, info[i].ForWriteData, info[i].ForWriteFastBits, true );
			}
		}


		public DataChunkWrite GetDataChunk( WkBk wkbk, bool wtm, bool forWriteData, bool forWriteFastBits )
		{
			return GetDataChunk( wkbk, wtm, forWriteData, forWriteFastBits, true );
		}


		public bool IsDataChunkAvailable( WkBk wkbk, bool wtm )
		{
			int chunkIndex = wkBkWtm_To_ChunkIndex[wkbk.Index+(wtm?0:wkbk.Count.Index)];
			return chunkIndex!=-1;
		}


		public DataChunkWrite GetDataChunk( WkBk wkbk, bool wtm, bool forWriteData, bool forWritePotentialNew, bool readBeforeUsing )
		{
			lock ( this ) {

#if DEBUG
				if ( wkbk.Pawn != pieces.ContainsPawn )
					throw new Exception();
#endif
				int chunkIndex = wkBkWtm_To_ChunkIndex[wkbk.Index+(wtm?0:wkbk.Count.Index)];
				if ( chunkIndex != -1 ) {
#if DEBUG
					if ( !(dataChunksMemoryWrite[chunkIndex].DataChunkJoined && dataChunksMemoryWrite[chunkIndex].DataChunk.Pieces==pieces && dataChunksMemoryWrite[chunkIndex].DataChunk.WkBk == wkbk && dataChunksMemoryWrite[chunkIndex].DataChunk.Wtm == wtm) )
						throw new Exception();
#endif
					GiveDataChunk( chunkIndex, forWriteData, forWritePotentialNew );
					return (DataChunkWrite)dataChunksMemoryWrite[chunkIndex].DataChunk;
				}
				else if ( emptyChunkCount!=0 ) {
					for ( int i=0 ; i<dataChunksMemoryWrite.Length ; i++ ) {
						if ( !dataChunksMemoryWrite[i].DataChunkJoined ) {
							taBaWork.GetDataChunk(wtm, wkbk).Join(dataChunksMemoryWrite[i],readChunkCounter,false);
							if ( readBeforeUsing )
								ReadDataChunk( dataChunksMemoryWrite[i], pieces );
							wkBkWtm_To_ChunkIndex[wkbk.Index+(wtm?0:wkbk.Count.Index)] = i;
							emptyChunkCount--;
#if DEBUG
							if ( dataChunksMemoryWrite[i].UsingCount != 0 )
								throw new Exception( "Unexpeceted Using Count" );
#endif
							GiveDataChunk( i, forWriteData, forWritePotentialNew );
							return (DataChunkWrite)dataChunksMemoryWrite[i].DataChunk;
						}
					}
				}
				else {
					int loops = 0;

					do {
						if ( ++nextChunkIndexToReuse == dataChunksMemoryWrite.Length ) {
							nextChunkIndexToReuse = fixedDataChunkCount;
							if ( ++loops == 2 )
								throw new Exception( "all data chunks in use" );
						}
					} while ( dataChunksMemoryWrite[nextChunkIndexToReuse].UsingCount != 0 );
					int i= nextChunkIndexToReuse;
					SaveDataChunk( i );
					if ( dataChunksMemoryWrite[i].DataChunkJoined )
						wkBkWtm_To_ChunkIndex[dataChunksMemoryWrite[i].DataChunk.WkBk.Index + (dataChunksMemoryWrite[i].DataChunk.Wtm ? 0 : dataChunksMemoryWrite[i].DataChunk.WkBk.Count.Index)] = -1;
					dataChunksMemoryWrite[i].DataChunk.Unjoin();
					taBaWork.GetDataChunk(wtm, wkbk).Join(dataChunksMemoryWrite[i],readChunkCounter,false);
					if ( readBeforeUsing )
						ReadDataChunk( dataChunksMemoryWrite[i], pieces );
					wkBkWtm_To_ChunkIndex[wkbk.Index + (wtm ? 0 : wkbk.Count.Index)] = i;
					GiveDataChunk( i, forWriteData, forWritePotentialNew );
					return (DataChunkWrite)dataChunksMemoryWrite[i].DataChunk;
				}

				throw new Exception( "Minimum number of necessary chunks are not available!" );
			}
		}


#if DEBUG
		public void VerifyAllUsingCountIsZero()
		{
			for ( int i=0 ; i<dataChunksMemoryWrite.Length ; i++ ) {
				if ( dataChunksMemoryWrite[i].UsingCount != 0 )
					throw new Exception( "Using count nonzero for one DataChunk." );
			}
		}
#endif

		private void GiveDataChunk( int chunkIndex, bool forWriteData, bool forWriteFastBits )
		{
			DataChunkMemoryWrite dcmw = dataChunksMemoryWrite[chunkIndex];
			DataChunkWrite       dcw  = (DataChunkWrite)dcmw.DataChunk;

			// Using a dataChunk multiple times is currently not used.
			// But in TaskMv with fastCalculation on the same DataChunk might be requested multiple times
			// due king moves can result in the same WkBk-Destination(mirroring).
			// In this case the using count can be >=2,
			// however in this case the same thread is used.
			// So allow it!
//			if ( dcmw.UsingCount != 0 ) {
//				throw new Exception( "Chunk is used multiple times" );
//			}

			dcmw.WriteData         |= forWriteData;
			dcmw.WritePotentialNew |= forWriteFastBits;

			if ( dcmw.UsingCount++ == 0 ) {
				dcw.ResCountConvert = taBaWork.GetResCountConvert( dcw.Wtm );
			}
		}


		public void FreeDataChunk( DataChunkWrite data )
		{
			lock ( this ) {
				while ( Calc.Pause )
					System.Threading.Thread.Sleep( 500 );

				if ( --data.DataChunkMemory.UsingCount == 0 )
					data.ResCountConvert = null;
			}
		}

		public void CloseDueUserAbort()
		{
			if ( taBaWork != null ) {
				taBaWork.CloseAndDelete( true );
				taBaWork = null;
			}
			dataChunksMemoryWrite = null;
		}

		public void Close()
		{
			if ( taBaWork != null ) {
				Flush();
				taBaWork.Close( false );
				taBaWork = null;
				dataChunksMemoryWrite = null;
			}
		}

		private void Flush()
		{
			Message.Line( "Write all buffered Data" );
			Progress.Max = dataChunksMemoryWrite.Length;
			taBaWork.WriteData();
			if ( dataChunksMemoryWrite != null ) {
				for ( int i=0 ; i<dataChunksMemoryWrite.Length ; i++ ) {
					Progress.Value = i;
					if ( dataChunksMemoryWrite[i].DataChunkJoined ) {
						SaveDataChunk( i );
						dataChunksMemoryWrite[i].DataChunk.Unjoin();
					}
				}
			}
		}


		private void SaveDataChunk( int index )
		{
			taBaWork.Save( (DataChunkWrite)dataChunksMemoryWrite[index].DataChunk, dataChunksMemoryWrite[index].WriteData, dataChunksMemoryWrite[index].WritePotentialNew );
		}


		public static long ReadChunkCounter
		{
			get{ return readChunkCounter; }
		}


		private void ReadDataChunk( DataChunkMemoryWrite dataChunk, Pieces pieces )
		{
			readChunkCounter++;
			taBaWork.LoadDataChunk( (DataChunkWrite)dataChunk.DataChunk );
		}


		public long Compress( CalcTB calc, Pieces pieces )
		{
			CompressionType compType = Settings.CompressionType;


			TaBaWrite taBaWrite = calc.TaBasesWrite.TaBaWrite;

			PieceGroupReorder pieceGroupReorderWtm = PieceGroupReorder.GetFromString( pieces, Settings.TbInfo.Get( pieces ).PieceGroupReorderWtm );
			PieceGroupReorder pieceGroupReorderBtm = PieceGroupReorder.GetFromString( pieces, Settings.TbInfo.Get( pieces ).PieceGroupReorderBtm );
			int blockSize = BlockSplitterWrite.WriteBlockSize;
			RecalcResults recalcRes = Settings.CompressRecalculateRes;

            ChunkBlockSplitterWrite chunkBlockSplitter = new ChunkBlockSplitterWrite( pieces, blockSize, pieceGroupReorderWtm, pieceGroupReorderBtm, recalcRes != RecalcResults.Disabled );

			TaBaRead taBaRead = new TaBaRead( pieces, blockSize, taBaWrite.WtmMaxWiIn, taBaWrite.WtmMaxLsIn, taBaWrite.BtmMaxWiIn, taBaWrite.BtmMaxLsIn, compType, pieceGroupReorderWtm, pieceGroupReorderBtm );
			ResToIntConverter resToIntConverterWtm = taBaRead.GetResToIntConverter(true);
			ResToIntConverter resToIntConverterBtm = taBaRead.GetResToIntConverter(false);

			Settings.TbInfo.Current.BitsPerResWtm = taBaRead.GetResToIntConverter(true).MaxBitsForInteger;
			Settings.TbInfo.Current.BitsPerResBtm = taBaRead.GetResToIntConverter(false).MaxBitsForInteger;

			BlockSplitterWrite bsw = new BlockSplitterWrite( calc, TaBaRead.GetFilename(pieces), compType, chunkBlockSplitter.BlockCount, chunkBlockSplitter.BlockSize, pieceGroupReorderWtm.ToInteger(), pieceGroupReorderBtm.ToInteger(), recalcRes );
			//
			//  Do Compression
			//
			Threading.Do( new TasksCompress(compType,calc,resToIntConverterWtm,resToIntConverterBtm,pieceGroupReorderWtm,pieceGroupReorderBtm,taBaRead.GetAllDataChunks(),bsw,chunkBlockSplitter,recalcRes) );

			Settings.TbInfo.Current.Bytes = bsw.Close( Calc.Abort );

			string compressionInfo = "Blocks=" + chunkBlockSplitter.BlockCount.ToString() + "  " + Settings.CompressionSettingsString;
			Message.Text( new string(' ',pieces.ToString().Length) + "             wtm: " + pieceGroupReorderWtm.GetString(pieces) + "  btm: " + pieceGroupReorderBtm.GetString(pieces) + "  FileSize=" + Tools.LongToKiloMegaGiga(bsw.FileSize) +"B" );
			taBaWork.AddToLog( pieces.ToString() + "    " + App.Version4 + "   " + Settings.CompressionSettingsString + "   " + compressionInfo );
			return bsw.FileSize;
		}

		public void CloseAndDelete( bool appendLog )
		{
			taBaWork.WriteLog(TBaccess.GetFilenameLog(pieces.ToString()), appendLog);
			taBaWork.CloseAndDelete(true);
			taBaWork = null;
		}



	}



}
