using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;


namespace TBacc
{
	public sealed class DataChunkRead : DataChunk
	{
		public   const  int            ArrayItemSizeInBytes                = sizeof(Int16);

		private  byte[]                data;
		private  Res[]                 intToRes;
		private  int                   bitsPerEntry;
		private  TaBaRead              taBaRead;
		private  bool                  dataValid;                 // false if not yet loaded

		public DataChunkRead( TaBaRead taBaRead, Pieces pieces, bool wtm, WkBk wkbk, long firstIndex, long indexCount, Res[] intToRes, int bitsPerEntry, int chunkIndex ) : base( wtm, wkbk, pieces, firstIndex, indexCount, chunkIndex )
		{
			this.intToRes                        = intToRes;
			this.bitsPerEntry                    = bitsPerEntry;
			this.taBaRead                        = taBaRead;
			this.dataValid                       = false;
		}


		public bool DataValid
		{
			get { return dataValid; }
			set { dataValid = value; }
		}



		public override void Join( DataChunkMemory mem, long counterToIdentifyOldestEntry, bool fixedJoin )
		{
			base.Join(mem,counterToIdentifyOldestEntry,fixedJoin);
			DataChunkMemoryRead dcm = (DataChunkMemoryRead)mem;
			data                    = dcm.Memory;
		}


		public override void Unjoin()
		{
			base.Unjoin();
			data                      = null;
			dataValid = false;
		}
		

		public Res Get( long index )
		{
			return intToRes[(((int)data[2*index])<<8) | data[2*index+1]];
		}
		
		public byte[] Data
		{
			get{ return data; }
		}
		
		
		public void CalcMd5( MD5 md5, byte[] buffer )
		{
			IndexPos indexPosComp       = new IndexPos( wkBk, pieces, wtm );
			IndexPos indexPosMd5        = new IndexPos( wkBk, pieces, wtm, IndexPosType.Verify, null );
			long     indexCountMd5      = indexPosMd5.IndexCount;
			IndexEnumerator indexEnumerator = new IndexEnumerator( indexPosMd5, indexPosComp, false );
			for ( long indexOffset=0 ; indexOffset<indexCountMd5 ; indexOffset+=32768 ) {
				int currentIndexCount = (int)Math.Min( 32768, indexCountMd5-indexOffset );
				for ( int i=0 ; i<currentIndexCount ; i++ ) {
					int val;
					if ( indexEnumerator.IndexPosDstValid )
						val = Get( indexEnumerator.IndexDst ).Value;
					else
						val = Res.IllegalPos.Value;
					buffer[2*i]   = (byte)(val&255);
					buffer[2*i+1] = (byte)(val/256);
					indexEnumerator.IncSrcIndex();
				}
#if DEBUG
				if ( Config.SaveDataChunksAtMd5 ) {
					byte[] tmp = new byte[2*currentIndexCount];
					Array.Copy( buffer, tmp, 2*currentIndexCount );
					if ( !Directory.Exists(TBaccess.DebugFolder) )
						Directory.CreateDirectory( TBaccess.DebugFolder );
					File.WriteAllBytes( Path.Combine( TBaccess.DebugFolder, pieces.ToString() + "_" + wkBk.Index.ToString("0000") + "_" + (wtm ? "wtm" : "btm") + "_" + (indexOffset/32768).ToString("0000") + ".bin" ), tmp );
				} 
#endif

				md5.TransformBlock( buffer, 0, 2*currentIndexCount, buffer, 0 );
			}
		}

	}
}
