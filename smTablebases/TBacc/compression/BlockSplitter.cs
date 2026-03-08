using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TBacc
{
	/// <summary>
	/// Bytes          Size(Bytes)    Data
	/// ..             ..             Header
	/// 1 ... 4        4              Block count
	///                8              Offset to Block 0 data
	///                8              Offset to Block 1 data
	/// ...
	///                8              Offset to Block blockCount-1 data 
	///                8              File Size
	///                x              Block 0 data
	/// </summary>
	public class BlockSplitter
	{
		protected  FileStream          fileStream;
		protected  long[]              blockOffsetsInBytes;
		protected  CompressionType     compressionType;
		protected  const long          byteOffsetToBlockOffsetTable = TBHeader.HeaderSize + 4;
		protected  int                 threadCount;

		protected BlockSplitter()
		{
		}


		protected void Init( int blockCount )
		{
			blockOffsetsInBytes = new long[blockCount + 1];
		}


		public int BlockCount
		{
			get{ return blockOffsetsInBytes.Length-1; }
		}


		public int GetBlockSize( int blockIndex )
		{
			return (int)(blockOffsetsInBytes[blockIndex+1]-blockOffsetsInBytes[blockIndex]);
		}


		public CompressionType CompressionType
		{
			get{ return compressionType; }
		}


		public long FileSize
		{
			get {  return blockOffsetsInBytes[blockOffsetsInBytes.Length-1]; }
		}
	}
}
