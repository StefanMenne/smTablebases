using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public struct TaBaWriteHeader
	{
		private const int logSizeInBytes = 1000;
		public  const int HeaderSizeInBytes = 2 + 8 + 8 + 4 + 4 + 4 + logSizeInBytes;

		public long   FinalResCount                  ;  // Ill; stale mate; Win; lose with count 0
		public long   FinalResToProcessCount         ;  // Ill; stale mate; Processed Win; Processed lose with count 0
		public int    ResCountConvertMaxBitsWtm      ;
		public int    ResCountConvertMaxBitsBtm      ;
		public int    MaxDtmHm;
		public int    WtmMaxWinIn, WtmMaxLsIn, BtmMaxWinIn, BtmMaxLsIn;
		public string Log;

		public TaBaWriteHeader( bool dummy )
		{
			FinalResCount = FinalResToProcessCount = 0L;
			ResCountConvertMaxBitsWtm  = ResCountConvertMaxBitsBtm = MaxDtmHm    = WtmMaxWinIn = 0;
			WtmMaxLsIn                 = BtmMaxWinIn               = BtmMaxLsIn  = 0;
			Log = "";
		}


		public TaBaWriteHeader( Storage storage )
		{
			FinalResCount              = storage.ReadLong();
			FinalResToProcessCount     = storage.ReadLong();
			ResCountConvertMaxBitsWtm  = storage.ReadInt();
			ResCountConvertMaxBitsBtm  = storage.ReadInt();
			MaxDtmHm                   = storage.ReadInt();
			WtmMaxWinIn                = storage.ReadInt();
			WtmMaxLsIn                 = storage.ReadInt();
			BtmMaxWinIn                = storage.ReadInt();
			BtmMaxLsIn                 = storage.ReadInt();
			int byteCountLogString = storage.ReadInt();
			byte[] logStringBuffer     = new byte[byteCountLogString];
			storage.Load( -1, logStringBuffer, byteCountLogString );
			Log = System.Text.Encoding.UTF8.GetString( logStringBuffer );
		}


		public void Write( Storage storage )
		{
			storage.WriteLong( FinalResCount );
			storage.WriteLong( FinalResToProcessCount );
			storage.WriteInt( ResCountConvertMaxBitsWtm );
			storage.WriteInt( ResCountConvertMaxBitsBtm );
			storage.WriteInt( MaxDtmHm );
			storage.WriteInt( WtmMaxWinIn );
			storage.WriteInt( WtmMaxLsIn );
			storage.WriteInt( BtmMaxWinIn );
			storage.WriteInt( BtmMaxLsIn );
			byte[] logStringBuffer = System.Text.Encoding.UTF8.GetBytes(Log);
			storage.WriteInt( logStringBuffer.Length );
			storage.Save( -1, logStringBuffer, logStringBuffer.Length );
		}



	}
}
