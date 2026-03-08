using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class TaskTaBaReadInfo
	{
		public Pieces     Pieces;
		public int        BlockIndex;
		public int        FirstTaskIndex;
		public int        LastTaskIndexP1;
		public int        UnfinishedTaskCount;
	}


	public abstract class TasksTaBaRead : Tasks
	{
		private TaskTaBaReadInfo[] taskIndexToInfo;
		private int                taskTaBaReadInfoCount;
		private TaBasesRead        taBasesRead;


		public TasksTaBaRead( CalcTB calc ) : base( calc )
		{
		}


		public void GenerateInfo( MyTask[] t, int threadCount )
		{
			List<MyTask> newList = new List<MyTask>();

			taBasesRead       = this is TasksMd5 ? ((TasksMd5)this).TaBasesRead : calcTB.TaBasesRead;
			taskIndexToInfo   = new TaskTaBaReadInfo[t.Length];
			taBasesRead.ExpliciteFreeOfTmpBlockStorage = threadCount>1;


			TaBaRead            taBaRead                        = null;
			int                 taBaReadPiecesIndex             = -1;
			int                 currentBlockIndex               = -1;
			TaskTaBaReadInfo    taskTaBaReadInfo                = null;

#if DEBUG
			BlockSplitterRead.ActivateVerifyingDoubleLoadingOfBlocks();
#endif

			for ( int i=0 ; i<t.Length ; i++ ) {
				MyTaskTaBaRead  tsk   = (MyTaskTaBaRead)t[i];
				Pieces        pieces  = tsk.PiecesTaBaRead;
				WkBk          wkBk  = tsk.WkBkTaBaRead;
				bool          wtm   = tsk.WtmTaBaRead;

				if ( pieces.Index != taBaReadPiecesIndex ) {
					taBaRead = taBasesRead.GetTaBa( pieces );
					taBaReadPiecesIndex = pieces.Index;
					currentBlockIndex = -1;
				}



				if ( taBaRead.MultipleChunksInOneBlock ) {
					int blockIndex =  taBaRead.ChunkBlockSplitter.GetFirstBlock(wkBk,wtm);
					if ( blockIndex != currentBlockIndex ) {   // first task of new group
						if ( taskTaBaReadInfo != null ) {
							// finish old group
							taskTaBaReadInfo.LastTaskIndexP1 = i;
							taskTaBaReadInfo.UnfinishedTaskCount = i-taskTaBaReadInfo.FirstTaskIndex;
						}
						taskTaBaReadInfo = new TaskTaBaReadInfo(){ Pieces = pieces, BlockIndex = blockIndex, FirstTaskIndex = i };
						taskTaBaReadInfoCount++;
						currentBlockIndex = blockIndex;
					}
					taskIndexToInfo[i] = taskTaBaReadInfo;
				}
				else
					taskIndexToInfo[i] = null;

			}
		}


		public override int FindTaskToCalculate( int threadIndex, int firstNotYetCalculated, MyTask[] task, List<ThreadInfo> ti, int threadCount )
		{
			bool             checkedToFreeTmpBlockStorage = false;
			MyTaskTaBaRead     secondChoiceMyTask             = null;
			TmpBlockStorage  secondChoiceTmpBlockStorage  = null;

			for ( int i=firstNotYetCalculated ; i<task.Length ; i++ ) {
				if ( task[i].State==TaskState.NotYetCalculated && TasksCanBeCalculatedParallel(task[i],ti,task) ) {
					MyTaskTaBaRead     myTaskTaBaRead = (MyTaskTaBaRead)task[i];
					TaskTaBaReadInfo info = taskIndexToInfo[i];
					if ( info == null )
						return i; // chunksPerBlock == -1    no problem to execute the task

					TmpBlockStorage tbs = taBasesRead.GetTmpBlockStorage( info.BlockIndex, info.Pieces.Index );
					if ( tbs!=null ) {
						// TmpBlockStorage found, so there was already a task accessing this block
						if ( tbs.IsLoaded ) {
							myTaskTaBaRead.TmpBlockStorage = tbs;
							return i;   // the data is already loaded and decompressed. Perfect to execute the task.
						}
						else {
							if ( secondChoiceMyTask == null ) {   // if we don't find better we will use this and block until loaded
								secondChoiceMyTask            = myTaskTaBaRead;
								secondChoiceTmpBlockStorage = tbs;
							}
							i = SkipTasks( i, info.Pieces.Index, info.BlockIndex ) - 1;
							// if we choose this task, we will very likely blocked until the other task loaded and decompressed the block. Better choose another task.
							continue;
						}
					}
					else {
						if ( !checkedToFreeTmpBlockStorage ){  // check only once
							checkedToFreeTmpBlockStorage = true;

							for ( int j=0 ; j<taBasesRead.CountTmpBlockStorage ; j++ ) {
								tbs = taBasesRead.GetTmpBlockStorage( j );
								if ( tbs.IsEmpty ) {
									taBasesRead.ReuseTmpBlockStorage( j, info.Pieces, ((MyTaskTaBaRead)task[i]).WtmTaBaRead, info.BlockIndex );
									myTaskTaBaRead.TmpBlockStorage = tbs;
									return i;
								}
							}
						}
					}
				}
			}

			if ( secondChoiceMyTask == null )
				return -1; // no task available that is allowed to run in parallel; so exit thread
			else {
				secondChoiceMyTask.TmpBlockStorage = secondChoiceTmpBlockStorage;   // will block but maybe better than close the thread
				return secondChoiceMyTask.Index;
			}

		}


		private int SkipTasks( int currentTaskIndex, int piecesIndex, int blockIndex )
		{
			while ( ++currentTaskIndex < taskIndexToInfo.Length ) {
				TaskTaBaReadInfo info = taskIndexToInfo[currentTaskIndex];
				if ( info==null || info.BlockIndex!=blockIndex || info.Pieces.Index!=piecesIndex )
					break;
			}
			return currentTaskIndex;
		}


		public override void StartTaskWithoutThreading( MyTask myTask, int freeThreads, int threadIndex, bool singleThreaded )
		{
			if ( singleThreaded ) {
				TaskTaBaReadInfo info = taskIndexToInfo[myTask.Index];
				if ( info != null ) {
					TmpBlockStorage tbs = taBasesRead.GetTmpBlockStorage(0);
					if ( tbs.BlockIndex!=info.BlockIndex || tbs.PiecesIndex!=info.Pieces.Index )
						taBasesRead.ReuseTmpBlockStorage( 0, info.Pieces, ((MyTaskTaBaRead)myTask).WtmTaBaRead, info.BlockIndex );
					((MyTaskTaBaRead)myTask).TmpBlockStorage = tbs;
				}
			}
			base.StartTaskWithoutThreading( myTask, freeThreads, threadIndex, singleThreaded );
		}


		public override void TaskFinishedWithoutThreadung( MyTask myTask )
		{
			MyTaskTaBaRead myTaskTaBaRead = (MyTaskTaBaRead)myTask;
			TaskTaBaReadInfo info = taskIndexToInfo[myTask.Index];
			if ( info != null ) {
				TmpBlockStorage tmpBlockStorage = myTaskTaBaRead.TmpBlockStorage;

				if ( --info.UnfinishedTaskCount == 0 )
					taBasesRead.ClearTmpBlockStorage( taBasesRead.GetTmpBlockStorage( info.BlockIndex, info.Pieces.Index ).Index );
			}

			base.TaskFinishedWithoutThreadung( myTask );
		}


		public override void FinishedAllTasks( bool aborted )
		{
#if DEBUG
			taBasesRead.Verify();
			BlockSplitterRead.DeactivateVerifyingDoubleLoadingOfBlocks();
#endif
			taBasesRead.ExpliciteFreeOfTmpBlockStorage = false;
		}

	}
}
