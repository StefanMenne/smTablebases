using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TBacc;

namespace smTablebases
{
	public class PosTbInfo
	{
		public static event EventHandler Changed;

		public   bool                          Valid;
		public   bool                          AllResAvailable;
		public   UserPos                       UserPos;
		public   string                        Text;
		public   Res                           Res;
		public   MoveInfo[]                      MvInfo;
		public   bool                          AutoResponse = false;
		private bool                          cancel;



		public PosTbInfo( UserPos userPos )
		{
			UserPos = userPos;
			Text    = "Search in TB";
			Res     = Res.No;

			List<MoveInfo> mv = userPos.GetMoves();
			MvInfo = mv.ToArray();
		}


		public void StartToRetrieve()
		{
			ThreadPool.QueueUserWorkItem( o => CalcInThread() );
		}


		public void Cancel()
		{
			cancel = true;
		}


		private void CalcInThread()
		{
			try {
				Res = UserPos.GetResult();
				Text = Res.ToString();
				Valid = true;
			}
			catch( Exception ex ) {
				Res = Res.IllegalPos;
				Text = ex.Message;
				Valid = false;
			}


			for ( int i=0 ; i<MvInfo.Length && !cancel && Valid ; i++ ) {
				OnChanged();
				try { 
					MvInfo[i].Res = MvInfo[i].Pos.GetResult().HalfMoveAwayFromMate;
					MvInfo[i].Info = MvInfo[i].Res.ToString();
				}
				catch ( Exception ex ) {
					Res = Res.IllegalPos;
					Text = ex.Message;
					Valid = false;
				}
			}
			AllResAvailable = Valid;
			OnChanged();
		}


		private void OnChanged()
		{
			if ( Changed != null )
				Changed( this, EventArgs.Empty );
		}
	}
}
