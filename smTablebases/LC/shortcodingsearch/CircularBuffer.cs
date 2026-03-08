using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CircularBuffer<T>
	{
		private T[]   array;
		private int   firstItem  = 0;
		private int   countItems = 0;
		private int   mask;


		public CircularBuffer( int maxCount )
		{
			int bitCount = Tools.Log2ForAnyNumber( maxCount ) + 1;
			array        = new T[1<<bitCount];
			mask         = (1<<bitCount)-1;
		}


		public int Count
		{
			get {  return countItems; }
		}


		public T this[int index]
		{
			get{
#if DEBUG
				if ( index >= countItems )
					throw new IndexOutOfRangeException();
#endif			
				return array[(firstItem+index)&mask];
			}
			set{
#if DEBUG
				if ( index >= countItems )
					throw new IndexOutOfRangeException();
#endif			
				array[(firstItem+index)&mask] = value;
			}
		}


		public void RemoveAtFront( int count = 1 )
		{
#if DEBUG
			if ( count > countItems )
				throw new Exception();
#endif
			firstItem   = (firstItem+count)&mask;
			countItems -= count;
		}


		public void RemoveAtEnd( int count = 1 )
		{
#if DEBUG
			if ( count > countItems )
				throw new Exception();
#endif
			countItems -= count;
		}


		public T GetAndRemoveFirst()
		{
#if DEBUG
			if ( 1 > countItems )
				throw new Exception();
#endif
			T v = this[0];
			firstItem   = (firstItem+1)&mask;
			countItems--;
			return v;
		}


		public T GetAndRemoveLast()
		{
#if DEBUG
			if ( 1 > countItems )
				throw new Exception();
#endif
			T v = this[Count-1];
			countItems --;
			return v;
		}


		public void AddAtFront( int count=1 )
		{
#if DEBUG
			if ( count+countItems >= array.Length )
				throw new Exception();
#endif
			firstItem   = (firstItem+array.Length-count)&mask;
			countItems += count;
		}


		public void AddAtFront( T item )
		{
#if DEBUG
			if ( countItems+1 >= array.Length )
				throw new Exception();
#endif
			firstItem   = (firstItem+array.Length-1)&mask;
			countItems++;
			this[0] = item;
		}


		public void AddAtEnd( int count=1 )
		{
#if DEBUG
			if ( count+countItems >= array.Length )
				throw new Exception();
#endif
			countItems += count;
		}


		public void AddAtEnd( T item )
		{
#if DEBUG
			if ( 1+countItems >= array.Length )
				throw new Exception();
#endif
			countItems += 1;
			this[countItems-1] = item;

		}



	}
}
