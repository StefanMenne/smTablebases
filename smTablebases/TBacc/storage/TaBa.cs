using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace TBacc
{

	public enum TaBaOpenType
	{
		ReadOnly,
		CreateNewForWrite,
		OpenForWrite
	}


	public class TaBa
	{
		protected   long            byteCountWtm;
		protected   long            byteCountBtm;
		protected   long            byteCount                  = 0L;
		protected   Pieces          pieces;
		protected   Storage         storage;
		protected   string          filename;
		protected   DataChunk[]     dataChunk;
		protected   int             btmOffset;
		protected   long            maxIndexCountPerChunkWtm = 0L;
		protected   long            maxIndexCountPerChunkBtm = 0L;

		public TaBa( Pieces p )
		{
			pieces             = p;
		}

		public Pieces Pieces
		{
			get{ return pieces; }
		}

		public DataChunk GetDataChunk( bool wtm, WkBk wkbk )
		{
			return dataChunk[wkbk.Index+(wtm?0:btmOffset)];
		}

		public DataChunk GetDataChunk( int index )
		{
			return dataChunk[index];
		}

		public int DataChunkCount
		{
			get{ return dataChunk.Length; }
		}

		public virtual void Close( bool dontSaveToDisk )
		{
			storage.Close( dontSaveToDisk );
		}

		public long MaxIndexCountPerChunkWtm
		{
			get{ return maxIndexCountPerChunkWtm; }
		}

		public long MaxIndexCountPerChunkBtm
		{
			get { return maxIndexCountPerChunkBtm; }
		}

		public long ByteCount
		{
			get{ return byteCount; }
		}

		public Storage Storage
		{
			get{ return storage; }
		}


		public string Filename
		{
			get{ return filename; }
		}


	}
}
