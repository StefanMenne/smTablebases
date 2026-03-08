using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public struct Lines
	{
		private long val;

		public int Get( int index )
		{
			return ((int)(val>>(index<<3))) & 255;
		}

		public void Set( int index, int lineValue )
		{
			int startBit = index<<3;
			val = val ^ ((((val>>startBit)&255)^lineValue)<<startBit);
		}

		public static implicit operator Lines( int v )
		{
			return new Lines() { val = v };
		}
	}
}
