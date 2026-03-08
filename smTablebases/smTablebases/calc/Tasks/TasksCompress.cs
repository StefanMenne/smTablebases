using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TBacc;


namespace smTablebases
{
	public class TasksCompress : Tasks
	{
		private DataChunk[]                        dataChunks;
		private CompressionType                    compressionType;
		private BlockSplitterWrite                 blockSplitterWrite;
		private byte[][]                           memory;
		private int[][]                            posToVirtualPos;
		private Compressor[]                       compressorForThread;   // one compressor for each thread;
		private BlockingCollection<Compressor>     compressorFree;        // currently unused compressors
		private BlockingCollection<Compressor>     compressorToWrite;     // compressor with already compressed data ready to write
		private ChunkBlockSplitterWrite            chunkBlockSplitter;

		private ResToIntConverter      resToIntConverterWtm,             resToIntConverterBtm;
		private PieceGroupReorder        pieceGroupReorderWtm,               pieceGroupReorderBtm;
		private RecalcResults          recalcRes;
		private int                    lcEncoderFinishedEncodingsInHundredthAtStart = LC.Encoder.FinishedEncodingsInHundredth;


		public TasksCompress( CompressionType compressionType, CalcTB calcTb, ResToIntConverter resToIntConverterWtm, ResToIntConverter resToIntConverterBtm, PieceGroupReorder pieceGroupReorderWtm, PieceGroupReorder pieceGroupReorderBtm, DataChunk[] dataChunks, BlockSplitterWrite blockSplitterWrite, ChunkBlockSplitterWrite chunkBlockSplitter, RecalcResults recalcRes ) : base( calcTb )
		{
			this.compressionType                  = compressionType;
			this.resToIntConverterWtm             = resToIntConverterWtm;
			this.resToIntConverterBtm             = resToIntConverterBtm;
			this.pieceGroupReorderWtm               = pieceGroupReorderWtm;
			this.pieceGroupReorderBtm               = pieceGroupReorderBtm;
			this.dataChunks                       = dataChunks;
			this.blockSplitterWrite               = blockSplitterWrite;
			this.chunkBlockSplitter               = chunkBlockSplitter;
			this.recalcRes                        = recalcRes;
		}


		public override MyTask[] Init( int threadCount )
		{
			compressorForThread = new Compressor[threadCount];
			for ( int i=0 ; i<threadCount ; i++ )
				compressorForThread[i] = new Compressor( compressionType, chunkBlockSplitter.BlockSize );

			if ( threadCount != 1 ) {
				compressorToWrite = new BlockingCollection<Compressor>();
				compressorFree = new BlockingCollection<Compressor>();
				for ( int i=0 ; i<threadCount ; i++ ) {
					compressorFree.Add( new Compressor( compressionType, chunkBlockSplitter.BlockSize ) );
				}
			}

			memory = new byte[threadCount][];
			posToVirtualPos = new int[threadCount][];
			for ( int i=0 ; i<threadCount ; i++ ) {
				memory[i]          = new byte[Config.BlockSize];
				if ( recalcRes != RecalcResults.Disabled )
					posToVirtualPos[i] = new int[Config.BlockSize];
			}
			int blockCount = calcTB.Pieces.GetIsSymmetric() ? (chunkBlockSplitter.BlockCount/2) : chunkBlockSplitter.BlockCount; // restore one half via SwitchSides
			Message.Line( "     Blocks=" + blockCount.ToString() + "   MultipleChunksInOneBlock=" + chunkBlockSplitter.MultipleChunksInOneBlock.ToString() + "     " + Settings.CompressionSettingsString );
			Message.Line();
			if ( chunkBlockSplitter.MultipleChunksInOneBlock ) {
				Message.Line( "Block       Virt-Size     IP-Size        Size     Compr.    Buckets                 (HT=Max HashTable chain length; HTfill=ht entries out of byteCount cur and max)" );
				Message.Line( "    Chunk " + (Config.BlockSize*Config.FactorVirtualPos).ToString("###,###,###").PadLeft(11) + "  " + ((Config.BlockSize*Config.FactorIpSizeDividedBy8)>>3).ToString("##,###,###").PadLeft(10) + "  " + Config.BlockSize.ToString("##,###,###").PadLeft(10) + "       Size HT used  HTfill         " );
			}
			else {
				Message.Line( "Block       Virt-Size        Size      Compr.    Buckets                 (HT=Max HashTable chain length; HTfill=ht entries out of byteCount cur and max)" );
				Message.Line( "    Chunk " + (Config.BlockSize*Config.FactorVirtualPos).ToString("###,###,###").PadLeft(11) + " " + Config.BlockSize.ToString("##,###,###").PadLeft(10) + "       Size HT used  HTfill         " );
			}
			Message.Line( "---------------------------------------------------------------------------" );
			List<MyTask> list = new List<MyTask>();
			for ( int i=0 ; i<blockCount ; i++ )
				list.Add( new MyTaskCompress( calcTB, calcTB.Pieces, i ) );

			NumerizeSteps(list);
			return tasks = list.ToArray();
		}


		public override int ThreadCount
		{
			get{
				return Math.Min( Settings.ThreadCountCompression, base.ThreadCount );
			}
		}


		public RecalcResults RecalcRes
		{
			get { return recalcRes; }
		}


		public override int ProgressCurrent
		{
			get {
				return (compressionType==CompressionType.LC&&tasks!=null) ? (LC.Encoder.FinishedEncodingsInHundredth-lcEncoderFinishedEncodingsInHundredthAtStart) : (base.ProgressCurrent);
			}
		}


		public override int ProgressMax
		{
			get {
				return (compressionType==CompressionType.LC&&tasks!=null) ? (100*tasks.Length) : (base.ProgressMax);
			}
		}


		public override void ControlThreadWork()
		{
			List<Compressor> comp = new List<Compressor>();

			while ( blockSplitterWrite.AddedBlocks<tasks.Length && !Calc.Abort ) {
				Compressor current = null;
				for ( int i=0 ; i<comp.Count ; i++ ) {
					if ( blockSplitterWrite.AddedBlocks == (int)comp[i].Tag ) {
						current = comp[i];
						comp.Remove( current );
						break;
					}
				}
				if ( current == null ) {
					Compressor c = compressorToWrite.Take();
					comp.Add( c );
				}
				else {
					WriteBlock( current );
					compressorFree.Add( current );
				}
			}
		}


		public ChunkBlockSplitterWrite ChunkBlockSplitter
		{
			get { return chunkBlockSplitter; }
		}


		public void WriteBlock( Compressor compressor )
		{
			Message.Line( compressor.Info );  // to get lines in the right order print it here and not before
			blockSplitterWrite.AddBlock( compressor.BufferOut, 0, compressor.BufferOutDataLength );
		}


		public override void FinishedAllTasks( bool aborted )
		{
			long uncompressedBytes = 0L;
			while ( compressorFree!=null && compressorFree.Count != 0 )
				uncompressedBytes += compressorFree.Take().UncompressedBytesSum;
			for ( int i=0 ; i<compressorForThread.Length ; i++ )
				uncompressedBytes += compressorForThread[i].UncompressedBytesSum;

			if ( calcTB.Pieces.GetIsSymmetric() ) {
				for ( int i=0 ; i<tasks.Length ; i++ )
					blockSplitterWrite.AddBlock( new byte[0], 0, 0 );
			}
		}


		public byte[] GetMem( int threadIndex )
		{
			return memory[threadIndex];
		}


		public int[] GetPosToVirtualPos( int threadIndex )
		{
			return posToVirtualPos[threadIndex];
		}



		public ResToIntConverter GetResToIntConverter( bool wtm )
		{
			return wtm ? resToIntConverterWtm : resToIntConverterBtm;
		}



		public PieceGroupReorder GetPieceGroupReorder( bool wtm )
		{
			return wtm ? pieceGroupReorderWtm : pieceGroupReorderBtm;
		}


		public DataChunkRead GetDataChunk( int index )
		{
			return (DataChunkRead)dataChunks[index];
		}


		public Compressor GetCompressor( int threadIndex, int blockIndex )
		{
			return compressorForThread[threadIndex];
		}

		public void CompressFinished( int threadIndex, Compressor compressor )
		{
			// give compressor to writing task and then block until a compressor is available
			// Ensures that if a new task is requested this task can also be processed.

			compressorToWrite.Add( compressor );

			ThreadInfo ti = Threading.GetThreadInfo( threadIndex );
			ti.ThreadState = MyThreadState.WaitForFreeBufferToStoreCompressedData;
			Compressor c = compressorFree.Take();
			ti.ThreadState = MyThreadState.Running;
			compressorForThread[threadIndex] = c;
		}
	}
}
