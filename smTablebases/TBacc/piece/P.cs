using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public abstract class P : Piece
	{
		private P()
		{}

		protected P( int[] delta, int[] deltaX, int[] deltaY, bool[] capMv, bool[] pawnTwoFieldMv ) : base( delta, deltaX, deltaY, capMv, pawnTwoFieldMv )
		{
		}

		public override bool IsP
		{
			get { return true; }
		}

		public override char AsCharacter
		{
			get { return 'P'; }
		}

		public override int AsInt3
		{
			get { return 5; }
		}

		public override bool IsSingleStep
		{
			get {
				return true;
			}
		}

		public override int MvBound
		{
			get { return 2; }
		}
	}
}
