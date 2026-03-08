using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace smTablebases
{
	public abstract class Tasks
	{
		protected CalcTB calcTB;
		protected MyTask[] tasks;
		protected int    taskFinishedCount = 0;
		public string InfoText = "";
		 

		public Tasks( CalcTB calc )
		{
			this.calcTB = calc;
		}


		public CalcTB CalcTB
		{
			get{ return calcTB; }
		}


		public virtual int ThreadCount
		{
			get{ return calcTB.ThreadCount; }
		}


		public virtual int ProgressMax
		{
			get { return (tasks==null) ? -1 : tasks.Length; }
		}


		public virtual int ProgressCurrent
		{
			get { return taskFinishedCount; }
		}


		public virtual int FindTaskToCalculate( int threadIndex, int firstNotYetCalculated, MyTask[] task, List<ThreadInfo> ti, int threadCount )
		{
			int bestTaskIndex = -1;
			int bestTaskValue = -1;

			for ( int i=firstNotYetCalculated ; i<task.Length ; i++ ) {
				if ( task[i].State==TaskState.NotYetCalculated && TasksCanBeCalculatedParallel(task[i],ti,task) ) {
					int taskValue = GetTaskValue( task[i] );
					if ( taskValue > bestTaskValue ) {
						bestTaskIndex = i;
						bestTaskValue = taskValue;
						if ( bestTaskValue == 100 )
							return bestTaskIndex;   // 100 is max
					}
				}
			}

			return bestTaskIndex;
		}

		public abstract MyTask[] Init( int threadCount );
		

		public virtual void StartTaskWithoutThreading( MyTask myTask, int freeThreads, int threadIndex, bool singleThreaded )
		{
			myTask.PrepareCalcWithoutThreading( this, freeThreads, threadIndex );
		}


		public virtual void TaskFinishedWithoutThreadung( MyTask myTask )
		{
			taskFinishedCount++;
			myTask.FinishCalcWithoutThreading( this );
		}

		public abstract void FinishedAllTasks( bool aborted );


		/// <summary>
		/// Give a value from 0...100 which rates the given Task.
		/// 100   best to execute
		/// 0     saddest to execute
		/// 
		/// Negative values means current task and all following until (not included) -value have the value 0.
		/// E.g.:     GetTaskValue( t ) = -25   and t.Index=20  means Tasks 20, 21, 22, 23, 24 have value 0.
		/// </summary>
		public virtual int GetTaskValue( MyTask t )
		{
			return 100;
		}


		public virtual bool TasksCanBeCalculatedParallel( MyTask a, List<ThreadInfo> ti, MyTask[] task )
		{
			for ( int k=0 ; k<ti.Count ; k++ ) {
				if ( ti[k].IsRunning && !TasksCanBeCalculatedParallel( a, task[ti[k].TaskIndex] ) )
					return false;
			}
			return true;
		}


		public virtual bool TasksCanBeCalculatedParallel( MyTask a, MyTask b )
		{
			return true;
		}

		
		/// <summary>
		/// Called by an additional Thread which runs during processing of Task's 
		/// </summary>
		public virtual void ControlThreadWork()
		{
		}


		public static void NumerizeSteps( List<MyTask> s )
		{
			for ( int i=0 ; i<s.Count ; i++ )
				s[i].Index = i;
		}

		public override string ToString()
		{
			if ( calcTB == null )
				return base.ToString();
			else
				return calcTB.Pieces.ToString() + " " + base.ToString();
		}
	}
}
