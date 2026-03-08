using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TBacc;

namespace smTablebases
{
	public sealed class TaBaWrite : TaBa
	{

		private long                  totalIndexCount = 0L;
		private ResCountConvert       resCountConvertWtm, resCountConvertBtm;
		private long                  deltaFinalResCount        = 0L;
		private TaBaWriteHeader       header;

		public TaBaWrite( Pieces p, TaBaOpenType ot, int threadCount ) : base( p )
		{
			pieces                 = p;
			btmOffset              = WkBk.GetCount( pieces ).Index;

			filename = GetFilename(p);
			if ( !Directory.Exists( Path.GetDirectoryName(filename) ) )
				Directory.CreateDirectory( Path.GetDirectoryName(filename) );

			storage = new Storage( filename, ot==TaBaOpenType.CreateNewForWrite, Settings.ReadWriteTmpBuffer );
			if ( ot == TaBaOpenType.OpenForWrite ) {
				storage.Position = 0;
				header = new TaBaWriteHeader( storage );
				long bytePos = TaBaWriteHeader.HeaderSizeInBytes;
				resCountConvertWtm = new ResCountConvert( storage, bytePos );
				bytePos += resCountConvertWtm.ReadWriteByteCount;
				resCountConvertBtm = new ResCountConvert( storage, bytePos );
			}
			else {
				header = new TaBaWriteHeader( true );
				resCountConvertWtm = new ResCountConvert( ResWithCount.BitCount, TbInfoFileList.Current.GetBitsPerResWtm() );
				resCountConvertBtm = new ResCountConvert( ResWithCount.BitCount, TbInfoFileList.Current.GetBitsPerResBtm() );
			}

			List<DataChunk> list = new List<DataChunk>();
			int chunkIndex = 0;
			foreach ( bool wtm in Tools.BoolArray ) {
				for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
					IndexPos    indexPos              = new IndexPos(wkBk,pieces,wtm);
					long           indexCountCurrent   = indexPos.IndexCount;
					DataChunk      dataChunkWrite        = new DataChunkWrite( pieces, wtm, wkBk, totalIndexCount, indexCountCurrent, GetBitsPerEntry(wtm), chunkIndex );
					if ( wtm )
						maxIndexCountPerChunkWtm = Math.Max( maxIndexCountPerChunkWtm, indexCountCurrent );
					else
						maxIndexCountPerChunkBtm = Math.Max( maxIndexCountPerChunkBtm, indexCountCurrent );
					totalIndexCount                  += indexCountCurrent;
					list.Add( dataChunkWrite );
				}
			}
			dataChunk = list.ToArray();
		}

		public void Init()
		{
			byteCount = ByteCountData;
			for ( int i=0 ; i<dataChunk.Length ; i++ ) {
				DataChunkWrite dcw = (DataChunkWrite)dataChunk[i];
				dcw.Init( byteCount );
				byteCount += dcw.GetByteCount();
			}
		}

		public void AddToLog( string s )
		{
			header.Log += s;
		}

		public void WriteLog( string filename, bool append )
		{
			if ( append )
				File.WriteAllText( filename, File.ReadAllText(filename) + "\r\n" + header.Log );
			else
				File.WriteAllText( filename, header.Log );
		}

		public ResCountConvert GetResCountConvert( bool wtm )
		{
			if ( wtm )
				return resCountConvertWtm;
			else
				return resCountConvertBtm;
		}


		public void UpdateResCountConvertMaxBitCount()
		{
			header.ResCountConvertMaxBitsWtm = Math.Max( header.ResCountConvertMaxBitsWtm, resCountConvertWtm.CurrentlyUsedBitCountForIndices );
			header.ResCountConvertMaxBitsBtm = Math.Max( header.ResCountConvertMaxBitsBtm, resCountConvertBtm.CurrentlyUsedBitCountForIndices );
		}


		public int ResCountConvertMaxBitsWtm
		{
			get { return header.ResCountConvertMaxBitsWtm; }
		}


		public int ResCountConvertMaxBitsBtm
		{
			get { return header.ResCountConvertMaxBitsBtm; }
		}


		public int MaxDtmHm
		{
			get { return header.MaxDtmHm; }
			set { header.MaxDtmHm = value; }
		}

		public int WtmMaxWiIn
		{
			get { return header.WtmMaxWinIn; }
			set { header.WtmMaxWinIn = value; }
		}

		public int WtmMaxLsIn
		{
			get { return header.WtmMaxLsIn; }
			set { header.WtmMaxLsIn = value; }
		}

		public int BtmMaxWiIn
		{
			get { return header.BtmMaxWinIn; }
			set { header.BtmMaxWinIn = value; }
		}

		public int BtmMaxLsIn
		{
			get { return header.BtmMaxLsIn; }
			set { header.BtmMaxLsIn = value; }
		}

		public ResCountConvert ResCountConvertWtm
		{
			get { return resCountConvertWtm; }
			set{ resCountConvertWtm = value; }
		}

		public ResCountConvert ResCountConvertBtm
		{
			get { return resCountConvertBtm; }
			set{  resCountConvertBtm = value;	}
		}

		public int GetBitsPerEntry( bool wtm )
		{
			return wtm ? BitsPerEntryWtm : BitsPerEntryBtm;
		}

		public int BitsPerEntryWtm
		{
			get{ return resCountConvertWtm.MaxIndexBitCount; }
			}

		public int BitsPerEntryBtm
		{
			get { return resCountConvertBtm.MaxIndexBitCount; }
		}

		public long TotalIndexCount
		{
			get{ return totalIndexCount; }
		}

		public string AddFinalResCount( long finalResCount, long deltaFinalResToProcessCount )
		{
			header.FinalResCount += (deltaFinalResCount = finalResCount);
			header.FinalResToProcessCount += deltaFinalResToProcessCount;

			// regard only nonEp; The additional EP-cap-move is processed already in EpCapAndInit; So the two equal positions
			// (one non EP and one EP) getting final in the same passIndex
			if ( header.FinalResCount == totalIndexCount )
				return "100 %";
			else
				return (100.0*((double)header.FinalResCount/(double)totalIndexCount)).ToString("##0.00") + " %";
		}


		public bool AllPosFinal
		{
			get{ return header.FinalResCount==totalIndexCount; }
		}


		public long DeltaFinalResCount
		{
			get{ return deltaFinalResCount; }
		}

		public long FinalResCount
		{
			get{ return header.FinalResCount; }
		}

		public long FinalResToProcessCount
		{
			get { return header.FinalResToProcessCount; }
		}


		public void CloseAndDelete( bool dontSaveToDisk )
		{
			Close( dontSaveToDisk );
			File.Delete( filename );
		}


		public override void Close( bool dontSaveToDisk )
		{
			base.Close( dontSaveToDisk );
		}

		public void WriteData()
		{
			storage.Position = 0;
			header.Write( storage );
			long bytePos = TaBaWriteHeader.HeaderSizeInBytes;
			resCountConvertWtm.Save( storage, bytePos );
			bytePos += resCountConvertWtm.ReadWriteByteCount;
			resCountConvertBtm.Save( storage, bytePos );
		}

		public int ByteCountData
		{
			get{ return TaBaWriteHeader.HeaderSizeInBytes + resCountConvertWtm.ReadWriteByteCount + resCountConvertBtm.ReadWriteByteCount; }
		}


		public void LoadDataChunk( DataChunkWrite data )
		{
			data.Load( storage );
		}


		public void Save( DataChunkWrite data, bool saveData, bool savePotentialNew )
		{
			data.Save( storage, saveData, savePotentialNew );
		}


		public static string GetFilename( Pieces pieces )
		{
			string filename = Path.Combine(TBaccess.GetDatabaseFolder(pieces.PieceCount+2),pieces.ToString()+ "_tmp.bin");
			string filenameInTmpFolder;
			if ( Settings.TmpFolder!=null && Settings.TmpFolder.Length!=0 && Directory.Exists(Settings.TmpFolder) )
				filenameInTmpFolder = Path.Combine(Settings.TmpFolder,pieces.ToString()+ "_tmp.bin");
			else
				filenameInTmpFolder = "";

			if ( filenameInTmpFolder.Length!=0 && !File.Exists(filename) )
				return filenameInTmpFolder;
			else
				return filename;
		}

	}
}
