using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;


namespace TBacc
{
	public sealed class TaBaRead : TaBa
	{
		private  BlockSplitterRead       bsr;

		private  ResToIntConverter       resToIntConverterWtm, resToIntConverterBtm;
		private  PieceGroupReorder       pieceGroupReorderWtm, pieceGroupReorderBtm;
		private  int                     pieceGroupIndicesReorderType;
		private  ChunkBlockSplitter      chunkBlockSplitter;
	
		private TaBaRead( Pieces p, TaBaOpenType ot ) : base( p )
		{
			if ( p.PieceCount == 0 ) {
				resToIntConverterWtm = new ResToIntConverter( 1, 1 );
				resToIntConverterBtm = new ResToIntConverter( 1, 1 );
				pieceGroupReorderWtm                        = PieceGroupReorder.Get( p, true, PieceGroupReorderType.CompressionOptimized );
				pieceGroupReorderBtm                        = PieceGroupReorder.Get( p, true, PieceGroupReorderType.CompressionOptimized );
				pieceGroupIndicesReorderType                = 0;
				chunkBlockSplitter                        = new ChunkBlockSplitter( p, 1024, pieceGroupReorderWtm, pieceGroupReorderBtm, false );
			}
			else {
				filename = GetFilename(p);
				bsr = new BlockSplitterRead( filename, p );
				resToIntConverterWtm = new ResToIntConverter( bsr.WtmMaxWiIn, bsr.WtmMaxLsIn );
				resToIntConverterBtm = new ResToIntConverter( bsr.BtmMaxWiIn, bsr.BtmMaxLsIn );
				pieceGroupReorderWtm                        = PieceGroupReorder.GetFromInt( p, bsr.Header.PieceGroupReorderingTypeWtm );
				pieceGroupReorderBtm                        = PieceGroupReorder.GetFromInt( p, bsr.Header.PieceGroupReorderingTypeBtm );
				pieceGroupIndicesReorderType                = bsr.Header.PieceGroupIndicesReorderingType;
				chunkBlockSplitter                          = new ChunkBlockSplitter( pieces, bsr.Header.BlockSize, pieceGroupReorderWtm, pieceGroupReorderBtm, bsr.Header.RecalcRes != RecalcResults.Disabled );
			}

			CommonCreation();
		}


		public TaBaRead( Pieces p, int blockSize, int wtmMaxWiIn, int wtmMaxLsIn, int btmMaxWiIn, int btmMaxLsIn, CompressionType compType, PieceGroupReorder pieceGroupReorderWtm, PieceGroupReorder pieceGroupReorderBtm ) : base( p )
		{
			resToIntConverterWtm = new ResToIntConverter( wtmMaxWiIn, wtmMaxLsIn );
			resToIntConverterBtm = new ResToIntConverter( btmMaxWiIn, btmMaxLsIn );
			this.pieceGroupReorderWtm = pieceGroupReorderWtm; 
			this.pieceGroupReorderBtm = pieceGroupReorderBtm; 

			CommonCreation();

		}


		private void CommonCreation()
		{
			btmOffset                      = WkBk.GetCount( pieces ).Index;
			List<DataChunk> list           = new List<DataChunk>();
			long firstIndex                = 0L;
			int chunkIndex = 0;

			foreach ( bool wtm in Tools.BoolArray ) {
				ResToIntConverter resToIntConverter = wtm ? resToIntConverterWtm : resToIntConverterBtm;
				Res[] resToInt =  ResToIntConverter.GetIntToResTable( resToIntConverter );
				for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
					long           indexCountCurrent   = new IndexPos(wkBk,pieces,wtm).IndexCount;
					DataChunkRead info = new DataChunkRead( this, pieces, wtm, wkBk, firstIndex, indexCountCurrent, resToInt, resToIntConverter.MaxBitsForInteger, chunkIndex++ );
										
					if ( wtm )
						maxIndexCountPerChunkWtm = Math.Max( maxIndexCountPerChunkWtm, indexCountCurrent );
					else
						maxIndexCountPerChunkBtm = Math.Max( maxIndexCountPerChunkBtm, indexCountCurrent );
					firstIndex                          += indexCountCurrent;
					list.Add( info );
				}
			}
			dataChunk = list.ToArray();
		}


		public bool MultipleChunksInOneBlock
		{
			get { return chunkBlockSplitter.MultipleChunksInOneBlock; }
		}


		public int MaxChunksPerBlock
		{
			get { return chunkBlockSplitter.MaxChunksPerBlock; }
		}


		public ChunkBlockSplitter ChunkBlockSplitter
		{ 
			get {  return chunkBlockSplitter;} 
		}




		public DataChunk[] GetAllDataChunks()
		{
			return dataChunk;
		} 


		public PieceGroupReorder GetPieceGroupReordering( bool wtm )
		{
			return wtm ? pieceGroupReorderWtm : pieceGroupReorderBtm;
		}


		public ResToIntConverter GetResToIntConverter( bool wtm )
		{
			return wtm ? resToIntConverterWtm : resToIntConverterBtm; 
		}


		public void LoadDataChunkOutOfMultipleBlocks( DataChunkRead data, Decompressor decompressor )
		{
			bool wtm  = data.Wtm; 
			WkBk wkBk = data.WkBk;
			int firstBlockIndex = chunkBlockSplitter.GetFirstBlock( wkBk, wtm );
			int blockCount      = chunkBlockSplitter.GetBlockCount( wkBk, wtm );


			int blockPos, dataOutPosVirtual;			
			for ( int i=0 ; i<blockCount ; i++ ) {
				BlockInfoForChunkPart bicp = chunkBlockSplitter.GetBlockInfo( firstBlockIndex + i ) as BlockInfoForChunkPart;
				int blockSizeCompressed = bsr.ReadBlock( decompressor.BufferIn, firstBlockIndex + i );
				blockPos = dataOutPosVirtual = 0;
				chunkBlockSplitter.GetInfosForDecompressor( bicp, null, pieces, data.Data, ref blockPos, ref dataOutPosVirtual, GetPieceGroupReordering(wtm), GetResToIntConverter(wtm), bsr.Header.RecalcRes != RecalcResults.Disabled, decompressor.PosToVirtualPos );
				decompressor.CompressionType = bsr.CompressionType;
				decompressor.RecalcRes = bsr.Header.RecalcRes;
				int byteCountUncompressed = decompressor.Decompress( blockSizeCompressed );
				blockPos = dataOutPosVirtual = 0;
				chunkBlockSplitter.GetDataFromDecompressor( bicp, null, pieces, decompressor.BufferOut, data.Data, ref blockPos, ref dataOutPosVirtual, 0, GetPieceGroupReordering(wtm), GetResToIntConverter(wtm), bsr.Header.RecalcRes != RecalcResults.Disabled );
			}
		}


		public void LoadBlockContainingMultipleChunks( TmpBlockStorage tmpBlockStorage, int blockIndex, Decompressor decompressor )
		{
			int blockSize  = bsr.ReadBlock( decompressor.BufferIn, blockIndex );

			BlockInfoForMultipleChunks bimc = chunkBlockSplitter.GetBlockInfo( blockIndex ) as BlockInfoForMultipleChunks;

			int blockPos = 0, blockPosVirtual = 0;			

			for ( int i=0 ; i<bimc.Chunks.Length ; i++ ) {
				BlockInfoChunk bic = bimc.Chunks[i];
				bool wtm = bic.Wtm;
				chunkBlockSplitter.GetInfosForDecompressor( bimc, bic, pieces, tmpBlockStorage.Data, ref blockPos, ref blockPosVirtual, GetPieceGroupReordering(wtm), GetResToIntConverter(wtm), bsr.Header.RecalcRes != RecalcResults.Disabled, decompressor.PosToVirtualPos );
			}
			decompressor.CompressionType = bsr.CompressionType;
            decompressor.RecalcRes = bsr.Header.RecalcRes;
            int byteCountUncompressed = decompressor.Decompress( blockSize );

			blockPos = 0;
			blockPosVirtual = 0;	
			for ( int i=0 ; i<bimc.Chunks.Length ; i++ ) {
				BlockInfoChunk bic = bimc.Chunks[i];
				bool wtm = bic.Wtm;
				chunkBlockSplitter.GetDataFromDecompressor( bimc, bic, pieces, decompressor.BufferOut, tmpBlockStorage.Data, ref blockPos, ref blockPosVirtual, bic.ByteOffset, GetPieceGroupReordering(wtm), GetResToIntConverter(wtm), bsr.Header.RecalcRes != RecalcResults.Disabled );
			}
		}


		public void LoadDataChunk( byte[] data, WkBk wkBk, bool wtm, int byteCount, TmpBlockStorage tmpBlockStorage )
		{
			BlockInfoForMultipleChunks bimc = chunkBlockSplitter.GetBlockInfo( chunkBlockSplitter.GetFirstBlock(wkBk,wtm) ) as BlockInfoForMultipleChunks;
			BlockInfoChunk             bic  = bimc.GetChunkInfo(wkBk,wtm);
			int offset = bic.ByteOffset;
			byte[] tmpData = tmpBlockStorage.Data;
			for ( int j=0 ; j<byteCount ; j++ )
				data[j]   = tmpData[offset+j];
		}



		public override void Close(bool abort)
		{
			if ( pieces.PieceCount != 0 )
				bsr.Close();
		}



		public static TaBaRead OpenForRead( Pieces p, TaBaOpenType ot )
		{
			if ( p.PieceCount==0 || File.Exists( GetFilename(p) ) )
				return new TaBaRead( p, ot );
			else
				return null;
		}


		public static string GetFilename( Pieces pieces )
		{
			return TBaccess.GetFilename( pieces.ToString() );
		}




		public override string ToString()
		{
			if ( pieces.PieceCount == 0 )
				return pieces.ToString();
			return pieces.ToString() + "   " + bsr.Version + "   " + bsr.CompressionType.ToString();
		}




	}
}
