using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public sealed class Q : Piece
	{
		public Q() : base( new int[]{  7,  8,  9,  1, -1, -7, -8, -9 }, new int[]{ -1,  0,  1,  1, -1,  1,  0, -1 }, new int[]{  1,  1,  1,  0,  0, -1, -1, -1 }, null, null )
		{
			MvDeltaBits = 0x817f887887897779UL;
		}


		public override bool IsQ
		{
			get { return true; }
		}
		
		public override char AsCharacter
		{
			get { return 'Q'; }
		}

		public override int AsInt3
		{
			get { return 1; }
		}

		public override int AsInt2
		{
			get { return 0; }
		}

		public override bool MvHorVert
		{
			get {
				return true;
			}
		}

		public override bool MvDiag
		{
			get {
				return true;
			}
		}

		public override int MvBound
		{
			get { return 27; }
		}
	}
}
