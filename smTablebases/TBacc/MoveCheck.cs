using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
    public static class MoveCheck
    {
        /// <summary>
        /// all fields that are between two fields in a diagonal, horizontal or vertical line;
        /// e.g. a1, d4 returns { b2, c3 }
        /// </summary>
        private static BitBrd[] piecePosAnotherPiecePos_To_FieldsBetween = new BitBrd[64*64];

        /// <summary>
        /// all fields that are behind 2nd piece in a diagonal, horizontal or vertical line including 2nd piece;
        /// e.g. a1, d4 returns { d4, e5, f6, g7, h8 }
        /// it is returned inverse (~) which means the fields "behind" are 0 and all others are 1
        /// </summary>
        private static BitBrd[] piecePosAnotherPiecePos_To_FieldsBehindInverse = new BitBrd[64*64];


        /// <summary>
        /// eight fields around king (except if it stands at the border or the other king has distance less 2)
        /// </summary>
        private static BitBrd[] kPosOtherKPos_to_MvFields = new BitBrd[64*64];

        static MoveCheck()
        {
            Calc_piecePosAnotherPiecePos_To_FieldsBetweenBehind();
            Calc_kPosOtherKPos_to_MvFields();
        }


        public static BitBrd PiecePosAnotherPiecePos_To_FieldsBetween( Field f1, Field f2 )
        {
            return piecePosAnotherPiecePos_To_FieldsBetween[ ((f1.Value)<<6) | f2.Value ];
        }

        public static BitBrd PiecePosAnotherPiecePos_To_FieldsBehind( Field f1, Field f2 )
        {
            return ~piecePosAnotherPiecePos_To_FieldsBehindInverse[ ((f1.Value)<<6) | f2.Value ];
        }

        public static BitBrd PiecePosAnotherPiecePos_To_FieldsBehindInverse(Field f1, Field f2)
        {
            return piecePosAnotherPiecePos_To_FieldsBehindInverse[((f1.Value) << 6) | f2.Value];
        }

        
        
        /// <summary>
        /// nine fields around king (except if it stands at the border or the other king has distance less 2)
        /// </summary>
        public static BitBrd KPosOtherKPos_To_MvFields( Field kToMv, Field kNotToMv )
        {
            return kPosOtherKPos_to_MvFields[kToMv.Value*64+kNotToMv.Value];
        }



        private static void Calc_piecePosAnotherPiecePos_To_FieldsBetweenBehind()
        {
            for ( Field f1=Field.A1 ; f1<=Field.H8 ; f1++ ) {
                for ( Field f2=Field.A1 ; f2<=Field.H8 ; f2++ ) {
                    BitBrd bb = new BitBrd();
                    int dx = f2.X - f1.X;
                    int dy = f2.Y - f1.Y;
                    
                    if ( f1!=f2 && (dx==0 || dy==0 || Math.Abs(dx)==Math.Abs(dy)) ) {
                        dx = (dx==0?0:(dx/Math.Abs(dx)));
                        dy = (dy==0?0:(dy/Math.Abs(dy)));
                        int delta = dx + 8*dy;
                        Field f = f1 + delta;
                        while ( f != f2 ) {
                            bb |= f.AsBit;
                            f = f + delta;
                        }
                        piecePosAnotherPiecePos_To_FieldsBetween[ ((f1.Value)<<6) | f2.Value ] = bb;

                        bb = new BitBrd();
                        while ( !Piece.IsMvToOutside( f, dx, dy ) ) {
                            bb |= f.AsBit;
                            f = f + delta;
                        }
                        bb |= f.AsBit;
                        piecePosAnotherPiecePos_To_FieldsBehindInverse[ ((f1.Value)<<6) | f2.Value ] = ~bb;
                    }
                    else if ( Math.Abs(dx*dy) == 2 ) {    // N
                        piecePosAnotherPiecePos_To_FieldsBetween[ ((f1.Value)<<6) | f2.Value ] = new BitBrd();
                        piecePosAnotherPiecePos_To_FieldsBehindInverse[ ((f1.Value)<<6) | f2.Value ]  = ~(f2.AsBit);
                    }
                    else {
                        piecePosAnotherPiecePos_To_FieldsBetween[ ((f1.Value)<<6) | f2.Value ] = new BitBrd();
                        piecePosAnotherPiecePos_To_FieldsBehindInverse[  ((f1.Value)<<6) | f2.Value ] = ~(new BitBrd());
                    }
                }
            }
        }


        private static void Calc_kPosOtherKPos_to_MvFields()
        {
            for ( Field kToMv=Field.A1 ; kToMv<=Field.H8 ; kToMv++ ) {
                for ( Field kOther=Field.A1 ; kOther<=Field.H8 ; kOther++ ) {
                    ulong v = 0;
                    for ( int dir=0 ; dir<Piece.K.Delta.Length ; dir++ ) {
                        Field f = kToMv + Piece.K.Delta[dir];
                        if ( !Piece.IsMvToOutside(kToMv,Piece.K.DeltaX[dir],Piece.K.DeltaY[dir]) && !Field.IsDist0or1(f,kOther) )
                            v |= 1UL<<f.Value;
                    }
                    kPosOtherKPos_to_MvFields[kToMv.Value*64+kOther.Value] = v;
                }
            }
        }

        public static bool IsCheck( Pieces pieces, Fields fields, Field wk, Field bk, bool w )
        {
            for ( int i=pieces.FirstPiece(!w) ; i<pieces.LastPiecePlusOne(!w) ; i++ ) {
                if ( CanPieceCapTo( pieces, fields, wk, bk, i, (w?wk:bk), !w ) )
                    return true;
            }
            return false;
        }

        public static bool IsCheckCaptured( Pieces pieces, Fields fields, Field wk, Field bk, bool w, int justCapturedPieceIndex )
        {
            for ( int i=pieces.FirstPiece(!w) ; i<pieces.LastPiecePlusOne(!w) ; i++ ) {
                if ( !(i==justCapturedPieceIndex) && CanPieceCapTo( pieces, fields, wk, bk, i, (w?wk:bk), !w ) )
                    return true;
            }
            return false;
        }


        /// <summary>
        /// true if piece can generally cap a piece on fDest and no piece is between.
        /// It is not checked weather there is really a piece on fDest to capture
        /// </summary>
        public static bool CanPieceCapTo( Pieces pieces, Fields fields, Field wk, Field bk, int pieceIndex, Field fDest, bool wtm )
        {
            Piece   p       = pieces.GetPieceType( pieceIndex );
            Field fSource = fields.Get( pieceIndex );
            
            if ( p.IsN ) {
                int dx = Math.Abs( fDest.X - fSource.X );
                int dy = Math.Abs( fDest.Y - fSource.Y );
                return dx*dy==2;
            }
            else if ( p.IsP ) {
                return Math.Abs(fDest.X-fSource.X)==1 && ((fDest.Y-fSource.Y)==(wtm?1:-1));
            }
            else {
                if ( fSource == fDest )
                    return false;
                if ( p.MvHorVert ) {
                    if ( fSource.Y==fDest.Y && !IsPieceBetweenHor(fields,pieces.PieceCount,wk,bk,fSource,fDest) )
                        return true;
                    else if ( fSource.X==fDest.X && !IsPieceBetweenVert(fields,pieces.PieceCount,wk,bk,fSource,fDest) ) 
                        return true;
                }
                if ( p.MvDiag ) {
                    if ( fSource.DiagLlUr==fDest.DiagLlUr && !IsPieceBetweenLlUr(fields,pieces.PieceCount,wk,bk,fSource,fDest) )
                        return true;
                    else if ( fSource.DiagUlLr==fDest.DiagUlLr && !IsPieceBetweenUlLr(fields,pieces.PieceCount,wk,bk,fSource,fDest) ) 
                        return true;
                }
            }
            return false;
        }
        

        /// <summary>
        /// Precondition: f1!=f2, f1.Y=f2.Y
        /// </summary>
        /// <returns>true if: f3 is between f1 and f2 f3!=f1 f3!=2</returns>
        private static bool IsPieceBetweenHor( Fields fields, int count, Field wk, Field bk, Field f1, Field f2 )
        {
            int y    = f1.Y;
            int xMin = Math.Min( f1.X, f2.X ) + 1;
            int xMax = Math.Max( f1.X, f2.X ) - 1;
            if ( xMin > xMax )
                return false;

            if ( (wk.Y == y && wk.X >= xMin && wk.X <= xMax) || (bk.Y == y && bk.X >= xMin && bk.X <= xMax) )
                return true;

            for ( int i=0 ; i<count ; i++ ) {
                Field f = fields.Get(i);
                if ( f.Y == y && f.X >= xMin && f.X <= xMax )
                    return true;
            }

            return false;
        }

        private static bool IsPieceBetweenVert( Fields fields, int count, Field wk, Field bk, Field f1, Field f2 )
        {
            int x    = f1.X;
            int yMin = Math.Min( f1.Y, f2.Y ) + 1;
            int yMax = Math.Max( f1.Y, f2.Y ) - 1;
            if ( yMin > yMax )
                return false;

            if ( (wk.X == x && wk.Y >= yMin && wk.Y <= yMax) || (bk.X == x && bk.Y >= yMin && bk.Y <= yMax) )
                return true;

            for ( int i=0 ; i<count ; i++ ) {
                Field f = fields.Get(i);
                if ( f.X == x && f.Y >= yMin && f.Y <= yMax )
                    return true;
            }
            return false;
        }

        private static bool IsPieceBetweenLlUr( Fields fields, int count, Field wk, Field bk, Field f1, Field f2 )
        {
            int llur = f1.DiagLlUr;
            int xMin = Math.Min( f1.X, f2.X ) + 1;
            int xMax = Math.Max( f1.X, f2.X ) - 1;
            if ( xMin > xMax )
                return false;

            if ( (wk.DiagLlUr == llur && wk.X >= xMin && wk.X <= xMax) || (bk.DiagLlUr == llur && bk.X >= xMin && bk.X <= xMax) )
                return true;

            for ( int i=0 ; i<count ; i++ ) {
                Field f = fields.Get(i);
                if ( f.DiagLlUr == llur && f.X >= xMin && f.X <= xMax )
                    return true;
            }
            return false;
        }

        private static bool IsPieceBetweenUlLr( Fields fields, int count, Field wk, Field bk, Field f1, Field f2 )
        {
            int ullr = f1.DiagUlLr;
            int xMin = Math.Min( f1.X, f2.X ) + 1;
            int xMax = Math.Max( f1.X, f2.X ) - 1;
            if ( xMin > xMax )
                return false;

            if ( (wk.DiagUlLr == ullr && wk.X >= xMin && wk.X <= xMax) || (bk.DiagUlLr == ullr && bk.X >= xMin && bk.X <= xMax) )
                return true;

            for ( int i=0 ; i<count ; i++ ) {
                Field f = fields.Get(i);
                if ( f.DiagUlLr == ullr && f.X >= xMin && f.X <= xMax )
                    return true;
            }
            return false;
        }

    }
}
