using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.ComponentModel;
using System.IO;
using TBacc;

namespace smTablebases
{
	public sealed class TbInfoList : List<TbInfo>, IBindingList
	{
		private int calcNowIndex = 0;

		public TbInfoList()
		{
			for ( int i=1 ; i<Pieces.Count ; i++ ) {
				Pieces pieces = Pieces.FromIndex(i);
				Add( new TbInfo() { Name=pieces.ToString(), PiecesIndex=i, PieceGroupReorderWtm=TbInfoFileList.GetPieceGroupReordering(pieces,true), PieceGroupReorderBtm=TbInfoFileList.GetPieceGroupReordering(pieces,false)  } );
			}
		}

		public TbInfoList( XmlNode node )
		{
			foreach (XmlNode n in node.ChildNodes) {
				if ( n.Name == "TbInfo" ) {
					Add( new TbInfo(n) );
				}
			}
		}

		public void ToXml( XmlWriter xmlWriter )
		{
			for ( int i=0 ; i<Count ; i++ ) {
				xmlWriter.WriteStartElement( "TbInfo" );
				this[i].ToXml( xmlWriter );
				xmlWriter.WriteEndElement();
			}
		}

		public TbInfo Get( int i )
		{
			return this[i];
		}


		public TbInfo Get( Pieces pieces )
		{
			return this[pieces.Index-1];
		}


		public int CalcNowIndex
		{
			get{ return calcNowIndex; }
			set{
				Get( calcNowIndex ).CalcNow = false;
				calcNowIndex = value;
				Get( calcNowIndex ).CalcNow = true;
			}
		}

		public void WriteTextFile( string filename )
		{
			long[] sumBytes = new long[16];

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Name     Bits per result wtm / btm                     File size");
			sb.AppendLine("--------------------------------------------------------------------------");
			for ( int i=0 ; i<Count ; i++ ) {
				TbInfo tbi = this[i];
				sb.AppendLine( tbi.Name.PadRight( 13 ) + tbi.BitsPerResWtm.ToString().PadLeft(3) + tbi.BitsPerResBtm.ToString().PadLeft(3) + "                                " + tbi.Bytes.ToString("###,###,###,###,##0").PadLeft(15) );
				if ( tbi.BitsPerResWtm == tbi.BitsPerResBtm )
					sumBytes[tbi.BitsPerResWtm] += tbi.Bytes;
			}

			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine( "Sum bytes for fixed bitsPerRes:" );
			for ( int i=0 ; i<16 ; i++ )
				sb.AppendLine( i.ToString().PadLeft(3) + " " + sumBytes[i].ToString("###,###,###,###,##0").PadLeft(15) );

			File.WriteAllText( filename, sb.ToString() );
		}


		public TbInfo Current
		{
			get{ return this[Settings.TbIndex]; }
		}


		public void MyAdd( TbInfo info )
		{
			this[info.PiecesIndex-1] = info;
			OnListChanged( new ListChangedEventArgs(ListChangedType.ItemChanged, info.PiecesIndex ) );
		}


		public void ItemChanged( int index )
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index ));
		}

		#region Binding List

		private ListChangedEventArgs    resetEvent      = new ListChangedEventArgs(ListChangedType.Reset, -1);
		private ListChangedEventHandler onListChanged;

		private void OnListChanged( ListChangedEventArgs ev )
		{
			if (onListChanged != null) {
				onListChanged(this, ev);
			}
		}

		event ListChangedEventHandler IBindingList.ListChanged
		{
			add { onListChanged += value; }
			remove { onListChanged -= value; }
		}

		bool IBindingList.AllowEdit
		{
			get{ return true; }
		}

		bool IBindingList.AllowNew
		{
			get{ return false; }
		}

		bool IBindingList.AllowRemove
		{
			get{ return false; }
		}

		bool IBindingList.IsSorted
		{
			get{ return false; }
		}

		ListSortDirection IBindingList.SortDirection
		{
			get{ return ListSortDirection.Ascending; }
		}

		PropertyDescriptor IBindingList.SortProperty
		{
			get{ return null; }
		}

		bool IBindingList.SupportsChangeNotification
		{
			get{ return true; }
		}

		bool IBindingList.SupportsSearching
		{
			get{ return false; }
		}

		bool IBindingList.SupportsSorting
		{
			get{ return false; }
		}

		void IBindingList.AddIndex( PropertyDescriptor property )
		{
			throw new NotImplementedException();
		}

		object IBindingList.AddNew()
		{
			throw new NotImplementedException();
		}

		void IBindingList.ApplySort( PropertyDescriptor property, ListSortDirection direction )
		{
			throw new NotImplementedException();
		}

		int IBindingList.Find ( PropertyDescriptor property, Object key )
		{
			throw new NotImplementedException();
		}

		void IBindingList.RemoveIndex( PropertyDescriptor property )
		{
			throw new NotImplementedException();
		}

		void IBindingList.RemoveSort()
		{
			throw new NotImplementedException();
		}

		#endregion


	}
}
