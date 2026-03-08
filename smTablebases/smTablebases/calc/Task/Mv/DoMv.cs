using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class DoMv : DoMvSingle
	{
		private  long[]                mv;


		public DoMv( FastBits fastBitsSrc, IndexPos indexPosSrc, DataChunkWrite dataSrc, int winInToGen, Res lsResToGen, IndexPos indexPosDst, int[] updateSrcResWithLsIndexToInfo ) : base( indexPosSrc, dataSrc, fastBitsSrc, winInToGen, lsResToGen, indexPosDst, updateSrcResWithLsIndexToInfo )
		{
			mv               = new long[indexPosSrc.GetMvCountBound()];
		}


		public override void DoMvAndUpdateSrcResWithWin( Fields flds, bool wtm, BitBrd occFlds, Res resToCombine )
		{
			srcResUpdater.FinalResToProcessCount--;
			srcResUpdater.FinalResAlreadyProcessedCount++;
			for ( int pieceGrpIdx=indexPosSrc.FirstPieceGrpIdxToMv ; pieceGrpIdx<indexPosSrc.LastPieceGrpIdxToMvPlus1 ; pieceGrpIdx++ ) {
				int mvCount = indexPosSrc.GetBackMvNoCapDestIndex( flds, mv, wtm, pieceGrpIdx, occFlds );
				for ( int j=0 ; j<mvCount ; j++ ) {
					srcResUpdater.UpdateSrcResWithWin( mv[j], wtm, resToCombine );
				}
			}
		}


		public override void DoMvAndUpdateSrcResWithLs( Fields flds, bool wtm, BitBrd occFlds )
		{
			srcResUpdater.FinalResToProcessCount--;
			srcResUpdater.FinalResAlreadyProcessedCount++;
			for ( int pieceGrpIdx=indexPosSrc.FirstPieceGrpIdxToMv ; pieceGrpIdx<indexPosSrc.LastPieceGrpIdxToMvPlus1 ; pieceGrpIdx++ ) {
				int mvCount = indexPosSrc.GetBackMvNoCapDestIndex( flds, mv, wtm, pieceGrpIdx, occFlds );
				for ( int j=0 ; j<mvCount ; j++ ) {
					srcResUpdater.UpdateSrcResWithLs( mv[j] );
				}
			}
		}


	}
}
