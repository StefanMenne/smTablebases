using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class Collection<T> : Immutable<Collection<T>> where T : Immutable<T>
	{
		private T[] items;


		public Collection( int countArrays, Stack<Collection<T>> pool ) : base( pool )
		{
			items = new T[countArrays];
		}

		
		public Collection<T> ChangeItem( int index, bool disposeOldInstance )
		{
			Collection<T> newCollection = Clone( disposeOldInstance );
			newCollection[index] = newCollection[index].Clone( true );
			return newCollection;
		}


		public T this[int index]
		{
			get{ return items[index]; }
			set{ items[index] = value; }
		}


		public int Count
		{
			get { return items.Length; }
		}


		public override void CopyFieldsTo( Collection<T> dst )
		{
			for ( int i=0 ; i<Count ; i++ ) {
				items[i].ShareInstance();
				dst[i] = items[i];	
			}
		}


		public override void Disposed()
		{
			for ( int i=0 ; i<Count ; i++ )
				items[i].Dispose();
		}


#if DEBUG
		public override void CalcMd5( MD5 md5 )
		{
			for ( int i=0 ; i<Count ; i++ )
				items[i].CalcMd5( md5 );
		}
#endif
	}
}
