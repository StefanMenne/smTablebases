using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public struct WkBkMvInfo
	{
		private WkBk         wkBkSrc;
		private WkBk         wkBkDst;
		private Field        kDestWithoutMirror;
		private MirrorType   mirror;

		public WkBkMvInfo( WkBk wkBkSrc, bool wtm, Field kDestWithoutMirror )
		{
			Field wkNew                = wtm?kDestWithoutMirror:wkBkSrc.Wk;
			Field bkNew                = wtm?wkBkSrc.Bk:kDestWithoutMirror;

			this.wkBkSrc               = wkBkSrc;
			this.wkBkDst               = new WkBk( wkNew, bkNew, wkBkSrc.Pawn );
			this.kDestWithoutMirror    = kDestWithoutMirror;
			this.mirror                = MirrorNormalize.WkBkToMirror( wkNew, bkNew, wkBkSrc.Pawn );
		}


		public WkBk WkBkSrc
		{
			get{ return wkBkSrc; }
		}

		public WkBk WkBkDst
		{
			get{ return wkBkDst; }
		}

		public MirrorType Mirror
		{
			get{ return mirror; }
		}

		public Field KDestWithoutMirror
		{
			get{ return kDestWithoutMirror; }
		}
	}
}
