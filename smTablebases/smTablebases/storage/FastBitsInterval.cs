using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public struct FastBitsInterval
	{
		private int winResToFindInNextStepWinIn;
		private int lsResToFindInNextStepLsIn;
		private int winResToFindInNextStepAfterNextOptimizationWinIn;
		private int lsResToFindInNextStepAfterNextOptimizationLsIn;




		public FastBitsInterval( int currentPassIndex, bool wtm )
		{
			// Create an interval where FastBits might be 1. All other fastBits are set to 0.

			currentPassIndex = currentPassIndex-1 - (currentPassIndex-1)%Config.OptimizeStepInterval;
			
			// Example for App.OptimizeStepInterval = 10 und passIndex=10
			//
			// currentPassIndex input:  1  2  3  4  5  6  7  8  9 10 11 12 ...
			// currentPassIndex:        0  0  0  0  0  0  0  0  0  0 10 11 ...  
			//
			//  pass    WTM        BTM       pass
			//  Index  --------|             Index
			//          -9     |   
			//   10            |---------
			//          10     |    10
			//         --------|              10
			//         -10     |   -10
			//   11            |---------
			//          11     |    11
			//         --------|              11
			//                 |   -11
			//                 |---------
			//
			//
			//   passIndex    col       res To gen                                resToSearch
			//   10           wtm       wtm     winResToGen=10   WinIn=10          btm    winResRoGen.HalfMoveToMate=-10  LsIn=9
			//                          wtm     lsResToGen =-10  LsIn = 9          btm    lsResRoGen.HalfMoveToMate=9     WinIn=9
			//   10           btm       btm     winResToGen=10   WinIn=10          wtm    winResRoGen.HalfMoveToMate=-10  LsIn=9
			//                          btm     lsResToGen =-11  LsIn =10          wtm    lsResRoGen.HalfMoveToMate=10    WinIn=10
			//   ----------------------------------------------------------------------------------------------------------------
			//   11           wtm       wtm     winResToGen=11   WinIn=11          btm    winResRoGen.HalfMoveToMate=-11  LsIn=10   *7
			//                          wtm     lsResToGen =-11  LsIn =10          btm    lsResRoGen.HalfMoveToMate=10    WinIn=10  *5
			//   11           btm       btm     winResToGen=11   WinIn=11          wtm    winResRoGen.HalfMoveToMate=-11  LsIn=0    *3
			//                          btm     lsResToGen =-12  LsIn =11          wtm    lsResRoGen.HalfMoveToMate=11    WinIn=11  *1
			//   ----------------------------------------------------------------------------------------------------------------
			//   20           wtm       wtm     winResToGen=20   WinIn=20          btm    winResRoGen.HalfMoveToMate=-20  LsIn=19
			//                          wtm     lsResToGen =-20  LsIn =19          btm    lsResRoGen.HalfMoveToMate=19    WinIn=19
			//   20           btm       btm     winResToGen=20   WinIn=20          wtm    winResRoGen.HalfMoveToMate=-20  LsIn=19
			//                          btm     lsResToGen =-21  LsIn =20          wtm    lsResRoGen.HalfMoveToMate=20    WinIn=20
			//   ----------------------------------------------------------------------------------------------------------------
			//   21           wtm       wtm     winResToGen=21  WinIn=21           btm    winResRoGen.HalfMoveToMate=-21  LsIn=20   *8
			//                          wtm     lsResToGen =-21 LsIn =20           btm    lsResRoGen.HalfMoveToMate=20    WinIn=20  *6
			//   21           btm       btm     winResToGen=21  WinIn=21           wtm    winResRoGen.HalfMoveToMate=-21  LsIn=20   *4
			//                          btm     lsResToGen =-22 LsIn =21           wtm    lsResRoGen.HalfMoveToMate=21    WinIn=21  *2
			//
			//   Optimize Step:
			//   10           wtm       FastBits only for positions 11<=WinIn<21   *1,*2          10<=LsIn<20    *3,*4
			//                          lsResRoGen(btm,11).HalfMoveToMate.WinIn=11
			//                          winResToGen(btm,11).HalfMoveToMate.LsIn=10
			//
			//                btm       FastBits only for positions 10<=WinIn<20   *5,*6          10<=LsIn<20    *7,*8
			//                          lsResRoGen(wtm,11).HalfMoveToMate.WinIn=11
			//                          winResToGen(wtm,11).HalfMoveToMate.LsIn=10
			//
			winResToFindInNextStepWinIn                       = Step.GetLsResToGen( currentPassIndex+1, !wtm ).HalfMoveToMate.WinIn;
			lsResToFindInNextStepLsIn                         = Step.GetWinResToGen( currentPassIndex+1 ).HalfMoveToMate.LsIn;
			winResToFindInNextStepAfterNextOptimizationWinIn  = Step.GetLsResToGen( currentPassIndex+Config.OptimizeStepInterval+1, !wtm ).HalfMoveToMate.WinIn;
			lsResToFindInNextStepAfterNextOptimizationLsIn    = Step.GetWinResToGen( currentPassIndex+Config.OptimizeStepInterval+1 ).HalfMoveToMate.LsIn;
		}

		public int WinInMin
		{
			get{ return winResToFindInNextStepWinIn; }
		}

		public int LsInMin
		{
			get { return lsResToFindInNextStepLsIn; }
		}

		public int WinInMaxPlus1
		{
			get { return winResToFindInNextStepAfterNextOptimizationWinIn; }
		}

		public int LsInMaxPlus1
		{
			get { return lsResToFindInNextStepAfterNextOptimizationLsIn; }
		}


	}
}
