using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class MyTaskMtIllMvCnt : MyTaskPieces
	{
		private long        finalResCountCurrent, finalResToProcessCountCurrent;

		private DataChunkWrite data;


		public MyTaskMtIllMvCnt(CalcTB calc, WkBk wkBk, Pieces pieces, bool wtm) : base(calc, wkBk, pieces, wtm)
		{
		}


		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex)
		{
			data = calc.TaBasesWrite.GetDataChunk(wkBk, wtm, true, true );
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			bool          isWinOrDraw            = (wtm ? pieces.CountB : pieces.CountW) == 0;
			List<Move>      mv                    = new List<Move>();
			IndexPos      indexPos               = new IndexPos(wkBk, pieces, wtm);
			long          count                  = indexPos.IndexCount;
			FastBits      fastBits               = data.GetFastBits(count);

			MoveCounter moveCnt = new MoveCounter( pieces, wkBk, wtm );

			indexPos.SetToIndex( 0 );
			Fields f = indexPos.GetFields();
			FastBitsInterval fastBitsInterval = new FastBitsInterval(1,wtm);
#if !DEBUG
			data.SA_SetToFirst();
#endif

			for ( long i=0 ; i<count ; i++ ) {
				if ( indexPos.GetIsEp(i) ) {
#if !DEBUG
					data.SA_GetNextWithWrite();
#endif
					indexPos.IncIndex( ref f );
					finalResCountCurrent++;       // EP pos is assumed as final
					continue;
				}

#if DEBUG
				ResWithCount res = new ResWithCount(data.GetDebug(i,"MtIllMcCnt",null));
#else
				ResWithCount res = new ResWithCount( data.SA_GetNextWithWrite() );
#endif
				if ( isWinOrDraw )
					res = res.Combine(Res.Draw);

				if ( moveCnt.UpdatePos(f) ) {
					if ( res.IsWinOrDraw ) {
						// cap move results is win; therefore move count is not important so if pos is not illegal use 0

						finalResCountCurrent++;
						if ( !res.IsDraw ) {
							finalResToProcessCountCurrent++;
						}
						if ( !res.IsDraw && res.Res.WinIn<fastBitsInterval.WinInMaxPlus1 )
							fastBits.Set(i);
					}
					else {
						int mvCount = moveCnt.CalcMvCount(!res.IsInit);

						if ( mvCount == -1 ) {   // mate
							res = ResWithCount.IsMt;
							finalResCountCurrent++;
							finalResToProcessCountCurrent++;
							fastBits.Set(i);
						}
						else if ( mvCount == 0 ) {
							finalResCountCurrent++;
							finalResToProcessCountCurrent++;
							if ( res.IsInit ) {      // no capture move possible
								res = ResWithCount.StaleMt;
								finalResToProcessCountCurrent--;
							}
							else {    // final lose result
								// early loose res processing
								fastBits.Set( i );
							}
						}
						else { // moveCount != 0   ; lose
							res = res.SetMoveCount( mvCount );
						}
					}

				}
				else {
					res = ResWithCount.IllegalPos;
					finalResCountCurrent++;
				}
#if DEBUG
				data.SetDebug( indexPos, i, res.Value, "MtIllMvCnt", null, VerifyResType.VerifyFinals );
#else
				data.SA_SetCurrent( res.Value );
#endif

				indexPos.IncIndex( ref f );
			}

#if !DEBUG
			data.SA_Finish();
#endif
		}


		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			calc.TaBasesWrite.FreeDataChunk(data);
			TasksMtIllMvCnt.FinalResToProcessCount      += finalResToProcessCountCurrent;
			TasksMtIllMvCnt.FinalResCount               += finalResCountCurrent;
		}



		public override bool IsMtIllMvCnt
		{
			get { return true; }
		}
	}
}
