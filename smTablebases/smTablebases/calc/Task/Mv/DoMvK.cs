using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class DoMvK : DoMvSingle
	{
		private   MirrorType        mirrorType;


		public DoMvK( FastBits fastBitsSrc, IndexPos indexPosSrc, DataChunkWrite dataSrc, int winInToGen, MirrorType mirrorType, Res lsResToGen, IndexPos indexPosDst, int[] updateSrcResWithLsIndexToInfo ) : base( indexPosSrc, dataSrc, fastBitsSrc, winInToGen, lsResToGen, indexPosDst, updateSrcResWithLsIndexToInfo )
		{
			this.mirrorType = mirrorType;
		}		


		public override void DoMvAndUpdateSrcResWithWin( Fields flds, bool wtm, BitBrd occFlds, Res resToCombine )
		{
			if ( mirrorType == MirrorType.None ) {
				if ( !indexPosSrc.SetSortedFields(flds) )
					return;
			}
			else {
				flds = flds.MirrorBack( mirrorType );
				if ( !indexPosSrc.SetFields(flds) )
					return;
			}
			srcResUpdater.UpdateSrcResWithWin( indexPosSrc.GetIndex(), wtm, resToCombine );
		}


		public override void DoMvAndUpdateSrcResWithLs( Fields flds, bool wtm, BitBrd occFlds )
		{
			if ( mirrorType == MirrorType.None ) {
				if ( !indexPosSrc.SetSortedFields(flds) )
					return;
			}
			else {
				flds = flds.MirrorBack( mirrorType );
				if ( !indexPosSrc.SetFields(flds) )
					return;
			}
			srcResUpdater.UpdateSrcResWithLs( indexPosSrc.GetIndex() );
		}


	}
}
