using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TBacc
{
	public struct TBHeader
	{
		public byte[]           Version;
		public CompressionType  CompressionType;
		public int              WtmMaxWiIn, WtmMaxLsIn, BtmMaxWiIn, BtmMaxLsIn;
		public int              BitsPerEntryWtm, BitsPerEntryBtm;
		public int              BlockSize;
		public int              PieceGroupReorderingTypeWtm, PieceGroupReorderingTypeBtm, PieceGroupIndicesReorderingType;  
		public RecalcResults    RecalcRes;

		public const int        ForFutureUse = 50;

		public const int        HeaderSize        = 4 + 1 + 4 + 4 + 4 + 4 + 4 + 3*4 + 1 + 1 + ForFutureUse;

		public TBHeader( byte[] version, CompressionType compressionType, int wtmMaxWiIn, int wtmMaxLsIn, int btmMaxWiIn, int btmMaxLsIn, int pieceGroupReorderingTypeWtm, int pieceGroupReorderingTypeBtm, int pieceGroupIndicesReorderingType, RecalcResults recalcRes, int blockSize )
		{
			this.Version                             = version;
			this.CompressionType                     = compressionType;
			this.WtmMaxWiIn                          = wtmMaxWiIn;
			this.WtmMaxLsIn                          = wtmMaxLsIn;
			this.BtmMaxWiIn                          = btmMaxWiIn;
			this.BtmMaxLsIn                          = btmMaxLsIn;
			this.BitsPerEntryWtm                     = ResToIntConverter.GetBitCount( WtmMaxWiIn, WtmMaxLsIn );
			this.BitsPerEntryBtm                     = ResToIntConverter.GetBitCount( BtmMaxWiIn, BtmMaxLsIn );
			this.BlockSize                           = blockSize;
			this.PieceGroupReorderingTypeWtm           = pieceGroupReorderingTypeWtm;
			this.PieceGroupReorderingTypeBtm           = pieceGroupReorderingTypeBtm;
			this.PieceGroupIndicesReorderingType       = pieceGroupIndicesReorderingType;
			this.RecalcRes                           = recalcRes;
		}


		public TBHeader( FileStream fileStream )
		{
			Version = new byte[4];
			Version[0] = (byte)fileStream.ReadByte();
			Version[1] = (byte)fileStream.ReadByte();
			Version[2] = (byte)fileStream.ReadByte();
			Version[3] = (byte)fileStream.ReadByte();

			CompressionType = (CompressionType)fileStream.ReadByte();

			WtmMaxWiIn                          = ReadInt( fileStream );
			WtmMaxLsIn                          = ReadInt( fileStream );
			BtmMaxWiIn                          = ReadInt( fileStream );
			BtmMaxLsIn                          = ReadInt( fileStream );
			BlockSize                           = ReadInt( fileStream );
			PieceGroupReorderingTypeWtm           = ReadInt( fileStream );
			PieceGroupReorderingTypeBtm           = ReadInt( fileStream );
			PieceGroupIndicesReorderingType       = ReadInt( fileStream );
			RecalcRes                           = (RecalcResults)fileStream.ReadByte();
            fileStream.ReadByte();

			fileStream.Seek( ForFutureUse, SeekOrigin.Current );

			this.BitsPerEntryWtm  = ResToIntConverter.GetBitCount( WtmMaxWiIn, WtmMaxLsIn );
			this.BitsPerEntryBtm  = ResToIntConverter.GetBitCount( BtmMaxWiIn, BtmMaxLsIn );			
		}


		public void Write( FileStream fileStream )
		{
			fileStream.WriteByte( Version[0] );
			fileStream.WriteByte( Version[1] );
			fileStream.WriteByte( Version[2] );
			fileStream.WriteByte( Version[3] );
			fileStream.WriteByte( (byte)CompressionType );
			WriteInt( fileStream, WtmMaxWiIn );
			WriteInt( fileStream, WtmMaxLsIn );
			WriteInt( fileStream, BtmMaxWiIn );
			WriteInt( fileStream, BtmMaxLsIn );
			WriteInt( fileStream, BlockSize );
			WriteInt( fileStream, PieceGroupReorderingTypeWtm );
			WriteInt( fileStream, PieceGroupReorderingTypeBtm );
			WriteInt( fileStream, PieceGroupIndicesReorderingType );
			fileStream.WriteByte( (byte)RecalcRes );
			fileStream.WriteByte( (byte)1 );
			fileStream.Seek( ForFutureUse, SeekOrigin.Current );
		}

		private static int ReadInt( FileStream fs )
		{
			return fs.ReadByte() | (fs.ReadByte() << 8) | (fs.ReadByte() << 16) | (fs.ReadByte() << 24);
		}

		private void WriteInt( FileStream fs, int val )
		{
			fs.WriteByte((byte)(val & 0xff));
			fs.WriteByte((byte)((val >> 8) & 0xff));
			fs.WriteByte((byte)((val >> 16) & 0xff));
			fs.WriteByte((byte)((val >> 24) & 0xff));
		}

		public string VersionString
		{
			get { return Version[0].ToString() + "." + Version[1].ToString() + "." + Version[2].ToString() + "." + Version[3].ToString(); }
		}
	}
}
