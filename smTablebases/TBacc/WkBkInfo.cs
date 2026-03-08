using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public sealed class WkBkInfo
	{
		private Field[]      wM;
		private Field[]      bM;
		public  WkBkMvInfo[] DstToWmvInfo;
		public  WkBkMvInfo[] DstToBmvInfo;

		public WkBkInfo( Field[] wM, Field[] bM )
		{
			this.wM = wM;
			this.bM = bM;
		}

		public Field GetMv( bool wtm, int index )
		{
			return wtm ? (wM[index]) : (bM[index]);
		}

		public int GetCount( bool wtm )
		{
			return wtm ? CountW : CountB;
		}

		public int CountW
		{
			get{ return wM.Length; }
		}

		public int CountB
		{
			get{ return bM.Length; }
		}

		public WkBkMvInfo[] GetDstToMvInfo( bool wtm )
		{
			return wtm ? DstToWmvInfo : DstToBmvInfo;
		}
	}
}
