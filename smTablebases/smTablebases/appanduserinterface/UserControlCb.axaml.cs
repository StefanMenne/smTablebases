using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using smTablebases.Chessboard;
using TBacc;
using Piece = TBacc.Piece;

namespace smTablebases
{
	public partial class UserControlCb : UserControl
	{
		private List<UserPos>                 history            = new List<UserPos>();
		private int                           historyIndex       = -1;
		private UserPos                       currentPos         = null;
		private PosTbInfo                     posTbInfo          = null;
		private ObservableCollection<MoveInfo>  listBoxMvObservableCollection = new ObservableCollection<MoveInfo>();
		private bool                          ignoreEpComboBoxEvents = false;
		private System.Threading.Tasks.Task   dialogTask         = null;


		public UserControlCb()
		{
			InitializeComponent();
			PosTbInfo.Changed       += PosTbInfo_Changed;
			Cb.UserCanSetupBoard     = true;
			Cb.Changed              += cb_Changed;
			Cb.MoveEntered          += cb_MoveEntered;
			Cb.Clear( true );
			UpdateHistoryButtons();
			ListBoxMoves.ItemsSource = listBoxMvObservableCollection;
		}


		private void ShowUserPos( UserPos pos, bool autoResponse )
		{
			currentPos = pos;
			Chessboard.Piece[,] b = new Chessboard.Piece[8, 8];
			b[pos.Wk.X, pos.Wk.Y] = smTablebases.Chessboard.Piece.WK;
			b[pos.Bk.X, pos.Bk.Y] = smTablebases.Chessboard.Piece.BK;
			for ( int i=0 ; i<pos.Count ; i++ )
				b[ pos.GetPiecePos(i).X, pos.GetPiecePos(i).Y] = (smTablebases.Chessboard.Piece)( pos.GetPieceType(i).AsInt3 + ((int)( pos.IsW(i) ? smTablebases.Chessboard.Piece.WK : smTablebases.Chessboard.Piece.BK )) );
			Cb.Set( b );
			CheckBoxWtm.IsChecked = pos.Wtm;
			UpdateEpControls();
			UpdateCBInfoFromPos( currentPos, autoResponse );
		}


		private void UpdateEpControls()
		{
			ignoreEpComboBoxEvents = true;
			Field[] epCapField = EP.GetEp( currentPos.Pieces, currentPos.Fields, currentPos.Wtm );
			CheckBoxEnPassant.IsEnabled = epCapField!=null && epCapField.Length>0;
			ComboBoxEnPassant.Items.Clear();
			if ( epCapField != null ) {
				foreach( Field epf in epCapField ) {
					ComboBoxEnPassant.Items.Add( epf.ToString() );
				}
			}
			if ( currentPos.EpCapDst.IsNo ) {
				if ( ComboBoxEnPassant.Items.Count > 0 )
					ComboBoxEnPassant.SelectedIndex = 0;
				CheckBoxEnPassant.IsChecked = false;
			}
			else {
				ComboBoxEnPassant.SelectedItem = currentPos.EpCapDst.ToString();
				CheckBoxEnPassant.IsChecked = true;
			}
			ignoreEpComboBoxEvents = false;
		}


		private void ButtonBoardClear_Click( object sender, RoutedEventArgs e )
		{
			Cb.Clear( true );
			UpdateCBInfoFromPos( currentPos = null, false );
		}


		private void cb_Changed(object sender, EventArgs e)
		{
			UserPos up = (currentPos==null) ?  CBToPos(CheckBoxWtm.IsChecked == true, GetControlsEpField()) : CBToPos( currentPos.Wtm, currentPos.EpCapDst );
			if ( up!=null && currentPos != up ) {
				ClearHistory( up );
				UpdateCBInfoFromPos( currentPos = up, false );
				UpdateEpControls();
			}
		}


		private void cb_MoveEntered( object sender, MoveEventArgs e )
		{
			foreach( MoveInfo mv in ListBoxMoves.Items ) {
				if ( mv.Src.Value == e.FieldSrc && mv.Dst.Value == e.FieldDst ) {
					AddToHistory( mv.Pos );
					ShowUserPos( mv.Pos, true );
					return;
				}
			}
		}


		private void PerformMove( UserPos newUserPos )
		{
			if ( newUserPos != null ) {
				AddToHistory( newUserPos );
				ShowUserPos(newUserPos, false);
			}
		}


		public UserPos GetBestMove( bool wtm )
		{
			Res    bestRes = Res.IllegalPos;
			MoveInfo bestMove = null;

			for ( int i=0 ; i<posTbInfo.MvInfo.Length ; i++ ) {
				bestRes = bestRes.Combine( posTbInfo.MvInfo[i].Res );
				if ( Res.Compare( bestRes, posTbInfo.MvInfo[i].Res ) )
					bestMove = posTbInfo.MvInfo[i];
			}

			if ( bestMove == null )
				return null;
			else
				return bestMove.Pos;
		}



		private void CheckBoxWtm_IsCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (currentPos == null)
				return;

			if (CheckBoxWtm.IsChecked == true && !currentPos.Wtm)
			{
				ShowUserPos(new UserPos(currentPos.Wk, currentPos.Bk, currentPos.Pieces, currentPos.Fields, true, Field.No), false);
				ClearHistory(currentPos);
			}
			else if (CheckBoxWtm.IsChecked == false && currentPos.Wtm)
			{
				ShowUserPos(new UserPos(currentPos.Wk, currentPos.Bk, currentPos.Pieces, currentPos.Fields, false, Field.No), false);
				ClearHistory(currentPos);
			}
		}


		private UserPos CBToPos( bool wtm, Field ep )
		{
			Piece[] cbIntToPiece = new Piece[]{ null, Piece.K, Piece.Q, Piece.R, Piece.B, Piece.N, Piece.PW, Piece.K, Piece.Q, Piece.R, Piece.B, Piece.N, Piece.PB };
			List<int> piecesLiest  = new List<int>();
			List<int> fieldList = new List<int>();
			bool valid = true;
			for ( int i=0 ; i<64 ; i++ ) {
                smTablebases.Chessboard.Piece p = Cb.Get(i%8,i/8);
				if ( p!=smTablebases.Chessboard.Piece.NO ) {
					piecesLiest.Add( (int)p );
					fieldList.Add( i );
				}
			}
			valid &= piecesLiest.Contains((int)smTablebases.Chessboard.Piece.WK) && piecesLiest.Contains((int)smTablebases.Chessboard.Piece.BK) && piecesLiest.IndexOf((int)smTablebases.Chessboard.Piece.WK)==piecesLiest.LastIndexOf((int)smTablebases.Chessboard.Piece.WK) && piecesLiest.IndexOf((int)smTablebases.Chessboard.Piece.BK)==piecesLiest.LastIndexOf((int)smTablebases.Chessboard.Piece.BK);
			listBoxMvObservableCollection.Clear();
			if ( !valid ){
				TextBlockBoardInfo.Text = "";
				return null;
			}
			int[] pieces = piecesLiest.ToArray();
			int[] fields = fieldList.ToArray();
			Array.Sort( pieces, fields );
			Piece[] pa = new Piece[pieces.Length];
			int countW = 0;
			for ( int i=0 ; i<pa.Length ; i++ ) {
				pa[i] = cbIntToPiece[pieces[i]];
			}

			Fields f = new Fields();
			int j=0 ;
			Field wk=Field.A1, bk=Field.A1;
			for ( int i=0 ; i<fields.Length ; i++ ) {
				if ( pieces[i] == 1 )
					wk = new Field( fields[i] );
				else if ( pieces[i] == 7 ) {
					countW = j;
					bk = new Field( fields[i] );
				}
				else {
					f = f.SetNew( j++, new Field(fields[i]) );
				}
			}

			return new  UserPos( wk, bk, Pieces.FromPieces(pa), f, wtm, ep );
		}



		private void UpdateCBInfoFromPos( UserPos pos, bool autoResponse )
		{
			if ( posTbInfo != null ) {
				posTbInfo.Cancel();
				posTbInfo = null;
				UpdateButtons();
			}

			if ( pos==null || pos.Pieces.PieceCount >5 )
				return;


			posTbInfo = new PosTbInfo(pos) { AutoResponse = autoResponse&&(CheckBoxAutoResponse.IsChecked==true) };
			Cb.UserCanEnterMoves = posTbInfo.MvInfo.Length!=0;

			listBoxMvObservableCollection.Clear();
			for ( int i=0 ; i<posTbInfo.MvInfo.Length ; i++ )
				listBoxMvObservableCollection.Add( posTbInfo.MvInfo[i] );

			PosTbInfo_Changed( null, EventArgs.Empty );
			posTbInfo.StartToRetrieve();
		}


		private void PosTbInfo_Changed( object sender, EventArgs e )
		{
			if ( posTbInfo != null ) {
				Dispatcher.UIThread.Invoke((Action)(() => {
					TextBlockBoardInfo.Text  = posTbInfo.Text;
					ListBoxMoves.UpdateLayout();
					UpdateButtons();
					if ( posTbInfo.AllResAvailable && CheckBoxAutoResponse.IsChecked == true  && posTbInfo.AutoResponse ) {
						UserPos newUserPos = GetBestMove( CheckBoxWtm.IsChecked==true );
						PerformMove( newUserPos );
					}
				}));
			}
		}


		private void UpdateButtons()
		{
			ButtonDoMove.IsEnabled = posTbInfo!=null && posTbInfo.AllResAvailable && posTbInfo.MvInfo.Length!=0;
		}


		private void ListBoxMoves_DoubleTapped(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			if ( ListBoxMoves.SelectedItem != null ) {
				UserPos up = ((MoveInfo)ListBoxMoves.SelectedItem).Pos;
				AddToHistory( up );
				ShowUserPos( up, true );
			}
		}


		private void CheckBoxEnPassant_IsCheckedChanged(object sender, RoutedEventArgs e)
		{
			ignoreEpComboBoxEvents = true;
			ComboBoxEnPassant.IsEnabled = CheckBoxEnPassant.IsChecked == true;
			ignoreEpComboBoxEvents = false;
			CheckEpChanged(CheckBoxEnPassant.IsChecked == true);
		}


		private void ComboBoxEnPassant_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			CheckEpChanged( CheckBoxEnPassant.IsChecked==true );
		}


		private void CheckEpChanged( bool epEnabled )
		{
			if ( !ignoreEpComboBoxEvents ) {
				Field ep = (epEnabled&&ComboBoxEnPassant.SelectedItem!=null) ? new Field(((string)ComboBoxEnPassant.SelectedItem)) : Field.No;
				if ( currentPos.EpCapDst != ep ) {
					currentPos = new UserPos( currentPos.Wk, currentPos.Bk, currentPos.Pieces, currentPos.Fields, currentPos.Wtm, ep );
					UpdateCBInfoFromPos( currentPos, false );
					ClearHistory( currentPos );
				}
			}
		}


		private Field GetControlsEpField()
		{
			return (CheckBoxEnPassant.IsChecked==true) ? new Field(((string)ComboBoxEnPassant.SelectedItem)) : Field.No;
		}


	private async void ButtonGetPos_Click(object sender, RoutedEventArgs e)
		{
			SelectMaxMovePosWindow sw = new SelectMaxMovePosWindow( Settings.TbInfo );
			bool? result = await sw.ShowDialog<bool?>(MainWindow.Instance);
			if ( result == true && sw.GetItem()!=null ) {
				Pieces pieces = Pieces.FromString( sw.GetItem() );
				UserPos up = new UserPos( Settings.TbInfo.Get( pieces.Index-1 ).MaxMatePos );
				ShowUserPos( up, false );
				ClearHistory( up );
			}
		}

		private void ButtonDoMove_Click(object sender, RoutedEventArgs e)
		{
			if ( currentPos!=null ) {
				UserPos newUserPos = GetBestMove( CheckBoxWtm.IsChecked==true );
				PerformMove( newUserPos );
			}
		}




		#region History

		private void ClearHistory( UserPos pos )
		{
			history.Clear();
			historyIndex = -1;
			if (pos == null)
				UpdateHistoryButtons();
			else
				AddToHistory(pos);
		}

		private void AddToHistory( UserPos pos )
		{
			historyIndex++;
			history.RemoveRange( historyIndex, history.Count-historyIndex );
			history.Add( pos );
			UpdateHistoryButtons();
		}

		private void ButtonForward_Click(object sender, RoutedEventArgs e)
		{
			ShowUserPos( history[++historyIndex], false );
			UpdateHistoryButtons();
		}

		private void ButtonBackward_Click(object sender, RoutedEventArgs e)
		{
			ShowUserPos(history[--historyIndex], false);
			UpdateHistoryButtons();
		}

		private void UpdateHistoryButtons()
		{
			ButtonBackward.IsEnabled = (historyIndex!=-1 && historyIndex>0);
			ButtonForward.IsEnabled  = (historyIndex!=-1 && historyIndex<history.Count-1);
		}


		#endregion


	}
}
