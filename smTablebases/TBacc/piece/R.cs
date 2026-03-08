using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public sealed class R : Piece
	{
		public R() : base( new int[]{  8,  1, -1, -8 }, new int[]{  0,  1, -1,  0 }, new int[]{  1,  0,  0, -1 }, null, null )
		{
			MvDeltaBits = 0x817f8878UL;
		}

		public override bool IsR
		{
			get { return true; }
		}
		
		public override char AsCharacter
		{
			get { return 'R'; }
		}

		public override int AsInt3
		{
			get { return 2; }
		}

		public override int AsInt2
		{
			get { return 1; }
		}

		public override bool MvHorVert
		{
			get {
				return true;
			}
		}

		public override int MvBound
		{
			get { return 14; }
		}
	}
}
