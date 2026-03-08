using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace TBacc
{
	public class TaBasesRead
	{
		private static int                           loadChunkCounter              = 0;

		private DataChunkMemoryRead[]                dataChunkMemoryRead           = null;
		private TaBaRead[]                           taBa                          = null;
		private TmpBlockStorage[]                    tmpBlockStorage               = null;
		private TmpBlockStorage                      tmpBlockStorageSingleThreaded = new TmpBlockStorage();       
		private Decompressor[]                       decompressors                 = null;
		private bool                                 expliciteFreeOfTmpBlockStorage = false; // to optimize which storage to be freed
		private Pieces[]                             pieces;
#if DEBUG
		public static Action<DataChunkRead>          VerifyLoadedDataChunk          = null;
#endif


		public static TaBasesRead OpenAllSubPieces( out string log, Pieces p, int threadCount )
		{
			TaBasesRead tbr = new TaBasesRead( out log, p.GetSubPieces(), threadCount );
			return (tbr.dataChunkMemoryRead==null) ? null : tbr;
		}

		
		public static TaBasesRead OpenSingle( Pieces p, int threadCount )
		{
			string log;
			TaBasesRead tbr = new TaBasesRead( out log, new List<Pieces>(new Pieces[] { p }), threadCount );
			return (tbr.dataChunkMemoryRead==null) ? null : tbr;
		}


		private TaBasesRead( out string log, List<Pieces> p, int threadCount )
		{
			pieces = p.ToArray();
			bool multipleChunksInOneBlockForAny = false;
			int blockSize = Config.BlockSize;	
			log = "";
			long countBytesPerChunk = 0;
			int maxChunksPerBlock = 0;

			decompressors = new Decompressor[threadCount];
			for ( int i=0 ; i<decompressors.Length ; i++ )
				decompressors[i] = new Decompressor( Config.BlockSize );

			taBa= new TaBaRead[ (p.Count==0) ? 0 : (p.Max( pi => pi.Index )+1) ];			 

			foreach( Pieces sp in p ) {
				int idx = sp.Index;
				TaBaRead tb = TaBaRead.OpenForRead( sp, TaBaOpenType.ReadOnly );
				if ( tb==null ) {
					CloseAll( true );
					throw new Exception(	sp.ToString() + " missing " );
				}
				multipleChunksInOneBlockForAny |= tb.MultipleChunksInOneBlock;
				log += tb.ToString() + "\r\n";
				maxChunksPerBlock = Math.Max( maxChunksPerBlock, tb.MaxChunksPerBlock );
				long bytesPerChunkWtm = 2 * (int)tb.MaxIndexCountPerChunkWtm;
				long bytesPerChunkBtm = 2 * (int)tb.MaxIndexCountPerChunkBtm;
				countBytesPerChunk = Math.Max( countBytesPerChunk, Math.Max( bytesPerChunkWtm, bytesPerChunkBtm ) );
				taBa[idx] = tb;
				int piecesIndex = sp.Index;
			}
			dataChunkMemoryRead = new DataChunkMemoryRead[ threadCount + ( (maxChunksPerBlock==-1) ? 0 : 2*maxChunksPerBlock ) ]; 
			for ( int i=0 ; i<dataChunkMemoryRead.Length ; i++ ) {
				dataChunkMemoryRead[i] = new DataChunkMemoryRead( countBytesPerChunk );
#if DEBUG
				dataChunkMemoryRead[i].Index = i;
#endif
			}

			if ( multipleChunksInOneBlockForAny ) {
				tmpBlockStorage = new TmpBlockStorage[(threadCount==1)?1:(threadCount+2)];
				for ( int i=0 ; i<tmpBlockStorage.Length ; i++ ) {
					tmpBlockStorage[i] = new TmpBlockStorage();
					tmpBlockStorage[i].Index = i;
				}
			}
		}


		public bool Contains( Pieces pieces )
		{
			return this.pieces.Contains<Pieces>( pieces );
		}


		public void CloseAll( bool abort )
		{
#if DEBUG
			Verify();
#endif
			for ( int i=0 ; i<taBa.Length ; i++ ) {
				if ( taBa[i] != null )
					taBa[i].Close( abort );
			}
			taBa = null;
			dataChunkMemoryRead = null;
		}


		public static int LoadChunkCounter
		{
			get{ return loadChunkCounter; }
		}


		public TaBaRead GetTaBa( Pieces pieces )
		{
			return taBa[pieces.Index];
		}


		public bool ExpliciteFreeOfTmpBlockStorage
		{
			get { return expliciteFreeOfTmpBlockStorage; }
			set {  expliciteFreeOfTmpBlockStorage = value; }
		}


		public DataChunkRead GetDataChunk( Pieces pieces, WkBk wkbk, bool wtm )
		{
			TaBaRead taBaRead = GetTaBa(pieces);

			int  emptyIndex    = -1;
			long emptyIndexAge = long.MaxValue;

			for ( int i=0 ; i<dataChunkMemoryRead.Length ; i++ ) {
				if ( dataChunkMemoryRead[i].DataChunkJoined && dataChunkMemoryRead[i].DataChunk.Pieces == pieces && dataChunkMemoryRead[i].DataChunk.WkBk == wkbk && dataChunkMemoryRead[i].DataChunk.Wtm == wtm ) {
					dataChunkMemoryRead[i].UsingCount++;
					return (DataChunkRead)dataChunkMemoryRead[i].DataChunk;
				}
				else if ( dataChunkMemoryRead[i].UsingCount==0 ) {
					if ( !dataChunkMemoryRead[i].DataChunkJoined ) {
						emptyIndex = i;
						emptyIndexAge = -1;
					}
					else if ( dataChunkMemoryRead[i].LastJoin < emptyIndexAge ) {
						emptyIndex    = i;
						emptyIndexAge = dataChunkMemoryRead[i].LastJoin;						
					}
				}
			}
			if ( emptyIndex == -1 )
				throw new Exception( "No unused DataChunk found" );


			if ( dataChunkMemoryRead[emptyIndex].DataChunkJoined )
				dataChunkMemoryRead[emptyIndex].DataChunk.Unjoin();
			taBaRead.GetDataChunk(wtm, wkbk).Join(dataChunkMemoryRead[emptyIndex],loadChunkCounter++, false);
			dataChunkMemoryRead[emptyIndex].UsingCount = 1;
			return (DataChunkRead)dataChunkMemoryRead[emptyIndex].DataChunk;
		}


		public void LoadDataChunkSingleThreaded( DataChunkRead data )
		{
			TaBaRead taBaRead = GetTaBa( data.Pieces );
			if ( taBaRead.MultipleChunksInOneBlock ) {
				int blockIndex = taBaRead.ChunkBlockSplitter.GetFirstBlock( data.WkBk, data.Wtm );
				if ( tmpBlockStorageSingleThreaded.BlockIndex != blockIndex || tmpBlockStorageSingleThreaded.PiecesIndex != data.Pieces.Index )
					tmpBlockStorageSingleThreaded.CreateNew( data.Pieces.Index, blockIndex );
			}
			LoadDataChunk( taBaRead, data, 0, tmpBlockStorageSingleThreaded );
		}


		public void LoadTmpBlockStorage( TaBaRead taBaRead, int threadIndex, int blockIndex, TmpBlockStorage tmpBlockStorage )
		{
			lock( tmpBlockStorage ) {
				if ( tmpBlockStorage.IsLoaded )
					return;

				taBaRead.LoadBlockContainingMultipleChunks( tmpBlockStorage, blockIndex, decompressors[threadIndex] );
				tmpBlockStorage.IsLoaded = true;
			}
		}



		public void FreeDataChunk( DataChunkRead data )
		{
			data.DataChunkMemory.UsingCount--;
		}


		public TmpBlockStorage GetTmpBlockStorage( int blockIndex, int piecesIndex )
		{ 
			for ( int i=0 ; i<tmpBlockStorage.Length ; i++ ) {
				TmpBlockStorage tbs = tmpBlockStorage[i];
				if ( tbs.BlockIndex == blockIndex && tbs.PiecesIndex == piecesIndex )
					return tbs;
			}
			return null;
		}


		public bool HasTmpBlockStorage
		{
			get {  return tmpBlockStorage!=null; }
		}

		public int CountTmpBlockStorage
		{
			get { return tmpBlockStorage.Length; }
		}


		public TmpBlockStorage GetTmpBlockStorage( int i )
		{
			return tmpBlockStorage[i];
		} 


		public void LoadDataChunk( TaBaRead taBaRead, DataChunkRead dataChunk, int threadIndex, TmpBlockStorage tmpBlockStorage )
		{
			WkBk wkBk = dataChunk.WkBk;
	
			if ( dataChunk.Pieces.GetIsSymmetric() && dataChunk.GetDataChunkIndex()>=DataChunkIndex.GetHalfCount(dataChunk.Pieces.ContainsPawn) ) {
				// at symmetric TB's like KQKQ, KRBKRB half positions are redundant; keep redundancy inside memory but remove on disk
				// read the mirrored chunk and reorder 
				// Leave TmpBlockStorage unoptimized: It will contain the original BlockIndex but will be loaded with the blockIndex to access
				// the single physically available data.
				Pieces pieces = dataChunk.Pieces;
				bool wtm  = dataChunk.Wtm;
				MirrorType mirror = MirrorNormalize.WkBkToMirror( wkBk.Bk, wkBk.Wk, pieces.ContainsPawn );
				WkBk wkBkLoad = wkBk.Reverse();
				bool wtmLoad  = !wtm;

				if ( taBaRead.MultipleChunksInOneBlock ) 
					LoadTmpBlockStorage( taBaRead, threadIndex, taBaRead.ChunkBlockSplitter.GetFirstBlock(wkBkLoad,wtmLoad), tmpBlockStorage );

				lock ( dataChunk ) {     // lock dataChunk to prevent double access of the same DataChunkRead 
					if ( !dataChunk .DataValid ) {
						if ( taBaRead.MultipleChunksInOneBlock ) {
							LoadSmallDataChunk( dataChunk, wkBkLoad, wtmLoad, (int)(2*dataChunk.IndexCount), threadIndex, tmpBlockStorage );
						}
						else {
							LoadBigDataChunk( dataChunk, threadIndex );
						}
						IndexPos indexPos     = new IndexPos( wkBk,     pieces, wtm     );
						IndexPos indexPosLoad = new IndexPos( wkBkLoad, pieces, wtmLoad );
						long count = indexPos.IndexCount;

						for ( long i=0 ; i<count ; i++ ) {
							// replace in disjunct sets
							// first current set; if some index < i then it was already processed
							// if index = i then the set is complete
							long k = i;
							do {
								k = ReorderNextIndex( k, indexPos, indexPosLoad, mirror, pieces.CountW );
							} while ( k>i  );
							if ( k < i )
								continue;


							byte tmp1 = dataChunk.Data[2*i], tmp2 = dataChunk.Data[2*i+1];
							long j=i;
							while( true ) {
								long indexLoad = ReorderNextIndex( j, indexPos, indexPosLoad, mirror, pieces.CountW );

								if ( indexLoad == i ) {
									dataChunk.Data[2*j]   = tmp1;
									dataChunk.Data[2*j+1] = tmp2;
									break;
								}
								dataChunk.Data[2*j]   = dataChunk.Data[2*indexLoad];
								dataChunk.Data[2*j+1] = dataChunk.Data[2*indexLoad+1];
								j = indexLoad;
							}
						}
					}
				}	
			}
			else { 
				if ( taBaRead.MultipleChunksInOneBlock ) {
					LoadTmpBlockStorage( taBaRead, threadIndex, taBaRead.ChunkBlockSplitter.GetFirstBlock(dataChunk.WkBk,dataChunk.Wtm), tmpBlockStorage );
					lock ( dataChunk ) {     
						if ( !dataChunk .DataValid )
							LoadSmallDataChunk( dataChunk, dataChunk.WkBk, dataChunk.Wtm, (int)(2*dataChunk.IndexCount), threadIndex, tmpBlockStorage );
					}
				}				
				else {
					lock ( dataChunk ) {    
						if ( !dataChunk .DataValid )
							LoadBigDataChunk( dataChunk, threadIndex );
					}
				}
			}
		}


		private void LoadSmallDataChunk( DataChunkRead data, WkBk wkBk, bool wtm, int byteCount, int threadIndex, TmpBlockStorage tmpBlockStorage )
		{
			TaBaRead     taBaRead     = GetTaBa( data.Pieces ); 
			taBaRead.LoadDataChunk( data.Data, wkBk, wtm, byteCount, tmpBlockStorage );	
			data.DataValid = true;

#if DEBUG
			if ( VerifyLoadedDataChunk != null )
				VerifyLoadedDataChunk( data );
#endif
		}


		private void LoadBigDataChunk( DataChunkRead data, int threadIndex )
		{
			TaBaRead     taBaRead     = GetTaBa( data.Pieces ); 
			if ( data.Pieces.PieceCount == 0 ) {
				int remVal = taBaRead.GetResToIntConverter(data.Wtm).ResToInt( Res.Draw );	
				data.Data[0] = (byte)(remVal>>8);
				data.Data[1] = (byte)(remVal&255);
				data.DataValid = true;
				return;
			}
			taBaRead.LoadDataChunkOutOfMultipleBlocks( data, decompressors[threadIndex] );
			data.DataValid = true;
#if DEBUG
			if ( VerifyLoadedDataChunk != null )
				VerifyLoadedDataChunk( data );
#endif
		}


		private long ReorderNextIndex( long index, IndexPos ip1, IndexPos ip2, MirrorType mirror, int countPiecesSwitch )
		{
			ip1.SetToIndex( index );
			if ( ip1.GetIsEp( index ) ) {
				Field epDblStepDst, epCapSrc;
				Fields f = ip1.GetFieldsEP( out epDblStepDst, out epCapSrc );
				f = f.Mirror( mirror | MirrorType.MirrorOnHorizontal );
				f = f.SwitchSides( countPiecesSwitch, countPiecesSwitch );
				epDblStepDst = epDblStepDst.Mirror( mirror | MirrorType.MirrorOnHorizontal );
				epCapSrc     = epCapSrc.Mirror( mirror | MirrorType.MirrorOnHorizontal );
				ip2.SetFieldsEP( f, epDblStepDst, epCapSrc );
			}
			else { 
				Fields f = ip1.GetFields();
				f = f.Mirror( mirror );
				f = f.SwitchSides( countPiecesSwitch, countPiecesSwitch );
				ip2.SetFields( f );
			}
			return ip2.GetIndex();
		}

		public void ReuseTmpBlockStorage( int i, Pieces pieces, bool wtm, int blockIndex )
		{
			TaBaRead taBaRead = GetTaBa( pieces );
			tmpBlockStorage[i].CreateNew( pieces.Index, blockIndex );
		}


		public void ClearTmpBlockStorage( int i )
		{
			tmpBlockStorage[i].Clear();
		}
		

#if DEBUG
		public void Verify()
		{
			if ( dataChunkMemoryRead != null ) {
				for ( int i=0 ; i<dataChunkMemoryRead.Length ; i++ ) {
					if ( dataChunkMemoryRead[i].UsingCount != 0 )
						throw new Exception( "Using count is not 0" );
				}
			}
		}
#endif

	}
}
