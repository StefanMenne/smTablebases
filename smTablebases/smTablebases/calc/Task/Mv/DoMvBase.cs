using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public abstract class DoMvBase
	{
		protected int                   winInToGen;
		protected int                   lsInToGen;
		protected Res                   lsResToGen;
		protected IndexPos              indexPosSrc;

		public DoMvBase( IndexPos indexPosSrc, int winInToGen, Res lsResToGen )
		{
			this.indexPosSrc                               = indexPosSrc;
			this.winInToGen                                = winInToGen;
			this.lsResToGen                                = lsResToGen;
			this.lsInToGen                                 = lsResToGen.LsIn;
		}
			

		public abstract void DoMvAndUpdateSrcResWithWin( Fields flds, bool wtm, BitBrd occFlds, Res resToCombine );
		public abstract void DoMvAndUpdateSrcResWithLs( Fields flds, bool wtm, BitBrd occFlds );
		public abstract long FinalResToProcessCount {    get;   }
		public abstract long FinalResCount {             get;   }


	}
}
