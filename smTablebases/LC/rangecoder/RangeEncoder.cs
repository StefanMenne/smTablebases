﻿//#define DEBUG_ENCODER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class RangeEncoder : RangeCoder
	{
		public  double         SumPaddingBits     = 0.0D;
		private byte           cache              = 0xff;  // will be overwritten without being accessed except in the case that the first step is a cacheSize increment (to handle this case write ff)
		private int            cacheSize          = 0;     // start with empty cache; cache will later always have at least one byte

		private UInt64         low                = 0x0000000000000000UL;
#if DEBUG_ENCODER
		private BigInteger     lowFull;
		private int            lowFullByteCount   = 6;
#endif


		public RangeEncoder( byte[] bufferOut ) : base( bufferOut )
		{
		}


#if DEBUG
		public static int Encode( byte[] dataIn, int dataLength, byte[] dataOut )
		{
			RangeEncoder encoder = new RangeEncoder( dataOut );
			double[][] prob = new double[8][];
			int p2 = 1;
			for ( int i=0 ; i<prob.Length ; i++ ) {
				prob[i] = new double[p2];
				p2<<=1;
				for ( int k=0 ; k<prob[i].Length ; k++ )
					prob[i][k] = 0.5d;
			}

			for ( int i=0 ; i<dataLength ; i++ ) {
				byte b = dataIn[i];
				int k = 0;
				for ( int j=0 ; j<8 ; j++ ) {
					bool bit = ((b>>(7-j))&1)==1;
					encoder.AddBit( bit, prob[j][k] );
					if ( bit ) {
						prob[j][k] = Probability.Increase( prob[j][k], 0.01d );
						k += (1<<j);
					}
					else
						prob[j][k] = Probability.Decrease( prob[j][k], 0.01d );
				}
			}
			return encoder.Close();
		}
#endif


#if DEBUG
		public double PosInBits
		{
			get { return 8.0 * (bufferPos + cacheSize) + 48.0 - Math.Log(range,2); }
		}
#endif


#if DEBUG
		public double AddBits( uint value, int countBits )
#else
		public void AddBits( uint value, int countBits )
#endif
		{
#if DEBUG
			double size = 0.0;
#endif
			uint p2 = 1U<<countBits;
			while( countBits-- != 0 ) {
				p2 >>= 1;
#if DEBUG
				size += AddBit( (value&p2)!=0 );
#else
				AddBit( (value&p2)!=0 );
#endif
			}
#if DEBUG
			return size;
#endif
		}


#if DEBUG
		public double AddBit( bool bit, double probabilityForTrue=0.5d )
#else
		public void AddBit( bool bit, double probabilityForTrue=0.5d )
#endif
		{
#if DEBUG
			double pos = PosInBits;
			VerifyBefore();
#endif
			UInt64 bound = (UInt64) (range * probabilityForTrue);

			if ( bit ) {
				range  = bound;
			}
			else {
				low   += bound;
#if DEBUG_ENCODER
				lowFull += bound;
#endif
				range -= bound;
			}
#if DEBUG
			VerifyAfter();
#endif 

			while ( range < rangeShiftTreshold ) {
#if DEBUG_ENCODER
				lowFull <<= 8;
				lowFullByteCount++;
#endif
				range <<= 8;
				WriteLow();
			}
#if DEBUG
			return PosInBits - pos;
#endif
		}


#if DEBUG
		public double AddInt( int indexToAdd, int[] occurence, int occurenceSum )
#else
		public void AddInt( int indexToAdd, int[] occurence, int occurenceSum )
#endif
		{
#if DEBUG
			double pos = PosInBits;
			VerifyBefore();
			int sumVer = 0;
			for ( int i=0 ; i<=indexToAdd ; i++ ) {
				if ( occurence[i] < 0 )
					throw new Exception();
				sumVer += occurence[i];
			}
			if ( occurence[indexToAdd]==0 || sumVer>occurenceSum )
				throw new Exception();				
#endif
			int sum = 0;
			for ( int i=0 ; i<indexToAdd ; i++ )
				sum += occurence[i];

			ulong rangePerOccurence  = range/((uint)occurenceSum);
			//ulong rangeRoundingError = range - rangePerOccurence*((uint)occurenceSum);
			low   += rangePerOccurence * ((uint)sum);
			range  = rangePerOccurence * ((uint)occurence[indexToAdd]);


			// If occurenceSum <= 1024 we have rangeRoundingError < 1024
			// range <= 2^40
			// => LostBits <= log(2^40) - log(2^40-2^10) <= 1.35 * 10^(-9)
			// So we have to code 1,000,000 integers to lose one bit.
			//if ( indexToAdd == 0 )
			//	range += rangeRoundingError;
			//else
			//	low += rangeRoundingError;
#if DEBUG
			VerifyAfter();
#endif 

			while ( range < rangeShiftTreshold ) {
				range <<= 8;
				WriteLow();
			}
#if DEBUG
			return PosInBits - pos;
#endif
		}


#if DEBUG
		public double AddIntProbabilitySum1( int indexToAdd, double[] probability, int lastAllowedIndex )
#else
		public void AddIntProbabilitySum1( int indexToAdd, double[] probability, int lastAllowedIndex )
#endif
		{
#if DEBUG
			double pos = PosInBits;
			VerifyBefore();
			double sumVer = 0.0;
			for ( int i=0 ; i<=indexToAdd ; i++ ) {
				if ( probability[i] < 0.0D )
					throw new Exception();
				sumVer += probability[i];
			}
			if ( probability[indexToAdd]<0.000001d || sumVer>1.0000001d )
				throw new Exception();				
#endif
			// be careful to calculate the same intervals in Encoder and Decoder when using double. Rounding Errors!
			// intervalPoints are defined by:    (ulong)(Range*(SumProbabilities))
			double sum = 0.0d;
			for ( int i=0 ; i<indexToAdd ; i++ )
				sum += probability[i];

			low   += (ulong)(range * sum);
			range  = ((indexToAdd==lastAllowedIndex) ? range : ((ulong)(range * (sum + probability[indexToAdd])))) - ((ulong)(range*sum));  // take care to produce the correct rounding errors !

#if DEBUG
			VerifyAfter();
#endif 

			while ( range < rangeShiftTreshold ) {
				range <<= 8;
				WriteLow();
			}
#if DEBUG
			return PosInBits - pos;
#endif
		}
	

#if DEBUG
		public double AddInt( int indexToAdd, double[] probability, int allowedToCodeIndexCount )
#else
		public void AddInt( int indexToAdd, double[] probability, int allowedToCodeIndexCount )
#endif
		{
#if DEBUG
			if ( indexToAdd>=allowedToCodeIndexCount )
				throw new Exception();
			double pos = PosInBits;
			VerifyBefore();
			double sumVer = 0.0;
			for ( int i=0 ; i<=indexToAdd ; i++ ) {
				sumVer += (double)probability[i];
			}
			if ( probability[indexToAdd]<0.000001d || sumVer>1.0000001d || sumVer<0.000001 )
				throw new Exception();				
#endif

			double sum = 0.0d;
			for ( int i=0 ; i<indexToAdd ; i++ )
				sum += probability[i];
			double factorAllowedIndices;
			if ( probability.Length == allowedToCodeIndexCount )
				factorAllowedIndices = 1.0d;
			else {
				double sumAllCodeableProbabilities = sum;
				for ( int i=indexToAdd ; i<allowedToCodeIndexCount ; i++ )
					sumAllCodeableProbabilities += probability[i];
				factorAllowedIndices = 1.0d/sumAllCodeableProbabilities;
			}

	
			UInt64 delta = (ulong)(range * sum * factorAllowedIndices);
			low += delta;
			range = ((indexToAdd==allowedToCodeIndexCount-1) ? range : ((ulong)(range * (sum + probability[indexToAdd]) * factorAllowedIndices)) ) - delta;  // take care to produce the correct rounding errors; bit-exact results as in decoder
#if DEBUG
			VerifyAfter();
#endif 

			while ( range < rangeShiftTreshold ) {
				range <<= 8;
				WriteLow();
			}
#if DEBUG
			return PosInBits - pos;
#endif
		}


		public int Close()
		{
			SumPaddingBits = 5*8 + Math.Log( ((double)range)/((double)rangeShiftTreshold), 2 ); 
			// call now 7 times WriteLow; the first adds 0-8 bits padding; the last fills only the cache; all other 8 bits padding
			for ( int i=0 ; i<7 ; i++ )
				WriteLow();
			return bufferPos;
		}


		private void WriteLow()
		{
			// shift low
			if ( low >= lowCarryTreshold || low < lowShiftTreshold ) {
				if ( low >= lowCarryTreshold )
					cache++;
				while( cacheSize-- > 0 ) {
#if DEBUG_ENCODER
					if ( ((lowFull>>((lowFullByteCount-1-bufferPos)*8)) & 0xff) != cache )
						throw new Exception();
//					TBaccess.AddLine( "Write " + cache.ToString("X2") );
#endif
					buffer[bufferPos++] = cache;
					cache = (low>=lowCarryTreshold) ? ((byte)0x00) : ((byte)0xff);
				}
				cache     = (byte)(low>>40);
				cacheSize = 1;
#if DEBUG
//				TBaccess.AddLine( "Cache " + cache.ToString("X2") );
#endif
			}
			else { // shift but don't store value because not yet fixed due to a possible carry bit
				cacheSize++;  // increase amount of bytes to write later
#if DEBUG
//				TBaccess.AddLine( "CacheSizeIncrease " + cacheSize.ToString() );
#endif
			}
			low = ( low & lowMask ) << 8;
#if DEBUG
			verifyLow <<= 8;
			verifyRange <<= 8;         // range is always extended when WriteLow is called (except at closing)
#endif
		}


#if DEBUG
		public UInt64 Low
		{
			get { return low; }
		}


		private ulong verifyLow, verifyRange;
		private void VerifyBefore()
		{
			verifyLow                  = low;
			verifyRange                = range;
		}
		private void VerifyAfter()
		{
			// check if new interval is inside old interval
			if ( low < verifyLow || (low+range) > (verifyLow+verifyRange) )
				throw new Exception();
		}


#endif



		public override string ToString()
		{
			return bufferPos.ToString() + " " + low.ToString( "X16" ) + " " + range.ToString( "X16" );
		}
	}
}
