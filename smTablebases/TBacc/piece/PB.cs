using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public sealed class PB : P
	{
		public PB() : base( new int[]{ -8, -16, -7, -9 }, new int[]{ 0, 0, 1, -1 }, new int[]{ -1, -2, -1, -1 }, new bool[]{  false, false,  true,  true }, new bool[]{  false,  true, false, false } )
		{
		}

		public override int AsInt2
		{
			get { return 5; }
		}

		public override bool IsPB
		{
			get {	return true; }
		}

	}
}
