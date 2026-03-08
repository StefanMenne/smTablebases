﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using smTablebases;
using TBacc;

namespace smTablebases
{
	public class GridThreading
	{
		public static     GridThreading                         Instance;
		private readonly  IBrush[]                              BrushArray = new IBrush[]
		{
			new SolidColorBrush(Color.FromRgb(0x58, 0x5b, 0x70)),
			new SolidColorBrush(Color.FromRgb(0xa6, 0xe3, 0xa1)),
			new SolidColorBrush(Color.FromRgb(0x74, 0xc7, 0xec)),
			new SolidColorBrush(Color.FromRgb(0xf9, 0xe2, 0xaf))
		};

		private           Grid                                  grid;
		private           Ellipse[]                             ellipse;


        public static void CreateThreadingGrid( Grid grid )
        {
            Instance = new GridThreading(grid);
        }



	    private GridThreading( Grid grid )
	    {
		    this.grid  = grid;

		    StackPanel sp = new StackPanel();
		    foreach ( MyThreadState gts in Enum.GetValues(typeof(MyThreadState)) ) {
			    StackPanel sp2 = new StackPanel();
			    sp2.Orientation = Orientation.Horizontal;
			    sp.Children.Add( sp2 );
			    Ellipse el = new Ellipse();
			    el.Width=8;
			    el.Height=8;
			    el.Fill=BrushArray[(int)gts];
			    sp2.Children.Add( el );
			    Label label = new Label();
			    label.Content = gts.ToString();
			    sp2.Children.Add( label );
		    }

		    ToolTip.SetTip(grid, sp);
	    }


		public void SetThreadCount( List<ThreadInfo> list )
		{
			Dispatcher.UIThread.InvokeAsync(() => {
				try
				{
					int threadCount = list.Count;
					int ellipseCount = threadCount;
					if ( ellipse==null || ellipse.Length != ellipseCount ) {
						grid.Children.Clear();
						grid.RowDefinitions.Clear();
						grid.Children.Clear();
						ellipse = new Ellipse[ellipseCount];
						grid.RowDefinitions.Add( new RowDefinition() );
						grid.RowDefinitions.Add( new RowDefinition() );
						grid.ColumnDefinitions.Clear();
						for ( int i=0 ; i<(ellipse.Length+1)/2 ; i++ )
							grid.ColumnDefinitions.Add( new ColumnDefinition() );
						for ( int i=0 ; i<ellipse.Length ; i++ ) {
							ellipse[i]                     = new Ellipse();
							ellipse[i].Fill                = BrushArray[0];
							ellipse[i].Width               = 8;
							ellipse[i].Height              = 8;
							ellipse[i].VerticalAlignment   = VerticalAlignment.Center;
							ellipse[i].HorizontalAlignment = HorizontalAlignment.Center;
							ellipse[i].Margin = new Thickness( 0, 0, 2, 0 );
							Grid.SetRow(    ellipse[i], i%2 );
							Grid.SetColumn( ellipse[i], i/2 );
							grid.Children.Add( ellipse[i] );
						}
					}
				}
				catch ( Exception ex )
				{
					Message.AddLogLine( "Exception in GridThreading.SetThreadCount: " + ex.Message );
				}
			});
		}


		public void Update()
		{
			if ( ellipse != null ) {
				for ( int i=0 ; i<ellipse.Length ; i++ )
					ellipse[i].Fill = BrushArray[(int)Threading.GetThreadInfo(i).ThreadState];
			}
		}

	}
}
