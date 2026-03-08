using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	/// <summary>
	/// Three things are done:
	///
	/// - Set FastBits for the next interval
	///    (indicates weather a position needs to processed or simply can be skipped)
	///
	/// - remove unused indices from ResCountConverter
	///
	/// - Unknown/not finalized losing results with low "LooseIn" can be replaced with Unknown-Init-Res.
	///   Because when the move counter is decremented next time the result will get better than current so it will be overwritten.
	///
	/// </summary>
	public class TasksOptimize : Tasks
	{
#if DEBUG
		public static long CountEp, CountNo, CountStMt, CountWi, CountWiProcessed, CountLsFin, CountLsFinProcessed, CountLsUnk, CountRem, CountIll, CountInit;
#endif

		public  int[] IndexInfoWtm, IndexInfoBtm;
		private int[] mvCountToResIndexWtm, mvCountToResIndexBtm;
		private Step step;

		public TasksOptimize( CalcTB calc, Step step ) : base( calc )
		{
			this.step = step;
		}

		public Step Step => step;

		public override MyTask[] Init( int threadCount )
		{
#if DEBUG
			CountNo = CountStMt = CountWi = CountWiProcessed = CountLsFin = CountLsFinProcessed = CountLsUnk = CountRem = CountIll = CountInit = CountEp = 0L;
#endif
			TaBaWrite   taBaWrite       = calcTB.TaBasesWrite.TaBaWrite;
			List<MyTask> list = new List<MyTask>();
			foreach ( bool wtm in Tools.BoolArray )
				for ( WkBk wkBk = WkBk.First(calcTB.Pieces) ; wkBk<wkBk.Count ; wkBk++ )
					list.Add( new MyTaskOptimize( calcTB, wkBk, calcTB.Pieces, wtm) );
			NumerizeSteps(list);


			IndexInfoWtm = GenerateIndexInfo( taBaWrite, true,  out mvCountToResIndexWtm );
			IndexInfoBtm = GenerateIndexInfo( taBaWrite, false, out mvCountToResIndexBtm );

			return tasks = list.ToArray();
		}



		private int[] GenerateIndexInfo( TaBaWrite taBaWrite, bool wtm, out int[] mvCountToResIndex )
		{
			int lastProcessedLsIn = step.WinResToGen.HalfMoveToMate.LsIn;
			ResCountConvert        resCountConvert       = taBaWrite.GetResCountConvert( wtm );
			FastBitsInterval       fastBitsInterval         = new FastBitsInterval( step.PassIndex+1, wtm );

			mvCountToResIndex = new int[calcTB.Pieces.GetMvCountBound(wtm)+1];
			for ( int i=0 ; i<mvCountToResIndex.Length ; i++ ) {
				ResWithCount resWithCount = new ResWithCount( i, Res.Init );
				mvCountToResIndex[i] = resCountConvert.ValueToIndex( resWithCount.Value );
			}

			int[] indexInfo = new int[resCountConvert.CurrentlyUsedIndexBound+1];
			for ( int i=0 ; i<resCountConvert.Count ; i++ ) {
				int index, value;
				resCountConvert.Get( i, out index, out value );
				ResWithCount resWithCount = new ResWithCount( value );
				Res          res          = resWithCount.Res;

				// Init with moveCount=0 is possible; it is used in Init step but should be eliminated inside MtIllMvCount
				if ( (res.IsInit && resWithCount.IsUnknown) || (res.IsLs && resWithCount.IsUnknown && resWithCount.Res.LsIn<=lastProcessedLsIn)  ) {
					int moveCount = resWithCount.MoveCount;

					if ( mvCountToResIndex[moveCount] == -1 )
						mvCountToResIndex[moveCount] = index;

					if ( mvCountToResIndex[moveCount] == index )
						indexInfo[index] = -2;
					else
						indexInfo[index] = mvCountToResIndex[moveCount];
				}
				else if ( res.IsWin && res.WinIn>=fastBitsInterval.WinInMin && res.WinIn<fastBitsInterval.WinInMaxPlus1 ) {
					indexInfo[index] = -4;           // -4:  Set fastBit  and "not yet found"
				}
				else {
					indexInfo[index] = -2;           // -2: "not yet found"
				}
			}

			return indexInfo;
		}




		public override void FinishedAllTasks( bool aborted )
		{
			if ( aborted )
				return;
#if DEBUG
			long countFinal     = TasksOptimize.CountEp + TasksOptimize.CountIll + TasksOptimize.CountLsFin + TasksOptimize.CountRem + TasksOptimize.CountStMt + TasksOptimize.CountWi;
			long countToProcess = TasksOptimize.CountWi - TasksOptimize.CountWiProcessed + TasksOptimize.CountLsFin - TasksOptimize.CountLsFinProcessed;

			if ( !Calc.Abort && (countFinal != calcTB.TaBasesWrite.TaBaWrite.FinalResCount || countToProcess != calcTB.TaBasesWrite.TaBaWrite.FinalResToProcessCount ) )
				throw new Exception( "Final count not OK" );
#endif

			calcTB.TaBasesWrite.TaBaWrite.ResCountConvertWtm = CreateNewResCountConvertMaster( calcTB.TaBasesWrite.TaBaWrite.ResCountConvertWtm, IndexInfoWtm, mvCountToResIndexWtm );
			calcTB.TaBasesWrite.TaBaWrite.ResCountConvertBtm = CreateNewResCountConvertMaster( calcTB.TaBasesWrite.TaBaWrite.ResCountConvertBtm, IndexInfoBtm, mvCountToResIndexBtm );
		}



		/// <summary>
		/// ThreadSafety: Only called Single Threaded
		/// </summary>
		private ResCountConvert CreateNewResCountConvertMaster( ResCountConvert oldMaster, int[] indexInfo, int[] mvCountToResIndex )
		{
			for ( int i=0 ; i<indexInfo.Length ; i++ ) {
				if ( indexInfo[i] >= 0 )
					indexInfo[i] = -2;
				else if ( indexInfo[i] == -4 )
					indexInfo[i] = -2;
				else if ( indexInfo[i] == -3 )
					indexInfo[i] = -1;
			}

			for ( int i=0 ; i<mvCountToResIndex.Length ; i++ ) {
				int index = mvCountToResIndex[i];
				if ( index != -1 ) {
					ResWithCount resWithCount = new ResWithCount( oldMaster.IndexToValue(index) );
					Res          res          = resWithCount.Res;
					if ( !res.IsInit && indexInfo[index] != -2 ) {
						// if Init resul was available it was used; so we know no InitRes is available;
						// create it now; do only if at least one result was found;
						// otherwise just leave the -2 to remove the entry
                        resWithCount = resWithCount.SetRes( Res.Init );
						int newValue = resWithCount.Value;
						indexInfo[index] = newValue;
					}
				}
			}

			return new ResCountConvert( oldMaster, indexInfo );
		}
	}
}
