using System;
using System.IO;
using TBacc;


namespace smTablebases
{
	public class BlockSplitterWrite : BlockSplitter
	{
		private int                      offsetIndex     = 0;


		public BlockSplitterWrite( CalcTB calc, string filename, CompressionType compType, int blockCount, int blockSize, int pieceGroupReorderIntegerWtm, int pieceGroupReorderIntegerBtm, RecalcResults recalcRes ) : base()
		{
			Init( blockCount );
			compressionType = compType;
			fileStream = new FileStream( filename, FileMode.Create, FileAccess.Write, FileShare.None, Settings.WriteBuffer );
			TaBaWrite tbw = calc.TaBasesWrite.TaBaWrite;
			TBHeader header = new TBHeader( new byte[]{ (byte)App.Version[0], (byte)App.Version[1], (byte)App.Version[2], (byte)App.Version[3] }, compressionType, tbw.WtmMaxWiIn, tbw.WtmMaxLsIn, tbw.BtmMaxWiIn, tbw.BtmMaxLsIn, pieceGroupReorderIntegerWtm, pieceGroupReorderIntegerBtm, PieceGroupIndexTables.IndexReorderType, recalcRes, blockSize );
			header.Write( fileStream );
			Tools.WriteIntToStream( fileStream, blockCount );
			fileStream.Seek( byteOffsetToBlockOffsetTable +  8*(blockCount+1), SeekOrigin.Begin );
		}

		public static int WriteBlockSize
		{
			get{
				return Config.BlockSize;
			}
		}


		public int AddedBlocks
		{
			get { return offsetIndex; }
		}


		public void AddBlock( byte[] buff, int first, int count )
		{
			blockOffsetsInBytes[offsetIndex++] = fileStream.Position;
			fileStream.Write( buff, 0, count );
		}


		public long Close( bool abort )
		{
			long length = -1;

			if ( !abort ) {
				length = blockOffsetsInBytes[offsetIndex++] = fileStream.Position;

				if ( blockOffsetsInBytes.Length != offsetIndex )
					throw new Exception( "Block count does not match." );

				fileStream.Seek( byteOffsetToBlockOffsetTable, SeekOrigin.Begin );
				for ( int i=0 ; i<blockOffsetsInBytes.Length ; i++ ) {
					Tools.WriteLongToStream( fileStream, blockOffsetsInBytes[i] );
				}
			}
			fileStream.Close();
			fileStream.Dispose();

			return length;
		}
	}
}
