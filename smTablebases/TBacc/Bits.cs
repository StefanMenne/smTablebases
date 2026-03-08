using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace TBacc
{
	public readonly struct Bits
	{
		public readonly ulong Value;


        public const ulong EmptyVal           = 0x0UL;
        public const ulong AllVal             = 0xFFFFFFFFFFFFFFFFUL;


		public  static readonly Bits Empty                 = new Bits( EmptyVal );
		public  static readonly Bits All                   = new Bits( AllVal );


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bits( ulong value ) => this.Value = value;

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty => Value == 0UL;

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNotEmpty => Value != 0UL;

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bits LowestBit => Value & (ulong)(-(long)Value);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int OneBitValue => BitOperations.TrailingZeroCount(Value); 




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bits SingleBitSet( int bitNumber )
		{
			return new Bits( 1UL<<bitNumber );
		}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bits operator &( Bits a, Bits b )
		{
			return new Bits( a.Value & b.Value );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bits operator |( Bits a, Bits b )
		{
			return new Bits( a.Value | b.Value );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bits operator ~( Bits a )
		{
			return new Bits( ~a.Value );
		}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Bits( ulong v ) 
		{
			return new Bits( v );
		}




        //public static Bits operator ^(Bits a, Bits b)
        //{
        //    return new Bits(a.value ^ b.value);
        //}


        //public static Bits operator >>(Bits a, int s)
        //{
        //    return new Bits(a.value >> s);
        //}


        //public static Bits operator <<(Bits a, int s)
        //{
        //    return new Bits(a.value << s);
        //}


        //public static Bits operator -(Bits a, Bits b)
        //{
        //    return new Bits(a.value - b.value);
        //}


        //public static bool operator ==(Bits a, Bits b)
        //{
        //    return a.value == b.value;
        //}


        //public static bool operator !=(Bits a, Bits b)
        //{
        //    return a.value != b.value;
        //}


        //public override string ToString()
        //{
        //	string s = "";
        //	ulong v = value;
        //	for ( int i=0 ; i<64 ; i++ ) {
        //		if ( (v&1) == 1 )
        //			s += i.ToString() + ",";
        //		v >>= 1;
        //	}
        //	return (s.Length==0) ? "Empty" : (s.Remove( s.Length-1 ));
        //}

    }
}
