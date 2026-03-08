using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public sealed class N : Piece
	{
		public N() : base( new int[]{  17,  10,  -6, -15, -17, -10,  6, 15 }, new int[]{   1,   2,   2,   1,  -1,  -2, -2, -1 }, new int[]{   2,   1,  -1,  -2,  -2,  -1,  1,  2 }, null, null ) 
		{
			MvDeltaBits = 0x868a8f91767a716fUL;
		}

		public override bool IsN
		{
			get { return true; }
		}

		public override char AsCharacter
		{
			get { return 'N'; }
		}

		public override int AsInt3
		{
			get { return 4; }
		}

		public override int AsInt2
		{
			get { return 3; }
		}

		public override bool IsSingleStep
		{
			get {
				return true;
			}
		}

		public override int MvBound
		{
			get { return 8; }
		}

		public static bool IsMv( Field src, Field dst )
		{
			int dx = dst.X-src.X;
			int dy = dst.Y-src.Y;
			return Math.Abs(dx*dy) == 2;
		}

	}
}
