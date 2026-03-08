using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using TBacc;


namespace smTablebases
{
	public readonly struct Step
	{
		public readonly int  PassIndex           = 1;
		
		public Step( )
		{
		}
		
		private Step( int passIndex )
		{
			this.PassIndex = passIndex;
		}
		
		public Step Next()
		{
			return new Step( PassIndex+1 );
		}
	

		public Res WinResToGen
		{
			get{ return Res.FromInt(PassIndex); }
		}

		public Res GetLsResToGen( bool wtm )
		{
			if ( wtm && PassIndex == 1 )
				return Res.No;
			else
				return Res.FromInt( wtm?(-PassIndex):(-PassIndex-1) );
		}
		
		
		public static Res GetWinResToGen( int passIdx )
		{
			return Res.FromInt(passIdx);
		}
		
		public static Res GetLsResToGen( int passIdx, bool wtm )
		{
			if ( wtm && passIdx == 1 )
				return Res.No;
			else
				return Res.FromInt( wtm?(-passIdx):(-passIdx-1) );
		}
		

		public static int WinInOrLoseInAfterMv_To_CurrentWinInOrLoseIn( bool isWinAftermove, int winInOrLoseInAfterMove )
		{
			if ( isWinAftermove )
				return winInOrLoseInAfterMove;
			else 
				return winInOrLoseInAfterMove+1;
		}
	}
}
