using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;

namespace TBacc
{
    public readonly struct BitBrd
    {
        public readonly ulong Value;


        public const ulong EmptyVal           = 0x0UL;
        public const ulong AllVal             = 0xFFFFFFFFFFFFFFFFUL;
        public const ulong BorderLeftVal      = 0x0101010101010101UL; // A-File
        public const ulong BorderRightVal     = 0x8080808080808080UL; // H-File
        public const ulong BorderTopVal       = 0xFF00000000000000UL; // Rank 8
        public const ulong BorderBottomVal    = 0x00000000000000FFUL; // Rank 1
        public const ulong BorderVal          = 0xFF818181818181FFUL;
        public const ulong NonBorderVal       = 0x007E7E7E7E7E7E00UL;
        public const ulong DiagA1H8Val        = 0x8040201008040201UL;
        public const ulong DiagH1A8Val        = 0x0102040810204080UL;
        public const ulong CenterVal          = 0x0000001818000000UL; // D4, E4, D5, E5
        public const ulong Center2Val         = 0x00003C24243C0000UL; 
        public const ulong AllWithoutA1Val    = 0xFFFFFFFFFFFFFFFEUL;
        public const ulong Line1AndLine8Val   = 0xFF000000000000FFUL;


        public static readonly BitBrd Empty           = new BitBrd(EmptyVal);
        public static readonly BitBrd All             = new BitBrd(AllVal);
        public static readonly BitBrd BorderLeft      = new BitBrd(BorderLeftVal);
        public static readonly BitBrd BorderRight     = new BitBrd(BorderRightVal);
        public static readonly BitBrd BorderTop       = new BitBrd(BorderTopVal);
        public static readonly BitBrd BorderBottom    = new BitBrd(BorderBottomVal);
        public static readonly BitBrd Border          = new BitBrd(BorderVal);
        public static readonly BitBrd NonBorder       = new BitBrd(NonBorderVal);
        public static readonly BitBrd DiagA1H8        = new BitBrd(DiagA1H8Val);
        public static readonly BitBrd DiagH1A8        = new BitBrd(DiagH1A8Val);
        public static readonly BitBrd Center          = new BitBrd(CenterVal);
        public static readonly BitBrd Center2         = new BitBrd(Center2Val);
        public static readonly BitBrd AllWithoutA1    = new BitBrd(AllWithoutA1Val);
        public static readonly BitBrd Line1AndLine8   = new BitBrd(Line1AndLine8Val);


        private static readonly BitBrd[]   field_TO_9NeighbourFields         = new BitBrd[64];
        private static readonly BitBrd[]   field_TO_MvsN                     = new BitBrd[64];		
        private static readonly BitBrd[][] field_field_TO_innerLineBits      = new BitBrd[64][];   // ..a..b..  => 00011000
        private static readonly BitBrd[][] field_field_TO_lineBitsTillBorder = new BitBrd[64][];   // ..a..b..  => 00011111 


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd(ulong value)
        {
            this.Value = value;
        }


        static BitBrd()
        {
            for ( Field f1=Field.A1 ; f1<Field.Count ; f1++ ) {
                field_field_TO_innerLineBits[f1.Value] = new BitBrd[64];
                field_field_TO_lineBitsTillBorder[f1.Value] = new BitBrd[64];
                for ( Field f2=Field.A1 ; f2<Field.Count ; f2++ ) {
                    field_field_TO_innerLineBits[f1.Value][f2.Value] = GetLine( f1, f2, false, false );
                    field_field_TO_lineBitsTillBorder[f1.Value][f2.Value] = GetLineToEnd( f1, f2, false );
                    if ( Field.IsDist0or1(f1,f2) )
                        field_TO_9NeighbourFields[f1.Value] |= f2.AsBit;
                    if ( N.IsMv( f1, f2 ) )
                        field_TO_MvsN[f1.Value] |= f2.AsBit;

                }
            }
        }


        public int BitCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return BitOperations.PopCount(Value); }
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Value == 0UL; }
        }

        public bool IsNotEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Value != 0UL; }
        }

        public BitBrd LowestBit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Value & (ulong) (-(long) Value); }
            //public BitBrd LowestBit => 1ul << (BitOperations.TrailingZeroCount(value));    // maybe faster
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Field f)
        {
            return ((Value >> f.Value) & 1) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd XorField(Field f)
        {
            return new BitBrd( Value ^ (1UL<<f.Value) ) ;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int index)
        {
            return ((Value >> index) & 1) != 0;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsNot(Field f) => ((Value >> f.Value) & 1) == 0;


        public Field LowestField
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Field(BitOperations.TrailingZeroCount(Value)); }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd Mirror(MirrorType mt)
        {
            BitBrd b = this;
            if ((mt & MirrorType.MirrorOnVertical) == MirrorType.MirrorOnVertical)
                b = b.MirrorOnVertical();
            if ((mt & MirrorType.MirrorOnHorizontal) == MirrorType.MirrorOnHorizontal)
                b = b.MirrorOnHorizontal();
            if ((mt & MirrorType.MirrorOnDiagonal) == MirrorType.MirrorOnDiagonal)
                b = b.MirrorOnDiagonal();
            return b;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd MirrorOnVertical()
        {
            ulong k1 = 0x5555555555555555L;
            ulong k2 = 0x3333333333333333L;
            ulong k4 = 0x0f0f0f0f0f0f0f0fL;

            ulong v = Value;
            v = ((v >> 1) & k1) | ((v & k1) << 1);
            v = ((v >> 2) & k2) | ((v & k2) << 2);
            v = ((v >> 4) & k4) | ((v & k4) << 4);
            return new BitBrd(v);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd MirrorOnHorizontal()
        {
            return new BitBrd(BinaryPrimitives.ReverseEndianness(Value));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd MirrorOnDiagonal()
        {
            const ulong k1 = 0x5500550055005500L;
            const ulong k2 = 0x3333000033330000L;
            const ulong k4 = 0x0f0f0f0f00000000L;

            ulong v = Value;
            ulong t = k4 & (v ^ (v << 28));
            v ^= t ^ (t >> 28);
            t = k2 & (v ^ (v << 14));
            v ^= t ^ (t >> 14);
            t = k1 & (v ^ (v << 7));
            v ^= t ^ (t >> 7);
            return new BitBrd(v);
        }





        // unused
        //public BitBrd GetLine( int y ) => (this << (y << 3)) & 255;


        /// <summary>
        /// Shifts/rotates the given row into Bits 0 to 7. Bit 0 contains the Bit from the first line.
        /// </summary>
        //public BitBrd GetRow( int x ) => (((this >> (7 - x)) & BorderRight) * MagicNumberForGetRow) << 56;


        /// <summary>
        /// Shifts/rotates the given diag into Bits 0 to 7. x pos remains e.g. for d=1, C2 will be in Bit 2.
        /// d = x-y
        ///  -7 -6 -5 -4 -3 -2 -1  0
        ///  -6 -5 -4 -3 -2 -1  0  1
        ///  -5 -4 -3 -2 -1  0  1  2
        ///  -4 -3 -2 -1  0  1  2  3
        ///  -3 -2 -1  0  1  2  3  4
        ///  -2 -1  0  1  2  3  4  5
        ///  -1  0  1  2  3  4  5  6
        ///   0  1  2  3  4  5  6  7
        /// </summary>
        //public BitBrd GetDiagLlUr( int d ) => return (((this<<(d<<3))&DiagA1H8)*BorderLeft)<<56;


        /// <summary>
        /// Shifts/rotates the given diag into Bits 0 to 7. x pos remains e.g. for d=1, C7 will be in Bit 2.
        /// d = x+y-7
        ///   0  1  2  3  4  5  6  7
        ///  -1  0  1  2  3  4  5  6
        ///  -2 -1  0  1 -2  3  4  5
        ///  -3 -2 -1  0  1  2  3  4
        ///  -4 -3 -2 -1  0  1  2  3
        ///  -5 -4 -3 -2 -1  0  1  2
        ///  -6 -5 -4 -3 -2 -1  0  1
        ///  -7 -6 -5 -4 -3 -2 -1  0
        /// </summary>
        // public BitBrd GetDiagUlLr( int d ) => (((this << (d << 3)) & DiagH1A8) * BorderLeft) << 56;



        //public BitBrd MirrorBack( MirrorType mt )
        //{
        //	BitBrd b = this;
        //	if ( (mt&MirrorType.MirrorOnDiagonal) == MirrorType.MirrorOnDiagonal )
        //		b = b.MirrorOnDiagonal();
        //	if ((mt & MirrorType.MirrorOnHorizontal) == MirrorType.MirrorOnHorizontal)
        //		b = b.MirrorOnHorizontal();
        //	if ( (mt&MirrorType.MirrorOnVertical) == MirrorType.MirrorOnVertical )
        //		b = b.MirrorOnVertical();
        //	return b;
        //}



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd SingleBitSet( int bitNumber )
        {
            return new BitBrd( 1UL<<bitNumber );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd[] Get_field_field_TO_innerLineBitsArray( Field f )
        {
            return field_field_TO_innerLineBits[f.Value];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd[] Get_field_field_TO_lineBitsTillBorderArray( Field f )
        {
            return field_field_TO_lineBitsTillBorder[f.Value];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd PawnPromFields( bool w )
        {
            return w ? BorderTop : BorderBottom;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd Line( int lineNr )
        {
            return BorderBottom << 8*lineNr;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd Row( int rowNr )
        {
            return BorderLeft << rowNr;
        }


        public static BitBrd GetLine( Field start, Field end, bool includeStart, bool includeEnd )
        {
            if ( start.IsNo || end.IsNo || (start.X!=end.X && start.Y!=end.Y && start.DiagLlUr!=end.DiagLlUr && start.DiagUlLr!=end.DiagUlLr) )
                return BitBrd.Empty;
            if ( start == end )
                return (includeStart||includeEnd) ? start.AsBit : BitBrd.Empty;
            BitBrd bb = new BitBrd();
            int    dx       = end.X-start.X;
            int    dy       = end.Y-start.Y;
            int    fieldCnt = Math.Max(Math.Abs(dx),Math.Abs(dy));
            dx = dx/fieldCnt;
            dy = dy/fieldCnt;
            int    delta    = dy*8+dx;
            Field  f        = includeStart ? start : start+delta;
            while( f!=end ) {
                bb |= f;
                f += delta;
            }
            if ( includeEnd )
                bb |= end;
            return bb;
        }

        public static BitBrd GetLineToEnd( Field start, Field end, bool includeStart )
        {
            if ( start==end || start.IsNo || end.IsNo || (start.X!=end.X && start.Y!=end.Y && start.DiagLlUr!=end.DiagLlUr && start.DiagUlLr!=end.DiagUlLr) )
                return BitBrd.Empty;
            BitBrd bb = new BitBrd();
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            int fieldCnt = Math.Max(Math.Abs(dx), Math.Abs(dy));
            dx = dx / fieldCnt;
            dy = dy / fieldCnt;			
            int    delta    = dy*8+dx;
            Field  f        = includeStart ? start : start+delta;
            bb |= f;
            while( !Piece.IsMvToOutside(f,dx,dy) ) {
                f += delta;
                bb |= f;
            }
            return bb;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator -( BitBrd a, BitBrd b )
        {
            return new BitBrd( a.Value - b.Value );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator &( BitBrd a, BitBrd b )
        {
            return new BitBrd( a.Value & b.Value );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator |( BitBrd a, BitBrd b )
        {
            return new BitBrd( a.Value | b.Value );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator ^( BitBrd a, BitBrd b )
        {
            return new BitBrd( a.Value ^ b.Value );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator >>( BitBrd a, int s )
        {
            return new BitBrd( a.Value >> s );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator <<( BitBrd a, int s )
        {
            return new BitBrd( a.Value << s );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator ~( BitBrd a )
        {
            return new BitBrd( ~a.Value );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd operator *(BitBrd a, BitBrd b)
        {
            return new BitBrd( a.Value * b.Value );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==( BitBrd a, BitBrd b )
        {
            return a.Value ==b.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=( BitBrd a, BitBrd b )
        {
            return a.Value!=b.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator BitBrd( ulong v ) 
        {
            return new BitBrd( v );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator BitBrd( Field f )
        {
            return f.AsBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd GetFldNeighbour( Field f )
        {
            return field_TO_9NeighbourFields[f.Value];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitBrd GetMvsN( Field f )
        {
            return field_TO_MvsN[ f.Value ];
        }

    }
}
