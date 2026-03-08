using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public static class BitManipulation
	{
		static BitManipulation()
		{
		}

		/// <summary>
		/// Clears bits index, index+1, ... , index+count-1 from v1 and insert v2 there
		/// </summary>
		public static long SetBits( long v1, int index, int v2, int count )
		{
			return ( v1 & (~(FirstNBits(count)<<index)) ) | ( ((long)v2) << index );
		}


		/// <summary>
		/// Clears bits index, index+1, ... , index+count-1 from v1 and insert v2 there
		/// </summary>
		public static long SetBits( long v1, int index, long v2, int count )
		{
			return ( v1 & (~(FirstNBits(count)<<index)) ) | ( v2 << index );
		}


		/// <summary>
		/// Clears bits index, index+1, ... , index+count-1 from v1 and insert v2 there
		/// </summary>
		public static int SetBits( int v1, int index, int v2, int count )
		{
			return ( v1 & (~(FirstNBitsInt(count)<<index)) ) | ( ((int)v2) << index );
		}

		public static long RemoveBits( long v, int index, int count )
		{
			return (v&FirstNBits(index)) | ((v&(~FirstNBits(index+count)))>>count) ;
		}

		public static long InsertBits( long v, int indexInsert, int countInsert, long insert )
		{
			return ((v&~FirstNBits(indexInsert))<<countInsert) | (insert<<indexInsert) | (v&FirstNBits(indexInsert));
		}

		public static long SetZeroHighBits( long v, int startIndex )
		{
			return (v&FirstNBits(startIndex));
		}

		public static long SetZeroLowBits( long v, int count )
		{
			return v&(~(FirstNBits(count)));
		}

		private static long FirstNBits( int n )
		{
			return (1L<<n)-1;
		}

		private static int FirstNBitsInt( int n )
		{
			return (1<<n)-1;
		}
	}
}
