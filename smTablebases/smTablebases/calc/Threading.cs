using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TBacc;



namespace smTablebases
{
	public enum TaskState
	{
		NotYetCalculated,
		Running,
		Finished
	}


	public sealed class Threading
	{
		public  static System.Threading.Tasks.TaskFactory    TaskFactory;
		public  static object                                LockObject                 = new object();
		private static MyTask[]                                task;
		private static Tasks                                 tasks;
		private static int                                   finishedCount, firstNotYetCalculated;
		private static List<ThreadInfo>                      threadInfo                 = new List<ThreadInfo>();
		private static int                                   freeThreads                = -1;
		private static int                                   threadCount                = -1;


		static Threading()
		{
			TaskFactory = new System.Threading.Tasks.TaskFactory( CancellationToken.None, System.Threading.Tasks.TaskCreationOptions.LongRunning, System.Threading.Tasks.TaskContinuationOptions.LongRunning, System.Threading.Tasks.TaskScheduler.Default );
		}


		public static void CloseApp()
		{
			for ( int i=0 ; i<threadInfo.Count ; i++ )
				threadInfo[i].Close( i );
		}


		private static string GetInfo()
		{
			string s = "Thread-ID's: ";
			
			for ( int i=0 ; i<threadInfo.Count ; i++ ) {
				s += threadInfo[i].Thread.Id.ToString();
				if ( i != threadInfo.Count-1 )
					s += ", ";
			}
			return s;
		}
		
		
		public static async Task DoAsync( Tasks t )
		{
			await Task.Run(() => {
				Do(t); 
			});
		}
		

		public static void Do( Tasks t )
		{
			threadCount             = freeThreads = (Settings.PiecesSrc.PieceCount<=1) ? 1 : t.ThreadCount;
			tasks                   = t;

			task = t.Init( threadCount );
			if ( task == null )
				return;
			for ( int i=0 ; i<task.Length ; i++ )
				task[i].State = TaskState.NotYetCalculated;

			Progress.Max  = task.Length * 100;
			finishedCount = firstNotYetCalculated = 0;

			if ( threadCount==1 ) {
				CalcTasksSingleThreaded();
				tasks.ControlThreadWork();
			}
			else {
				if ( threadInfo.Count != threadCount ) {
					while ( threadInfo.Count < threadCount ) {
						int threadIndex = threadInfo.Count;
						threadInfo.Add( new ThreadInfo() );
						threadInfo[threadInfo.Count-1].Thread = TaskFactory.StartNew( () => RunInThread( threadIndex ), System.Threading.Tasks.TaskCreationOptions.AttachedToParent );
					}
					while ( threadInfo.Count > threadCount ) {
						threadInfo[threadInfo.Count-1].Close( threadInfo.Count-1 );
						threadInfo.RemoveAt( threadInfo.Count-1 );
					}
					GridThreading.Instance.SetThreadCount( threadInfo );
					Message.AddLogLine( GetInfo() );
				}

				for ( int i=0 ; i<threadInfo.Count ; i++ ) {
					threadInfo[i].SemaphoreWorkAvailable.Release();
				}
				tasks.ControlThreadWork();
				for ( int i=0 ; i<threadInfo.Count ; i++ ) {
					threadInfo[i].SemaphoreWorkFinished.WaitOne();
				}
			}

			t.FinishedAllTasks( Calc.Abort );
#if DEBUG
			if ( Config.DebugGeneral && !Calc.Abort ) {
				for ( int i=0 ; i < task.Length ; i++ ) {
					if ( task[i].State != TaskState.Finished )
						throw new Exception( "Not all tasks processed!" );
				}
			}
#endif
			task  = null;
			tasks = null;
		}


		public static bool GetProgress( out long current, out long max )
		{
			Tasks t = tasks;  
			if ( t==null ) {
				current = max = -1;
				return false;
			}
			else { 
				current = t.ProgressCurrent;
				max     = t.ProgressMax;
				return true;
			}
		}

		public static ThreadInfo GetThreadInfo( int index )
		{ 
			return threadInfo[index];
		}
		

		private static void RunInThread( int threadIndex )
		{
			ThreadInfo ti = threadInfo[threadIndex];
			while ( true ) { 
				ti.ThreadState = MyThreadState.WaitForWork;
				ti.SemaphoreWorkAvailable.WaitOne();
				if ( ti.ToClose )
					return;
				ti.ThreadState = MyThreadState.Running;
				try{
					MyTask currentMyTask = null;
					do {
						int index;
				
						currentMyTask = null;
						lock (LockObject) {
							index = tasks.FindTaskToCalculate( threadIndex, firstNotYetCalculated, task, threadInfo, threadCount );
							if ( index != -1 ) {
								threadInfo[threadIndex].RunningMyTask = task[index]; 
								task[index].State                   = TaskState.Running;
								while ( firstNotYetCalculated<task.Length && task[firstNotYetCalculated].State!=TaskState.NotYetCalculated )
									firstNotYetCalculated++;

								ti.IsRunning = true;
								ti.TaskIndex = index;
								currentMyTask = task[index];
								tasks.StartTaskWithoutThreading( currentMyTask, --freeThreads, threadIndex, false );
							}
						}

						if ( currentMyTask != null ) {
							while ( Calc.Pause )
								Thread.Sleep( 500 );
							currentMyTask.Do( tasks, threadIndex, false );

							lock( LockObject ) {
								threadInfo[threadIndex].RunningMyTask = null;
								task[index].State = TaskState.Finished;
								tasks.TaskFinishedWithoutThreadung( currentMyTask );
								freeThreads++;
								Progress.Value = finishedCount++;
								ti.IsRunning = false;
								ti.TaskIndex = -1;
							}
						}
					} while ( !Calc.Abort && currentMyTask!=null );
				}
				catch ( Exception ex ) {
					if ( App.ShowException ) {
						App.ShowException = false;
						Calc.Stop = StopType.StopTb;
						MsgBox.Show( ex.ToString() );
					}
					freeThreads++;
				}
				ti.SemaphoreWorkFinished.Release();
			}
		}


		private static void CalcTasksSingleThreaded()
		{
			for ( int i=0 ; i<task.Length&&!Calc.Abort ; i++ ) {
				Progress.Value = i;
				while( Calc.Pause )
					Thread.Sleep( 500 );
				tasks.StartTaskWithoutThreading( task[i], 1, 0, true );
				task[i].Do( tasks, 0, true );
				task[i].State = TaskState.Finished;
				tasks.TaskFinishedWithoutThreadung( task[i] );
			}
		}


	}
}
