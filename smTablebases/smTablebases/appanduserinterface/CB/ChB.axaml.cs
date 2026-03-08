﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace smTablebases.Chessboard
{
    public enum Piece
    {
        NO =  0,
        WK =  1,
        WQ =  2,
        WR =  3,
        WB =  4,
        WN =  5,
        WP =  6,
        BK =  7,
        BQ =  8,
        BR =  9,
        BB = 10,
        BN = 11,
        BP = 12
    }

    public class MoveEventArgs : EventArgs
    {
        public int FieldSrc;
        public int FieldDst;

        public MoveEventArgs( int src, int dst )
        {
            FieldSrc = src;
            FieldDst = dst;
        }
    }


    public partial class ChB : UserControl
    {
        public event EventHandler                Changed;
        public event EventHandler<MoveEventArgs> MoveEntered;

        private Piece[,]     b;
        private UserControl[,]   controlFields     = new UserControl[8,8];
        private bool         moveInput         = false;
        private int          moveFirstField    = -1;
        private Point        lastPointerPosition = new Point(0, 0);

        public ChB()
        {
            InitializeComponent();
            Clear( false );
            myGrid.PointerPressed += myGrid_PointerPressed;
        }

        public bool UserCanSetupBoard
        {
            get{ return myGrid.ContextMenu != null; }
            set{
                myGrid.ContextMenu = new ContextMenu();

                myGrid.PointerReleased += (s, e) =>
                {
                    if (e.InitialPressMouseButton == MouseButton.Right)
                    {
                        myGrid.ContextMenu?.Open();
                    }
                };
                foreach ( Piece p in Enum.GetValues(typeof(Piece)) ) {
                    MenuItem mi = new MenuItem();
                    Control c = GetPieceControl(p,false);
                    mi.Tag = p;
                    mi.Click += delegate( object sender, RoutedEventArgs e) {
                        Point po = lastPointerPosition;
                        int x = Math.Max( Math.Min( 7, (int)( po.X * 8D / myGrid.Bounds.Width  ) ), 0 );
                        int y = Math.Max( Math.Min( 7, (int)( po.Y * 8D / myGrid.Bounds.Height ) ), 0 );
                        b[x,7-y] = (Piece)(((MenuItem)sender).Tag);
                        Update();
                    };
                    c.Width = 30;
                    c.Height = 30;
                    mi.Header = c;
                    myGrid.ContextMenu.Items.Add( mi );
                }
            }
        }

        public bool UserCanEnterMoves
        {
            get{ return moveInput; }
            set{ moveInput=value; }
        }


        public void Clear( bool emptyBoard )
        {
            b = new Piece[8,8];

            for ( int i=0 ; i<64 ; i++ )
                b[i/8,i%8] = Piece.NO;

            if ( !emptyBoard ) {
                b[0,0] = b[7,0] = Piece.WR;
                b[1,0] = b[6,0] = Piece.WN;
                b[2,0] = b[5,0] = Piece.WB;
                b[3,0] = Piece.WQ;
                b[4,0] = Piece.WK;
                b[0,1] = b[1,1] = b[2,1] = b[3,1] = b[4,1] = b[5,1] = b[6,1] = b[7,1] = Piece.WP;
                b[0,7] = b[7,7] = Piece.BR;
                b[1,7] = b[6,7] = Piece.BN;
                b[2,7] = b[5,7] = Piece.BB;
                b[3,7] = Piece.BQ;
                b[4,7] = Piece.BK;
                b[0,6] = b[1,6] = b[2,6] = b[3,6] = b[4,6] = b[5,6] = b[6,6] = b[7,6] = Piece.BP;
            }
            Update();
        }

        public Piece Get( int x, int y )
        {
            return b[x,y];
        }

        public void Set( Piece[,] b )
        {
            bool equal = true;
            for ( int y=0; y<8; y++ ) {
                for ( int x=0; x<8; x++ ) {
                    equal &= this.b[x, y] == b[x, y];
                    this.b[x, y] = b[x, y];
                }
            }
            if ( !equal )
                Update();
        }

        private void Update()
        {
            myGrid.Children.Clear();
            moveFirstField = -1;
            for ( int y=7 ; y>=0 ; y-- ) {
                for ( int x=0 ; x<8 ; x++ ) {
                    UserControl c = GetPieceControl( b[x,y], (((x+y)%2)==0) );
                    controlFields[x,y] = c;
                    myGrid.Children.Add( c );
                }
            }
            OnChanged();
        }

        private UserControl GetPieceControl( Piece p, bool blackField )
        {
            Type[] types = new Type[]{ typeof(Empty), typeof(WK), typeof(WQ), typeof(WR), typeof(WB), typeof(WN), typeof(WP), typeof(BK), typeof(BQ), typeof(BR), typeof(BB), typeof(BN), typeof(BP) };
            UserControl c = (UserControl)Activator.CreateInstance( types[(int)p] );
            c.Background = new SolidColorBrush( blackField ? Color.FromRgb(0x7a, 0x6a, 0x55) : Color.FromRgb(0xd4, 0xc8, 0xb0) );
            return c;
        }


        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BoundsProperty)
            {
                int size = (int)Math.Min(Bounds.Width, Bounds.Height);
                grid.ColumnDefinitions[0].Width = grid.RowDefinitions[0].Height = new GridLength(size);
            }
        }


        private void myGrid_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            Point p = lastPointerPosition = e.GetPosition(myGrid);
            if ( !moveInput )
                return;

            int x = Math.Max( Math.Min( 7, (int)( p.X * 8D / myGrid.Bounds.Width  ) ), 0 );
            int y = 7-Math.Max( Math.Min( 7, (int)( p.Y * 8D / myGrid.Bounds.Height ) ), 0 );

            if ( 0<=x && x<8 && 0<=y && y<8 ) {
                if ( moveFirstField==-1 ) {
                    controlFields[x,y].BorderBrush = new SolidColorBrush(Color.FromRgb(0x89, 0xb4, 0xfa));
                    controlFields[x,y].BorderThickness = new Thickness( 4D );
                    moveFirstField = x+y*8;
                }
                else {
                    int xOld = moveFirstField%8;
                    int yOld = moveFirstField/8;
                    controlFields[xOld,yOld].BorderThickness = new Thickness( 0D );
                    moveFirstField = -1;
                    if ( xOld!=x || yOld!=y ) {
                        OnMoveEntered( new MoveEventArgs( xOld+yOld*8, x+y*8 ) );
                    }
                }
            }
        }

        private void OnChanged()
        {
            if ( Changed != null )
                Changed( this, EventArgs.Empty );
        }

        private void OnMoveEntered( MoveEventArgs e )
        {
            if ( MoveEntered != null )
                MoveEntered( this, e );
        }

    }
}
