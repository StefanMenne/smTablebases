using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public struct NextIndexWithOccFieldsInfoSequential
	{
		public Bits BitsOcc;
	}


	/// <summary>
	/// Example for SetToFirstWithOcc( A6|B3|B6 )
	/// 
	///               o  o  o  o  o  x  o  o  x  o  o  o  o  x  o  o  o  o  o 
	///  Field       C3 C2 C1 B8 B7 B6 B5 B4 B3 B2 B1 A8 A7 A6 A5 A4 A3 A2 A1
	///  index       18 17 16 15 14 13 12 11 10 09 08 07 06 05 04 03 02 01 00
	///  
	/// AllOccIdxCnt = 3
	/// AllOccIdx    = 5, 10, 13
	/// 1 <= CurOccCnt <=3
	/// 
	/// </summary>
	public struct NextIndexWithOccFieldsInfo
	{
		public Bits BitsOcc;

		public int          AllOccIdxCnt;           // the amount of Fields/Indices one at least should be occupied
		public Values64     AllOccIdx;              // the indices which at least one should be occupied
		public int          OccIdx;                 // the overall index that specifies which fields are currently chosen to be occupied
		public int          OccIdxCnt;              // the maximum-1 for OccIdx
		public int          OtherIdx;               // the index that specifies the other Fields that cannot be occupied 
		public int          OtherIdxCnt;
		public int          CurOccCnt;            
		public Values64[]	indexToPieceIndicesOcc;
		public Values64[]   indexToPieceIndicesOther;
		public int[]        OccIdxToIdx;
		public int[]        OthIdxToIdx;
	}


}
