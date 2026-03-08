﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LC
{


	public struct DistLength
	{
		private const long DistMask       = 0x0000fffffff;   // 28      needed are 24 + 3(for virtual dists)
		private const long LengthMask     = 0x0ff00000000;
		private const long TypeMask       = 0x00070000000;
		private const int  FirstLengthBit = 32; 
		private const int  FirstTypeBit   = 28;
		private const long IsLiteralBits  = ((long)CodingItemType.Literal)<<FirstTypeBit;
		private const long IsRep0SBits    = ((long)CodingItemType.Rep0S)<<FirstTypeBit;
		private const long IsExpDistBits  = ((long)CodingItemType.ExpDist)<<FirstTypeBit;
		private const long IsHistBits     = ((long)CodingItemType.Hist)<<FirstTypeBit;


		public static DistLength Literal = new DistLength(){ value=IsLiteralBits };
		public static DistLength Rep0S   = new DistLength(){ value=IsRep0SBits   };

		private long value;


		public DistLength( int dist, int lengthIndex, int type )
		{
			value = (long) ( ((long)lengthIndex) << FirstLengthBit | (((long)type)<<FirstTypeBit) | (uint)dist );
		}


		public static DistLength Rep0SWithDist( int dist )
		{
			return new DistLength( dist, 0, CodingItemType.Rep0S );
		}


		public static DistLength ExpDist( int dist, int lengthIndex )
		{
			return new DistLength( dist, lengthIndex, CodingItemType.ExpDist );
		}


		public int Dist
		{
			get { return (int)(value & DistMask); }
		}


		public int LengthIndex
		{
			get { return (int)(value >> FirstLengthBit); }
		}


		public bool IsLiteral
		{
			get { return (value & TypeMask) == IsLiteralBits; }
		}


		public bool IsRep0S
		{
			get { return (value & TypeMask) == IsRep0SBits; }
		}


		public bool IsHist
		{
			get { return (value & TypeMask) == IsHistBits; }
		}


		public bool IsExpDist
		{
			get { return (value & TypeMask) == IsExpDistBits; }
		}


		public int Type
		{
			get { return (int)((value&TypeMask)>>FirstTypeBit); }
		}


		public bool IsLiteralOrRep0S
		{
			get { return IsLiteral || IsRep0S; }
		}


		public int GetDelta( LengthInfo lengthInfo )
		{
			return IsLiteralOrRep0S ? 1 : lengthInfo.IndexToLength(LengthIndex);
		}


		public override string ToString()
		{
			if ( IsLiteral )
				return "Literal";
			else if ( IsRep0S )
				return "Rep0S Dist=" + Dist.ToString();
			else if ( CodingItemType.IsRep( Type ) )
				return "Rep" + Type.ToString() + "Dist=" + Dist.ToString() + "  LengthIndex=" + LengthIndex.ToString();	
			else if ( IsHist )
				return "His Dist=" + Dist.ToString() + "  LengthIndex=" + LengthIndex.ToString();
			else if ( IsExpDist )
				return "ExpDist Dist=" + Dist.ToString() + "  LengthIndex=" + LengthIndex.ToString();
			else
				throw new Exception();
		}
	}
}
