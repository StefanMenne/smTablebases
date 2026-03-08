using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TBacc
{
    public readonly struct Field : IEquatable<Field>
    {
        public readonly int Value;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field( int f ) => this.Value = f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field( int x, int y ) => this.Value = y << 3 | x;


        public Field( string s )
        {
            s = s.Trim().ToLower();
            if ( s.Length == 2 )
                Value = (((int)s[0])-((int)'a')) + 8*(((int)s[1])-((int)'1'));
            else 
                Value = No.Value;
        }


        public const int NoVal = -1, CountVal = 64;
        public const byte A8v = 56, B8v = 57, C8v = 58, D8v = 59, E8v = 60, F8v = 61, G8v = 62, H8v = 63;
        public const byte A7v = 48, B7v = 49, C7v = 50, D7v = 51, E7v = 52, F7v = 53, G7v = 54, H7v = 55;
        public const byte A6v = 40, B6v = 41, C6v = 42, D6v = 43, E6v = 44, F6v = 45, G6v = 46, H6v = 47;
        public const byte A5v = 32, B5v = 33, C5v = 34, D5v = 35, E5v = 36, F5v = 37, G5v = 38, H5v = 39;
        public const byte A4v = 24, B4v = 25, C4v = 26, D4v = 27, E4v = 28, F4v = 29, G4v = 30, H4v = 31;
        public const byte A3v = 16, B3v = 17, C3v = 18, D3v = 19, E3v = 20, F3v = 21, G3v = 22, H3v = 23;
        public const byte A2v =  8, B2v =  9, C2v = 10, D2v = 11, E2v = 12, F2v = 13, G2v = 14, H2v = 15;
        public const byte A1v =  0, B1v =  1, C1v =  2, D1v =  3, E1v =  4, F1v =  5, G1v =  6, H1v =  7;

        public static readonly Field Count = new Field(CountVal);
        public static readonly Field No    = new Field(NoVal);
        public static readonly Field A8 = new Field(A8v); public static readonly Field B8 = new Field(B8v); public static readonly Field C8 = new Field(C8v); public static readonly Field D8 = new Field(D8v); public static readonly Field E8 = new Field(E8v); public static readonly Field F8 = new Field(F8v); public static readonly Field G8 = new Field(G8v); public static readonly Field H8 = new Field(H8v);
        public static readonly Field A7 = new Field(A7v); public static readonly Field B7 = new Field(B7v); public static readonly Field C7 = new Field(C7v); public static readonly Field D7 = new Field(D7v); public static readonly Field E7 = new Field(E7v); public static readonly Field F7 = new Field(F7v); public static readonly Field G7 = new Field(G7v); public static readonly Field H7 = new Field(H7v);
        public static readonly Field A6 = new Field(A6v); public static readonly Field B6 = new Field(B6v); public static readonly Field C6 = new Field(C6v); public static readonly Field D6 = new Field(D6v); public static readonly Field E6 = new Field(E6v); public static readonly Field F6 = new Field(F6v); public static readonly Field G6 = new Field(G6v); public static readonly Field H6 = new Field(H6v);
        public static readonly Field A5 = new Field(A5v); public static readonly Field B5 = new Field(B5v); public static readonly Field C5 = new Field(C5v); public static readonly Field D5 = new Field(D5v); public static readonly Field E5 = new Field(E5v); public static readonly Field F5 = new Field(F5v); public static readonly Field G5 = new Field(G5v); public static readonly Field H5 = new Field(H5v);
        public static readonly Field A4 = new Field(A4v); public static readonly Field B4 = new Field(B4v); public static readonly Field C4 = new Field(C4v); public static readonly Field D4 = new Field(D4v); public static readonly Field E4 = new Field(E4v); public static readonly Field F4 = new Field(F4v); public static readonly Field G4 = new Field(G4v); public static readonly Field H4 = new Field(H4v);
        public static readonly Field A3 = new Field(A3v); public static readonly Field B3 = new Field(B3v); public static readonly Field C3 = new Field(C3v); public static readonly Field D3 = new Field(D3v); public static readonly Field E3 = new Field(E3v); public static readonly Field F3 = new Field(F3v); public static readonly Field G3 = new Field(G3v); public static readonly Field H3 = new Field(H3v);
        public static readonly Field A2 = new Field(A2v); public static readonly Field B2 = new Field(B2v); public static readonly Field C2 = new Field(C2v); public static readonly Field D2 = new Field(D2v); public static readonly Field E2 = new Field(E2v); public static readonly Field F2 = new Field(F2v); public static readonly Field G2 = new Field(G2v); public static readonly Field H2 = new Field(H2v);
        public static readonly Field A1 = new Field(A1v); public static readonly Field B1 = new Field(B1v); public static readonly Field C1 = new Field(C1v); public static readonly Field D1 = new Field(D1v); public static readonly Field E1 = new Field(E1v); public static readonly Field F1 = new Field(F1v); public static readonly Field G1 = new Field(G1v); public static readonly Field H1 = new Field(H1v);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field Mirror( MirrorType mt )
        {
            int v = Value;
            if ( (mt&MirrorType.MirrorOnVertical) == MirrorType.MirrorOnVertical )
                v ^= 0x07;
            if ( (mt&MirrorType.MirrorOnHorizontal) == MirrorType.MirrorOnHorizontal )
                v ^= 0x38;
            if ( (mt&MirrorType.MirrorOnDiagonal) == MirrorType.MirrorOnDiagonal )
                v = ((v&7)<<3) |(v>>3);
            return new Field( v );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field MirrorBack(MirrorType mt)
        {
            int v = Value;
            if ( (mt&MirrorType.MirrorOnDiagonal) == MirrorType.MirrorOnDiagonal )
                v = (X<<3)|Y;
            if ( (mt&MirrorType.MirrorOnHorizontal) == MirrorType.MirrorOnHorizontal )
                v ^= 0x38;
            if ( (mt&MirrorType.MirrorOnVertical) == MirrorType.MirrorOnVertical )
                v ^= 0x07;
            return new Field( v );
        }



        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNo => this.Value==NoVal;


        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd AsBit => new BitBrd(1UL << Value);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Y => Value>>3;

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int X => Value&7;

        //  -7 -6 -5 -4 -3 -2 -1  0
        //  -6 -5 -4 -3 -2 -1  0  1
        //  -5 -4 -3 -2 -1  0  1  2
        //  -4 -3 -2 -1  0  1  2  3
        //  -3 -2 -1  0  1  2  3  4
        //  -2 -1  0  1  2  3  4  5
        //  -1  0  1  2  3  4  5  6
        //   0  1  2  3  4  5  6  7
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int DiagLlUr => X - Y;



        //  7  8  9 10 11 12 13 14
        //  6  7  8  9 10 11 12 13
        //  5  6  7  8  9 10 11 12
        //  4  5  6  7  8  9 10 11
        //  3  4  5  6  7  8  9 10
        //  2  3  4  5  6  7  8  9
        //  1  2  3  4  5  6  7  8
        //  0  1  2  3  4  5  6  7 
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int DiagUlLr => X + Y;


        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDist0or1( Field a, Field b ) => Math.Abs(a.X - b.X) <= 1 && Math.Abs(a.Y - b.Y) <= 1;


        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPawnGrndLine( bool w ) => Y == (w ? 1 : 6);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPawnFourthLine( bool w ) => Y == (w ? 3 : 4);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPawnPromLine( bool w ) => Y == (w ? 7 : 0);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLine0Or7 => Y==0 || Y==7;

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field PawnBackMv( bool wtm ) => this + (wtm ? -8 : 8);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field PawnBackCap( bool wtm, bool srcIsLeft )
        {
            int newX = this.X + (srcIsLeft ? -1 : 1);
            if ( newX<0 || newX>=8 )
                return Field.No;
            else 
                return new Field( newX, Y+(wtm?-1:1) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==( Field a, Field b ) => a.Value == b.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=( Field a, Field b ) => a.Value != b.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >( Field a, Field b ) => a.Value > b.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <( Field a, Field b ) => a.Value < b.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=( Field a, Field b ) => a.Value <= b.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=( Field a, Field b ) => a.Value >= b.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Field operator ++(Field f) => new Field( f.Value + 1 );


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Field operator +( Field a, int delta ) => new Field( a.Value + delta );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Field operator -( Field a, int delta ) => new Field( a.Value - delta );


        public override string ToString()
        {
            if ( this == Field.No )
                return "No";
            else 
                return (new string[]{"a","b","c","d","e","f","g","h"})[X] + (Y+1).ToString();
        }

        public bool Equals(Field other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Field other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
