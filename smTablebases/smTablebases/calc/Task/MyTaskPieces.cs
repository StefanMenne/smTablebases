using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public abstract class MyTaskPieces : MyTaskWtm
	{
		protected Pieces pieces;
		protected WkBk   wkBk;


		public MyTaskPieces( CalcTB calc, WkBk wkBk, Pieces pieces, bool wtm ) : base( calc, wtm )
		{
			this.pieces   = pieces;
			this.wkBk     = wkBk;
		}


		public WkBk WkBk
		{
			get {  return wkBk; }
		}

		public Pieces Pieces
		{
			get{ return pieces; }
		}
	}
}
