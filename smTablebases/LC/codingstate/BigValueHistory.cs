using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class BigValueHistory : BigValueHistoryBase
	{
		private Queue<int>          queue;
		private int                 windowSize;
		private int[]               valueToRankHash         = new int[16384]; // Bits 0-13: index1; Bits 14-27: index2; Bits 28-29:  0=empty, 1=index1; 2=index1+index2; 3=index1+index2+hashcollision 
		private int                 minOccurence;


		public BigValueHistory( int windowSize, InitValues initValues, int minOccurence )
		{
			this.windowSize        = windowSize;
			this.minOccurence      = minOccurence;
			queue                  = new Queue<int>( windowSize );
			values                 = new int[windowSize+1];
			occurence              = new int[windowSize+1];
			for ( int i=0 ; i<windowSize ; i++ )
				queue.Enqueue( initValues.IndexToValue(i) );
			Init( windowSize, initValues, minOccurence );
		
			if ( SettingsFix.MaxVirtualDistBits != 27 )
				throw new Exception( "Optimized for 27 Bits maximum. <27 bits possible without change. >27 small changes necessary." );

			BuildHash();
		}


		public void AddValue( int value )
		{
			queue.Enqueue( value );

			int  oldestValue    = queue.Dequeue();
			if ( oldestValue != value )
				UpdateOccurence( value, ValueToRank(value,int.MaxValue), oldestValue, ValueToRank(oldestValue,int.MaxValue), minOccurence );
		}


		public int WindowSize
		{
			get { return windowSize; }
		}


		public override int ValueToRank( int value, int maxRank )
		{
			int rk = valueToRankHash[GetHash(value)];
			int t  = rk>>28;

			if ( t == 0 )
				return -1;
			else if ( t==1 ) {
				int i1 = rk&16383;
				return (i1 < maxRank && values[i1]==value) ? i1 : -1;		
			}
			else {
				int i1 = rk&16383;
				int i2 = (rk>>14)&16383;
				if ( values[i1] == value )
					return i1 < maxRank ? i1 : -1;
				else if ( values[i2] == value )	
					return i2 < maxRank ? i2 : -1;
				else if ( t==3 ) {
					int count = Math.Min( countDifferentValues, maxRank );
					for ( rk=0 ; rk<count ; rk++ ) {
						if ( values[rk] == value )
							return rk;
					}
					return -1;
				}
				else // t=2
					return -1;
			}
		}

		protected override void SwapValues( int i, int j )
		{
			base.SwapValues( i, j );
			int h1 = GetHash(values[i]);
			int h2 = GetHash(values[j]);
			if ( h1==h2 )
				return;
			int v1 = valueToRankHash[h1];
			int v2 = valueToRankHash[h2];
			
			if ( (v2&16383) == i )
				valueToRankHash[h2] = (v2&(~16383)) | j;
			else if ( ((v2>>14)&16383) == i  )
				valueToRankHash[h2] = (v2&(~(16383<<14))) | (j<<14);
			else if ( v2>>28 != 3 )
				throw new Exception();

			if ( (v1&16383) == j )
				valueToRankHash[h1] = (v1&(~16383)) | i;
			else if ( ((v1>>14)&16383) == j  )
				valueToRankHash[h1] = (v1&(~(16383<<14))) | (i<<14);
			else if ( v1>>28 != 3 )
				throw new Exception();
			 
			//valueToRankHash[h1] = i;
			//valueToRankHash[h2] = j;
		}

		private void BuildHash()
		{
			for ( int i=0 ; i<valueToRankHash.Length ; i++ )
				valueToRankHash[i] = 0;
			for ( int i=0 ; i<countDifferentValues ; i++ )
				AddToHash( values[i], i );
		}

		private int GetHash( int v )
		{
			return (v>>26) + ((v>>13)&8191) + (v&8191);
		}

		protected override void ValueAdded( int value, int index )
		{
			AddToHash( value, index );
		}

		protected override void ValueRemoved( int value, int index )
		{
			int h = GetHash(value);
			int v = valueToRankHash[h];

			if ( v>>28 == 1 ) {
				valueToRankHash[h] = 0;
			}
			else if ( v>>28 == 2 ) {
				if ( (v&16383) == index )
					valueToRankHash[h] = (1<<28) | ((v>>14)&16383);
				else
					valueToRankHash[h] = (1<<28) | (v&16383);
			}
			else if ( v>>28 == 3 ) {
				valueToRankHash[h] = 0;
				for ( int i=0 ; i<countDifferentValues ; i++ ) {
					if ( GetHash(values[i]) == h )
						AddToHash(values[i],i);
				}
			}
			else if ( v>>28 == 0 )
				throw new Exception();
		}

		private void AddToHash( int val, int idx )
		{
			int h = GetHash(val);
			int v = valueToRankHash[h];
			if ( v>>28 == 0 )
				valueToRankHash[h] = (1<<28) | idx;
			else if ( v>>28 == 1 )
				valueToRankHash[h] = (2<<28) | (idx<<14) | (valueToRankHash[h]&16383);
			else 
				valueToRankHash[h] = (3<<28) | (valueToRankHash[h]&((1<<28)-1));
		}


//#if DEBUG
		//private static Random rnd = new Random( 0 );

		//private void VerifyHash()
		//{
		//	Dictionary<int,int> dict = new Dictionary<int,int>();

		//	for ( int i=0 ; i<countDifferentValues ; i++ ) {
		//		int h = GetHash( values[i] );
		//		if ( dict.ContainsKey(h) ) 
		//			dict[h] = Math.Min( 3, dict[h]+1 );
		//		else
		//			dict.Add( h, 1 );
		//		if ( ValueToRank(values[i],countDifferentValues) != i )
		//			throw new Exception();
		//	}

		//	for ( int i=0 ; i<valueToRankHash.Length ; i++ ) {
		//		int v = valueToRankHash[i];
		//		if ( dict.ContainsKey(i) ) {
		//			if ( v>>28 != dict[i] )
		//				throw new Exception();
		//		}
		//		else {
		//			if ( v != 0 )
		//				throw new Exception();
		//		}

		//	}

		//	for ( int i=0 ; i<10 ; i++ ) {
		//		int value = rnd.Next( 100000 );
		//		bool found = false;
		//		for ( int j=0 ; j<countDifferentValues ; j++ ) 
		//			found |= values[j] == value;
		//		if ( !found && ValueToRank(value,countDifferentValues)!=-1 )
		//			throw new Exception();
		//	}
		//}
//#endif


	}
}
