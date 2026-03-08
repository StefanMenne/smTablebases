using TBacc;


namespace smTablebases
{
	public class ChunkBlockSplitterWrite : ChunkBlockSplitter
	{

		public ChunkBlockSplitterWrite( Pieces pieces, int blockSize, PieceGroupReorder pieceGroupReorderWtm, PieceGroupReorder pieceGroupReorderBtm, bool removeRecalculateablePositions ) : base( pieces, blockSize, pieceGroupReorderWtm, pieceGroupReorderBtm, removeRecalculateablePositions)
		{

		}


		public void FillBlockForCompressor( BlockInfo blockInfo, BlockInfoChunk blockInfoChunk, DataChunkWrite dataIn, byte[] blockData, int[] posToVirtualPos, PieceGroupReorder pieceGroupReorder, ResToIntConverter resToIntConverter, bool removeRecalculateablePositions, ref int blockPos, ref int blockPosVirtual )
		{
			IndexPos               indexPos        = new IndexPos( dataIn.WkBk, dataIn.Pieces, dataIn.Wtm );
			IndexPos               indexPos64      = new IndexPos( dataIn.WkBk, dataIn.Pieces, dataIn.Wtm, IndexPosType.Compress, pieceGroupReorder );
			IndexEnumerator        indexEnumerator = new IndexEnumerator( indexPos64, indexPos, false );

			long indexCountIn;
			long firstIndexIn;
			if ( blockInfo is BlockInfoForChunkPart ) {
				BlockInfoForChunkPart bicp = blockInfo as BlockInfoForChunkPart;
				firstIndexIn          = bicp.FirstDataChunkIndex;
				indexCountIn        = bicp.DataChunkIndexCount;
			}
			else {
				firstIndexIn          = 0;
				indexCountIn        = indexPos64.IndexCount;
				//if ( blockPos != blockInfoChunk.ByteOffset )
				//	throw new Exception();
			}
			indexEnumerator.AddToSrcIndex( firstIndexIn );


			for ( int j=0 ; j<indexCountIn ; j++ ) {
				int  val = -1;
				if ( indexEnumerator.IndexPosDstValid ) {
					long index = indexEnumerator.IndexDst;
#if DEBUG
					Res res = new ResWithCount( dataIn.GetDebug( index, "TaBaRead Compression", null ) ).Res;
#else
					Res res = new ResWithCount( dataIn.Get( index ) ).Res;
#endif
					if ( (!removeRecalculateablePositions) || (!res.IsIllegalPos) )
						val = resToIntConverter.ResToInt(res);

				}


				if ( posToVirtualPos != null ) {
					if ( val == -1 ) {
						blockPosVirtual++;
						blockPosVirtual++;
					}
					else {
						blockData[blockPos]                = (byte)(val>>8);
						posToVirtualPos[blockPos++]        = blockPosVirtual++;
						blockData[blockPos]                = (byte)(val&0xff);
						posToVirtualPos[blockPos++]        = blockPosVirtual++;

					}
				}
				else {
					if ( val != -1 ) {
						blockData[blockPos++]                = (byte)(val>>8);
						blockData[blockPos++]                = (byte)(val&0xff);
					}
				}

				indexEnumerator.IncSrcIndex();
			}

			//if ( blockInfo is BlockInfoForChunkPart ) {
			//	if ( blockInfo.CountBytesWithoutRecalculateable != blockPos )
			//		throw new Exception();
			//}

		}

	}
}
