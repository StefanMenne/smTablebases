﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace TBacc
{
	/// <summary>
	/// temporary storage that contains an already decompressed block which contains multiple DataChunks.
	/// On Request the part needed is copied out.
	/// 
	/// ThreadSafety:
	/// 
	/// STATE A: Members A are only written inside TasksTaBaRead which performs a lock itself.
	/// So they are only accessed by one thread a time. 
	///
	/// STATE B:    is Loaded = false;     TaskTaBaReadInfo.UnfinishedTaskCount > 0
	/// Afterwards Members A are unchanged until TaskTaBaReadInfo.UnfinishedTaskCount = 0 
	/// And TmpBlockStorage is locked while changing MEMBERS B and setting isLoaded to True.
	/// 
	/// STATE C:   isLoaded = true
	/// MEMBERS A and MEMBERS B are no more changed. There is only read access.
	/// 
	/// </summary>
	public class TmpBlockStorage
	{
		// MEMBERS A
		private int              blockIndex              = -1;
		private int              piecesIndex             = -1;
		private int[]            offsets                 = new int[2*WkBk.GetCount(true).Index];
		
		// MEMBERS B
		private volatile bool    isLoaded                = false;
		private byte[]           data                    = null;


		// other MEMBERS
		public  int              Index;


		public TmpBlockStorage()
		{
		}


		public int BlockIndex
		{
			get { return blockIndex; }
		}


		public int PiecesIndex
		{
			get { return piecesIndex; }
		}


		public bool IsEmpty
		{
			get { return piecesIndex == -1; }
		}


		public void Clear()
		{
			piecesIndex = -1;
		}


		public byte[] Data
		{
			get {  return data;  }
		}


		public int GetOffset( int i )
		{
			return offsets[i];
		}


		public void SetOffset( int i, int offset )
		{
			offsets[i] = offset;
		}


		public void CreateNew( int piecesIndex, int blockIndex )
		{
			if ( data == null )   // reuse if already created
				data = new byte[(Config.BlockSize*Config.FactorIpSizeDividedBy8)>>3];
			this.piecesIndex         = piecesIndex;
			this.blockIndex        = blockIndex;
			isLoaded               = false;
		}


		public int OrderNumber
		{
			get { 
				return ((piecesIndex==-1) ? 0 : ((piecesIndex<<16) | blockIndex));
			}
		}


		public bool IsLoaded
		{
			get {  return isLoaded; }
			set { isLoaded = value; }
		}


		public override string ToString()
		{
			if ( piecesIndex == -1 )
				return "Empty";
			return "Pieces=" + piecesIndex.ToString() + " Chunk=" + "Loaded=" + isLoaded.ToString() + " blockIndex=" + blockIndex.ToString();
		}
	}
}
