using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	// IndexType
	// #########
	//
	// Example for CodingItemTypeLengthIndices = 4     =>     stateIndexCount = 9
	//
	//        LL0   L0   0   0M           
	//   LLL  LL1   L1   1   1M
	//        LL2   L2   2   2M
	//        LL3   L3   3   3M
	//
	// where e.g. ... means            History 
	//                                 last                 before             oldest
	//        LLL                      Lit                  Lit                Lit
	//        LL2                      Lit                  Lit                Match with lengthIndex=1
	//        L0                       Lit                  Rep0S Match  
	//        1                        Match lengthIndex=0
	//        3M                       Match lengthIndex=2  Any Match 
	//
	public struct Last3CodingsStateIndex
	{
		private const int                typeCount                      = 4;
		private const int                stateIndexCountIsLiteral       = 4*SettingsFix.IsLiteralLengthIndices + 1;
		private const int                posMaskType                    = ((1<<SettingsFix.CodingItemTypePosBits)-1);
		private const int                posMaskIsLiteral               = ((1<<SettingsFix.IsLiteralPosBits)-1);
		private static readonly int[]    CodingItemStateToIndex4        = new int[CodingItemType.Count];


		public static Last3CodingsStateIndex Init = new Last3CodingsStateIndex(){ indexType = 0 };

		private int indexType;
		private int indexIsLiteral;


		static Last3CodingsStateIndex()
		{
			for ( int i=0 ; i<CodingItemStateToIndex4.Length ; i++ )
				CodingItemStateToIndex4[i] = -9999;
			CodingItemStateToIndex4[CodingItemType.Rep0] = CodingItemStateToIndex4[CodingItemType.Rep1] = CodingItemStateToIndex4[CodingItemType.Rep2] = CodingItemStateToIndex4[CodingItemType.Rep3] = 0;
			CodingItemStateToIndex4[CodingItemType.Hist]    = 1;
			CodingItemStateToIndex4[CodingItemType.ExpDist] = 2;
			CodingItemStateToIndex4[CodingItemType.Rep0S]   = 3;
		}


		public static int IndexCountType
		{
			get{ return 17; }
		}


		public static int IndexCountIsLiteral
		{
			get { return stateIndexCountIsLiteral*(1<<SettingsFix.IsLiteralPosBits); }
		}

		public int GetIndexType( int pos )
		{
			return (indexType<<SettingsFix.CodingItemTypePosBits) | (pos&posMaskType);
		}


		public int GetIndexIsLiteral( int pos )
		{
			return (indexIsLiteral<<SettingsFix.IsLiteralPosBits) | (pos&posMaskIsLiteral);
		}


		public Last3CodingsStateIndex CodeLiteral()
		{
			return new Last3CodingsStateIndex(){ indexType=(indexType>3*typeCount) ? (indexType-2*typeCount) : Math.Max( 0, indexType-typeCount ), indexIsLiteral=(indexIsLiteral>3*SettingsFix.IsLiteralLengthIndices) ? (indexIsLiteral-2*SettingsFix.IsLiteralLengthIndices) : Math.Max( 0, indexIsLiteral-SettingsFix.IsLiteralLengthIndices ) };
		}


		public Last3CodingsStateIndex CodeMatch( int type, int lengthIndex )
		{
			int idx = CodingItemStateToIndex4[type];
			lengthIndex = (type==CodingItemType.Rep0S) ? 0 : Math.Min(SettingsFix.IsLiteralLengthIndices-1,lengthIndex+1);
			return new Last3CodingsStateIndex() {  indexType= (indexType>2*typeCount) ? (3*typeCount+idx+1) : (2*typeCount+idx+1), indexIsLiteral=(indexIsLiteral>2*SettingsFix.IsLiteralLengthIndices) ? (3*SettingsFix.IsLiteralLengthIndices+lengthIndex+1) : (2*SettingsFix.IsLiteralLengthIndices+lengthIndex+1) };
		}


		public static bool operator==( Last3CodingsStateIndex a, Last3CodingsStateIndex b )
		{
			return a.indexType == b.indexType && a.indexIsLiteral==b.indexIsLiteral;
		}


		public static bool operator!=( Last3CodingsStateIndex a, Last3CodingsStateIndex b )
		{
			return a.indexType != b.indexType || a.indexIsLiteral!=b.indexIsLiteral;
		}


		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}


		public override int GetHashCode()
		{
			return indexType;
		}

	}
}
