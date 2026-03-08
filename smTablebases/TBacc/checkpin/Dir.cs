using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public struct Dir
	{
		public static Dir First      = new Dir( 0 );
		public static Dir Horizontal = new Dir( 0 );
		public static Dir Vertical   = new Dir( 1 );
		public static Dir LlUr       = new Dir( 2 );
		public static Dir UlLr       = new Dir( 3 );
		public static Dir Count      = new Dir( 4 );
		public static Dir No         = new Dir( 4 );

		private static readonly int[] delta  = new int[] { 1, 8,  9, -7 };
		private static readonly int[] deltaX = new int[] { 1, 0,  1,  1 };
		private static readonly int[] deltaY = new int[] { 0, 1,  1, -1 };

		private int value;

		public Dir( int val )
		{
			value = val;
		}

		public bool IsVertical
		{
			get{ return value == 1; }
		}

		public bool IsNo
		{
			get{ return value == 4; }
		}

		public int Delta
		{
			get { return delta[value]; }
		}

		public int DX
		{
			get{ return deltaX[value]; }
		}

		public int DY
		{
			get { return deltaY[value]; }
		}

		public int Value
		{
			get { return value; }
		}


		public int GetPos( Field f )
		{
			return IsVertical ? f.Y : f.X;
		}

		public static Dir Get( Field f1, Field f2 )
		{
			if ( f1.Y == f2.Y )
				return Dir.Horizontal;
			else if (f1.X == f2.X)
				return Dir.Vertical;
			else if (f1.DiagLlUr == f2.DiagLlUr)
				return Dir.LlUr;
			else if (f1.DiagUlLr == f2.DiagUlLr)
				return Dir.UlLr;
			else
				return Dir.No;
		}

		public static bool operator ==( Dir a, Dir b )
		{
			return a.value == b.value;
		}

		public static bool operator !=( Dir a, Dir b )
		{
			return a.value == b.value;
		}

		public static bool operator <( Dir a, Dir b )
		{
			return a.value < b.value;
		}

		public static bool operator >( Dir a, Dir b )
		{
			return a.value > b.value;
		}

		public static Dir operator ++( Dir a )
		{
			a.value++;
			return a;
		}

		public override bool Equals( object obj )
		{
			if ( obj is Dir )
				return ((Dir)obj).value == value;
			else
				return false;
		}

		public override int GetHashCode()
		{
			return value;
		}
	}
}
