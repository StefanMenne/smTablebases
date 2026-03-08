using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class InitValues
	{
		private int[] values;
		private int   count          = 0;
		private int   sumOccurence   = 0;


		public InitValues( int minValue, int maxValue )
		{
			sumOccurence = count = maxValue-minValue+1;
			values = new int[2*count];
			for ( int i=0 ; i<count ; i++ ) {
				values[2*i]   = minValue + i;
				values[2*i+1] = 1;
			}
		}


		public InitValues( int[] values )
		{
			this.values = values;
			count = values.Length / 2;
			for ( int i=0 ; i<count ; i++ )
				sumOccurence += values[2*i+1];
		}


		public int IndexToValue( int index )
		{
			index = index%sumOccurence;

			for ( int i=0 ; i<count ; i++ ) {
				index -= values[2*i+1];
				if ( index < 0 )
					return values[2*i];
			}
			throw new Exception();
		}


		public int IndexToRank( int index )
		{
			index = index%sumOccurence;

			for ( int i=0 ; i<count ; i++ ) {
				index -= values[2*i+1];
				if ( index < 0 )
					return i;
			}
			throw new Exception();
		}


		public int GetCountDifferentValues()
		{
			return count;				
		}


		public int RankToValue( int rank )
		{
			return values[2*rank];
		}


		public int RankToOccurence( int rank )
		{
			return values[2*rank+1];
		}


		public int SumOccurence
		{
			get { return sumOccurence; }
		}
	}
}
