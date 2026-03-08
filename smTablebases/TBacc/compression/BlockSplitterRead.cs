using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Concurrent;

namespace TBacc
{
	public class BlockSplitterRead : BlockSplitter
	{
#if DEBUG
		private        int                            piecesIndex        = 0; 
		private static ConcurrentDictionary<int,bool> loadedDictionary = null;
#endif

		public  TBHeader        Header;

#if DEBUG
		public static void ActivateVerifyingDoubleLoadingOfBlocks()
		{
			if ( Config.VerifyDoubleBlockLoad )
				loadedDictionary = new ConcurrentDictionary<int,bool>(); 
		}

		public static void DeactivateVerifyingDoubleLoadingOfBlocks()
		{
			loadedDictionary = null; 
		}
#endif



		public BlockSplitterRead( string filename, Pieces pieces ) : base()
		{
#if DEBUG
			piecesIndex = pieces.Index;
#endif	
			fileStream      = new FileStream( filename, FileMode.Open, FileAccess.Read, FileShare.Read, Config.ReadBufferSize );
			Header          = new TBHeader( fileStream );
			compressionType = Header.CompressionType;
			int blockCount = Tools.ReadIntFromStream( fileStream );
			Init( blockCount );
			for ( int i=0 ; i<=blockCount ; i++ ) {
				blockOffsetsInBytes[i] = Tools.ReadLongFromStream( fileStream );
			}
		}


		public int ReadBlock( byte[] buffer, int blockIndex )
		{
#if DEBUG
			if ( loadedDictionary!=null ) {
				int key = piecesIndex<<16 | blockIndex;

				if ( loadedDictionary.ContainsKey( key ) )
					throw new Exception( "Block read twice \"" + fileStream.Name + "\" " + blockIndex.ToString() );				
				loadedDictionary.GetOrAdd( key, true );
			}
#endif
			int blockSize = (int)(blockOffsetsInBytes[blockIndex+1] - blockOffsetsInBytes[blockIndex]);
			lock( this ) {
				fileStream.Seek( blockOffsetsInBytes[blockIndex], SeekOrigin.Begin );
				fileStream.Read( buffer, 0, blockSize );
			}
			return blockSize;
		}

		
		public int BlockSize
		{
			get{ return Header.BlockSize; }
		}


		public string Version
		{
			get{ return Header.VersionString; }
		}


		public int WtmMaxWiIn
		{
			get{ return Header.WtmMaxWiIn; }
		}

		
		public int WtmMaxLsIn
		{
			get { return Header.WtmMaxLsIn; }
		}


		public int BtmMaxWiIn
		{
			get { return Header.BtmMaxWiIn; }
		}


		public int BtmMaxLsIn
		{
			get { return Header.BtmMaxLsIn; }
		}

		public int BitsPerEntryWtm
		{
			get { return Header.BitsPerEntryWtm; }
		}

		public int BitsPerEntryBtm
		{
			get { return Header.BitsPerEntryBtm; }
		}

		public void Close()
		{
			fileStream.Close();
			fileStream.Dispose();
		}






	}
}
