using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public abstract class MyTaskCapOrProm : MyTaskTaBaRead
	{
		protected  Pieces             PiecesSrc, piecesDst;
		protected  Piece              PromPiece;
		protected  int                promPieceIndex     = -1;
		protected  int                firstCapIndex    = -1;
		protected  bool             sideSwitchNeeded = false;
		protected  TaBaRead         taBaReadDst;
		protected   PieceGroupReorder  PieceGroupReorderDst;
		protected   bool             wtmDst;          // wtm for dst position; including performed sideSwitch
		protected   Field            kDestWithoutMirror;
		protected   MirrorType       mirror;
		protected   WkBk             wkBkSrc;
		protected   WkBk             wkBkDst;



		public MyTaskCapOrProm( CalcTB calc, WkBk wkBkSrc, Pieces piecesSrc, bool wtm ) : base( calc, wtm )
		{
			this.wkBkSrc = this.wkBkDst = wkBkSrc;
			this.PiecesSrc   = piecesSrc;
		}

		protected virtual void Init()
		{
			sideSwitchNeeded = TaBasesWrite.IsDoubleTaBa( piecesDst );
			piecesDst          = sideSwitchNeeded ? piecesDst.SwitchSides() : piecesDst;
			wkBkDst          = sideSwitchNeeded ? wkBkDst.Reverse().Mirror(MirrorType.MirrorOnHorizontal) : wkBkDst;
			if ( sideSwitchNeeded ) {             // if true also btm
				if ( piecesDst.ContainsPawn )
					mirror = MirrorType.MirrorOnHorizontal | MirrorNormalize.WkBkToMirror( kDestWithoutMirror.Mirror(MirrorType.MirrorOnHorizontal), wkBkSrc.Wk.Mirror(MirrorType.MirrorOnHorizontal), piecesDst );
				else
					mirror = MirrorNormalize.WkBkToMirror( kDestWithoutMirror, wkBkSrc.Wk, piecesDst );   // mirrorType includes both steps 1. move 2. switch side
			}
			taBaReadDst = calc.TaBasesRead.GetTaBa( piecesDst );
			wtmDst = !wtm ^ sideSwitchNeeded;
			PieceGroupReorderDst = taBaReadDst.GetPieceGroupReordering( wtmDst );
		}

		public int FirstCapIndex
		{
			get { return firstCapIndex; }
		}

		public int PromPieceIndex
		{
			get { return promPieceIndex; }
		}

		public Pieces PiecesDst
		{
			get { return piecesDst; }
		}

		public Piece PromPieceType
		{
			get { return PromPiece; }
		}

		public bool SideSwitchNeeded
		{
			get { return sideSwitchNeeded; }
		}


		public Field KDestWithoutMirror
		{
			get { return kDestWithoutMirror; }
		}


		public MirrorType Mirror
		{
			get { return mirror; }
		}


		public WkBk WkBkSrc
		{
			get { return wkBkSrc; }
		}


		public bool WtmDst
		{
			get { return !wtm ^ sideSwitchNeeded; }
		}


		public override bool WtmTaBaRead
		{
			get { return WtmDst; }
		}


		public override Pieces PiecesTaBaRead
		{
			get { return piecesDst; }
		}


		public override WkBk WkBkTaBaRead
		{
			get { return wkBkDst; }
		}

		public WkBk WkBkDst
		{
			get{ return wkBkDst; }
		}

		public override bool IsCapOrProm
		{
			get { return true; }
		}


		public override string ToString()
		{
			string s = "Cap WkBkDst=" + wkBkDst.ToString() + "  PiecesDst=" + piecesDst.ToString() + base.ToString();
			return s;
		}




	}
}
