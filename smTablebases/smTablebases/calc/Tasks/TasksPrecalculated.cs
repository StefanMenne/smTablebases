using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;
using System.Collections;

namespace smTablebases
{
	/// <summary>
	/// During the move calculation the work is splitted into Tasks.
	/// But not all can be calculated parallel. This class gives an fast possibility to check
	/// if two tasks can be calculated in parallel.
	/// </summary>

	public class TasksPrecalculated
	{
		private static TasksPrecalculated[] tasksPrecalculted = new TasksPrecalculated[4];


		static TasksPrecalculated()
		{
			tasksPrecalculted[0] = new TasksPrecalculated( false, false );
			tasksPrecalculted[1] = new TasksPrecalculated( false, true  );
			tasksPrecalculted[2] = new TasksPrecalculated( true,  false );
			tasksPrecalculted[3] = new TasksPrecalculated( true,  true  );
		}


		public static TasksPrecalculated Get( bool wtm, bool piecesContainsPawn )
		{
			return tasksPrecalculted[ (wtm?2:0) + (piecesContainsPawn?1:0) ];
		}


		private MyTask[]     tasks;
		private BitArray   canBeCalculatedParallel;


		public TasksPrecalculated( bool wtm, bool piecesContainsPawn )
		{
			List<MyTask> list = new List<MyTask>();

			for ( WkBk wkBk=WkBk.First(piecesContainsPawn) ; wkBk<wkBk.Count ; wkBk++ )
				list.Add( new MyTaskMv(null,wkBk,wtm) );


			// Sort Tasks so that CanBeCalculatedParallel is true for near beside Tasks.
			// If wtm this is already true; but not for btm
			if ( !wtm )
				list.Sort( (a, b) => ((MyTaskMv)a).WkBkDst.Bk.Value.CompareTo(((MyTaskMv)b).WkBkDst.Bk.Value) );

			smTablebases.Tasks.NumerizeSteps( list );
			tasks = list.ToArray();

			canBeCalculatedParallel = new BitArray( tasks.Length * (tasks.Length-1) / 2 );

			// only to speed up the calculation; if all mirrored dest king positions are disjunct then no src
			// WkBk can be the same
			BitBrd[] mirFields = new BitBrd[64];
			for ( Field f=Field.A1 ; f<Field.Count ; f++ ) {
				BitBrd mir = f.AsBit | f.Mirror(MirrorType.MirrorOnVertical).AsBit;
				if ( !piecesContainsPawn )
					mir |= f.Mirror(MirrorType.MirrorOnHorizontal).AsBit | f.Mirror(MirrorType.MirrorOnDiagonal).AsBit | f.Mirror(MirrorType.MirrorOnDiagonal|MirrorType.MirrorOnHorizontal).AsBit | f.Mirror(MirrorType.MirrorOnDiagonal|MirrorType.MirrorOnVertical).AsBit | f.Mirror(MirrorType.MirrorOnVertical|MirrorType.MirrorOnHorizontal).AsBit | f.Mirror(MirrorType.MirrorOnDiagonal|MirrorType.MirrorOnHorizontal|MirrorType.MirrorOnVertical).AsBit;
				mirFields[f.Value] = mir;
			}

			for ( int i=0 ; i<tasks.Length ; i++ ) {
				for ( int j=i+1 ; j<tasks.Length ; j++ ) {
					bool canBeCalcParallelCurrent = ((mirFields[((MyTaskMv)tasks[i]).WkBkDst.Wk.Value]&mirFields[((MyTaskMv)tasks[j]).WkBkDst.Wk.Value]).IsEmpty &&
					                                 (mirFields[((MyTaskMv)tasks[i]).WkBkDst.Bk.Value]&mirFields[((MyTaskMv)tasks[j]).WkBkDst.Bk.Value]).IsEmpty     ) ||
					                                CanBeCalculatedTheSameTime( (MyTaskMv)tasks[i], (MyTaskMv)tasks[j] );
					canBeCalculatedParallel.Set( (j*(j-1)/2)+i, canBeCalcParallelCurrent );
				}
			}
		}


		public bool GetTaskCanBeCalculatedParallel( MyTask a, MyTask b )
		{
			int max, min;
			if ( a.Index < b.Index ) {
				max = b.Index;
				min = a.Index;
			}
			else {
				max = a.Index;
				min = b.Index;
			}

			return canBeCalculatedParallel.Get( ( (max*(max-1)) >> 1 ) + min );
		}


		private bool CanBeCalculatedTheSameTime( MyTaskMv a, MyTaskMv b )
		{
			WkBkMvInfo[] aaMv = a.GetMvInfo();
			WkBkMvInfo[] bbMv = b.GetMvInfo();
			for ( int i=0 ; i<aaMv.Length ; i++ ) {
				for ( int j=0 ; j<bbMv.Length ; j++ ) {
					if ( aaMv[i].WkBkSrc == bbMv[j].WkBkSrc )
						return false;
				}
			}
			for ( int i=0 ; i<aaMv.Length ; i++ ) {
				if ( aaMv[i].WkBkSrc == b.WkBkDst )
					return false;
			}
			for ( int i=0 ; i<bbMv.Length ; i++ ) {
				if ( bbMv[i].WkBkSrc == a.WkBkDst )
					return false;
			}

			return true;
		}


		public MyTask[] Tasks
		{
			get{ return tasks; }
		}

	}
}
