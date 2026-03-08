using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class TasksFinalize : Tasks
	{
		public  TbInfo  TbInfo         = null;
		private int     resWithCountDrawIndexWtm, resWithCountDrawIndexBtm;


		public TasksFinalize( CalcTB calc ) : base( calc )
		{
		}


		public override MyTask[] Init( int threadCount )
		{
#if DEBUG
			calcTB.TaBasesWrite.VerifyAllUsingCountIsZero();
#endif
			Pieces pieces = calcTB.Pieces;

			int[] resIndexToInfoWtm = CreateTable( calcTB.TaBasesWrite.TaBaWrite.ResCountConvertWtm, out resWithCountDrawIndexWtm );
			int[] resIndexToInfoBtm = CreateTable( calcTB.TaBasesWrite.TaBaWrite.ResCountConvertBtm, out resWithCountDrawIndexBtm );

			List<MyTask> list = new List<MyTask>();
			foreach ( bool wtm in Tools.BoolArray ) {
				int[] resIndexToInfo = wtm ? resIndexToInfoWtm : resIndexToInfoBtm;
				for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ )
					list.Add( new MyTaskFinalize( calcTB, wkBk, pieces, wtm, resIndexToInfo ) );
			}
			NumerizeSteps(list);

			return tasks = list.ToArray();
		}


		private int[] CreateTable( ResCountConvert resCountConvert, out int resWithCountDrawIndex )
		{
			int[] resIndexToInfoTable = new int[resCountConvert.CurrentlyUsedIndexBound];

			for( int i=0 ; i<resCountConvert.Count ; i++ ) {
				int index, value;
				resCountConvert.Get( i, out index, out value );
				ResWithCount resWithCount = new ResWithCount( value );
				Res          res          = resWithCount.Res;

				int info;
				if ( res.IsIllegalPos )
					info = 5;
				else if ( res.IsStMt || resWithCount.IsUnknown )
					info = 3;
				else if ( res.IsWin || res.IsLs )
					info = 4;
				else if ( res.IsDraw )
					info = 2;
				else if ( res.IsInit )                     // Init result with move count = 0 ; only used before MtIllMvCnt
					info = 6;
				else throw new KeyNotFoundException();

				resIndexToInfoTable[index] = info;
			}

			resWithCountDrawIndex = resCountConvert.ValueToIndexAdd( ResWithCount.Draw.Value );
			return resIndexToInfoTable;
		}


		public int GetResWithCountDrawIndex( bool wtm )
		{
			return wtm ? resWithCountDrawIndexWtm : resWithCountDrawIndexBtm;
		}


		public override void FinishedAllTasks( bool aborted )
		{
			if ( aborted )
				return;
			long wtmCountWin=0, btmCountWin=0, wtmCountLs=0, btmCountLs=0, wtmCountRem=0, btmCountRem=0;
			int  maxWinInHmWtm=0, maxWinInHmBtm=0, maxLsInHmWtm=-2, maxLsInHmBtm=-2;   // LsIn=0=IsMt possible !   WinIn=0 not possible
			Pos? wtmMaxWiPos=null, wtmMaxLsPos=null, btmMaxWiPos=null, btmMaxLsPos=null;

			for ( int i=0 ; i<tasks.Length ; i++ ) {
				MyTaskFinalize tf = (MyTaskFinalize)tasks[i];
				if ( tf.Wtm ) {
					wtmCountWin += tf.ResCount[0];
					wtmCountLs  += tf.ResCount[1];
					wtmCountRem += tf.ResCount[2];
					if (  maxWinInHmWtm < tf.MaxWiInHm ) {
						wtmMaxWiPos = tf.MaxWiPos;
						maxWinInHmWtm = tf.MaxWiInHm;
					}
					if (  maxLsInHmWtm < tf.MaxLsInHm ) {
						wtmMaxLsPos = tf.MaxLsPos;
						maxLsInHmWtm = tf.MaxLsInHm;
					}
				}
				else {
					btmCountWin += tf.ResCount[0];
					btmCountLs  += tf.ResCount[1];
					btmCountRem += tf.ResCount[2];
					if (  maxWinInHmBtm < tf.MaxWiInHm ) {
						btmMaxWiPos = tf.MaxWiPos;
						maxWinInHmBtm = tf.MaxWiInHm;
					}
					if (  maxLsInHmBtm < tf.MaxLsInHm ) {
						btmMaxLsPos = tf.MaxLsPos;
						maxLsInHmBtm = tf.MaxLsInHm;
					}
				}
			}

			long wtmCountValid = wtmCountWin + wtmCountLs + wtmCountRem;
			long btmCountValid = btmCountWin + btmCountLs + btmCountRem;

			double wtmPercentWin  = (wtmCountWin  * 100.0D) / wtmCountValid;
			double btmPercentWin  = (btmCountWin  * 100.0D) / btmCountValid;
			double wtmPercentLose = (wtmCountLs   * 100.0D) / wtmCountValid;
			double btmPercentLose = (btmCountLs   * 100.0D) / btmCountValid;

			Pos? maxMatePos;
			int maxDtmHm = Math.Max( Math.Max(maxWinInHmWtm,maxWinInHmBtm), Math.Max(maxLsInHmWtm,maxLsInHmBtm) );
			if ( maxDtmHm==0 )
				maxMatePos = null;
			else if ( maxDtmHm==maxWinInHmWtm )
				maxMatePos = wtmMaxWiPos.Value;
			else if ( maxDtmHm==maxLsInHmWtm )
				maxMatePos = wtmMaxLsPos.Value;
			else if ( maxDtmHm==maxWinInHmBtm )
				maxMatePos = btmMaxWiPos.Value;
			else if ( maxDtmHm==maxLsInHmBtm )
				maxMatePos = btmMaxLsPos.Value;
			else
				throw new Exception();

			string maxMatePosString = maxMatePos.HasValue ? maxMatePos.Value.ToString(maxDtmHm==Math.Max(maxWinInHmWtm,maxLsInHmWtm)) : "-";
			TbInfo.WtmMaxWinIn=maxWinInHmWtm;
			TbInfo.WtmMaxLoseIn=maxLsInHmWtm;
			TbInfo.BtmMaxWinIn=maxWinInHmBtm;
			TbInfo.BtmMaxLoseIn=maxLsInHmBtm;
			TbInfo.WtmPecentWin=wtmPercentWin;
			TbInfo.WtmPecentLose=wtmPercentLose;
			TbInfo.BtmPecentWin=btmPercentWin;
			TbInfo.BtmPecentLose=btmPercentLose;
			TbInfo.MaxMatePos=maxMatePosString;
			TbInfo.State=TBState.FinishedUnverified;
			calcTB.TaBasesWrite.TaBaWrite.MaxDtmHm   = maxDtmHm;
			calcTB.TaBasesWrite.TaBaWrite.WtmMaxWiIn = (maxWinInHmWtm+1)/2;
			calcTB.TaBasesWrite.TaBaWrite.WtmMaxLsIn = maxLsInHmWtm/2;
			calcTB.TaBasesWrite.TaBaWrite.BtmMaxWiIn = (maxWinInHmBtm+1)/2;
			calcTB.TaBasesWrite.TaBaWrite.BtmMaxLsIn = maxLsInHmBtm/2;

			InfoText = "    Draw=" + (100.0*(wtmCountRem+btmCountRem)/(calcTB.TaBasesWrite.TaBaWrite.TotalIndexCount)).ToString("0.00") + "%";
			calcTB.TaBasesWrite.TaBaWrite.UpdateResCountConvertMaxBitCount();

		}
	}
}
