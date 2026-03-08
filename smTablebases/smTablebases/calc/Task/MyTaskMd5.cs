using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;
using System.Security.Cryptography;

namespace smTablebases
{
	public sealed class MyTaskMd5 : MyTaskTaBaRead
	{
		private DataChunkRead         dataRead;
		private Pieces                  pieces;
		private WkBk                  wkBk;

		public MyTaskMd5( WkBk wkBk, Pieces pieces, bool wtm ) : base( null, wtm )
		{
			this.pieces   = pieces;
			this.wkBk   = wkBk;
		}


		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			TasksMd5 tasksMd5 = (TasksMd5)tasks;
			dataRead = tasksMd5.TaBasesRead.GetDataChunk( pieces, wkBk, wtm );
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			TasksMd5 tasksMd5 = (TasksMd5)tasks;
			MD5 md5 = MD5.Create();
			LoadDataChunk( tasksMd5.TaBasesRead, dataRead, threadIndex );
			dataRead.CalcMd5( md5, tasksMd5.GetBuffer(threadIndex) );
			md5.TransformFinalBlock( new byte[0], 0, 0 );
			Buffer.BlockCopy( md5.Hash, 0, TasksMd5.Hash, 16*Index, 16 );
		}


		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			TasksMd5 tasksMd5 = (TasksMd5)tasks;
			tasksMd5.TaBasesRead.FreeDataChunk( dataRead );
		}


		public static void FinishedAllTasks(CalcTB calc)
		{

		}

		public WkBk WkBk
		{
			get {  return wkBk; }
		}

		public Pieces Pieces
		{
			get{ return pieces; }
		}

		public override bool IsMd5
		{
			get { return true; }
		}


		public override bool WtmTaBaRead
		{
			get { return wtm; }
		}


		public override WkBk WkBkTaBaRead
		{
			get { return wkBk; }
		}


		public override Pieces PiecesTaBaRead
		{
			get { return pieces; }
		}
	}
}