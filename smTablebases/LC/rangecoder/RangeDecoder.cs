﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace LC
{
	public sealed class RangeDecoder : RangeCoder
	{
        // Big challenge is to do the same rounding errors as in encoder. Otherwise it will fail.
        // Easy way would be to simply do same steps in encoder and compare with another variable that
        // represents the final bigNumber which is always between low and low+bound.
        // But to reduce overhead only one variable bigNumberMinusLow is used which contains always the difference of 
        // the bigNumber and bigLow.
        // Where:
        // bigLow := the number represented by 
        //              1. all written bytes
        //              2. cachebyte + ff bytes if cacheCount>1
        //              3. low variable
        // during encoding process on the same step
        // bigNumber := the number represented by all bytes in the buffer which has been processed already
        private UInt64 bigNumberMinusLow    = 0x0000000000000000UL;

#if DEBUG
		private UInt64 low                  = 0x0000000000000000UL;
		private int    bufferPosVerify      = 0;
		private byte   cache                = 0xff;
		private int    cacheSize            = 0;
		private UInt64 bigNumberMinusLowVer = 0x0000000000000000UL;
#endif


		public RangeDecoder( byte[] buffernIn ) : base( buffernIn )
		{
			for ( int i=0 ; i < 6 ; i++ ) { 
#if DEBUG
				bigNumberMinusLowVer = (bigNumberMinusLowVer<<8) | ((UInt64)buffer[bufferPos]);
#endif
				bigNumberMinusLow = (bigNumberMinusLow<<8) | ((UInt64)buffer[bufferPos++]);
			}
#if DEBUG
			if ( bigNumberMinusLow != bigNumberMinusLowVer )
				throw new Exception();
#endif
		}


#if DEBUG
		public static void Decode( byte[] dataIn, int dataLength, byte[] dataOut )
		{
			RangeDecoder decoder = new RangeDecoder( dataIn );
			double[][] prob = new double[8][];
			int p2 = 1;
			for ( int i=0 ; i<prob.Length ; i++ ) {
				prob[i] = new double[p2];
				p2<<=1;
				for ( int k=0 ; k<prob[i].Length ; k++ )
					prob[i][k] = 0.5d;
			}

			for ( int i=0 ; i<dataLength ; i++ ) {
				byte b = 0;
				int k = 0;
				for ( int j=0 ; j<8 ; j++ ) {
//					bool bit = ((b>>(7-j))&1)==1;
					bool bit = decoder.GetBit( prob[j][k] );
					b<<=1;
					if ( bit ) {
						prob[j][k] = Probability.Increase( prob[j][k], 0.01d );
						k += (1<<j);
						b|=1;
					}
					else
						prob[j][k] = Probability.Decrease( prob[j][k], 0.01d );
				}
				dataOut[i] = b;
			}
		}
#endif


		public int GetBits( int count )
		{
			int v = 0;
			while ( count-- != 0 )
				v = (v<<1) | (GetBit() ? 1 : 0);
			return v;
		}


		public bool GetBit( double probabilityForTrue = 0.5d )
		{
			// case 1                             case 2
			//
			// +---- bigLow + range               +---- bigLow + range  
			// |                                  |
			// |                                  |
			// |                                  +----  bigNumber
			// |                                  |
			// |                                  |
			// +----  bigLow + bound              +----  bigLow + bound
			// |                                  |
			// |                                  |
			// +----  bigNumber                   |
			// |                                  |
			// |                                  | 
			// |                                  |
			// +----   bigLow                     +----  bigLow
			//
			// case 1:   bigNumber         <  bigLow + bound      =>      bigNumber - bigLow         <    bound
			// case 2:   bigNumber         >= bigLow + bound      =>      bigNumber - bigLow         >=   bound
			// where bigNumber and bigLow as defined above
			UInt64 bound = (UInt64) (range * probabilityForTrue);


			bool bit = bigNumberMinusLow < bound;
#if DEBUG
//			TBaccess.AddLine( (bit ? "1" : "0") + " " + low.ToString("X16") + "-" + (low+bound).ToString("X16") + "-" + (low+range).ToString("X16") + "     " + propabilityForTrue.ToString() );
#endif
			if ( bit ) {
				range  = bound;
			}
			else {
#if DEBUG
				low                  += bound;
				bigNumberMinusLowVer -= bound;
#endif
				bigNumberMinusLow    -= bound;    // encoder does here low += bound
				range                -= bound;
			}
			while ( range < rangeShiftTreshold ) {
#if DEBUG
				EmulateEncoderLowHandling();
#endif
				range <<= 8;
				bigNumberMinusLow = (bigNumberMinusLow<<8) | ((UInt64)buffer[bufferPos++]);
#if DEBUG
				if ( bigNumberMinusLow != bigNumberMinusLowVer )
					throw new Exception();
#endif
			}
			return bit;
		}


		public int GetInt( int[] occurence, int occurenceSum )
		{
			ulong rangePerOccurence  = range / ((uint)occurenceSum);
			//ulong rangeRoundingError = range - rangePerOccurence*((uint)occurenceSum);

			UInt64 bound    = 0UL;//rangeRoundingError;
			UInt64 oldBound = 0UL;

			for ( int i=0 ; i<occurence.Length ; i++ ) {
				bound += rangePerOccurence * ((uint)occurence[i]);
				if ( bigNumberMinusLow < bound ) {
#if DEBUG
					low                  += oldBound;
					bigNumberMinusLowVer -= oldBound;
#endif
					range              = bound - oldBound;
					bigNumberMinusLow -= oldBound;
					while ( range < rangeShiftTreshold ) {
#if DEBUG
						EmulateEncoderLowHandling();
#endif
						range <<= 8;
						bigNumberMinusLow = (bigNumberMinusLow<<8) | ((UInt64)buffer[bufferPos++]);
					}
					return i;
				}
				oldBound = bound;
			}
			throw new ArgumentOutOfRangeException();
		}


		public int GetIntProbabilitySum1( double[] probability, int lastAllowedIndex )
		{
			double probabilitySum = 0.0d;
			UInt64 oldPoint = 0UL;

			for ( int i=0 ; i<probability.Length ; i++ ) {
				probabilitySum += probability[i];
				UInt64 newPoint = (i==lastAllowedIndex) ? range : ((UInt64)(range * probabilitySum));

				if ( bigNumberMinusLow < newPoint ) {
#if DEBUG
					low                  += oldPoint;
					bigNumberMinusLowVer -= oldPoint;
#endif
					range              = newPoint - oldPoint;
					bigNumberMinusLow -= oldPoint;
					while ( range < rangeShiftTreshold ) {
#if DEBUG
						EmulateEncoderLowHandling();
#endif
						range <<= 8;
						bigNumberMinusLow = (bigNumberMinusLow<<8) | ((UInt64)buffer[bufferPos++]);
					}
					return i;
				}
				oldPoint = newPoint;
			}
			throw new ArgumentOutOfRangeException();
		}


		public int GetInt( double[] probability, int allowedToCodeIndexCount )
		{
			double probabilitySum = 0.0d;
			UInt64 oldPoint = 0UL;

			double factorAllowedIndices;
			if ( probability.Length == allowedToCodeIndexCount )
				factorAllowedIndices = 1.0d;
			else {
				double sumAllCodeableProbabilities = 0.0d;
				for ( int i=0 ; i<allowedToCodeIndexCount ; i++ )
					sumAllCodeableProbabilities += probability[i];
				factorAllowedIndices = 1.0d/sumAllCodeableProbabilities;
			}


			for ( int i=0 ; i<probability.Length ; i++ ) {
				probabilitySum += probability[i];
				UInt64 newPoint = ((i==(allowedToCodeIndexCount-1)) ? range : ((UInt64)(range * probabilitySum * factorAllowedIndices )));

				if ( bigNumberMinusLow < newPoint ) {
#if DEBUG
					low                  += oldPoint;
					bigNumberMinusLowVer -= oldPoint;
#endif
					range              = newPoint - oldPoint;
					bigNumberMinusLow -= oldPoint;
					while ( range < rangeShiftTreshold ) {
#if DEBUG
						EmulateEncoderLowHandling();
#endif
						range <<= 8;
						bigNumberMinusLow = (bigNumberMinusLow<<8) | ((UInt64)buffer[bufferPos++]);
					}
					return i;
				}
				oldPoint = newPoint;
			}
			throw new ArgumentOutOfRangeException();
		}




#if DEBUG
		public int BufferPosVerify
		{
			get { return bufferPosVerify; }
		}


		public UInt64 Low
		{
			get { return low; }
		}
#endif

#if DEBUG
		private void EmulateEncoderLowHandling()
		{
			// shift low
			if ( low >= lowCarryTreshold || low < lowShiftTreshold ) {
				if ( low >= lowCarryTreshold )
					cache++;
				while( cacheSize-- > 0 ) {
					if ( buffer[bufferPosVerify++] != cache )
						throw new Exception();
					cache = (low>=lowCarryTreshold) ? ((byte)0x00) : ((byte)0xff);
				}
				cache     = (byte)(low>>40);
				cacheSize = 1;
			}
			else { // shift but don't store value because not yet fixed due possible carry bit
				cacheSize++;  // increase amount of bytes to write later
			}

			bigNumberMinusLowVer += ( low & ~lowMask );
			low = ( low & lowMask ) << 8;
			bigNumberMinusLowVer  = (bigNumberMinusLow<<8) | ((UInt64)buffer[bufferPos]); // add new byte
		}


		public override string ToString()
		{
			return bufferPosVerify.ToString() + " " + low.ToString( "X16" ) + " " + range.ToString( "X16" );
		}
#endif
	}
}
