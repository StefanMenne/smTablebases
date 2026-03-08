﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.ComponentModel;
using System.Windows;
using Avalonia;
using Avalonia.Media;

namespace smTablebases
{
	public enum TBState
	{
		NotAvailable            = 0,
		NOK                     = 1,
		FinishedUnverified      = 2,
		VerifiedOK              = 3,
		Count                   = 4
	}




	public sealed class TbInfo : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler     PropertyChanged;

		public  string  Name                { get; set; }
		private string  pieceGroupReorderWtm;
		private string  pieceGroupReorderBtm;
		public  long    Bytes;
		public  int     WtmMaxWinIn;
		public  int     BtmMaxWinIn;
		public  int     WtmMaxLoseIn;
		public  int     BtmMaxLoseIn;
		public  double  WtmPecentWin;
		public  double  WtmPecentLose;
		public  double  BtmPecentWin;
		public  double  BtmPecentLose;
		public  string  MaxMatePos;
		public  int     PiecesIndex;
		public  int     BitsPerResWtm;
		public  int     BitsPerResBtm;
		private TBState state;
		private bool    calcNow;

		private static Brush[] stateBrush   = new Brush[2*(int)TBState.Count];
		//private static LinearGradientBrush verifiedBrush   = new LinearGradientBrush();


		public TbInfo()
		{
		}


		public TbInfo( XmlNode root )
		{
			foreach ( XmlNode node in root.ChildNodes ) {
				if ( node.Name == "Name" ) {
					Name = node.InnerText;
				}
				if ( node.Name == "PiecesIndex" ) {
					PiecesIndex = int.Parse( node.InnerText );
				}
				if ( node.Name == "Bytes" ) {
					Bytes = long.Parse( node.InnerText );
				}
				if ( node.Name == "WtmMaxWinIn" ) {
					WtmMaxWinIn = int.Parse( node.InnerText );
				}
				if ( node.Name == "BtmMaxWinIn" ) {
					BtmMaxWinIn = int.Parse( node.InnerText );
				}
				if ( node.Name == "WtmMaxLoseIn" ) {
					WtmMaxLoseIn = int.Parse( node.InnerText );
				}
				if ( node.Name == "BtmMaxLoseIn" ) {
					BtmMaxLoseIn = int.Parse( node.InnerText );
				}
				if ( node.Name == "WtmPecentWin" ) {
					WtmPecentWin = double.Parse( node.InnerText );
				}
				if ( node.Name == "WtmPecentLose" ) {
					WtmPecentLose = double.Parse( node.InnerText );
				}
				if ( node.Name == "BtmPecentWin" ) {
					BtmPecentWin = double.Parse( node.InnerText );
				}
				if ( node.Name == "BtmPecentLose" ) {
					BtmPecentLose = double.Parse( node.InnerText );
				}
				if ( node.Name == "BitsPerResWtm" ) {
					BitsPerResWtm = int.Parse( node.InnerText );
				}
				if ( node.Name == "BitsPerResBtm" ) {
					BitsPerResBtm = int.Parse( node.InnerText );
				}
				if ( node.Name == "PieceGroupReorderWtm" ) {
					pieceGroupReorderWtm = node.InnerText;
				}
				if ( node.Name == "PieceGroupReorderBtm" ) {
					pieceGroupReorderBtm = node.InnerText;
				}
				if (node.Name == "MaxMatePos")
				{
					MaxMatePos = node.InnerText;
				}
				if ( node.Name == "State" )
				{
					State = (TBState)Enum.Parse( typeof(TBState), node.InnerText );
				}
			}
		}


		static TbInfo()
		{
			Color[] colors = new Color[]{ Color.FromRgb(0x58,0x5b,0x70), Color.FromRgb(0xf3,0x8b,0xa8), Color.FromRgb(0xf9,0xe2,0xaf), Color.FromRgb(0xa6,0xe3,0xa1) };

			for ( int i=0 ; i<2*(int)TBState.Count ; i++ ) {
				LinearGradientBrush brush = new LinearGradientBrush();
				brush.StartPoint = new RelativePoint( new Point( 0, 0 ), RelativeUnit.Relative );
				brush.EndPoint   = new RelativePoint( new Point( 1, 0 ), RelativeUnit.Relative );
				byte gray = (i<(int)TBState.Count) ? ((byte)0x31) : ((byte)0x25);
				brush.GradientStops.Add( new GradientStop(Color.FromRgb(gray,gray,gray), 0.0)  );
				brush.GradientStops.Add( new GradientStop(Color.FromRgb(gray,gray,gray), 0.80) );
				brush.GradientStops.Add(new GradientStop(colors[i % (int)TBState.Count], 1.0));
				stateBrush[i] = brush;

			}
		}


		public string PieceGroupReorderWtm
		{
			get {  return pieceGroupReorderWtm; }
			set {
				if ( pieceGroupReorderWtm != value ) {
					pieceGroupReorderWtm = value;
					OnPropertyChanged( "PieceGroupReorderWtm" );
				}
			}
		}


		public string PieceGroupReorderBtm
		{
			get {  return pieceGroupReorderBtm; }
			set {
				if ( pieceGroupReorderBtm != value ) {
					pieceGroupReorderBtm = value;
					OnPropertyChanged( "PieceGroupReorderBtm" );
				}
			}
		}

		public string NrString
		{
			get { return PiecesIndex.ToString("#,##0"); }
		}


		public string IndexString
		{
			get{ return Bytes.ToString("###,###,###,##0"); }
		}


		public string WtmMaxWinInString
		{
			get{ return WtmMaxWinIn.ToString("#,##0"); }
		}


		public string WtmMaxLoseInString
		{
			get{ return WtmMaxLoseIn.ToString("#,##0"); }
		}


		public string WtmPercentWinString
		{
			get{ return WtmPecentWin.ToString("##0.##"); }
		}


		public string WtmPercentLoseString
		{
			get{ return WtmPecentLose.ToString("##0.##"); }
		}


		public string BtmMaxWinInString
		{
			get{ return BtmMaxWinIn.ToString("#,##0"); }
		}


		public string BtmMaxLoseInString
		{
			get{ return BtmMaxLoseIn.ToString("#,##0"); }
		}


		public string BtmPercentWinString
		{
			get{ return BtmPecentWin.ToString("##0.##"); }
		}


		public string BtmPercentLoseString
		{
			get{ return BtmPecentLose.ToString("##0.##"); }
		}


		public TBState State
		{
			get{ return state; }
			set{
				if ( state != value ) {
					state = value;
					OnPropertyChanged("BackgroundBrush");
				}
			}
		}


		public bool Available
		{
			get{ return state== TBState.FinishedUnverified || state== TBState.VerifiedOK; }
		}


		public bool CalcNow
		{
			get{ return calcNow; }
			set{
				if ( calcNow != value ) {
					calcNow = value;
					OnPropertyChanged("BackgroundBrush");
				}
			}
		}


		public void ToXml( XmlWriter xmlWriter )
		{
			xmlWriter.WriteElementString( "Name",            Name );
			xmlWriter.WriteElementString( "PiecesIndex",     PiecesIndex.ToString() );
			xmlWriter.WriteElementString( "Bytes",           Bytes.ToString() );
			xmlWriter.WriteElementString( "WtmMaxWinIn",     WtmMaxWinIn.ToString() );
			xmlWriter.WriteElementString( "BtmMaxWinIn",     BtmMaxWinIn.ToString() );
			xmlWriter.WriteElementString( "WtmMaxLoseIn",    WtmMaxLoseIn.ToString() );
			xmlWriter.WriteElementString( "BtmMaxLoseIn",    BtmMaxLoseIn.ToString() );
			xmlWriter.WriteElementString( "WtmPecentWin",    WtmPecentWin.ToString() );
			xmlWriter.WriteElementString( "WtmPecentLose",   WtmPecentLose.ToString() );
			xmlWriter.WriteElementString( "BtmPecentWin",    BtmPecentWin.ToString() );
			xmlWriter.WriteElementString( "BtmPecentLose",   BtmPecentLose.ToString() );
			xmlWriter.WriteElementString( "BitsPerResWtm",   BitsPerResWtm.ToString() );
			xmlWriter.WriteElementString( "BitsPerResBtm",   BitsPerResBtm.ToString() );
			xmlWriter.WriteElementString( "PieceGroupReorderWtm", pieceGroupReorderWtm );
			xmlWriter.WriteElementString( "PieceGroupReorderBtm", pieceGroupReorderBtm );
			xmlWriter.WriteElementString( "MaxMatePos",    MaxMatePos );
			xmlWriter.WriteElementString( "State",         State.ToString());
		}


		public Brush ForegroundBrush
		{
			get{ return new SolidColorBrush(Color.FromRgb(0xcd, 0xd6, 0xf4)); }
		}


		public Brush BackgroundBrush
		{
			get{
				return (Brush) stateBrush[((int)state) + (calcNow?((int)TBState.Count):0)];
			}
		}


		private void OnPropertyChanged( string property )
		{
			if ( PropertyChanged != null )
				PropertyChanged( this, new PropertyChangedEventArgs( property ) );
		}

	}
}
