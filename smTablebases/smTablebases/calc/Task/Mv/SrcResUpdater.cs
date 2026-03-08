using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class SrcResUpdater
	{
		public  long                  FinalResAlreadyProcessedCount, FinalResToProcessCount;
		private DataChunkWrite        dataSrc;
		private FastBits              fastBitsSrc;
		private IndexPos              indexPosSrc;
		private int                   lsInToGen;
		private Res                   lsResToGen;
#if DEBUG
		public  IndexPos              indexPosDst;
#endif
		private int[]                 updateSrcResWithLsIndexToInfo;


		public static int  FastBitsIntervalMaxLsInPlus1;
		public static int  FastBitsIntervalMaxWinInPlus1;


		public SrcResUpdater( IndexPos indexPosSrc, FastBits fastBitsSrc, DataChunkWrite dataSrc, Res lsResToGen, IndexPos indexPosDst, int[] updateSrcResWithLsIndexToInfo )
		{
			this.indexPosSrc                               = indexPosSrc;
			this.dataSrc                                   = dataSrc;
			this.fastBitsSrc                               = fastBitsSrc;
			this.lsResToGen                                = lsResToGen;
			this.lsInToGen                                 = lsResToGen.LsIn;
			this.updateSrcResWithLsIndexToInfo             = updateSrcResWithLsIndexToInfo;
#if DEBUG
			this.indexPosDst                               = indexPosDst;
#endif
		}


		/// <summary>
		/// DstPos is lose. So update all results with win result.
		/// </summary>
		public void UpdateSrcResWithWin( long indexSrc, bool wtm, Res resToCombine )
		{
#if DEBUG
			ResWithCount resWithCountSrc = new ResWithCount( dataSrc.GetDebug(indexSrc,"UpdateSrcResWithWin", null ) );  // result for pos before move
#else
			ResWithCount resWithCountSrc = new ResWithCount( dataSrc.Get(indexSrc) );  // result for pos before move
#endif

			Res          resSrc          = resWithCountSrc.Res;
			// resToCombine is win with WinIn >= winResToGen.WinIn

			if ( !resSrc.IsIllegalPos && resSrc>resToCombine ) {
				// resSrc is lose, draw or win with higher dtm than resToCombine

				if ( resWithCountSrc.IsUnknown )
					FinalResToProcessCount++;
				else if ( resSrc.IsDraw ) {
					FinalResToProcessCount++;
					FinalResAlreadyProcessedCount--;
				}


				resWithCountSrc = new ResWithCount( resToCombine );

#if DEBUG
				if ( Debug.TrackPosition ) {
					indexPosSrc.SetToIndex( indexSrc );
					dataSrc.SetDebug( indexPosSrc, indexSrc, resWithCountSrc.Value, "Mv", indexPosDst, VerifyResType.VerifyFinals );
				}
				else
					dataSrc.Set( indexSrc, resWithCountSrc.Value );
#else
				dataSrc.Set( indexSrc, resWithCountSrc.Value );
#endif

				if ( resToCombine.WinIn<FastBitsIntervalMaxWinInPlus1 )
					fastBitsSrc.Set( indexSrc );
			}
		}


		public void UpdateSrcResWithLs( long indexSrc )
		{
			int resWithCountIndex = dataSrc.GetResWithCountIndex( indexSrc );
			int info              = updateSrcResWithLsIndexToInfo[resWithCountIndex];

			if ( info != -2 ) {    // -2 means resSrc is win and no update necessary
				if ( info == -1 ) {   // not yet calculated; calc now; this cannot be moved outside due src res might be updated twice
					// resToCombine = fixed lsResToGen
					ResWithCount resWithCountSrc = new ResWithCount( dataSrc.ResCountConvert.IndexToValue(resWithCountIndex) );  // result for pos before move
					Res          resSrc          = resWithCountSrc.Res;

					if ( resSrc.IsLsOrInit ) {
						info = 0;
						int moveCount = resWithCountSrc.MoveCount;
						if ( moveCount == 1 ) {
							resSrc = resSrc.Combine( lsResToGen );
							info |= 0x10000000;                 // resSrc.IsLsOrInit and moveCount==1
							resWithCountSrc = new ResWithCount( resSrc );
						}
						else {
							if ( resSrc.LsIn>lsInToGen )  // resSrc.LsIn returns -1 for Init
								resWithCountSrc = new ResWithCount( moveCount-1, resSrc ); // lose later than lsResToGen
							else
								resWithCountSrc = new ResWithCount( moveCount-1, Res.Init ); // move count is !=0 and next decrement will give better res; so it can be set to any lose value with lsIn<=lsResToGen.LsIn
						}
						info |= dataSrc.ResCountConvert.ValueToIndexAdd( resWithCountSrc.Value );
						updateSrcResWithLsIndexToInfo[resWithCountIndex] = info;
					}
					else {
						updateSrcResWithLsIndexToInfo[resWithCountIndex] = -2;
						return;
					}
				}

				if ( (info & 0x10000000) == 0x10000000 ) {   // resSrc.IsLsOrInit and move count==1
					FinalResToProcessCount++;
					fastBitsSrc.Set( indexSrc );
				}

#if DEBUG
				if ( Debug.TrackPosition ) {
					ResWithCount resWithCountSrcNew = new ResWithCount( dataSrc.ResCountConvert.IndexToValue(info&0xfffffff) );
					indexPosSrc.SetToIndex( indexSrc );
					dataSrc.SetDebug( indexPosSrc, indexSrc, resWithCountSrcNew.Value, "Mv", indexPosDst, VerifyResType.VerifyFinals );
				}
				else
					dataSrc.SetResWithCountIndex( indexSrc, (info&0xfffffff) );
#else
				dataSrc.SetResWithCountIndex( indexSrc, (info&0xfffffff) );
#endif
			}
		}


		public long FinalResCount
		{
			get { return FinalResAlreadyProcessedCount + FinalResToProcessCount; }
		}


	}
}
