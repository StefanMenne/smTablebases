using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TBacc;


namespace smTablebases
{

	public sealed class MoveInfo : INotifyPropertyChanged
	{
		private string        mvString;
		private string        info;
		public  UserPos       Pos;
		public  Field         Src;
		public  Field         Dst;
		public  Res           Res;


		public event PropertyChangedEventHandler PropertyChanged;


		public MoveInfo( Field src, Field dst, UserPos pos, string mvString, Res res )
		{
			Pos             = pos;
			this.mvString   = mvString;
			Src             = src;
			Dst             = dst;
			Res             = res;
		}


		public string Info
		{
			get { return info; }
			set {
				if ( info != value ) {
					info = value;
					NotifyPropertyChanged( "Info" );
				}
			}
		}


		public string MvString
		{
			get { return mvString; }
		}

		
		private void NotifyPropertyChanged( string propertyName )
		{
			if ( PropertyChanged != null )
				PropertyChanged( this, new PropertyChangedEventArgs(propertyName) );
		}


		public override string ToString()
		{
			return info;
		}
	}

}
