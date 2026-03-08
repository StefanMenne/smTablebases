using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using TBacc;

namespace smTablebases
{
	public enum VerifyResult
	{
		OK,
		SKIPPED,
		NOK
	}


	public static class MD5Verify
	{
		public static VerifyResult Verify( Pieces p, int tbIndex )
		{
			string   hash;
			VerifyResult result;
			Message.Text( p.ToString() + " Verify " );

			Message.Text( "   MD5 " );
			result = Verify( p, out hash );
			Message.Text( " " + result.ToString() );

			if ( result == VerifyResult.OK )
				Settings.TbInfo.Get(tbIndex).State = TBState.VerifiedOK;
			else if ( result == VerifyResult.SKIPPED )
				Settings.TbInfo.Get(tbIndex).State = TBState.FinishedUnverified;
			else if ( result == VerifyResult.NOK )
				Settings.TbInfo.Get(tbIndex).State = TBState.NOK;
			Settings.TbInfo.ItemChanged(tbIndex);
			return result;
		}


		public static VerifyResult Verify( Pieces pieces, out string hash )
		{
			string hashVer = TbInfoFileList.Current.MD5;
			hash = "";
			if ( hashVer==null || hashVer.Length == 0 )
				return VerifyResult.SKIPPED;
			hash = CalcHash( pieces );
			if ( hash == hashVer )
				return VerifyResult.OK;
			else
				return VerifyResult.NOK;
		}


		public static bool AddHash( Pieces pieces )
		{
			string hash = CalcHash( pieces );
			if ( hash==null )
				return false;
			else {
				TbInfoFileList.Current.MD5 = hash;
				TbInfoFileList.Save();
				return true;
			}
		}


		public static bool IsHashAvailable( Pieces piece )
		{
			return TbInfoFileList.Current.MD5.Length!=0;
		}


		private static string CalcHash( Pieces pieces )
		{
			Threading.Do( new TasksMd5( pieces ) );
			return HashToString( TasksMd5.Hash );
		}



		private static string HashToString( byte[] hash )
		{
			if ( hash == null )
				return null;
			StringBuilder strBuilder = new StringBuilder();
			for ( int i=0 ; i<hash.Length ; i++ )
				strBuilder.Append(hash[i].ToString("x2"));
			return strBuilder.ToString();
		}

	}
}
