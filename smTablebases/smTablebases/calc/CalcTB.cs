using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using TBacc;
using System.Collections.Concurrent;
using System.Runtime;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Threading;

namespace smTablebases
{
	/// <summary>
	/// This instance keeps alive for the complete calculation of a tablebase.
	/// </summary>
	public class CalcTB
	{
		private Pieces                               pieces                      = null;
		private TaBasesRead                          taBasesRead               = null;
		private TaBasesWrite                         taBasesWrite              = null;
		private int                                  threadCount               = Settings.ThreadCount;
		private IndexPos[]                           indexPos                  = null;

		/// <summary>
		/// Always called before any calculation.
		/// </summary>
		public static CalcTB Create( Pieces p )
		{
			CalcTB calcTB = new CalcTB( p );
			Message.Line(p.ToString() + " Start");
			calcTB.taBasesWrite = new TaBasesWrite( calcTB, p, false );

			bool suc = true;
			string log;
			try {
				suc = ( calcTB.taBasesRead = TaBasesRead.OpenAllSubPieces( out log, p, calcTB.ThreadCount )) != null;
			}
			catch ( Exception ex ) {
				Message.Line( ex.Message );
				suc = false;
				log = "";
			}
			calcTB.taBasesWrite.TaBaWrite.AddToLog( log );
#if DEBUG
			Dispatcher.UIThread.Invoke(  (Action)(()=>{
				Debug.Init( calcTB, p, MainWindow.Instance.GetTrackText() );
			}));
#endif

			return suc ? calcTB : null;
		}



		public CalcTB( Pieces p )
		{
			pieces = p;
			indexPos = new IndexPos[DataChunkIndex.GetCount(p.ContainsPawn)];
			foreach ( bool wtm in Tools.BoolArray ) {
				for ( WkBk wkbk=WkBk.First(p) ; wkbk<wkbk.Count ; wkbk++ ) {
					indexPos[DataChunkIndex.Get(wkbk,wtm)] = new IndexPos(wkbk,pieces,wtm);
				}
			}
		}


		public IndexPos GetIndexPos( WkBk wkbk, bool wtm )
		{
			return indexPos[DataChunkIndex.Get(wkbk,wtm)];
		}


		public Pieces Pieces
		{
			get{ return pieces; }
		}

		public string AddFinalResCount( long finalResCount, long deltaFinalResToProcessCount )
		{
			return taBasesWrite.TaBaWrite.AddFinalResCount( finalResCount, deltaFinalResToProcessCount );
		}


		public bool ContinueCalculation
		{
			get {
				bool finished = taBasesWrite.TaBaWrite.FinalResToProcessCount==0;
				finished |= (taBasesWrite.TaBaWrite.AllPosFinal && taBasesWrite.TaBaWrite.DeltaFinalResCount==0);
				return !finished;
			}
		}



		public void CloseReadTBs()
		{
			if ( taBasesRead != null ) {
				taBasesRead.CloseAll(false);
				taBasesRead = null;
			}
		}

		public bool PerformOptimizeStepNow( int passIndex )
		{
			return (passIndex%Config.OptimizeStepInterval) == 0;
		}


		public int ThreadCount
		{
			get{ return threadCount; }
		}


		public TaBasesRead TaBasesRead
		{
			get { return taBasesRead; }
		}


		public TaBasesWrite TaBasesWrite
		{
			get { return taBasesWrite; }
		}


		public void Verified( bool ok )
		{
			if ( ok ) {
				TbInfoFileList.Current.SetBitsPerResWtm( taBasesWrite.TaBaWrite.ResCountConvertMaxBitsWtm );
				TbInfoFileList.Current.SetBitsPerResBtm( taBasesWrite.TaBaWrite.ResCountConvertMaxBitsBtm );
				TbInfoFileList.Current.MaxDtmHm = taBasesWrite.TaBaWrite.MaxDtmHm;
			}
		}

	}
}
