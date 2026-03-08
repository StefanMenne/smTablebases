using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;

namespace TBacc
{
	public readonly struct Fields
	{
		public readonly long Bits;   // stores up to 10 fields with each 6 bits 

		private const long isNoBit                = 0x1000000000000000L;  // Binary:  0001000000000000000000000000000000000000000000000000000000000000
		private const long mirrorOnVerticalBits   = 0x01c71c71c71c71c7L;  // Binary:  0000000111000111000111000111000111000111000111000111000111000111
		private const long mirrorOnHorizontalBits = 0x0e38e38e38e38e38L;  // Binary:  0000111000111000111000111000111000111000111000111000111000111000

        public static readonly Fields No = new Fields(isNoBit);
        public static readonly Fields Last = new Fields(0xfffffffffffffff);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields( Field field0 ) => Bits = field0.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields( Field field0, Field field1 ) => Bits = field1.Value<<6 | field0.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields( Field field0, Field field1, bool isNo )
		{
			Bits = field1.Value<<6 | field0.Value;
			if ( isNo )
				Bits |= isNoBit;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Fields( long bits ) => this.Bits = bits;

        public bool IsNo
		{
			[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
			get{ return (Bits&isNoBit)!=0; }
		}

        public Field First
		{
			[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
			get{ return new Field((int)(Bits&63)); }
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field Get( int index )
		{
			return new Field( ((int)((Bits>>(6*index)))) & 63 );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains( Field f, int count )
		{
			for ( int i=0 ; i<count ; i++ ) {
				if ( Get(i) == f )
					return true;
			}
			return false;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf( Field f )
		{
			for ( int i=0 ; i<8 ; i++ ) {
				if ( Get(i) == f )
					return i;
			}
			throw new Exception();
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBrd GetBitBoard( int count )
		{
			BitBrd bb = new BitBrd();
			long   v  = Bits;

			do {
				bb  |= BitBrd.SingleBitSet( (int)(v&63) );
				v  >>= 6;
			} while( --count!=0 );
			
			return bb;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields SetNew(int index, Field value)
        {
            return new Fields( BitManipulation.SetBits(Bits, 6 * index, value.Value, 6) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields Remove(int index)
        {
            return new Fields( BitManipulation.RemoveBits(Bits, 6 * index, 6) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields Remove(int index, int count)
        {
            return new Fields(BitManipulation.RemoveBits(Bits, 6 * index, 6 * count));
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields Overwirte( int index, int count, Fields f )
		{
			return new Fields( BitManipulation.SetBits( Bits, 6*index, f.Bits, 6*count ) );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields RemoveFirst()
		{
			return new Fields( Bits >> 6 );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields Insert( int index, Field f )
		{
			return new Fields( BitManipulation.InsertBits( Bits, 6*index, 6, f.Value ) );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields SwitchSides( int newCountW_oldCountB, int newCountB_oldCountW )
		{
			return new Fields( BitManipulation.SetZeroHighBits(Bits>>(6*newCountB_oldCountW),6*newCountW_oldCountB) | BitManipulation.SetZeroHighBits(Bits<<(6*newCountW_oldCountB),6*(newCountB_oldCountW+newCountW_oldCountB)) );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields Mirror( MirrorType m )
		{
			long b = Bits;
			if ( (m&MirrorType.MirrorOnVertical) == MirrorType.MirrorOnVertical )
				b = b^mirrorOnVerticalBits;
            if ( (m&MirrorType.MirrorOnHorizontal) == MirrorType.MirrorOnHorizontal )
				b = b^mirrorOnHorizontalBits;
            if ( (m&MirrorType.MirrorOnDiagonal) == MirrorType.MirrorOnDiagonal )
				b = ((b & 0x0e38e38e38e38e38L) >> 3) | ((b & 0x01c71c71c71c71c7L) << 3);
            return new Fields( b );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields MirrorBack(MirrorType m)
		{
            long b = Bits;
            if ( (m&MirrorType.MirrorOnDiagonal) == MirrorType.MirrorOnDiagonal )
                b = ((b & 0x0e38e38e38e38e38L) >> 3) | ((b & 0x01c71c71c71c71c7L) << 3);
            if ( (m&MirrorType.MirrorOnHorizontal) == MirrorType.MirrorOnHorizontal )
                b = b ^ mirrorOnHorizontalBits;
            if ( (m&MirrorType.MirrorOnVertical) == MirrorType.MirrorOnVertical )
                b = b ^ mirrorOnVerticalBits;
            return new Fields( b );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields MirrorOnVertical()
		{
			return new Fields( Bits ^ mirrorOnVerticalBits );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields MirrorOnHorizontal()
		{
            return new Fields(Bits ^ mirrorOnHorizontalBits);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fields MirrorOnDiagonal()
		{
            return new Fields(((Bits & 0x0e38e38e38e38e38L) >> 3) | ((Bits & 0x01c71c71c71c71c7L) << 3));
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fields operator |( Fields a, Fields b )
		{
			return new Fields( a.Bits | b.Bits );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fields operator <<( Fields a, int b )
		{
			return new Fields( a.Bits << (6*b) );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fields operator >>( Fields a, int b )
		{
			return new Fields( a.Bits >> (6*b) );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare( Fields f1, Fields f2, int count )
		{
			return BitManipulation.SetZeroHighBits( f1.Bits, 6*count ).CompareTo( BitManipulation.SetZeroHighBits( f2.Bits, 6*count ) );
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareUnsorted( Fields f1, Fields f2, int count )
		{
			for ( int i=0 ; i<count; i++ ) { 
				if ( !f2.Contains( f1.Get(i), count) || !f1.Contains( f2.Get(i), count ) )
					return false;
			}
			return true;
		}


        public string ToString(int count)
        {
            if (IsNo)
                return "NO";
            string s = Get(0).ToString();
            for (int i = 1; i < count; i++)
                s += ", " + Get(i).ToString();
            return s;
        }


        //		public override string ToString()
        //		{
        //			return ToString( 8 );
        //		}

    }
}
