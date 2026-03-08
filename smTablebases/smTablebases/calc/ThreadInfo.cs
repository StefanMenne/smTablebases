using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TBacc;

namespace smTablebases
{
	public enum MyThreadState
	{
		Illegal,
		Running,
		WaitForWork,
		WaitForFreeBufferToStoreCompressedData,
	}


	public sealed class ThreadInfo
	{

		public System.Threading.Tasks.Task  Thread                  = null;
		public MyTask                         RunningMyTask             = null;
		public bool                         IsRunning               = false;
		public volatile bool                ToClose                 = false;
		public int                          TaskIndex               = -1;
		public object                       Tag                     = null;
		public MyThreadState                ThreadState             = MyThreadState.Illegal;
		public Semaphore                    SemaphoreWorkAvailable  = new Semaphore( 0, 1 );   // binary Semaphore
		public Semaphore                    SemaphoreWorkFinished   = new Semaphore( 0, 1 );   // binary Semaphore

		
		public ThreadInfo()
		{
		}


		public void Close( int threadIndex )
		{
			ToClose = true;
			SemaphoreWorkAvailable.Release();
		}

	}
}
