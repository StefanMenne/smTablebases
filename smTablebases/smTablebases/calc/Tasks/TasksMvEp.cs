using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class TasksMvEp : Tasks
	{
		public static FastBitsInterval FastBitsInterval;
		public static long FinalResCount, FinalResToProcessCount;
		private bool wtm;
		private Step step;
		private bool afterCap;

		public TasksMvEp( CalcTB cal, bool wtm, Step step, bool afterCap ) : base( cal )
		{
			this.wtm = wtm;
			this.step = step;
			this.afterCap = afterCap;
		}


		public override MyTask[] Init( int threadCount )
		{
			Pieces pieces = calcTB.Pieces;

			FastBitsInterval = new FastBitsInterval( step.PassIndex, !wtm );   // used for "one half move away from mate" => !wtm

			if ( !pieces.ContainsWpawnAndBpawn )
				return null;
			else {
				List<MyTask> list = new List<MyTask>();

				for ( WkBk wkBk = WkBk.First(pieces); wkBk < wkBk.Count; wkBk++ )
					list.Add(new MyTaskMvEp(calcTB,wkBk,pieces,wtm));
				NumerizeSteps(list);

				return tasks = list.ToArray();
			}
		}


		public override void FinishedAllTasks( bool aborted )
		{
			if ( aborted )
				return;
			calcTB.AddFinalResCount( FinalResCount, FinalResToProcessCount );
			FinalResCount = FinalResToProcessCount = 0;
		}


		public Step Step => step;
		public bool AfterCap => afterCap;
	}
}
