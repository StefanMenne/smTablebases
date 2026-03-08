using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public class ResToIntConverter
	{
		// example; maxWi=3, maLs=3 => remValue=7
		// int                         Res
		//  0                          Win in 1
		//  1                          Win in 2
		//  2                          Win in 3
		//  3                          Ls in 0 = Mt
		//  4                          Ls in 1
		//  5                          Ls in 2
		//  6                          Ls in 3
		//  7                          Draw
		//  8                          Ill

		private int remValue, illValue, maxValue;
		private int maxWi, maxLs;


		/// <summary>
		/// maxWi>=0    maxWi=0   no Win res available
		/// maxLs>=-1   maxLs=-1  no Ls/IsMt available      maxLs=0  only IsMt available
		/// </summary>
		public ResToIntConverter( int maxWi, int maxLs )
		{
			this.maxWi      = maxWi;
			this.maxLs      = maxLs;
			this.remValue   = maxWi + maxLs + 1;
			this.illValue   = remValue + 1;
			this.maxValue   = illValue;
		}


		public int ResToInt( Res res )
		{
			if ( res.IsWin )
				return res.WinInStarting0;
			else if ( res.IsLs )
				return res.LsIn + FirstLsOffset;
			else if ( res.IsDraw )
				return remValue;
			else if ( res.IsIllegalPos )
				return illValue;
			else
				throw new NotSupportedException();
		}


		public Res IntToRes( int value )
		{
			if ( value < maxWi )
				return Res.FromWiInStarting0( value );
			else if ( value < remValue )
				return Res.FromLsIn( value - FirstLsOffset );
			else if ( value == remValue )
				return Res.Draw;
			else if ( value == illValue )
				return Res.IllegalPos;
			else
				throw new NotSupportedException();
		}


		public int FirstLsOffset
		{
			get{ return maxWi; }
		}


		public int MaxValue
		{
			get{ return maxValue; }
		}


		public int MaxBitsForInteger
		{
			get{ return Tools.Log2ForAnyNumber( maxValue ) + 1; }
		}


		public static int GetBitCount( int maxWi, int maxLs )
		{
			return new ResToIntConverter(maxWi,maxLs).MaxBitsForInteger; 
		}


		public static Res[] GetIntToResTable( int maxWi, int maxLs )
		{
			ResToIntConverter resToIntConverter = new ResToIntConverter( maxWi, maxLs );
			return GetIntToResTable( resToIntConverter );
		}

		public static Res[] GetIntToResTable( ResToIntConverter resToIntConverter )
		{
			Res[] arr = new Res[resToIntConverter.MaxValue+1];
			for ( int i=0 ; i<=Res.MaxValue ; i++ ) {
				Res r = new Res( i );
				if ( ( r.IsWin && r.WinIn<=resToIntConverter.maxWi ) || ( r.IsLs && r.LsIn<=resToIntConverter.maxLs ) || r.IsIllegalPos || r.IsDraw )
					arr[resToIntConverter.ResToInt(r)] = r;
			}
			return arr;
		}


	}
}
