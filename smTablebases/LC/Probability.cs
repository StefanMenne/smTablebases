using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{

	public static class Probability
	{
		private const  double   decreaseFactor           = 31d/32d;
		private const  double   reverse65536             = 1.0d / 65536.0d;


		public static void SetEqualProbabilitySum1( double[] probability )
		{
			double v = 1.0d / probability.Length;
			for ( int i=0 ; i<probability.Length ; i++ ) 
				probability[i] = v;
		}


		public static void IncreaseOneProbabilityKeepSum1( double[] probability, int indexToIncrease, int indexCounToDecrease, double minProbability )
		{
			double delta = 0.0d;
			for ( int i=0 ; i<indexCounToDecrease ; i++ ) {
				if ( i!=indexToIncrease ) {
					delta           += probability[i];
					probability[i]   = Probability.Decrease( probability[i], minProbability );
					delta           -= probability[i];
				}
			}
			probability[indexToIncrease] = probability[indexToIncrease] + delta;

#if DEBUG
			double sum = 0d;
			for ( int i=0 ; i<probability.Length ; i++ )
				sum += probability[i];
			if ( Math.Abs( 1.0 - sum ) > 0.00001 )
				throw new Exception();
#endif
		}

		
		public static double ChangeProbability( double d, bool increase, double minProbability, double maxProbability )
		{
			return increase ? Increase(d,maxProbability) : Decrease(d,minProbability);
		}


		// new = 1 - (31/32) * ( 1 - old )
		public static double Increase( double d, double max )
		{
			double n = 1d - ( (1d-d) * decreaseFactor );
			if ( n > max )
				 n = max;
			return n;
		}


		// new = old * (31/32)
		public static double Decrease( double d, double min )
		{
			double n = d * decreaseFactor;
			if ( n < min )
				n = min;
			return n; 
		}

		
		public static double Inverse( double d )
		{
			return 1d - d;
		}
		

		public static double Get( double d, bool selectInverse )
		{
			return selectInverse ? d : Inverse(d);
		}


		public static string ToString( double d )
		{
			return d.ToString( "#,###,###,##0.000" );
		}
	}
}
