using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace smTablebases
{
	public class TbInfoFile
	{
		public     int      LineIndex;
		private    string   md5;
		private    string   pieceGroupReorderStringWtm, pieceGroupReorderStringBtm;
		private    int      bitsPerResWtm, bitsPerResBtm;
		private    int      maxDtmHm = -1;


		public TbInfoFile( string s, int lineIndex )
		{
			LineIndex                   = lineIndex;
			bitsPerResWtm               = ParseInt( s, 0, 2, 1,   18 );
			bitsPerResBtm               = ParseInt( s, 4, 2, 1,   18 );
			maxDtmHm                    = ParseInt( s, 8, 4, 0, 2000 );
			pieceGroupReorderStringWtm  = GetString( s, 14, 2, 10 );
			pieceGroupReorderStringBtm  = GetString( s, 25, 2, 10 );
			md5                         = GetString( s, 36, 32, 32 );
		}

		private int ParseInt( string s, int firstDigit, int countDigit, int min, int max )
		{
			if ( s.Length < firstDigit+countDigit )
				return -1;
			s = s.Substring( firstDigit, countDigit ).Trim();

			int v;
			if ( s=="-" || !int.TryParse( s, out v ) || v<min || v>max )
				return -1;
			else
				return v;
		}


		private string GetString( string s, int firtCharIndex, int minChars, int maxChars )
		{
			if ( s.Length >= firtCharIndex+minChars ) {
				if ( s.Length >= firtCharIndex+maxChars )
					s = s.Substring( firtCharIndex, maxChars );
				else
					s = s.Substring( firtCharIndex );
			}
			else
				s = "-";

			s = s.Trim();
			if ( s=="-" || s.Length==0 )
				s = null;
			return s;
		}

		public string MD5
		{
			get{ return md5; }
			set{ md5 = value; }
		}


		public int GetBitsPerResWtm()
		{
			if ( bitsPerResWtm == -1 )
				return BitsPerResHeuristic.Get( true, LineIndex );
			return bitsPerResWtm;
		}


		public int MaxDtmHm
		{
			get{ return maxDtmHm; }
			set{ maxDtmHm = value; }
		}

		public void SetBitsPerResWtm( int value )
		{
			bitsPerResWtm = value;
		}


		public string PieceGroupReoredStringWtm
		{
			get{ return pieceGroupReorderStringWtm; }
		}


		public string PieceGroupReoredStringBtm
		{
			get{ return pieceGroupReorderStringBtm; }
		}


		public int GetBitsPerResBtm()
		{
			if ( bitsPerResBtm == -1 )
			    return BitsPerResHeuristic.Get( false, LineIndex );
			return bitsPerResBtm;
		}


		public void SetBitsPerResBtm( int value )
		{
			bitsPerResBtm = value;
		}


		public int GetBitsPerRes( bool wtm )
		{
			return wtm ? GetBitsPerResWtm() : GetBitsPerResBtm();
		}


		public override string ToString()
		{
			string s = "";
			s +=        IntToString( bitsPerResWtm, 2 );
			s += "  " + IntToString( bitsPerResBtm, 2 );
			s += "  " + IntToString( maxDtmHm     , 4 );
			s += "  " + AddString(pieceGroupReorderStringWtm,11) + AddString(pieceGroupReorderStringBtm,11) + AddString(md5,32);
			return s;
		}


		private string AddString( string s, int chars )
		{
			if ( s==null )
				return "-".PadRight(chars);
			else
				return s.PadRight(chars);
		}


		private string IntToString( int v, int digitCount )
		{
			if ( v == -1 )
				return "-".PadLeft( digitCount );
			else
				return v.ToString().PadLeft( digitCount );
		}

	}
}
