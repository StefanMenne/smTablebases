using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class TasksInit : Tasks
	{
		public TasksInit( CalcTB calc ) : base( calc )
		{
		}


		public override MyTask[] Init( int threadCount )
		{
			Pieces pieces = calcTB.Pieces;

			List<MyTask> list = new List<MyTask>();
			foreach ( bool wtm in Tools.BoolArray ) {
				for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ )
					list.Add( new MyTaskInit( calcTB, wkBk, pieces, wtm ) );
			}
			NumerizeSteps(list);

			return tasks = list.ToArray();
		}


		public override void FinishedAllTasks( bool aborted )
		{
		}
	}
}
