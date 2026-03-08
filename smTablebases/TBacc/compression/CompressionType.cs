using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public enum CompressionType
	{
		LC                             =  0,
		Deflate                        =  1,
		Brotli                         =  2,
		NoCompression                  =  3,
		IllegalCompressionType         =  4
	}


	public static class CompressionTypeStrings
	{

		private static string[] strings = new string[] {
			"LC",                 "best compression ratio",
			"Deflate",            "very fast",
			"Brotli",             "good compromise",
            "No",                 "no compression"
		};


		public static string Get( CompressionType ct )
		{
			return strings[2*((int)ct)];
		}

		public static string GetDescription( CompressionType ct )
		{
			return strings[(2*((int)ct))+1];
		}


		public static CompressionType Parse( string s )
		{
			for ( int i=0 ; i<strings.Length ; i+=2 ) {
				if ( strings[i] == s )
					return (CompressionType)(i/2);
			}
			throw new Exception();
		}

		public static CompressionType FromInt( int i )
		{
			return (CompressionType)i;
		}

		public static int Count
		{
			get{ return strings.Length/2; }
		}


	}
}
