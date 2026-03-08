using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class DoMvAll : DoMvBase
	{
		private DoMv       doMv;
		private DoMvK[]    doMvK;


		public DoMvAll( FastBits fastBitsSrc, IndexPos indexPosSrc, DataChunkWrite dataSrc, int winInToGen, WkBkMvInfo[] moveInfo, FastBits[] fastBitsSrcMvK, IndexPos[] indexPosSrcMvK, DataChunkWrite[] dataSrcMvK, Res lsResToGen, IndexPos indexPosDst, int[] updateSrcResWithLsIndexToInfo ) : base( indexPosSrc, winInToGen, lsResToGen )
		{
			doMv  = new DoMv( fastBitsSrc, indexPosSrc, dataSrc, winInToGen, lsResToGen, indexPosDst, updateSrcResWithLsIndexToInfo );
			doMvK = new DoMvK[moveInfo.Length];
			for ( int i=0 ; i<doMvK.Length ; i++ )
				doMvK[i] = new DoMvK( fastBitsSrcMvK[i], indexPosSrcMvK[i], dataSrcMvK[i], winInToGen, moveInfo[i].Mirror, lsResToGen, indexPosDst, updateSrcResWithLsIndexToInfo );
		}		


		public override void DoMvAndUpdateSrcResWithWin( Fields flds, bool wtm, BitBrd occFlds, Res resToCombine )
		{
			doMv.DoMvAndUpdateSrcResWithWin( flds, wtm, occFlds, resToCombine );
			for ( int i=0 ; i<doMvK.Length ; i++ )
				doMvK[i].DoMvAndUpdateSrcResWithWin( flds, wtm, occFlds, resToCombine );
		}


		public override void DoMvAndUpdateSrcResWithLs( Fields flds, bool wtm, BitBrd occFlds )
		{
			doMv.DoMvAndUpdateSrcResWithLs( flds, wtm, occFlds );
			for ( int i=0 ; i<doMvK.Length ; i++ )
				doMvK[i].DoMvAndUpdateSrcResWithLs( flds, wtm, occFlds );
		}


		public override long FinalResCount
		{
			get {
				long count = doMv.FinalResCount;
				for ( int i=0 ; i<doMvK.Length ; i++ )
					count += doMvK[i].FinalResCount;
				return count;
			}
		}


		public override long FinalResToProcessCount
		{
			get {
				long count = doMv.FinalResToProcessCount;
				for ( int i=0 ; i<doMvK.Length ; i++ )
					count += doMvK[i].FinalResToProcessCount;
				return count;
			}
		}

	}
}
