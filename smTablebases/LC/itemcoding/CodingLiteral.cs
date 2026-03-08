using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CodingLiteral
	{
#if DEBUG
		private double[]        bitsCodedSizeSum;
		private int[]           bitsCodedCount;
#endif
		private RangeEncoder    rangeEncoder;
		private RangeDecoder    rangeDecoder;
		private int             bitCount;


		public CodingLiteral( RangeCoder rangeCoder, int bitCount )
		{
			this.bitCount                     = bitCount;
			rangeEncoder                      = (rangeCoder is RangeEncoder) ? (RangeEncoder)rangeCoder : null;
			rangeDecoder                      = (rangeCoder is RangeDecoder) ? (RangeDecoder)rangeCoder : null;
#if DEBUG
			bitsCodedSizeSum                  = new double[bitCount];
			bitsCodedCount                    = new int[bitCount];
#endif
		} 


		public int BitCount
		{
			get { return bitCount; }
		}


#if DEBUG
		public double Encode( int value, int proposedValue, double[] probabilities )
#else
		public void Encode( int value, int proposedValue, double[] probabilities )
#endif
		{
			int  index = 1;
			bool useProposedValue = (proposedValue>=0);
#if DEBUG
			double bitsSum = 0.0;
			double bitsCurrent;
#endif

			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit           = ((value>>(bitCount-i-1))&1)==1;
				int  indexProposed = index;
				if ( useProposedValue ) {
					bool proposedBit   = ((proposedValue>>(bitCount-i-1))&1)==1;
					indexProposed     += ( (1<<bitCount) * (proposedBit ? 2 : 1) );
					useProposedValue   = (bit==proposedBit);
				}
				double d = probabilities[indexProposed];
#if DEBUG
				bitsCurrent = rangeEncoder.AddBit( bit, d );
#else
				rangeEncoder.AddBit( bit, d );
#endif
				probabilities[indexProposed] = Probability.ChangeProbability( d, bit, SettingsFix.LiteralTreeMinProbability, SettingsFix.LiteralTreeMaxProbability );
				index = (index<<1) | (bit ? 1 : 0);
#if DEBUG
				bitsSum += bitsCurrent;
				bitsCodedSizeSum[i] += bitsCurrent;
				bitsCodedCount[i]++;
#endif
			} 
#if DEBUG
			return bitsSum;
#endif
		}


		public static double GetCodingSizeProbabilityProduct( int bitCount, int value, int proposedValue, double[] probabilities, bool getSize )
		{
			int  index = 1;
			bool useProposedValue = (proposedValue>=0);
			double prob = 1.0d;

			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit           = ((value>>(bitCount-i-1))&1)==1;
				int  indexProposed = index;
				if ( useProposedValue ) {
					bool proposedBit   = ((proposedValue>>(bitCount-i-1))&1)==1;
					indexProposed     += ( (1<<bitCount) * (proposedBit ? 2 : 1) );
					useProposedValue   = (bit==proposedBit);
				}
				double d = probabilities[indexProposed];
				if ( getSize )
					prob *= Probability.Get( d, bit );
				else { 
					probabilities[indexProposed] = Probability.ChangeProbability( d, bit, SettingsFix.LiteralTreeMinProbability, SettingsFix.LiteralTreeMaxProbability );
				}
				index = (index<<1) | (bit ? 1 : 0);
			} 
			return prob;
		}


		public int Decode( int proposedValue, double[] probabilities )
		{
			int  value = 1, mask = ((1<<bitCount)-1);
			bool useProposedValue = (proposedValue>=0);

			for ( int i=0 ; i<bitCount ; i++ ) {
				bool proposedBit   = ((proposedValue>>(bitCount-i-1))&1)==1;
				int  indexProposed = value;
				if ( useProposedValue ) 
					indexProposed += ( (1<<bitCount) * (proposedBit ? 2 : 1) );
				double d = probabilities[indexProposed];
				bool bit = rangeDecoder.GetBit( d );
				probabilities[indexProposed] = Probability.ChangeProbability( d, bit, SettingsFix.LiteralTreeMinProbability, SettingsFix.LiteralTreeMaxProbability );
				value = (value<<1) | (bit ? 1 : 0);
				useProposedValue &= (bit==proposedBit) ;
			} 

			return value & mask;
		}




#if DEBUG
		public override string ToString()
		{
			string s="";
			double sumTotal = 0.0D;
			int count = 0;

			for ( int i=0 ; i<bitsCodedSizeSum.Length ; i++ ) {
				if ( bitsCodedCount[i]==0 )
					s += "----- ";
				else { 
					s+= ( bitsCodedSizeSum[i] / bitsCodedCount[i] ).ToString("#,###,###,##0.000") + " ";
				}
				count = Math.Max( count, bitsCodedCount[i] );
				sumTotal += bitsCodedSizeSum[i];
			}
			if ( count == 0 )
				return "---";
			else
				return count.ToString("###,###,###").PadLeft(10) + (sumTotal/count).ToString("#,###,###,##0.000").PadLeft(7) + (sumTotal/8).ToString("#,###,###,##0.000").PadLeft(14) + " ( " + s + ")";
		}
#endif
	}
}
