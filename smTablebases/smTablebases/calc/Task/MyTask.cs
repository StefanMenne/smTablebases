using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public abstract class MyTask
	{
		public      TaskState                      State;
		protected   int                            index;
		protected   CalcTB                         calc;

		public virtual bool IsMv           { get{ return false; } }
		public virtual bool IsMvK          { get{ return false; } }
		public virtual bool IsCapOrProm    { get{ return false; } }
		public virtual bool IsEP           { get{ return false; } }
		public virtual bool IsMtIllMvCnt   { get{ return false; } }
		public virtual bool IsMd5          { get{ return false; } }

	
		public MyTask( CalcTB calc )
		{
			this.calc    = calc;
		}


		public int Index
		{
			get{ return index; }
			set{ index = value; }
		}


		protected TaBasesRead taBasesRead
		{
			get{ return calc.TaBasesRead; }
		}


		/// <summary>
		/// Preparation for calculation. Called without Threading.
		/// </summary>
		public abstract void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex );
		
		
		public abstract void Do( Tasks tasks, int threadIndex, bool singleThreaded );


		/// <summary>
		/// Finish calculation. Called without Threading.
		/// </summary>
		public abstract void FinishCalcWithoutThreading( Tasks tasks );



		public virtual bool IsMvEp { get{ return false; }	}



		public override string ToString()
		{
			return base.ToString() + " idx=" + index.ToString() + " State=" + State.ToString();
		}


	}


}
