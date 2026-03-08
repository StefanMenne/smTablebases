using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public class GetDataChunkInfo
	{
		private WkBk           wkBk;
		private bool           wtm;
		private bool           forWriteData, forWriteFastBits;
		private DataChunkWrite dataChunk;


		public GetDataChunkInfo( WkBk wkBk, bool wtm, bool forWriteData, bool forWritePotentialNew )
		{
			this.wkBk                 = wkBk;
			this.wtm                  = wtm;
			this.forWriteData         = forWriteData;
			this.forWriteFastBits = forWritePotentialNew;
		}

		public WkBk WkBk
		{
			get{ return wkBk; }
			set{ wkBk = value; }
		}

		public bool Wtm
		{
			get { return wtm; }
			set { wtm = value; }
		}

		public bool ForWriteData
		{
			get{ return forWriteData; }
			set{ forWriteData = value; }
		}

		public bool ForWriteFastBits
		{
			get { return forWriteFastBits; }
			set { forWriteFastBits = value; }
		}

		public DataChunkWrite DataChunk
		{
			get { return dataChunk; }
			set { dataChunk = value; }
		}

		public override string ToString()
		{
			return "WkBk="+wkBk.ToString()+ (wtm ?" wtm" : " btm") + " write=" + forWriteData.ToString() + "/" + forWriteFastBits.ToString();
		}
	}
}
