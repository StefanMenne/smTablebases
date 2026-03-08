using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace smTablebases
{
	public partial class SelectMaxMovePosWindow : Window
	{
		public SelectMaxMovePosWindow( TbInfoList list )
		{
			InitializeComponent();
			for ( int i=0 ; i<list.Count ; i++ ) {
				if ( list.Get(i).Available ) {
					listBox.Items.Add( list.Get(i).Name );
				}
			}
		}

		public string GetItem()
		{
			if ( listBox.SelectedItem == null )			
				return null;
			else 
				return (string)listBox.SelectedItem;
		}

		private void ButtonOk_Click(object sender, RoutedEventArgs e)
		{
			this.Close(true);
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close(false);
		}

	}
}
