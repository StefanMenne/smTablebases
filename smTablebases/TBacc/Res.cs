﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TBacc;

namespace TBacc
{
	/// <summary>
	/// 0    1    ...   1999    2000 2001   2002   ... 3999 4000      4001 4002 4002  4003
	/// Wi1  Wi2  ...   Wi2000  Draw Ls1999 Ls1998 ... Ls1  Ls0=IsMt  Init StMt  Ill  No
	/// </summary>
	public readonly struct Res
	{
		public  const  int  MaxDtm          = 1021;    // 11 bits;    0 .... 2042
		public  const  int  MaxValue        = noVal;
		private const  int  drawVal         = MaxDtm;
		private const  int  isMtVal         = 2*MaxDtm;
		private const  int  initVal         = 2*MaxDtm+1;
		private const  int  stMtVal         = 2*MaxDtm+2;   // only used temporarily; finally they will be draw
		private const  int  illegalPosVal   = 2*MaxDtm+3;
		private const  int  noVal           = 2*MaxDtm+4;

		public static readonly Res No         = new Res( noVal );          // not used in Tablebase calculation
		public static readonly Res Init       = new Res( initVal );        // all set to init on Init step
		public static readonly Res IllegalPos = new Res( illegalPosVal );
		public static readonly Res IsMt       = new Res( isMtVal );
		public static readonly Res Draw       = new Res( drawVal );
		public static readonly Res StMt       = new Res( stMtVal );

        public readonly int Value;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Res(int val)
        {
            this.Value = val;
        }

        public static Res FromWiInStarting0( int wiInStarting0 )
		{
			return new Res( wiInStarting0 );
		}

		public static Res FromLsIn( int lsIn )
		{
			return new Res( 2*MaxDtm - lsIn );
		}

		public static Res FromInt( int i )
		{
			if ( i==0 )
				return Res.Draw;
			else if ( i>0 )
				return new Res( i-1 );
			else //if ( i<0 )
				return new Res( 2*MaxDtm+1+i );
		}
		
		
	  

		public Res( Res res )
		{
			Value = res.Value;
		}


		public bool IsM
		{
			get{ return Value==isMtVal; }
		}

		public bool IsDraw
		{
			get{ return Value==drawVal; }
		}

		public bool IsNo
		{
			get { return Value == noVal; }
		}

		public bool IsIllegalPos
		{
			get{ return Value==illegalPosVal; }
		}

		public bool IsStMt
		{
			get { return Value == stMtVal; }
		}

		public bool IsDrawOrStMt
		{
			get { return IsDraw || IsStMt; }
		}

		public bool IsInit
		{
			get{ return Value==initVal; }
		}

		public bool IsWin
		{
			get{ return Value<MaxDtm; }
		}

		public bool IsWinOrDraw
		{
			get{ return Value<=MaxDtm; }
		}

		public bool IsLs
		{
			get{ return Value<=2*MaxDtm && Value>MaxDtm; }
		}

		public bool IsLsOrInit
		{
			get{ return Value<=2*MaxDtm+1 && Value>MaxDtm; }
		}

		public int WinInHalfMv
		{
			get{ return 2*Value+1; }
		}

		public int LsInHalfMv
		{
			get{ return 2*LsIn; }
		}

		public int WinIn
		{
			get{ return Value+1; }
		}

		public int WinInStarting0
		{
			get{ return Value; }
		}

		/// <summary>
		/// returns -1 for init
		/// </summary>
		public int LsIn
		{
			get{ return 2*MaxDtm-Value; }
		}

		public int Dtm
		{
			get{
				if ( IsWin )
					return Value+1;
				else if ( IsLs )
					return 2*MaxDtm+1-Value;
				else
					return 0;
			}
		}

		/// <summary>
		/// returns 0=000...000 if IsWinOrDraw or -1=111...111 otherwise 
		/// </summary>
		public int IsWinOrDrawBitMask
		{
			get{ return (MaxDtm-Value)>>16; }
		}


		/// <summary>
		/// 0=Draw 1=Win1 2=Win2 -1=IsMt -2=Ls1 -3=Ls2
		/// </summary>
		public int AsInt
		{
			get { 
				if ( IsDraw || IsStMt ) 
					return 0;
				else if ( IsWin )
					return Value+1;
				else if ( IsSpecial )
					return Value;
				else
					return Value-2*MaxDtm-1;
			}
		}


		public bool IsSpecial
		{
			get{ return Value > 2*MaxDtm; }
		}


		public Res Combine( Res res )
		{
			return new Res( Math.Min( Value, res.Value ) );
		}

		public Res HalfMoveToMate
		{
			get { 
				if ( IsSpecial || IsDraw )
					return this;
				else 
					return new Res( 2*MaxDtm-Value-(IsWin?0:1) );// IsWin ? Ls(WinIn) : Win(LsIn-1);
			}
		}

		public Res HalfMoveAwayFromMate
		{
			get { 
				if ( IsSpecial || IsDraw )
					return this;
				else 
					return new Res( 2*MaxDtm-Value-(IsWin?1:0) );// IsWin ? Ls(WinIn+1) : Win(LsIn);
			}
		}

		/// <summary>
		/// Only valid for lose result. Return value is win.
		/// </summary>
		public Res HalfMvAwayFromMateForLs
		{
			get { return new Res( 2*MaxDtm-Value ); }
		}


		public override string ToString()
		{
#if DEBUG
			string s = AsInt.ToString() + " (";
#else
			string s = "";
#endif
			if ( IsNo )
				s += "No";
			else if ( IsInit )
				s += "Init/Unknown";
			else if ( IsIllegalPos )
				s += "Invalid";
			else if ( IsDraw )
				s += "Draw";
			else if ( IsStMt )
				s += "StMt";
			else if ( IsM )
				return "Mate";
			else if ( IsWin )
				s += "Win in " + WinIn.ToString() /*+ " / " + WinInHalfMv + "HM"*/;
			else if ( IsLs )
				s += "Lose in " + LsIn.ToString() /*+ " / " + LsInHalfMv + "HM"*/;
			else throw new Exception();
			
#if DEBUG
			return s + ")";
#else
			return s;
#endif
		}


		public static bool operator >( Res a, Res b )
		{
			return a.Value > b.Value;
		}

		public static bool operator <( Res a, Res b )
		{
			return a.Value < b.Value;
		}

		public static bool operator <=( Res a, Res b )
		{
			return a.Value <= b.Value;
		}

		public static bool operator >=( Res a, Res b)
		{
			return a.Value >= b.Value;
		}

		public static bool Compare( Res r1, Res r2 )
		{
			return (r1.IsDrawOrStMt&&r2.IsDrawOrStMt) || r1.Value==r2.Value;
		}

	}
}
