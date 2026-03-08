using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	/// <summary>
	///
	///  Example for ResCountConvert update:
	///
	/// Indices    Res(cnt)    containedInDataChunks    indexInfo(Start;End)
	///
	/// 3          -4(5)         true                   -2   ->   -1
	/// 10         -6(5)         false                  3
	/// 14         -8(5)         true                   3
	///
	/// 2          -4(7)         true                   39
	/// 39         Init(7)       false                  -2    ->  -1
	/// 5          -6(7)         true                   39
	/// 6          -9(7)         false                  39
	///
	/// 27         7             false                  -2
	///
	/// 8          Inv           true                   -2    ->  -1
	///
	/// 33         -2(2)         false                   36
	/// 36         Init(2)       false                   -2
	/// 44         -8(2)         false                   36
	///
	/// </summary>
	public class MyTaskOptimize : MyTaskPieces
	{
		private        DataChunkWrite        data;
#if DEBUG
		private long                         countEpCurrent, countNoCur, countStMtCur, countWiCur, counWiProcessedCur, countLsFinCur, countLsFinProcessedCur, countLsUnkCur, countRemCur, countIllCur, countInitCur;
#endif

		public MyTaskOptimize( CalcTB calc, WkBk wkBk, Pieces pieces, bool wtm ) : base( calc, wkBk, pieces, wtm )
		{
		}




		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			data = calc.TaBasesWrite.GetDataChunk( wkBk, wtm, true, true );
#if DEBUG
			countNoCur = countStMtCur = countWiCur = counWiProcessedCur = countLsFinCur = countLsFinProcessedCur = countLsUnkCur = countRemCur = countIllCur = countInitCur = countEpCurrent = 0;
#endif
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			TasksOptimize          tasksOptimize            = (TasksOptimize)tasks;
			IndexPos               indexPos                 = new IndexPos( wkBk, pieces, wtm );
			long                   count                    = indexPos.IndexCount;
			ResCountConvert        resCountConvert          = data.ResCountConvert;
			FastBits               fastBits                 = data.GetFastBits( count );
			int                    lastProcessedWiIn        = tasksOptimize.Step.GetLsResToGen(!wtm).HalfMoveToMate.WinIn;
			int                    lastProcessedLsIn        = tasksOptimize.Step.WinResToGen.HalfMoveToMate.LsIn;
			int[]                  indexInfo                = wtm ? tasksOptimize.IndexInfoWtm : tasksOptimize.IndexInfoBtm;

			data.SA_SetToFirst();
			for ( long i=0 ; i<count ; i++ ) {
				int index = data.SA_GetNextIndexWithWrite();


#if DEBUG
				int val = resCountConvert.IndexToValue(index);
				ResWithCount resWithCount = new ResWithCount(val);
				Res res = resWithCount.Res;
				//
				// Final Count Verify
				//
				if ( indexPos.GetIsEp(i) )
					countEpCurrent++;
				else {
					if ( fastBits.Get(i) && res.IsWin )   // all fast bits should be set to 0 after processing them
						throw new Exception();

					if ( res.IsWin ) {
						countWiCur++;
						if ( res.WinIn <= lastProcessedWiIn )
							counWiProcessedCur++;
					}
					else if ( res.IsLs ) {
						if ( resWithCount.IsUnknown )
							countLsUnkCur++;
						else {
							countLsFinCur++;
							// early lose res processing
							if ( !fastBits.Get(i) )
								countLsFinProcessedCur++;
						}
					}
					else if ( res.IsDraw )
						countRemCur++;
					else if ( res.IsIllegalPos )
						countIllCur++;
					else if ( res.IsInit )
						countInitCur++;
					else if ( res.IsNo )
						countNoCur++;
					else if ( res.IsStMt )
						countStMtCur++;
					else
						throw new Exception();
				}
#else
#endif



				int info = indexInfo[index];
				if ( info != -1 ) {                // -1 = already found; no index replacement
					if ( info == -2 )              // -2 = not found yet; no index replacement
						indexInfo[index] = -1;
					else if ( info == -3 || info == -4 ) {   // fastBits handling
						if ( !indexPos.GetIsEp(i) ) {
							fastBits.Set( i );
						}
						indexInfo[index] = -3;                 // mark as found
					}
					else {                                     // indexReplacement
						if ( indexInfo[info] == -2 )
							indexInfo[info] = -1;
						data.SA_SetCurrentIndex( info );          // store value maybe with new index; hope that some indices are no more used
					}
				}
			}


			data.SA_Finish();
		}

		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
#if DEBUG
			TasksOptimize.CountIll            += countIllCur;
			TasksOptimize.CountInit           += countInitCur;
			TasksOptimize.CountLsFin          += countLsFinCur;
			TasksOptimize.CountLsUnk          += countLsUnkCur;
			TasksOptimize.CountRem            += countRemCur;
			TasksOptimize.CountWi             += countWiCur;
			TasksOptimize.CountStMt           += countStMtCur;
			TasksOptimize.CountNo             += countNoCur;
			TasksOptimize.CountWiProcessed    += counWiProcessedCur;
			TasksOptimize.CountLsFinProcessed += countLsFinProcessedCur;
			TasksOptimize.CountEp             += countEpCurrent;
#endif
			calc.TaBasesWrite.FreeDataChunk( data );
		}



	}
}
