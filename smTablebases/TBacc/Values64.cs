using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	/// <summary>
	/// Stores up to 10 6-Bit values.
	/// </summary>
	public struct Values64
	{
		private long bits;

		private Values64( long bits )
		{
			this.bits = bits;
		}

		public int Get( int index )
		{
			return  ((int)((bits>>(6*index)))) & 63;
		}

		public void Set( int index, int value )
		{
			bits = BitManipulation.SetBits( bits, 6*index, value, 6 );
		}

		public void Remove( int index )
		{
			bits = BitManipulation.RemoveBits( bits, 6*index, 6 );
		}

		public int IndexOf( int v, int count )
		{
			for ( int i=0 ; i<count ; i++ ) {
				if ( Get(i) == v )
					return i;
			}
			return -1;
		}

		public void Insert( int index, int v )
		{
			bits = BitManipulation.InsertBits( bits, 6*index, 6, v );
		}

		public long Bits
		{
			get{ return bits; }
		}

		public static Values64 operator |( Values64 a, Values64 b )
		{
			return new Values64( a.bits | b.bits );
		}

		public static Values64 operator <<( Values64 a, int b )
		{
			return new Values64( a.bits << (6*b) );
		}

		public static Values64 operator >>( Values64 a, int b )
		{
			return new Values64( a.bits >> (6*b) );
		}

		public static int Compare( Values64 f1, Values64 f2, int count )
		{
			return BitManipulation.SetZeroHighBits( f1.bits, 6*count ).CompareTo( BitManipulation.SetZeroHighBits( f2.bits, 6*count ) );
		}

		public override string ToString()
		{
			return ToString( 8 );
		}

		public string ToString( int count )
		{
			string s = Get(0).ToString();
			for ( int i=1 ; i<count ; i++ )
				s += ", " + Get(i).ToString();
			return s;
		}
	}
}
