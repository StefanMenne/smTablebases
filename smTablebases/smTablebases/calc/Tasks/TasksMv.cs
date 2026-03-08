using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class TasksMv : Tasks
	{
		public   static long     FinalResCount, FinalResToProcessCount;
		public   static bool     FastCalcDeactivatedPrinted = false;
		private         Pieces   pieces;
		private  int             nofFreeWriteDataChunks;
		private  bool            wtm;
		private  Step            step;

		private TasksPrecalculated  tasksPrecalculated;
		private int[]               updateSrcResWithLsIndexToInfoWtm, updateSrcResWithLsIndexToInfoBtm;


		public TasksMv( CalcTB calc, bool wtm, Step step ) : base( calc )
		{
			this.wtm = wtm;
			this.step = step;
			updateSrcResWithLsIndexToInfoWtm = new int[calc.TaBasesWrite.TaBaWrite.ResCountConvertWtm.MaxIndex];
			for ( int i=0 ; i<updateSrcResWithLsIndexToInfoWtm.Length ; i++ )
				updateSrcResWithLsIndexToInfoWtm[i] = -1;
			updateSrcResWithLsIndexToInfoBtm = new int[calc.TaBasesWrite.TaBaWrite.ResCountConvertBtm.MaxIndex];
			for ( int i=0 ; i<updateSrcResWithLsIndexToInfoBtm.Length ; i++ )
				updateSrcResWithLsIndexToInfoBtm[i] = -1;
		}


		public Pieces Pieces
		{
			get { return pieces; }
		}

		public Step Step => step;

		public int NofFreeWriteDataChunks
		{
			get { return nofFreeWriteDataChunks; }
			set { nofFreeWriteDataChunks = value; }
		}



		public override bool TasksCanBeCalculatedParallel( MyTask a, MyTask b )
		{
			return tasksPrecalculated.GetTaskCanBeCalculatedParallel( a, b );
		}


		public override MyTask[] Init( int threadCount )
		{
			nofFreeWriteDataChunks = calcTB.TaBasesWrite.DataChunkCount;
			FinalResCount = FinalResToProcessCount = 0;
			pieces     = Settings.PiecesSrc;
			MyTaskMv.FastBitsInterval = new FastBitsInterval(step.PassIndex,wtm);
			SrcResUpdater.FastBitsIntervalMaxLsInPlus1  = MyTaskMv.FastBitsInterval.LsInMaxPlus1;
			SrcResUpdater.FastBitsIntervalMaxWinInPlus1 = MyTaskMv.FastBitsInterval.WinInMaxPlus1;
			
			tasksPrecalculated = TasksPrecalculated.Get( wtm, pieces.ContainsPawn );
			return tasks = tasksPrecalculated.Tasks;
		}


		public override int GetTaskValue( MyTask t )
		{
			int value = 100;

			if ( !calcTB.TaBasesWrite.AllChunksInMemory ) {
				MyTaskMv ssg = (MyTaskMv)t;
				WkBkMvInfo[]   mv  = ssg.GetMvInfo();
				for ( int i=0 ; i<mv.Length ; i++ ) {
					if ( !calcTB.TaBasesWrite.IsDataChunkAvailable( mv[i].WkBkSrc, ssg.Wtm ) )
						value--;
				}
			}

			return value;
		}


		public override void FinishedAllTasks( bool aborted )
		{
			if ( aborted )
				return;
			InfoText = calcTB.AddFinalResCount( FinalResCount, FinalResToProcessCount ).PadLeft(8);
			FastCalcDeactivatedPrinted = false;
			if ( Settings.ShowResIndexCount )
				Message.Text( "     Res: " + calcTB.TaBasesWrite.TaBaWrite.ResCountConvertWtm.GetCountString() + " " + calcTB.TaBasesWrite.TaBaWrite.ResCountConvertBtm.GetCountString() );

		}


		public int[] GetUpdateSrcResWithLsIndexToInfoArray( bool wtm )
		{
			return wtm ? updateSrcResWithLsIndexToInfoWtm : updateSrcResWithLsIndexToInfoBtm;
		}
	}
}
