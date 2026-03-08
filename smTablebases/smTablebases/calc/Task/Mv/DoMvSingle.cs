using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public abstract class DoMvSingle : DoMvBase
	{
		protected   SrcResUpdater     srcResUpdater;


		public DoMvSingle( IndexPos indexPosSrc, DataChunkWrite dataSrc, FastBits fastBitsSrc, int winInToGen, Res lsResToGen, IndexPos indexPosDst, int[] updateSrcResWithLsIndexToInfo ) : base( indexPosSrc, winInToGen, lsResToGen )
		{
			srcResUpdater    = new SrcResUpdater( indexPosSrc, fastBitsSrc, dataSrc, lsResToGen, indexPosDst, updateSrcResWithLsIndexToInfo );
		}


		public override long FinalResCount
		{
			get { return srcResUpdater.FinalResCount; }
		}


		public override long FinalResToProcessCount
		{
			get { return srcResUpdater.FinalResToProcessCount; }
		}
	}
}
