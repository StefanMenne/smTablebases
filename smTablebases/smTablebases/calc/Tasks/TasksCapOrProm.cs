using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class TasksCapOrProm : TasksTaBaRead
	{
		private bool wtm;

		public TasksCapOrProm( CalcTB calc, bool wtm ) : base( calc )
		{
			this.wtm = wtm;
		}


		public override MyTask[] Init( int threadCount )
		{
			Pieces pieces = calcTB.Pieces;
			List<Pieces> subPieces = pieces.GetSubPieces();
			List<List<MyTask>> list = new List<List<MyTask>>();

			for ( int i=0 ; i<subPieces.Count ; i++ ) {
				foreach ( bool w in Tools.BoolArray ) {
					for ( WkBk wkBk=WkBk.First(true) ; wkBk<wkBk.Count ; wkBk++ )
						list.Add( new List<MyTask>() );
				}
			}

			for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
				if ( pieces.ContainsPawnColor(wtm) ) {
					AddTask( list, subPieces, new MyTaskPromMv( calcTB, wkBk, pieces, wtm, Piece.Q ) );
					AddTask( list, subPieces, new MyTaskPromMv( calcTB, wkBk, pieces, wtm, Piece.R ) );
					AddTask( list, subPieces, new MyTaskPromMv( calcTB, wkBk, pieces, wtm, Piece.B ) );
					AddTask( list, subPieces, new MyTaskPromMv( calcTB, wkBk, pieces, wtm, Piece.N ) );
				}

				for ( int capIndex=(wtm?pieces.CountW:0) ; capIndex<(wtm?pieces.PieceCount:pieces.CountW) ; capIndex=pieces.IncPieceIndexSkipSamePieceType(capIndex) ) {
					if ( (wtm?pieces.CountW:pieces.CountB) > 0 )    // piece other than k to mv
						AddTask( list, subPieces, new MyTaskCap( calcTB, wkBk, pieces, wtm, capIndex ) );

					if ( pieces.ContainsPawnColor(wtm) ) {
						AddTask( list, subPieces, new MyTaskPromCap( calcTB, wkBk, pieces, wtm, Piece.Q, capIndex ) );
						AddTask( list, subPieces, new MyTaskPromCap( calcTB, wkBk, pieces, wtm, Piece.R, capIndex ) );
						AddTask( list, subPieces, new MyTaskPromCap( calcTB, wkBk, pieces, wtm, Piece.B, capIndex ) );
						AddTask( list, subPieces, new MyTaskPromCap( calcTB, wkBk, pieces, wtm, Piece.N, capIndex ) );
					}

					for ( int mvKIndex=0 ; mvKIndex<wkBk.Info.GetCount( wtm ) ; mvKIndex++ ) {
						Field kDst = wkBk.Info.GetMv( wtm, mvKIndex );
						AddTask( list, subPieces, new MyTaskCapK( calcTB, wkBk, pieces, wtm, capIndex, kDst ) );
					}
				}
			}

			tasks = ToArray(list);
			GenerateInfo( tasks, threadCount );
			return tasks;
		}


		private static void AddTask( List<List<MyTask>> list, List<Pieces> subPieces, MyTaskCapOrProm t )
		{
			int index = subPieces.IndexOf(t.PiecesDst);
			int dataChunkIndex = DataChunkIndex.Get( t.WkBkDst, t.WtmDst );
			list[index*DataChunkIndex.GetCount(true)+dataChunkIndex].Add( t );
		}




		private static MyTask[] ToArray( List<List<MyTask>> list )
		{
			List<MyTask> l = new List<MyTask>();

			for ( int i=0 ; i<list.Count; i++ ) {
				if ( list[i].Count != 0 ) {
					MyTask[] a = new MyTask[list[i].Count];
					for ( int j=0 ; j<a.Length ; j++ ) {
						list[i][j].Index = l.Count;
						l.Add( list[i][j] );
					}
				}
			}
			return l.ToArray();
		}


		public override bool TasksCanBeCalculatedParallel( MyTask a, MyTask b )
		{
			MyTaskCapOrProm aa = (MyTaskCapOrProm)a;
			MyTaskCapOrProm bb = (MyTaskCapOrProm)b;
			return aa.WkBkSrc!=bb.WkBkSrc;
		}


		public override void FinishedAllTasks( bool aborted )
		{
			base.FinishedAllTasks( aborted );
		}
	}
}
