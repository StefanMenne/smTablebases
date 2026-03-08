using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TBacc;

namespace smTablebases
{
	/// <summary>
	/// This class stores the result and a move counter.
	/// The move counter is only stored for initial and lose positions. For win positions it is not needed.
	/// The move counter defines the count of moves that have not been taken into account for now.
	/// If a move to another position is evaluated, the move counter is decremented.
	/// If the counter is at 0 the result gets final.
	///
	/// It is always ensured that the move counter is 0 for draw and win results.
	/// </summary>
	public readonly struct ResWithCount
	{
		public const    int BitCount  = 18;

		// move counter is stored in upper bits
		private const   int moveCounterBitMask       = 0x7f;
		private const   int resultStartBit           = 7;

		public readonly int Value;


		public static readonly ResWithCount Init            = new ResWithCount( Res.Init        );
		public static readonly ResWithCount No              = new ResWithCount( Res.No          );
		public static readonly ResWithCount IllegalPos      = new ResWithCount( Res.IllegalPos  );
		public static readonly ResWithCount Draw            = new ResWithCount( Res.Draw       );
		public static readonly ResWithCount StaleMt         = new ResWithCount( Res.StMt        );
		public static readonly ResWithCount IsMt            = new ResWithCount( Res.IsMt        );


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount( int val)
		{
            this.Value = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount( int moveCount, Res res )
		{
			this.Value = moveCount | ((res.Value)<<resultStartBit);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount( Res res )
		{
			this.Value =  (res.Value<<resultStartBit);
		}

        public Res Res
		{
            [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Res( (Value>>resultStartBit) ); }
		}

        public bool IsUnknown
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return MoveCount!=0; }
		}

        public bool IsFinal
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {  return MoveCount==0; }
		}


        public bool IsDraw
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Res.IsDraw;}
		}

        public bool IsIllegalPos
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Res.IsIllegalPos; }
		}

        public bool IsInit
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Res.IsInit; }
		}

        public bool IsWin
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Res.IsWin; }
		}

        public bool IsWinOrDraw
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Res.IsWinOrDraw;}
		}

        public bool IsStaleMate
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Res.IsStMt; }
		}

        public bool IsLose
		{
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Res.IsLs; }
		}

        public int MoveCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Value & moveCounterBitMask; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount SetMoveCount( int count )
		{
#if DEBUG
                if (!(IsLose || IsInit))
                    throw new Exception();
#endif
            return new ResWithCount( (Value & (~moveCounterBitMask)) | count );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount Combine( Res r )
		{
			Res combinedResult  = Res.Combine(r);
			int newMoveCount = MoveCount & combinedResult.IsWinOrDrawBitMask;
			return new ResWithCount( (combinedResult.Value << resultStartBit) | newMoveCount );
		}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount CombineAndDecrementMoveCounter( Res r )
		{
			Res combinedResult  = Res.Combine(r);
			int newMoveCounter = MoveCount-1 & combinedResult.IsWinOrDrawBitMask;
			return new ResWithCount( (combinedResult.Value << resultStartBit) | newMoveCounter );
		}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount CombineAndCopyMoveCounter( ResWithCount r )
		{
			Res combinedResult  = Res.Combine( r.Res );
			int newMoveCounter = r.MoveCount & combinedResult.IsWinOrDrawBitMask;
			return new ResWithCount( (combinedResult.Value << resultStartBit) | newMoveCounter );
		}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ResWithCount SetRes(Res r)
        {
            return new ResWithCount( (Value & moveCounterBitMask) | ((r.Value) << resultStartBit) );
        }

        public override string ToString()
		{
			return Res.ToString() + (IsUnknown ? " moveCounter=" + MoveCount.ToString() : "");
		}

    }
}
