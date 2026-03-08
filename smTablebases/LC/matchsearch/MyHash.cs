using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public struct Entry
	{
		public Int64 Key;   // key could be made to int; because one id could be easily calculated from the idCombined 
		public int   Next;
		public Int64 Value;
	}


	public class MyHash
	{
		private int[]     buckets;
		private Entry[]   entries;
		private int       count        = 0;
		private int       indexLastGet = -1;

		// just for information
		private int maxEntriesPerBucket = 0, usedBuckets = 0;


		public MyHash( int bucketCount, int initialEntryCount=-1 )
		{
			buckets = new int[GetPrime(bucketCount)];
			for ( int i=0 ; i<buckets.Length ; i++ )
				buckets[i] = -1;
			entries = new Entry[ ( (initialEntryCount==-1) ? bucketCount : initialEntryCount ) ];
		}


		public int MaxEntriesPerBucket
		{
			get {  return maxEntriesPerBucket; }
		}


		public int UsedBuckets
		{
			get {  return usedBuckets; }
		}


		public void Add( Int64 key, Int64 value )
		{
			int hash       = GetHash(key);
			int index      = buckets[hash];
			if ( count == entries.Length ) {
				Entry[] entriesNew = new Entry[3*entries.Length/2];
				Array.Copy( entries, entriesNew, entries.Length );
				entries = entriesNew;
			}
			if ( index == -1 )
				usedBuckets++;
			entries[count] = new Entry(){ Key = key, Value = value, Next = index };
			buckets[hash]  = count++;
		}                


		public Int64 Get( Int64 key )
		{
			int hash       = GetHash(key);
			indexLastGet   = buckets[hash];

			int c = 0;
			while ( indexLastGet != -1 ) {
				if ( entries[indexLastGet].Key == key ) {
					if ( c > maxEntriesPerBucket )
						maxEntriesPerBucket = c;
					return entries[indexLastGet].Value;
				}
				indexLastGet = entries[indexLastGet].Next;
				c++;
			}
			return -1;
		}


		public void SetLastAccessed( Int64 value )
		{
			entries[indexLastGet].Value = value;
		}


		private int GetHash( Int64 v )
		{
			return (int)( v % buckets.Length );
		}


		public static readonly int[] primes = new int[]
		{
			3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597,
			1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627,
			52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827,
			807403, 968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369,
			10000019, 15000017, 17000023, 20000003, 25000009
		};


		public static int ExpandPrime(int oldSize)
		{
			int num = 2 * oldSize;
			if ( num > 2146435069 && 2146435069 > oldSize )
				return 2146435069;
			return GetPrime(num);
		}


		public static int GetPrime(int min)
		{
			if (min < 0)
				throw new ArgumentException();

			for (int i = 0; i < primes.Length; i++) {
				int num = primes[i];
				if (num >= min)
					return num;
			}
			for (int j = min | 1; j < 2147483647; j += 2) {
				if (IsPrime(j) && (j - 1) % 101 != 0)
					return j;
			}
			return min;
		}


		public static bool IsPrime(int candidate)
		{
			if ((candidate & 1) != 0) {
				int num = (int)Math.Sqrt((double)candidate);
				for (int i = 3; i <= num; i += 2) {
					if (candidate % i == 0)
						return false;
				}
				return true;
			}
			return candidate == 2;
		}
	}
}
