using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace smTablebases
{
	public partial class SelectTextWindow : Window
	{
		public SelectTextWindow( string[] s )
		{
			InitializeComponent();
			foreach( string st in s )
				listBox.Items.Add( st );
		}


		public string SelectedString
		{
			get { return (string)listBox.SelectedItem; }
			set { listBox.SelectedItem = value; }
		}


		private void buttonOk_Click( object sender, RoutedEventArgs e )
		{
			this.Close(true);
		}


		private void buttonCancel_Click( object sender, RoutedEventArgs e )
		{
			this.Close(false);
		}
	}
}
