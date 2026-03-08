﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace LC
{
#if !RELEASEFINAL
	public class Test
	{
		public static int      DataLength;
		public static string   TestFile;
		



		public static int CompressTest( int bytesPerItem, int literalPosBits, int prevByteHighBits, int level, LengthSet lengthSet, bool useVirtualPosCompression )
		{
			if ( !File.Exists(TestFile) ) {
				Log.Line( "File not found: \"" + TestFile + "\"" );
				return -1;
			}
			byte[]   data = File.ReadAllBytes( TestFile );

			if (DataLength == -1 && data.Length >= (1 << 24)) { 
				Log.Line( "File: \"" + TestFile + "\" too big."  );
				return -1;
			}


			if ( DataLength != -1 )
				Array.Resize<byte>( ref data, DataLength );
			Log.Line( "Read \"" + TestFile + "\"  (" + data.Length.ToString("#,###,###,###,##0") + ")" );
			
			int[] posToVirtualPos = null;

			if ( useVirtualPosCompression ) { 
				string filename = Path.ChangeExtension( TestFile, "p2v" );
				if ( File.Exists(filename) ) {
					Log.Line( "Read \"" + filename + "\"" );
					byte[] tmp = File.ReadAllBytes( filename );
					posToVirtualPos = new int[tmp.Length/4];
					Buffer.BlockCopy( tmp, 0, posToVirtualPos, 0, tmp.Length );					
				}
				else {
					Log.Line( "Create random Pos-VirtualPos tables." );
					posToVirtualPos = new int[data.Length];
					Random rnd = new Random( 0 );
					int virtualPos = 0;
					for ( int i=0 ; i<data.Length ; i++ ) {
						do {
							posToVirtualPos[i]            = virtualPos++;
						} while ( rnd.Next(16)<4 );				
					}
				}
			}

			byte[]   bufferOut = new byte[2*data.Length+1000000]; // store also a lot of debug data
			Encoder encoder = new Encoder();
			encoder.Settings.BytesPerItem = bytesPerItem;
			encoder.Settings.LiteralPosBits = literalPosBits;
			encoder.Settings.PrevByteHighBits = prevByteHighBits;
			encoder.Settings.Level = level;
			encoder.Settings.LengthSet = lengthSet;

			int compressedByteCount = encoder.Encode( data, data.Length, bufferOut, posToVirtualPos );
			if ( compressedByteCount == -1 )
				return -1;
			Log.Line( "Finished compression. Bytes: " + compressedByteCount.ToString( "###,###,###,##0" )  );

			byte[] dataVerify = new byte[data.Length];

			Stopwatch sw = new Stopwatch();
			sw.Start();
			Decoder decoder = new Decoder();
#if DEBUG
			decoder.DataVerify = data;
#endif
			decoder.Decode( bufferOut, dataVerify, posToVirtualPos );
			sw.Stop();
			Log.Line( "Decoding-Time: " + sw.ElapsedMilliseconds.ToString( "###,###,##0" ) + " ms" );


			Log.Line( "Verify" );

			for ( int i=0 ; i<data.Length ; i++ ) {
				if ( !( data[i]==dataVerify[i] ) )
					throw new Exception();
			}
			Log.Line( "Finished" );
			return compressedByteCount;
		}


		public static void CodeLog( DebugInfo info )
		{
#if DEBUG
			Encoder.DebugInfoToCode = info;
#endif
		}


#if DEBUG
		private static void RangeCoderTest()
		{
			int count = 8*1024;

			byte[] data1 = new byte[100000000];
			byte[] data2 = new byte[100000000];
			RangeEncoder re1 = new RangeEncoder( data1 );
			RangeEncoder re2 = new RangeEncoder( data2 );

			Random rnd = new Random(0);
			int[] occ = new int[10];
			double[] pro = new double[10];

			for ( int i=0 ; i<count ; i++ ) {
				for ( int j=0 ; j<occ.Length ; j++ )
					occ[j] = rnd.Next( 100 ) + 1;
				int val = rnd.Next( 10 );
				int sum = 0;
				for ( int j=0 ; j<occ.Length ; j++ )
					sum += occ[j];
				for ( int j=0 ; j<pro.Length ; j++ )
					pro[j] = ((double)occ[j]) / ((double)sum); 
				re1.AddInt( val, occ, sum );
				re2.AddInt( val, pro, pro.Length );
			}
			re1.Close();
			re2.Close();

			RangeDecoder rd1 = new RangeDecoder( data1 );
			RangeDecoder rd2 = new RangeDecoder( data2 );
			rnd = new Random(0);
			for ( int i=0 ; i<count ; i++ ) {
				for ( int j=0 ; j<occ.Length ; j++ )
					occ[j] = rnd.Next( 100 ) + 1;
				int val = rnd.Next( 10 );
				int sum = 0;
				for ( int j=0 ; j<occ.Length ; j++ )
					sum += occ[j];
				for ( int j=0 ; j<pro.Length ; j++ )
					pro[j] = ((double)occ[j]) / ((double)sum); 
				int valVer1 = rd1.GetInt( occ, sum );
				int valVer2 = rd2.GetInt( pro, pro.Length );
				if ( val != valVer1 || val != valVer2 )
					throw new Exception();
			}

			Log.Line( "INT:       " + re1.PosInBits.ToString("#,###.000") );
			Log.Line( "DOUBLE:    " + re2.PosInBits.ToString("#,###.000") );
		}
#endif

		
	}

#endif
}
