using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace TBacc
{
	public class Tools
	{
		public static bool[] BoolArray = new bool[]{true,false};

        private static long[] chooseKOutOfN = new long[128*10];


        static Tools()
        {
            for ( int k=0 ; k<10 ; k++ )
                for ( int n=0 ; n<128 ; n++ )
                    chooseKOutOfN[(k<<7)|n] = CalcKOutOfN( k, n );
        }

        public static long ChooseKOutOfN( int k, int n )
        {
            return chooseKOutOfN[(k<<7)|n];
        }

		public static long Pow( long basis, int exponent )
		{
			if ( exponent==0 )
				return 1L;
			else
				return basis*Pow( basis, exponent-1 );
		}

        private static long CalcKOutOfN( int k, int n )
        {
			if ( k>n )
				return 0L;
			if ( 2*k > n )
				return ChooseKOutOfN( n-k, n );
			else {
				long res=1;
				for ( int i=1 ; i<=k ; i++ )
					res = ( res * (n-k+i) ) / i;
				return res;
			}
		}

		public static string MillisecondsToString( long ms )
		{
			string s = "." + (ms%1000).ToString( "000" );
			ms = ms / 1000;
			s = ":" + (ms%60).ToString("00") + s;
			ms = ms / 60;
			s = ":" + (ms%60).ToString("00") + s;
			ms = ms / 60;
			s = ms.ToString() + s;
			return s;
		}

		public static string LongToKiloMegaGiga( long val, bool showKmg=true, bool forceMaxMega=false )
		{
			string s;
			double d;

			if ( val < 1024 )
				return val.ToString( "#,##0" );
			else if ( val < 1024*1024 ) {
				s = "K";
				d = ((double)val)/1024d;
			}
			else if ( val < 1024*1024*1024 || forceMaxMega ) {
				s = "M";
				d = ((double)val)/(1024d*1024d);
			}
			else  {
				s = "G";
				d = ((double)val)/(1024d*1024d*1024d);
			}

			if ( !showKmg )
				s = "";

			if ( d < 10.0d )
				return d.ToString( "#,##0.00" ) + s;
			else if ( d < 100.0d )
				return d.ToString( "#,##0.0" ) + s;
			else
				return d.ToString( "#,##0" ) + s;
		}


		/// <summary>
		/// Calcs log2
		/// </summary>
		/// <param name="v">v=2^x; bit count of v must be 1!</param>
		/// <returns>log2(v)</returns>
		public static int Log2( ulong v )
		{
			return BitOperations.TrailingZeroCount(v);
		}

        public static int Log2( long v )
		{
            return BitOperations.TrailingZeroCount(v);
        }
        
        public static int Log2ForAnyNumber( int v )
		{
			return Log2( RoundUpToPower2( v ) )-1;
		}

		private static int RoundUpToPower2( int v )
		{
			v |= v>>1;
			v |= v>>2;
			v |= v>>4;
			v |= v>>8;
			v |= v>>16;
			return v+1;
		}


		public static void WriteLongToStream( FileStream fs, long v )
		{
			for ( int i=0 ; i<8 ; i++ )
				fs.WriteByte( (byte)(v>>(8*i)) );		
		}

		public static void WriteIntToStream( FileStream fs, int v )
		{
			for ( int i=0 ; i<4 ; i++ )
				fs.WriteByte( (byte)(v>>(8*i)) );		
		}


		public static long ReadLongFromStream( FileStream fs )
		{
			long v = 0;
			for ( int i=0 ; i<8 ; i++ ) {
				int j = fs.ReadByte();
				v |= ((long)(byte)j) << (8*i);
			}
			return v;			
		}

		public static int ReadIntFromStream( FileStream fs )
		{
			int v = 0;
			for ( int i=0 ; i<4 ; i++ ) {
				int j = fs.ReadByte();
				v |= ((int)(byte)j) << (8*i);
			}
			return v;			
		}


		public static long GCD( long a, long b )
		{
			return b==0 ? a : GCD( b, a%b );
		}

	}
}
