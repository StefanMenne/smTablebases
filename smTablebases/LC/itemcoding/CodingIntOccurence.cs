﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CodingIntOccurence
	{
#if DEBUG
		private int             codedCount            = 0;
		private double          bitsCodedSizeSum      = 0.0D;
#endif
		private RangeEncoder    rangeEncoder;
		private RangeDecoder    rangeDecoder;

		public CodingIntOccurence( RangeCoder rangeCoder )
		{
			rangeEncoder                      = (rangeCoder is RangeEncoder) ? (RangeEncoder)rangeCoder : null;
			rangeDecoder                      = (rangeCoder is RangeDecoder) ? (RangeDecoder)rangeCoder : null;
		}


#if DEBUG
		public double Encode( int value, int[] occurence, int sumOccurence )
		{
			double costs = rangeEncoder.AddInt( value, occurence, sumOccurence );
			bitsCodedSizeSum += costs;
			codedCount++;
			return costs; 			
		}
#else
		public void Encode( int value, int[] occurence, int sumOccurence )
		{
			rangeEncoder.AddInt( value, occurence, sumOccurence );
		}
#endif


#if DEBUG
		public double EncodeProbabilitySum1( int index, double[] probability, int lastAllowedIndex )
		{
			double costs = rangeEncoder.AddIntProbabilitySum1( index, probability, lastAllowedIndex );
			bitsCodedSizeSum += costs;
			codedCount++;
			return costs; 			
		}
#else
		public void EncodeProbabilitySum1( int index, double[] probability, int lastAllowedIndex )
		{
			rangeEncoder.AddIntProbabilitySum1( index, probability, lastAllowedIndex );
		}
#endif


#if DEBUG
		public double Encode( int index, double[] probability, int allowedToCodeIndexCount )
		{
			double costs = rangeEncoder.AddInt( index, probability, allowedToCodeIndexCount );
			bitsCodedSizeSum += costs;
			codedCount++;
			return costs; 			
		}
#else
		public void Encode( int index, double[] probability, int allowedToCodeIndexCount )
		{
			rangeEncoder.AddInt( index, probability, allowedToCodeIndexCount );
		}
#endif


		public int DecodeProbabilitySum1( double[] probability, int lastAllowedIndex )
		{
			return rangeDecoder.GetIntProbabilitySum1( probability, lastAllowedIndex );
		}


		public int Decode( double[] probability, int allowedToCodeIndexCount )
		{
			return rangeDecoder.GetInt( probability, allowedToCodeIndexCount );
		}



#if DEBUG
		public override string ToString()
		{
			if ( codedCount == 0 )
				return "---";
			return codedCount.ToString("###,###,###,##0").PadLeft(10) + (bitsCodedSizeSum / codedCount).ToString("#,###,###,##0.000").PadLeft(7) + (bitsCodedSizeSum/8).ToString("#,###,###,##0.000").PadLeft(14);
		}
#endif




	}
}
