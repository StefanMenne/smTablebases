using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	/// <summary>
	/// Informations to combine two sets of matches.
	/// The Dists and "Roots in" are used for the left part of the new matches. They have to fit together.
	/// The BlockIds are used for the right part of the new matches.
	/// </summary>
	public class MatchCombineInfo
	{
		public int MatchLengthToGen;
		public int LeftMatchLength;
		public int LeftDistsInIndex;
		public int LeftRootsInIndex;
		public int LeftBlockIdsInIndex;
		public int RightBlockIdsInIndex;
		public int DistsOutIndex;
		public int BlockIdsOutIndex;
		public int RootsOutIndex;


		public MatchCombineInfo( int matchLengthToGen, int leftMatchLength, int leftDistsInIndex, int leftRootsInIndex, int leftBlockIdsInIndex, int rightBlockIdsInIndex, int distsOutIndex, int rootsOutIndex, int blockIdsOutIndex )
		{
			MatchLengthToGen     = matchLengthToGen;
			LeftMatchLength      = leftMatchLength;
			LeftDistsInIndex     = leftDistsInIndex;
			LeftRootsInIndex     = leftRootsInIndex;
			LeftBlockIdsInIndex  = leftBlockIdsInIndex;
			RightBlockIdsInIndex = rightBlockIdsInIndex;
			DistsOutIndex        = distsOutIndex;
			RootsOutIndex        = rootsOutIndex;
			BlockIdsOutIndex     = blockIdsOutIndex;
		}
	}


	public class MatchGenInfo
	{
		public int                  LengthsCount;
		public int                  InitialMatchLengthInItems;
		public int                  DistsOutIndex;
		public int                  RootsOutIndex;
		public int                  BlockIdsOutIndex;
		public MatchCombineInfo[]   MatchCombineInfo;


		//public static MatchGenInfo Length_1_2_4_8_16_32_64_128 = new MatchGenInfo(){
		//	InitialMatchLengthInItems = 1, DistsOutIndex=0, RootsOutIndex=0, BlockIdsOutIndex=0, LengthsCount=8, 
		//	MatchCombineInfo          = new MatchCombineInfo[]{
		//		//           blockIdsLeft-------------+                  
		//		//           rootsLeft-------------+  |  +---------------blockIdRight
		//		//           distsLeft----------+  |  |  |  +------------distOut      
		//		//           lengthLeft------+  |  |  |  |  |  +---------rootsOut
		//		//           length-----+    |  |  |  |  |  |  |  +------blockIdOut
		//		//                      |    |  |  |  |  |  |  |  |
		//		new MatchCombineInfo(   2,   1, 0, 0, 0, 0, 1, 1, 1 ),
		//		new MatchCombineInfo(   4,   2, 1, 1, 1, 1, 2, 0, 2 ),
		//		new MatchCombineInfo(   8,   4, 2, 0, 2, 2, 3, 1, 3 ),
		//		new MatchCombineInfo(  16,   8, 3, 1, 3, 3, 4, 0, 4 ),
		//		new MatchCombineInfo(  32,  16, 4, 0, 4, 4, 5, 1, 5 ),
		//		new MatchCombineInfo(  64,  32, 5, 1, 5, 5, 6, 0, 6 ),
		//		new MatchCombineInfo( 128,  64, 6, 0, 6, 6, 7, 1, 7 )
		//	}
		//};


		public static MatchGenInfo Length_2_4_8_16_32_64_128_256 = new MatchGenInfo(){
			InitialMatchLengthInItems = 2, DistsOutIndex=0, RootsOutIndex=0, BlockIdsOutIndex=0, LengthsCount=8,
			MatchCombineInfo          = new MatchCombineInfo[]{
				//           blockIdsLeft-------------+                  
				//           rootsLeft-------------+  |  +---------------blockIdRight
				//           distsLeft----------+  |  |  |  +------------distOut      
				//           lengthLeft------+  |  |  |  |  |  +---------rootsOut
				//           length-----+    |  |  |  |  |  |  |  +------blockIdOut
				//                      |    |  |  |  |  |  |  |  |
				new MatchCombineInfo(   4,   2, 0, 0, 0, 0, 1, 1, 1 ),
				new MatchCombineInfo(   8,   4, 1, 1, 1, 1, 2, 0, 2 ),
				new MatchCombineInfo(  16,   8, 2, 0, 2, 2, 3, 1, 3 ),
				new MatchCombineInfo(  32,  16, 3, 1, 3, 3, 4, 0, 4 ),
				new MatchCombineInfo(  64,  32, 4, 0, 4, 4, 5, 1, 5 ),
				new MatchCombineInfo( 128,  64, 5, 1, 5, 5, 6, 0, 6 ),
				new MatchCombineInfo( 256, 128, 6, 0, 6, 6, 7, 1, 7 )
			}
		};


		public static MatchGenInfo Length_2_3_4_8_16_32_64_128 = new MatchGenInfo(){
			InitialMatchLengthInItems = 1, DistsOutIndex=7, RootsOutIndex=0, BlockIdsOutIndex=7, LengthsCount=8,
			MatchCombineInfo          = new MatchCombineInfo[]{
				//           blockIdsLeft-------------+                  
				//           rootsLeft-------------+  |  +---------------blockIdRight
				//           distsLeft----------+  |  |  |  +------------distOut      
				//           lengthLeft------+  |  |  |  |  |  +---------rootsOut
				//           length-----+    |  |  |  |  |  |  |  +------blockIdOut
				//                      |    |  |  |  |  |  |  |  |
				new MatchCombineInfo(   2,   1, 7, 0, 7, 7, 0, 1, 0 ),
				new MatchCombineInfo(   3,   1, 7, 0, 7, 0, 1, 2, 1 ),
				new MatchCombineInfo(   4,   2, 0, 1, 0, 0, 2, 0, 2 ),
				new MatchCombineInfo(   8,   4, 2, 0, 2, 2, 3, 1, 3 ),
				new MatchCombineInfo(  16,   8, 3, 1, 3, 3, 4, 0, 4 ),
				new MatchCombineInfo(  32,  16, 4, 0, 4, 4, 5, 1, 5 ),
				new MatchCombineInfo(  64,  32, 5, 1, 5, 5, 6, 0, 6 ),
				new MatchCombineInfo( 128,  64, 6, 0, 6, 6, 7, 1, 7 )
			}
		};


		public static MatchGenInfo Length_2_3_4_5_8_16_32_64 = new MatchGenInfo(){
			InitialMatchLengthInItems = 1, DistsOutIndex=7, RootsOutIndex=0, BlockIdsOutIndex=7, LengthsCount=8,
			MatchCombineInfo          = new MatchCombineInfo[]{
				//           blockIdsLeft-------------+                  
				//           rootsLeft-------------+  |  +---------------blockIdRight
				//           distsLeft----------+  |  |  |  +------------distOut      
				//           lengthLeft------+  |  |  |  |  |  +---------rootsOut
				//           length-----+    |  |  |  |  |  |  |  +------blockIdOut
				//                      |    |  |  |  |  |  |  |  |
				new MatchCombineInfo(  2,    1, 7, 0, 7, 7, 0, 1, 0 ),  // gen match length 2 = 1 + 1
				new MatchCombineInfo(  3,    1, 7, 0, 7, 0, 1, 2, 1 ),  // gen match length 3 = 1 + 2
				new MatchCombineInfo(  4,    2, 0, 1, 0, 0, 2, 2, 2 ),  // gen match length 4 = 2 + 2
				new MatchCombineInfo(  5,    4, 2, 2, 2, 7, 3, 1, 3 ),  // gen match length 5 = 4 + 1
				new MatchCombineInfo(  8,    4, 2, 2, 2, 2, 4, 0, 4 ),
				new MatchCombineInfo( 16,    8, 4, 0, 4, 4, 5, 1, 5 ),
				new MatchCombineInfo( 32,   16, 5, 1, 5, 5, 6, 0, 6 ),
				new MatchCombineInfo( 64,   32, 6, 0, 6, 6, 7, 1, 7 )
			}
		};


		//public static MatchGenInfo Length_1_2_3_4_5_8_16_32 = new MatchGenInfo(){
		//	InitialMatchLengthInItems = 1, DistsOutIndex=0, RootsOutIndex=0, blockIdsOutIndex=0,  LengthsCount=8,
		//	MatchCombineInfo          = new MatchCombineInfo[]{
		//		//           blockIdsLeft-------------+                  
		//		//           rootsLeft-------------+  |  +---------------blockIdRight
		//		//           distsLeft----------+  |  |  |  +------------distOut      
		//		//           lengthLeft------+  |  |  |  |  |  +---------rootsOut
		//		//           length-----+    |  |  |  |  |  |  |  +------blockIdOut
		//		//                      |    |  |  |  |  |  |  |  |
		//		new MatchCombineInfo(   2,   1, 0, 0, 0, 0, 1, 1, 1 ),     //  2 =  1 +  1
		//		new MatchCombineInfo(   3,   1, 0, 0, 0, 1, 2, 2, 2 ),     //  3 =  1 +  2
		//		new MatchCombineInfo(   4,   2, 1, 1, 1, 1, 3, 2, 3 ),     //  4 =  2 +  2
		//		new MatchCombineInfo(   5,   4, 3, 2, 3, 0, 4, 1, 4 ),     //  5 =  4 +  1
		//		new MatchCombineInfo(   8,   4, 3, 2, 3, 3, 5, 0, 5 ),     //  8 =  4 +  4
		//		new MatchCombineInfo(  16,   8, 5, 0, 5, 5, 6, 1, 6 ),     // 16 =  8 +  8
		//		new MatchCombineInfo(  32,  16, 6, 1, 6, 6, 7, 0, 7 )      // 32 = 16 + 16
		//	}
		//};


		//public static MatchGenInfo Length_1_2_3_4_5_8_16_32_64_128_256 = new MatchGenInfo(){
		//	InitialMatchLengthInItems = 1, DistsOutIndex=0, RootsOutIndex=0, blockIdsOutIndex=0,  LengthsCount=11,
		//	MatchCombineInfo          = new MatchCombineInfo[]{
		//		//           blockIdsLeft-------------+                  
		//		//           rootsLeft-------------+  |  +---------------blockIdRight
		//		//           distsLeft----------+  |  |  |  +------------distOut      
		//		//           lengthLeft------+  |  |  |  |  |  +---------rootsOut
		//		//           length-----+    |  |  |  |  |  |  |  +------blockIdOut
		//		//                      |    |  |  |  |  |  |  |  |
		//		new MatchCombineInfo(   2,   1, 0, 0, 0, 0, 1, 1, 1 ),     //   2 =   1 +   1
		//		new MatchCombineInfo(   3,   1, 0, 0, 0, 1, 2, 2, 2 ),     //   3 =   1 +   2
		//		new MatchCombineInfo(   4,   2, 1, 1, 1, 1, 3, 2, 3 ),     //   4 =   2 +   2
		//		new MatchCombineInfo(   5,   4, 3, 2, 3, 0, 4, 1, 4 ),     //   5 =   4 +   1
		//		new MatchCombineInfo(   8,   4, 3, 2, 3, 3, 5, 0, 5 ),     //   8 =   4 +   4
		//		new MatchCombineInfo(  16,   8, 5, 0, 5, 5, 6, 1, 6 ),     //  16 =   8 +   8
		//		new MatchCombineInfo(  32,  16, 6, 1, 6, 6, 7, 0, 7 ),     //  32 =  16 +  16
		//		new MatchCombineInfo(  64,  32, 7, 0, 7, 7, 8, 1, 8 ),     //  64 =  32 +  32
		//		new MatchCombineInfo( 128,  64, 8, 1, 8, 8, 9, 0, 9 ),     // 128 =  64 +  64
		//		new MatchCombineInfo( 256, 128, 9, 0, 9, 9,10, 1,10 )      // 256 = 128 + 128
		//	}
		//};

		public static MatchGenInfo Length_2_3_4____273_RAM = new MatchGenInfo(){
			InitialMatchLengthInItems = 1, DistsOutIndex=271, RootsOutIndex=0, BlockIdsOutIndex=271, LengthsCount=272,
			MatchCombineInfo          = new MatchCombineInfo[272]
		};

		public static MatchGenInfo Length_2_3__9_10_16_32_64_128_256 = new MatchGenInfo(){
			InitialMatchLengthInItems = 1, DistsOutIndex=12, RootsOutIndex=0, BlockIdsOutIndex=12, LengthsCount=13,
			MatchCombineInfo          = new MatchCombineInfo[]{
				//            blockIdsLeft-------------+                  
				//            rootsLeft-------------+  |  +---------------blockIdRight
				//            distsLeft----------+  |  |  |  +------------distOut      
				//            lengthLeft------+  |  |  |  |  |  +---------rootsOut
				//            length-----+    |  |  |  |  |  |  |  +------blockIdOut
				//                       |    |  |  |  |  |  |  |  |
				new MatchCombineInfo(   2,    1,12, 0,12,12, 0, 1, 0 ),  // gen match length  2 = 1 + 1
				new MatchCombineInfo(   3,    2, 0, 1, 0,12, 1, 2, 1 ),  // gen match length  3 = 2 + 1
				new MatchCombineInfo(   4,    3, 1, 2, 1,12, 2, 1, 2 ),  // gen match length  4 = 3 + 1
				new MatchCombineInfo(   5,    4, 2, 1, 2,12, 3, 2, 3 ),  // gen match length  5 = 4 + 1
				new MatchCombineInfo(   6,    5, 3, 2, 3,12, 4, 1, 4 ),  // gen match length  6 = 5 + 1
				new MatchCombineInfo(   7,    6, 4, 1, 4,12, 5, 2, 5 ),  // gen match length  7 = 6 + 1
				new MatchCombineInfo(   8,    7, 5, 2, 5,12, 6, 1, 6 ),  // gen match length  8 = 7 + 1
				new MatchCombineInfo(   9,    8, 6, 1, 6,12, 7, 2, 7 ),  // gen match length  9 = 8 + 1
				new MatchCombineInfo(  16,    8, 6, 1, 6, 6, 8, 2, 8 ),  // gen match length 16 = 8 + 8
				new MatchCombineInfo(  32,   16, 8, 2, 8, 8, 9, 1, 9 ),  // gen match length 32 = 16 + 16
				new MatchCombineInfo(  64,   32, 9, 1, 9, 9,10, 2,10 ),  // gen match length 64 = 32 + 32
				new MatchCombineInfo( 128,   64,10, 2,10,10,11, 1,11 ),  // gen match length 128 = 64 + 64
				new MatchCombineInfo( 256,  128,11, 1,11,11,12, 2,12 )   // gen match length 256 = 128 + 128
			}
		};

		public static MatchGenInfo Length_2_3__16_17_32_64_128_256 = new MatchGenInfo(){
			InitialMatchLengthInItems = 1, DistsOutIndex=19, RootsOutIndex=0, BlockIdsOutIndex=19, LengthsCount=20,
			MatchCombineInfo          = new MatchCombineInfo[]{
				//            blockIdsLeft-------------+                  
				//            rootsLeft-------------+  |  +---------------blockIdRight
				//            distsLeft----------+  |  |  |  +------------distOut      
				//            lengthLeft------+  |  |  |  |  |  +---------rootsOut
				//            length-----+    |  |  |  |  |  |  |  +------blockIdOut
				//                       |    |  |  |  |  |  |  |  |
				new MatchCombineInfo(   2,    1,19, 0,19,19, 0, 1, 0 ),  // gen match length  2 = 1 + 1
				new MatchCombineInfo(   3,    2, 0, 1, 0,19, 1, 2, 1 ),  // gen match length  3 = 2 + 1
				new MatchCombineInfo(   4,    3, 1, 2, 1,19, 2, 1, 2 ),  // gen match length  4 = 3 + 1
				new MatchCombineInfo(   5,    4, 2, 1, 2,19, 3, 2, 3 ),  // gen match length  5 = 4 + 1
				new MatchCombineInfo(   6,    5, 3, 2, 3,19, 4, 1, 4 ),  // gen match length  6 = 5 + 1
				new MatchCombineInfo(   7,    6, 4, 1, 4,19, 5, 2, 5 ),  // gen match length  7 = 6 + 1
				new MatchCombineInfo(   8,    7, 5, 2, 5,19, 6, 1, 6 ),  // gen match length  8 = 7 + 1
				new MatchCombineInfo(   9,    8, 6, 1, 6,19, 7, 2, 7 ),  // gen match length  9 = 8 + 1
				new MatchCombineInfo(  10,    9, 7, 2, 7,19, 8, 1, 8 ),  // gen match length 10 = 9 + 1
				new MatchCombineInfo(  11,   10, 8, 1, 8,19, 9, 2, 9 ),
				new MatchCombineInfo(  12,   11, 9, 2, 9,19,10, 1,10 ),
				new MatchCombineInfo(  13,   12,10, 1,10,19,11, 2,11 ),
				new MatchCombineInfo(  14,   13,11, 2,11,19,12, 1,12 ),
				new MatchCombineInfo(  15,   14,12, 1,12,19,13, 2,13 ),
				new MatchCombineInfo(  16,   15,13, 2,13,19,14, 1,14 ),
				new MatchCombineInfo(  17,   16,14, 1,14,19,15, 2,15 ),
				new MatchCombineInfo(  32,   16,14, 1,14,14,16, 2,16 ),  // gen match length 32 = 16 + 16
				new MatchCombineInfo(  64,   32,16, 2,16,16,17, 1,17 ),  // gen match length 64 = 32 + 32
				new MatchCombineInfo( 128,   64,17, 1,17,17,18, 2,18 ),  // gen match length 128 = 64 + 64
				new MatchCombineInfo( 256,  128,18, 2,18,18,19, 1,19 )   // gen match length 256 = 128 + 128
			}

		};


		static MatchGenInfo()
		{
			Length_2_3_4____273_RAM.MatchCombineInfo[0] = new MatchCombineInfo( 2, 1, 271, 0, 271, 271, 0, 1, 0 );  // gen match length  2 = 1 + 1     rootsOut=1
			
			// i=1:    gen match length    3 =   2 + 1       rootsOut=0
			// i=2:    gen match length    4 =   3 + 1       rootsOut=1
			//
			// i=269:  gen match length  271 = 270 + 1       rootsOut=0
			// i=270:  gen match length  272 = 271 + 1       rootsOut=1
			for ( int i=1 ; i<=270 ; i++ )
				Length_2_3_4____273_RAM.MatchCombineInfo[i] = new MatchCombineInfo( i+2, i+1, i-1, (i%2), i-1, 271, i, 1-(i%2), i );

			// i=271:  gen match length  273 = 271 + 2       rootsOut=0
			Length_2_3_4____273_RAM.MatchCombineInfo[271] = new MatchCombineInfo( 273, 271, 269, 0, 269, 0, 271, 1, 271 );
		}
	}
}
