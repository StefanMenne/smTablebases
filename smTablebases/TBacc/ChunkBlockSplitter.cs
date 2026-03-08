using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public class ChunkBlockSplitter
	{
		protected BlockInfo[]     blockInfo;
		protected Pieces          pieces;
		protected int             wkBkCount;
		protected int             countDataChunks;
		protected int[]           chunkIndexToFirstBlock;
		protected int[]           chunkIndexToBlockCount;
		protected int             blockSize;
		protected bool            multipleChunksInOneBlock;
		protected int             maxChunksPerBlock;
		protected PieceGroupReorder PieceGroupReorderWtm, PieceGroupReorderBtm;



		public ChunkBlockSplitter( Pieces pieces, int blockSize, PieceGroupReorder pieceGroupReorderWtm, PieceGroupReorder pieceGroupReorderBtm, bool removeRecalculateablePositions)
		{
			this.pieces              = pieces;
			this.blockSize           = blockSize;
			this.PieceGroupReorderWtm  = pieceGroupReorderWtm;
			this.PieceGroupReorderBtm  = pieceGroupReorderBtm;
		
			CommonCreation( pieces, removeRecalculateablePositions);
		}


		private void CommonCreation( Pieces pieces, bool removeRecalculateablePositions)
		{
			int  blockSizeVirtual         = blockSize * Config.FactorVirtualPos;
			int  maxBlockSizeIp           = (blockSize * Config.FactorIpSizeDividedBy8) >> 3;
			long chunkIndicesUpperBound   = IndexPos.GetMaxIndiciesOverAllChunks( pieces );
			multipleChunksInOneBlock      = (blockSize/2)>3*chunkIndicesUpperBound && pieces.PieceCount!=0; 

			wkBkCount               = WkBk.GetCount( pieces ).Index;
			countDataChunks         = 2*wkBkCount;
			chunkIndexToFirstBlock  = new int[countDataChunks];
			chunkIndexToBlockCount  = new int[countDataChunks];

			List<BlockInfo> blockInfoList = new List<BlockInfo>();

			if ( multipleChunksInOneBlock ) {   // multiple chunks in one block
				int byteCountIp = 0, byteCountVirtual = 0, byteCountWithoutRecalculateable = 0;
				List<BlockInfoChunk> blockInfoChunkList = new List<BlockInfoChunk>();
				foreach( bool wtm in Tools.BoolArray ) {
					PieceGroupReorder pieceGroupReorder = wtm ? PieceGroupReorderWtm : PieceGroupReorderBtm;
					for ( WkBk wkbk=WkBk.First(pieces) ; wkbk<wkbk.Count ; wkbk++ ) {
						int      chunkIndex                             = DataChunkIndex.Get(wkbk,wtm);
						IndexPos indexPos64                             = new IndexPos( wkbk, pieces, wtm, IndexPosType.Compress, pieceGroupReorder );
						IndexPos indexPos                               = new IndexPos( wkbk, pieces, wtm );
						long     indicesVirtual                         = indexPos64.IndexCount;
						long     indices                                = indexPos.IndexCount;
						int      byteCountCurrentVirtual                = (int)indicesVirtual*2;   // represents IndexPos64 indices resp. virtual indices at LC
						int      byteCountCurrentIp                     = (int)indices*2;          // represents IndexPosCalc indices; illegal positions are filtered; only used for TmpBlockStorage handling 
						int      byteCountWithoutRecalculateableCurrent = removeRecalculateablePositions ? (2*(int)CountValid.GetValidCount(indexPos64)):byteCountCurrentIp;   // legal positions; recalculate able positions are filtered
						
						if ( byteCountWithoutRecalculateable + byteCountWithoutRecalculateableCurrent > blockSize || 
						     byteCountVirtual + byteCountCurrentVirtual > blockSizeVirtual || 
							 byteCountIp + byteCountCurrentIp > maxBlockSizeIp  ||(!wtm && wkbk.Index == 0)  ) {   

							int boundReason = 3;
							if ( byteCountWithoutRecalculateable + byteCountWithoutRecalculateableCurrent > blockSize )
								boundReason = 0;
							else if ( byteCountIp + byteCountCurrentIp > maxBlockSizeIp )
								boundReason = 1;
							else if ( byteCountVirtual + byteCountCurrentVirtual > blockSizeVirtual )
								boundReason = 2;

							// start a new block
							maxChunksPerBlock = Math.Max( maxChunksPerBlock, blockInfoChunkList.Count );
							blockInfoList.Add( new BlockInfoForMultipleChunks(){ Chunks=blockInfoChunkList.ToArray(), CountBytesVirtual=byteCountVirtual, CountBytes=byteCountIp, CountBytesWithoutRecalculateable=byteCountWithoutRecalculateable, BoundReason=boundReason } );
							blockInfoChunkList.Clear();
							byteCountWithoutRecalculateable = byteCountIp = byteCountVirtual = 0;
						}
						chunkIndexToFirstBlock[ chunkIndex ] = blockInfoList.Count;
						chunkIndexToBlockCount[ chunkIndex ] = 1;
						blockInfoChunkList.Add( new BlockInfoChunk(){ WkBk=wkbk, Wtm=wtm, ByteOffset=byteCountIp } );
						byteCountIp                     += byteCountCurrentIp;
						byteCountVirtual                += byteCountCurrentVirtual;
						byteCountWithoutRecalculateable += byteCountWithoutRecalculateableCurrent;
					}
				}
				blockInfoList.Add( new BlockInfoForMultipleChunks(){ Chunks=blockInfoChunkList.ToArray(), CountBytesVirtual=byteCountVirtual, CountBytes=byteCountIp, CountBytesWithoutRecalculateable=byteCountWithoutRecalculateable, BoundReason=3 } );
			}
			else {  // multiple blocks for one chunk
				maxChunksPerBlock = 1;
				foreach( bool wtm in Tools.BoolArray ) {
					PieceGroupReorder pieceGroupReorder = wtm ? PieceGroupReorderWtm : PieceGroupReorderBtm;
					for ( WkBk wkbk=WkBk.First(pieces) ; wkbk<wkbk.Count ; wkbk++ ) {
						int chunkIndex = DataChunkIndex.Get(wkbk,wtm);
						chunkIndexToFirstBlock[ chunkIndex ] = blockInfoList.Count;
						IndexPos indexPos     = new IndexPos( wkbk, pieces, wtm, IndexPosType.Compress, pieceGroupReorder );
						long indicesVirtual = indexPos.IndexCount;
						long firstIndex = 0;
						while ( indicesVirtual != 0 ) {
							long indexCountCurrent, validCount;
							int reason;
							if (removeRecalculateablePositions) {
								validCount = CountValid.GetValidCount( indexPos, firstIndex, (Config.BlockSize*Config.FactorVirtualPos)>>1, Config.BlockSize>>1, out indexCountCurrent, out reason );
							}
							else {
								if ( indicesVirtual < (Config.BlockSize>>1) ) {
									validCount = indexCountCurrent = indicesVirtual;
									reason = 3;
								}
								else {
									validCount = indexCountCurrent = (Config.BlockSize>>1);
									reason = 0;
								}
							}

							blockInfoList.Add( new BlockInfoForChunkPart() { Wtm=wtm, WkBk=wkbk, FirstDataChunkIndex=firstIndex, DataChunkIndexCount=indexCountCurrent, CountBytesVirtual=blockSize, CountBytes=-1, CountBytesWithoutRecalculateable=2*(int)validCount, BoundReason=reason } );
							firstIndex        += indexCountCurrent;
							indicesVirtual    -= indexCountCurrent;
						}
						chunkIndexToBlockCount[ chunkIndex ] = blockInfoList.Count - chunkIndexToFirstBlock[chunkIndex];
					}
				}
			}
			blockInfo = blockInfoList.ToArray();
		}


		public bool MultipleChunksInOneBlock
		{
			get { return multipleChunksInOneBlock; }
		}


		public int MaxChunksPerBlock
		{
			get { return maxChunksPerBlock; }
		}

		public int GetFirstBlock( WkBk wkBk, bool wtm )
		{
			return chunkIndexToFirstBlock[DataChunkIndex.Get(wkBk,wtm)];
		}


		public int GetBlockCount( WkBk wkBk, bool wtm )
		{
			return chunkIndexToBlockCount[DataChunkIndex.Get(wkBk,wtm)];
		}


		public int BlockCount
		{
			get { return blockInfo.Length; }
		}


		public BlockInfo GetBlockInfo( int blockIndex )
		{
			return blockInfo[blockIndex];
		}


		public int BlockSize
		{
			get { return blockSize; }
		}


		public void GetInfosForDecompressor( BlockInfo blockInfo, BlockInfoChunk blockInfoChunk, Pieces pieces, byte[] dataOut, ref int blockPos, ref int blockPosVirtual, PieceGroupReorder pieceGroupReorder, ResToIntConverter resToIntConverter, bool recalcRes, int[] posToVirtualPos )
		{
			long  indexCount, firstIndex;
			int   dataOutOffset;
			bool  wtm;
			WkBk  wkBk;


			if ( blockInfo is BlockInfoForChunkPart ) {
				BlockInfoForChunkPart bicp = blockInfo as BlockInfoForChunkPart;
				indexCount               = bicp.DataChunkIndexCount;
				firstIndex                 = bicp.FirstDataChunkIndex;
				dataOutOffset              = 0;
				wtm                        = bicp.Wtm;
				wkBk                       = bicp.WkBk;
			}
			else {
				firstIndex           = 0;
				indexCount         = -1;
				dataOutOffset        = blockInfoChunk.ByteOffset;
				wtm                  = blockInfoChunk.Wtm;
				wkBk                 = blockInfoChunk.WkBk;
			}


			IndexPos                       indexPosCalc    = new IndexPos( wkBk, pieces, wtm );
			IndexPos                       indexPosComp64  = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress, pieceGroupReorder );
			IndexEnumerator indexEnumerator = new IndexEnumerator( indexPosComp64, indexPosCalc, false );
			int illResVal = resToIntConverter.ResToInt( Res.IllegalPos );	
			if ( indexCount == -1 )
				indexCount = indexPosComp64.IndexCount;

			CheckAndPin cpStm = new CheckAndPin( wkBk, pieces, wtm ), cpSntm = new CheckAndPin( wkBk, pieces, !wtm );

			indexEnumerator.AddToSrcIndex( firstIndex );
			for ( int i=0 ; i<indexCount ; i++ ) {
				if ( indexEnumerator.IndexPosDstValid ) {  
					long dstIndex = indexEnumerator.IndexDst;
					bool isLegalPos = true;
					if ( recalcRes ) {
						if ( indexPosCalc.GetIsEp( dstIndex ) ) {
							Field epDblStepDst, epCapSrc;
							Pos pos = Pos.FromIndexPosEp( indexPosCalc, out epDblStepDst, out epCapSrc );
							isLegalPos = pos.GetIsValid(wtm,epDblStepDst,cpStm,cpSntm) && !indexPosCalc.IsRedundandEpPos();
						}
						else {
							isLegalPos = indexEnumerator.GetIsValid();							
						}
					}

					if ( isLegalPos ) {
						dataOut[dataOutOffset + 2*dstIndex    ] = (byte)0xff;   // will be overwritten later; value must only be different to illResValue
						dataOut[dataOutOffset + 2*dstIndex + 1] = (byte)0xff;   // will be overwritten later; value must only be different to illResValue
						posToVirtualPos[blockPos++]   = blockPosVirtual++;
						posToVirtualPos[blockPos++]   = blockPosVirtual++;
					}
					else {
						dataOut[dataOutOffset + 2*dstIndex    ] = (byte)((illResVal>>8)&0xff);
						dataOut[dataOutOffset + 2*dstIndex + 1] = (byte)(illResVal&0xff);
						blockPosVirtual+=2; 
					}
				}
				else {
					blockPosVirtual+=2; 
				}
				indexEnumerator.IncSrcIndex();
			}
		}


		public void GetDataFromDecompressor( BlockInfo blockInfo, BlockInfoChunk blockInfoChunk, Pieces pieces, byte[] blockData, byte[] dataOut, ref int blockPos, ref int blockPosVirtual, int dataOutOffset, PieceGroupReorder pieceGroupReorder, ResToIntConverter resToIntConverter, bool recalcRes )
		{
			long  indexCount;
			long  firstIndex;
			int   blockOffsetInBytes;
			bool  wtm;
			WkBk  wkBk;

			if ( blockInfo is BlockInfoForChunkPart ) {
				BlockInfoForChunkPart bicp = blockInfo as BlockInfoForChunkPart;
				indexCount               = bicp.DataChunkIndexCount;
				firstIndex                 = bicp.FirstDataChunkIndex;
				blockOffsetInBytes         = 0;
				wtm                        = bicp.Wtm;
				wkBk                       = bicp.WkBk;
			}
			else {
				firstIndex           = 0;
				indexCount         = -1;
				blockOffsetInBytes   = blockInfoChunk.ByteOffset;
				wtm                  = blockInfoChunk.Wtm;
				wkBk                 = blockInfoChunk.WkBk;
			}

			IndexPos                  indexPosComp    = new IndexPos( wkBk, pieces, wtm );
			IndexPos                  indexPosComp64  = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress, pieceGroupReorder );
			IndexEnumerator           indexEnumerator = new IndexEnumerator( indexPosComp64, indexPosComp, false );
			int   illResVal = resToIntConverter.ResToInt( Res.IllegalPos );	
			byte  illResValHigh = (byte)((illResVal>>8)&0xff), illResValLow = (byte)(illResVal&0xff);
			if ( indexCount == -1 )
				indexCount = indexPosComp64.IndexCount;

			indexEnumerator.AddToSrcIndex( firstIndex );

			for ( int i=0 ; i<indexCount ; i++ ) {
				if ( indexEnumerator.IndexPosDstValid ) {
					long dstIndex           = indexEnumerator.IndexDst;
					long dstIndexPlusOffset = dataOutOffset + 2*dstIndex;


					if ( dataOut[dstIndexPlusOffset] != illResValHigh || dataOut[dstIndexPlusOffset+1] != illResValLow ) {
						dataOut[dstIndexPlusOffset]   = blockData[blockPos++];
						dataOut[dstIndexPlusOffset+1] = blockData[blockPos++];
					}
				}
				indexEnumerator.IncSrcIndex();
			}
		}
	}
}
