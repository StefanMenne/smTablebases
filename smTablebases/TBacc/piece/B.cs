using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public sealed class B : Piece
	{
		public B() : base( new int[]{  7,  9, -7, -9 }, new int[]{ -1,  1,  1, -1 }, new int[]{  1,  1, -1, -1 }, null, null ) 
		{
			MvDeltaBits = 0x87897779UL;
		}

		public override bool IsB
		{
			get { return true; }
		}
		
		public override char AsCharacter
		{
			get { return 'B'; }
		}

		public override int AsInt3
		{
			get { return 3; }
		}

		public override int AsInt2
		{
			get { return 2; }
		}

		public override bool MvDiag
		{
			get {
				return true;
			}
		}

		public override int MvBound
		{
			get { return 13; }
		}
	}
}
