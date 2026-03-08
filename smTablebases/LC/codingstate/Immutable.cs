using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public abstract class Immutable<T> where T : Immutable<T>
	{
		private Stack<T>         pool;
		private int              countInstances = 1;
#if DEBUG
		public  int              InstanceId;
		private static int       InstanceIdCounter = 0;
#endif

		private Immutable()
		{
		}


		public Immutable( Stack<T> pool )
		{
			this.pool = pool;
#if DEBUG
			InstanceId = InstanceIdCounter++;
#endif
		}


		public void ResetCountInstances()
		{
			countInstances = 1;
		}


		public void Dispose()
		{
			if ( --countInstances == 0 ) {
				Disposed();
				pool.Push( (T)this );
			}
			else if ( countInstances < 0 )
				throw new ObjectDisposedException(null);
		}


		public void ShareInstance()
		{
#if DEBUG
			if ( countInstances<=0 )
				throw new ObjectDisposedException(null);
#endif
			countInstances++;
		}


		public T Clone( bool disposeOldInstance )
		{
			if ( disposeOldInstance && countInstances == 1 ) {
				return (T)this;
			}
			else {
				T newItem = pool.Pop();
				newItem.countInstances = 1;
				CopyFieldsTo( newItem );
				if ( disposeOldInstance )
					Dispose();
				return newItem;
			}
		}


		public abstract void CopyFieldsTo( T dst );
		public virtual void Disposed() 
		{ 
		}

#if DEBUG
		public virtual void CalcMd5( MD5 md5 )
		{
		}


		public int GetCountInstances()
		{
			return countInstances;
		}


		public override string ToString()
		{
			return "ID=" + InstanceId.ToString() + "  references=" + countInstances.ToString();
		}
#endif
	}
}
