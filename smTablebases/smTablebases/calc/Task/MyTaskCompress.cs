using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TBacc;

namespace smTablebases
{
	public class MyTaskCompress : MyTask
	{
		private int  blockIndex;


		public MyTaskCompress( CalcTB calcTb, Pieces pieces, int blockIndex ) : base( calcTb )
		{
			this.blockIndex     = blockIndex;
		}


		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			TasksCompress     tasksCompress                     = (TasksCompress)tasks;
			RecalcResults     recalcRes                         = tasksCompress.RecalcRes;
			byte[]            data                              = tasksCompress.GetMem( threadIndex );
			int[]             posToVirtualPos                   = tasksCompress.GetPosToVirtualPos( threadIndex );


			BlockInfo blockInfo = tasksCompress.ChunkBlockSplitter.GetBlockInfo( blockIndex );

			int blockPos = 0, blockPosVirtual = 0, chunkIndex = 0, countVirtualBytes, countBytes;
			string info;
			if ( blockInfo is BlockInfoForMultipleChunks ) {
				BlockInfoForMultipleChunks bi = blockInfo as BlockInfoForMultipleChunks;
				foreach ( BlockInfoChunk bic in bi.Chunks ) {
					bool wtm = bic.Wtm;
					ResToIntConverter resToIntConverter                 = tasksCompress.GetResToIntConverter( wtm );
					PieceGroupReorder   pieceGroupReorder                   = tasksCompress.GetPieceGroupReorder( wtm );
					DataChunkWrite         dataWrite       = (DataChunkWrite)calc.TaBasesWrite.GetDataChunk( bic.WkBk, bic.Wtm, false, false );
					DataChunkRead          dataRead        = tasksCompress.GetDataChunk( DataChunkIndex.Get(bic.WkBk,bic.Wtm) );
					tasksCompress.ChunkBlockSplitter.FillBlockForCompressor( bi, bic, dataWrite, data, posToVirtualPos, pieceGroupReorder, resToIntConverter, recalcRes != RecalcResults.Disabled, ref blockPos, ref blockPosVirtual );
					calc.TaBasesWrite.FreeDataChunk(dataWrite);
					chunkIndex++;
				}
				countBytes = blockPos;
				countVirtualBytes = blockPosVirtual;
				string s1 = countVirtualBytes.ToString("#,###,##0");
				string s2 = blockInfo.CountBytes.ToString("#,###,##0");
				string s3 = countBytes.ToString("#,###,##0");
				if ( blockInfo.BoundReason == 0 )
					s1 = " " + s1.PadLeft(11) + "  " + s2.PadLeft(10) + " " +  ("[" + s3).PadLeft(11) + "]";
				else if ( blockInfo.BoundReason == 1 )
					s1 = " " + s1.PadLeft(11) + " " + ("[" + s2).PadLeft(11) + "] " + s3.PadLeft(10) + " ";
				else if ( blockInfo.BoundReason == 2 )
					s1 = ("[" + s1).PadLeft(12) + "] " + s2.PadLeft(10) + "  " + s3.PadLeft(10) + " ";
				else
					s1 = " " + s1.PadLeft(11) + "  " + s2.PadLeft(10) + "  " + s3.PadLeft(10) + " ";
				info = blockIndex.ToString().PadLeft(4) + " " + chunkIndex.ToString().PadLeft(4) + s1;
				int countBytesCompressed = Compress( tasksCompress, threadIndex, data, posToVirtualPos, blockPos, singleThreaded, info );
			}
			else {
				BlockInfoForChunkPart bip = blockInfo as BlockInfoForChunkPart;
				chunkIndex = DataChunkIndex.Get(bip.WkBk,bip.Wtm);
				bool wtm = bip.Wtm;
				ResToIntConverter      resToIntConverter    = tasksCompress.GetResToIntConverter( wtm );
				PieceGroupReorder        pieceGroupReorder      = tasksCompress.GetPieceGroupReorder( wtm );
				DataChunkWrite         dataWrite            = (DataChunkWrite)calc.TaBasesWrite.GetDataChunk( bip.WkBk, bip.Wtm, false, false );
				DataChunkRead          dataRead             = tasksCompress.GetDataChunk( chunkIndex );

				countBytes = -blockPos;
				countVirtualBytes = -blockPosVirtual;
				tasksCompress.ChunkBlockSplitter.FillBlockForCompressor( bip, null, dataWrite, data, posToVirtualPos, pieceGroupReorder, resToIntConverter, recalcRes != RecalcResults.Disabled, ref blockPos, ref blockPosVirtual);
				countBytes += blockPos;
				countVirtualBytes += blockPosVirtual;
				calc.TaBasesWrite.FreeDataChunk(dataWrite);
				string s1 = countVirtualBytes.ToString("#,###,##0");
				string s3 = countBytes.ToString("#,###,##0");
				if ( blockInfo.BoundReason == 0 )
					s1 = " " + s1.PadLeft(11) + " " + ("[" + s3).PadLeft(11) + "] ";
				else if ( blockInfo.BoundReason == 2 )
					s1 = ("[" + s1).PadLeft(12) + "] " + s3.PadLeft(10) + "  ";
				else
					s1 = " " + s1.PadLeft(11) + "  " + s3.PadLeft(10) + "  ";
				info = blockIndex.ToString().PadLeft(4) + " " + chunkIndex.ToString().PadLeft(4) + s1;
				int countBytesCompressed = Compress( tasksCompress, threadIndex, data, posToVirtualPos, blockPos, singleThreaded, info );
			}
		}


		private int Compress( TasksCompress tasksCompress, int threadIndex, byte[] data, int[] posToVirtualPos, int byteCount, bool singleThreaded, string info )
		{
			Compressor compressor = tasksCompress.GetCompressor( threadIndex, Index );
			compressor.PosToVirtualPos = posToVirtualPos;
			System.Buffer.BlockCopy( data, 0, compressor.BufferIn, 0, byteCount );
#if DEBUG
			if ( Settings.SaveUncompressedBlocks ) {
				byte[] tmp = new byte[byteCount];
				Array.Copy( data, tmp, byteCount );
				if ( !Directory.Exists(App.DebugFolder) )
					Directory.CreateDirectory( App.DebugFolder );
				File.WriteAllBytes( Path.Combine( App.DebugFolder, calc.Pieces.ToString() + "_" + index.ToString("0000") + ".bin" ), tmp );
				if ( posToVirtualPos != null ) {
					tmp = new byte[4*byteCount];
					Buffer.BlockCopy( posToVirtualPos, 0, tmp, 0, 4*byteCount );
					File.WriteAllBytes( Path.Combine( App.DebugFolder, calc.Pieces.ToString() + "_" + index.ToString("0000") + ".p2v" ), tmp );
					int countVirtual = posToVirtualPos[byteCount-1]+1;
				}
			}
#endif
			compressor.Tag = Index;     // task index = blockIndex
			int countBytesCompressed = compressor.Compress( byteCount );

			compressor.Info = info + countBytesCompressed.ToString("#,###,##0").PadLeft(10) + " " + compressor.Info;

			if ( singleThreaded )     // if single threaded then write data directly
				tasksCompress.WriteBlock( compressor );
			else
				tasksCompress.CompressFinished( threadIndex, compressor );

			return countBytesCompressed;
		}


		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
		}

	}
}
