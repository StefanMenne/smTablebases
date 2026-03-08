using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class TasksMtIllMvCnt : Tasks
	{
		public static long FinalResCount, FinalResToProcessCount;


		public TasksMtIllMvCnt( CalcTB calc ) : base( calc )
		{
		}

		public override MyTask[] Init( int threadCount )
		{

			List<MyTask> list = new List<MyTask>();
			foreach (bool wtm in Tools.BoolArray)
				for (WkBk wkBk = WkBk.First(calcTB.Pieces); wkBk < wkBk.Count; wkBk++)
					list.Add(new MyTaskMtIllMvCnt(calcTB, wkBk, calcTB.Pieces, wtm));
			NumerizeSteps(list);

			return tasks = list.ToArray();
		}


		public override void FinishedAllTasks( bool aborted )
		{
			if ( aborted )
				return;
			InfoText = calcTB.AddFinalResCount( FinalResCount, FinalResToProcessCount ).PadLeft(8);
			FinalResCount = FinalResToProcessCount = 0;
		}
	}
}
