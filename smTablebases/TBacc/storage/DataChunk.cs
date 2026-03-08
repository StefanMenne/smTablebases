using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace TBacc
{
	public abstract class DataChunk
	{
#if DEBUG
		public     int              TrackIndex      = -1;
#endif

		protected  DataChunkMemory  dataChunkMemory;
		protected  Pieces           pieces;
		protected  bool             wtm;
		protected  WkBk             wkBk;
		protected  long             firstIndex;
		protected  long             indexCount;
		protected  int              chunkIndex;


		public DataChunk( bool wtm, WkBk wkbk, Pieces pieces, long firstIndex, long indexCount, int chunkIndex )
		{
			this.pieces         = pieces;
			this.wtm             = wtm;
			this.wkBk            = wkbk;
			this.firstIndex      = firstIndex;
			this.indexCount    = indexCount;
			this.chunkIndex      = chunkIndex;
		}


		public int GetDataChunkIndex()
		{
			return DataChunkIndex.Get(wkBk,wtm);
		}


		public DataChunkMemory DataChunkMemory
		{
			get{ return dataChunkMemory; }
		}


		public virtual bool IsWriteDataChunk
		{
			get { return false; }
		}

		public bool Wtm
		{
			get{ return wtm; }
		}

		
		public WkBk WkBk
		{
			get{ return wkBk; }
		}


		public Pieces Pieces
		{
			get { return pieces; }
		}

		
		public long IndexCount
		{
			get{ return indexCount; }
		}





		
		public virtual void Join( DataChunkMemory mem, long counterToIdentifyOldestEntry, bool fixedJoin )
		{
			dataChunkMemory   = mem;
			mem.DataChunk     = this;
			mem.LastJoin      = counterToIdentifyOldestEntry;

		}


		public virtual void Unjoin()
		{
			dataChunkMemory.DataChunk = null;
			dataChunkMemory           = null;
		}	
		
		public override string ToString()
		{
			return "WkBk=" + wkBk.Index.ToString() + " Pieces=" + pieces.ToString() + (wtm?" wtm ":" btm "); 
		}	
	}
}
