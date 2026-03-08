using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public sealed class K : Piece
	{
		public K() : base( new int[]{  7,  8,  9,  1, -1, -7, -8, -9 }, new int[]{ -1,  0,  1,  1, -1,  1,  0, -1 }, new int[]{  1,  1,  1,  0,  0, -1, -1, -1 }, null, null )
		{
		}

		public override bool IsK
		{
			get {
				return true;
			}
		}

		public override char AsCharacter
		{
			get { return 'K'; }
		}

		public override int AsInt3
		{
			get { return 0; }
		}

		public override int AsInt2
		{
			get { throw new Exception(); }
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
	}
}
