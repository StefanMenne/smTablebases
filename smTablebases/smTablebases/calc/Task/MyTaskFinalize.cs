using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;
using System.Threading;

namespace smTablebases
{
	public sealed class MyTaskFinalize : MyTaskPieces
	{
		public  long[]         ResCount     = new long[3];   // [0] win;  [1] ls;  [2] draw
		public  Pos?           MaxWiPos     = null;
		public  Pos?           MaxLsPos     = null;
		public  int            MaxWiInHm    = 0;
		public  int            MaxLsInHm    = 0;

		private DataChunkWrite data;
		private int[]          resIndexToInfoTable;


		public MyTaskFinalize( CalcTB calc, WkBk wkBk, Pieces pieces, bool wtm, int[] resIndexToInfoTable ) : base( calc, wkBk, pieces, wtm )
		{
			this.resIndexToInfoTable = resIndexToInfoTable;
		}


		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			data = calc.TaBasesWrite.GetDataChunk( wkBk, wtm, true, false );
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			TasksFinalize      tasksFinalize = (TasksFinalize)tasks;
			IndexPos           indexPos      = new IndexPos( wkBk, pieces, wtm );
			int                resWithCountDrawIndex = tasksFinalize.GetResWithCountDrawIndex( wtm );


			long count = indexPos.IndexCount;
			data.SA_SetToFirst();

			for ( long i=0 ; i<count ; i++ ) {
#if DEBUG
				int resIndex = data.SA_GetNextIndexWithWrite_DEBUG(  i, "Finish", null );
#else
				int resIndex = data.SA_GetNextIndexWithWrite();
#endif
				int info     = resIndexToInfoTable[resIndex];


				if ( info <= 2 ) {           // info = 0, 1, 2     only count win, ls, draw
					ResCount[info]++;
				}
				else if ( info == 3 ) {      // info = 3           res is stale mate or unknown and has to be changed to draw
#if DEBUG
					indexPos.SetToIndex( i );
					data.SA_SetCurrentIndex_DEBUG( indexPos, i, resWithCountDrawIndex, "Finalize", null );
#else
					data.SA_SetCurrentIndex( resWithCountDrawIndex );
#endif
					ResCount[2]++;
				}
				else if ( info == 4 ) {      // info = 4           res first time found afterwards set to 0 or 1
					ResWithCount rwc = new ResWithCount( data.Get( i ) );
					indexPos.SetToIndex( index );
					if ( rwc.IsWin ) {
						if ( rwc.Res.WinInHalfMv>MaxWiInHm ) {
							MaxWiPos = Pos.FromIndexPos( indexPos );
							MaxWiInHm = rwc.Res.WinInHalfMv;
						}
						ResCount[0] ++;
						resIndexToInfoTable[resIndex] = 0;        // maybe will be set from several threads the same time
					}
					else if ( rwc.IsLose ) {
						if ( rwc.Res.LsInHalfMv>MaxLsInHm ) {
							MaxLsPos = Pos.FromIndexPos( indexPos );
							MaxLsInHm = rwc.Res.LsInHalfMv;
						}
						ResCount[1] ++;
						resIndexToInfoTable[resIndex] = 1;       // maybe will be set from several threads the same time
					}
				}

#if DEBUG
				else if ( info == 6 )              // Res = Init with move count = 0;
					throw new Exception();
#endif
			}
			data.SA_Finish();
		}


		public override void FinishCalcWithoutThreading(Tasks tasks)
		{
			calc.TaBasesWrite.FreeDataChunk( data );
		}
	}
}
