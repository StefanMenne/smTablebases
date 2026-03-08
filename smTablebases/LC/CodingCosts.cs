﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LC
{



	public struct CodingCosts
	{
		[StructLayout(LayoutKind.Explicit)]
		public struct DoubleStruct
		{
			[FieldOffset(0)] public double D;
			[FieldOffset(0)] public ulong  UL;
		}


		public static CodingCosts BigValue = new CodingCosts(){ dbl=1.0d, exponent=100000000 }; 
		public static CodingCosts Null     = new CodingCosts(){ dbl=1.0d, exponent=0         }; 


		// double literal notation is in decimal system!    Build it bit by bit to ensure no rounding problems
		private static readonly double normalizeTreshold = ( new DoubleStruct(){ UL=0x0170000000000000 } ).D;   // 1.0 * 2^-1000     Bit 0-51 = mantis = 0   ;  Bit 52-62 = exponent = 23     ; Bit 63 = sign = 0 = +
		private static readonly double normalizeMult     = ( new DoubleStruct(){ UL=0x7e70000000000000 } ).D;   // 1.0 * 2^1000      Bit 0-51 = mantis = 0   ;  Bit 52-62 = exponent = 2023   ; Bit 63 = sign = 0 = +

		/// <summary>
		/// value = dbl * 2^(-1000*exponent)
		/// 
		/// coding size = log( 1/value ) = ... = -log(dbl) + 1000 * exponent
		/// 
		/// </summary>
		private double dbl;
		private int    exponent;


		public CodingCosts Add( double probability )
		{
			double d = dbl * probability;
			if ( d < normalizeTreshold ) 
				return new CodingCosts(){ dbl=d*normalizeMult, exponent=exponent+1 };
			else
				return new CodingCosts() { dbl=d, exponent=exponent };
		}


		public double GetBitSize()
		{
			return -Math.Log( dbl, 2.0d ) + (1000*exponent);
		}


		public override string ToString()
		{
			return GetBitSize().ToString( "#,###,###,##0.000" );
		}


		public static double ProbabilityFromBitCount( int bitCount )
		{
			double res = 1.0d;
			for ( int i=0 ; i<bitCount ; i++ )
				res *= 0.5d;
			return res;
		}


		/// <summary>
		/// order is regarding the coding size not the value
		/// </summary>
		public static bool operator<( CodingCosts a, CodingCosts b )
		{
			if ( a.exponent == b.exponent )
				return a.dbl > b.dbl;
			else if ( a.exponent+1 == b.exponent )
				return (a.dbl * normalizeMult) > b.dbl;
			else if ( a.exponent == b.exponent+1 )
				return a.dbl > (b.dbl * normalizeMult);
			else return a.exponent<b.exponent;
		}


		/// <summary>
		/// order is regarding the coding size not the value
		/// </summary>
		public static bool operator>( CodingCosts a, CodingCosts b )
		{
			if ( a.exponent == b.exponent )
				return a.dbl < b.dbl;
			else if ( a.exponent+1 == b.exponent )
				return (a.dbl * normalizeMult) < b.dbl;
			else if ( a.exponent == b.exponent+1 )
				return a.dbl < (b.dbl * normalizeMult);
			else return a.exponent>b.exponent;
		}

	}
}
