using System;
using System.Threading;
using System.Diagnostics;
using TBacc;
using System.Runtime;
using System.Globalization;
using System.Threading.Tasks;


namespace smTablebases
{
	public class TbFinishedEventArgs : EventArgs
	{
		public bool CalculationWillContinue;
	}

	public enum StopType {
		StopTb,
		Stop4Men,
		Stop5Men,
		Never
	}


	public class Calc
	{
		public static bool Pause = false;
		public static event EventHandler<TbFinishedEventArgs> TablebaseFinished;
		private static StopType stop = StopType.Never;


		public static async Task GenAsync()
		{
			try
			{
				Stopwatch    stopwatchTotal         = new Stopwatch();
				stopwatchTotal.Start();
				CalcTB calcTB = null;
				long totalSize = 0;
				int countCalc = 0;

				bool calculationToContinue = true;
				while ( calculationToContinue )
				{
					(bool res, long size) = await GenTbAsync();
					if (!res)
						break;
					totalSize += size;
					countCalc++;

					// Large Object Heap Compaction
					Stopwatch sw = new Stopwatch();
					sw.Start();
					GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					GC.Collect();
					sw.Stop();
					Message.AddLogLine( "LargeObjectHeapCompaction: " + sw.ElapsedMilliseconds.ToString( "#,###,###,##0", CultureInfo.InvariantCulture ) + " ms    MEM: " + GC.GetTotalMemory(false).ToString("###,###,###,##0") );

					int menBefore = Settings.PiecesSrc.PieceCount;
					Settings.TbIndex = Settings.TbIndex + 1;
					int menNow = Settings.PiecesSrc.PieceCount;

					int countMenNow  = Settings.PiecesSrc.PieceCount;
					int countMenPrev = Pieces.FromIndex( Settings.PiecesSrc.Index-1 ).PieceCount;

					if ( Stop == StopType.StopTb )
						calculationToContinue = false;
					else if (Stop == StopType.Stop4Men && countMenNow == 3 && countMenPrev == 2)
						calculationToContinue = false;
					else if (Stop == StopType.Stop5Men && countMenNow == 4 && countMenPrev == 3)
						calculationToContinue = false;

					OnTbFinished( new TbFinishedEventArgs(){ CalculationWillContinue=calculationToContinue } );
				}
				stopwatchTotal.Stop();
				Message.AddLogLine( $"Calculated {countCalc} tablebases in {stopwatchTotal.Elapsed}, with size: " + (((double)totalSize)/(1024*1024)).ToString("G5",CultureInfo.InvariantCulture) +  $" MB = {totalSize:N0} Bytes"  );
				Message.AddLogLine( "and with compression: " + Settings.GetCompressionSettingsStr() );
			}
			catch ( Exception ex ) {
				Message.Line( "" );
				Message.Line( "EXCEPTION" );
				Message.Line( "" );
				Message.Line( ex.ToString() );
			}
			Progress.Reset();
		}


		private static async Task<(bool,long)> GenTbAsync()
		{
			Stopwatch stopwatch = new Stopwatch();

			Pieces p = Settings.PiecesSrc;
			int tbIndex = Settings.TbIndex;
			if (tbIndex != Settings.TbIndex)
				throw new Exception( "Piece.Index does not match Settings" );
			CalcTB calcTB = CalcTB.Create( p );   // Init
			MainWindow.Instance.SetTitle( p.Index.ToString() );

			string pStr = p.ToString() + " ";

			// InitTb
			await GenStepAsync( new TasksInit( calcTB ),
					p.ToString() + " Init       all=" + calcTB.TaBasesWrite.TaBaWrite.TotalIndexCount.ToString("###,###,###,##0") );

			// EnPassantCap
			EPcalc.EpCapAndInit( calcTB );

			// WTM_CAP
			await GenStepAsync( new TasksCapOrProm(calcTB,true ),
				p.ToString() + (p.ContainsPawnColor(true) ? " Cap/Prom " : " Cap ") + "WTM" );

			// BTM_CAP
			await GenStepAsync( new TasksCapOrProm(calcTB,false ),
				p.ToString() + (p.ContainsPawnColor(false) ? " Cap/Prom " : " Cap ") + "BTM" );

			calcTB.CloseReadTBs();                   // close all chunks from (already calculated before) referenced tablebases

			// Mark Mate, Stalemate and Illegal positions   ;    fill move count
			await GenStepAsync( new TasksMtIllMvCnt(calcTB), pStr + "Mate, Stalemate, Ill, Move Count" );

			if ( p.ContainsWpawnAndBpawn ) {

				// White to Move;    move results from "MtIllMvCnt" step to en passant positions
				await GenStepAsync( new TasksMvEp(calcTB,true, new Step(), true ), p.ToString() + " WTM EP" );

				// Black to Move;    move results from "MtIllMvCnt" step to en passant positions
				await GenStepAsync( new TasksMvEp(calcTB,false, new Step(), true ), p.ToString() + " BTM EP" );

			}

			Step step = new  Step();
			while ( true ) {

				// White to move      generate moves
				await GenStepAsync(new TasksMv(calcTB,true, step),
					 pStr + "WTM Wi=" + step.WinResToGen.WinIn.ToString().PadRight(4) + (step.GetLsResToGen(true).IsNo ? "" : (" Ls=" + step.GetLsResToGen(true).LsIn.ToString())).PadRight(9) );

				// White to move      move results from move step before to EN PASSANT positions
				if ( p.ContainsWpawnAndBpawn )
					await GenStepAsync( new TasksMvEp(calcTB,true,step,false), pStr + "WTM EP" );

				// white to move move generation finalization step
				MyTaskMv.Finalize(calcTB, true, 0);

				if ( !calcTB.ContinueCalculation )
					break;

				// Black to move      generate moves
				await GenStepAsync(new TasksMv(calcTB,false, step),
					pStr + "BTM Wi=" + step.WinResToGen.WinIn.ToString().PadRight(4) + (step.GetLsResToGen(false).IsNo ? "" : (" Ls=" + step.GetLsResToGen(false).LsIn.ToString())).PadRight(9) );

				// Black to move      move results from move step before to EN PASSANT positions
				if ( p.ContainsWpawnAndBpawn )
					await GenStepAsync( new TasksMvEp(calcTB,false,step,false), pStr + "BTM EP" );

				// Black to move move generation finalization step
				MyTaskMv.Finalize(calcTB, false, 0);

				if ( !calcTB.ContinueCalculation )
					break;

				// Optimize
				if ( calcTB.PerformOptimizeStepNow(step.PassIndex) ) {
					calcTB.TaBasesWrite.TaBaWrite.UpdateResCountConvertMaxBitCount();
					await GenStepAsync( new TasksOptimize(calcTB,step), pStr + "Optimize" );
				}

				step = step.Next();
			}

			Message.Line(p.ToString() + " MvGen Finished");        // MV_FinishedCompletely

			// FinalizeEnPassant  not needed

			// Finalize Tablebase
			TasksFinalize tf = new TasksFinalize( calcTB );
			tf.TbInfo = Settings.TbInfo.Get( calcTB.Pieces );
			await GenStepAsync( tf, pStr + "Finalize" );

			long size = 0;
			Message.Text(p.ToString() + " Compress");
			if (Settings.GenAllPieceGroupReorderings) {                      // Compress
				string[] reorderings = PieceGroupReorder.GetAllStrings(p);

				for (int i = 0; i < reorderings.Length; i++) {
					Settings.TbInfo.Get(p).PieceGroupReorderWtm = reorderings[i];
					Settings.TbInfo.Get(p).PieceGroupReorderBtm = reorderings[i];
					size += calcTB.TaBasesWrite.Compress(calcTB, p);
				}
			}
			else {
				size = calcTB.TaBasesWrite.Compress(calcTB,p);
			}

			// Verify with MD5
			Message.Line();
			VerifyResult md5Ok = MD5Verify.Verify(p,tbIndex);
			calcTB.Verified( md5Ok != VerifyResult.NOK );
			calcTB.TaBasesWrite.CloseAndDelete(false);
			Message.Line( 70, "00:00:00"/*stopwatchStep.Elapsed.ToString()*/ );
			return (md5Ok != VerifyResult.NOK,size);
		}

		private static async Task GenStepAsync( Tasks tasks, string text )
		{
			Stopwatch s = new Stopwatch();
			s.Start();
			Message.Text( text );
			await Threading.DoAsync( tasks );
			if ( tasks.InfoText.Length>0 )
				Message.Text( 67-tasks.InfoText.Length, tasks.InfoText );
			s.Stop();
			Message.Line( 70, s.Elapsed.ToString() );
		}


		public static StopType Stop
		{
			get{ return stop; }
			set{
				stop = value;
			}
		}


		public static bool Abort
		{
			get{ return false; }
		}


		private static void OnTbFinished( TbFinishedEventArgs e )
		{
			if ( TablebaseFinished != null )
				TablebaseFinished( null, e );
		}

	}
}
