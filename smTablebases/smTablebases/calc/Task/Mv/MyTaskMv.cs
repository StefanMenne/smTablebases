using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class MyTaskMv : MyTaskWtm
	{
		public static bool                FastBit = true;
		public static FastBitsInterval    FastBitsInterval;

		public  static bool   PrintInfos     = false;

		private    WkBkMvInfo[]             moveInfo;
		private    bool                     fastCalculation          = false;
		private    Res                      winResToGen, lsResToGen;
		private    int                      winResWithCountToGen_ForFastBits;
		private    WkBk[]                   wkBkSrc;
		private    WkBk                     wkBkDst;
		private    Field[]                  kDestWithoutMirror;
		private    IndexPos[]               indexPosSrcMvK;
		private    DataChunkWrite[]         dataSrcMvK;
		private    DataChunkWrite           dataSrc, dataDst;
		private    FastBits[]               fastBitsSrcMvK;
		private    IndexPos                 indexPosSrc, indexPosDst;
		private    int                      lsValueDst;
		private    int                      count;
		private    BitBrd                   wkBkBits;
		private    long                     finalResCountCurrent, finalResToProcessCountCurrent;


		public MyTaskMv( CalcTB calc, WkBk wkBkDst, bool wtm ) : base( calc, wtm )
		{
			this.wkBkDst = wkBkDst;
		}


		public WkBk WkBkDst
		{
			get{ return wkBkDst; }
		}


		public override bool IsMv
		{
			get{ return true; }
		}


		public WkBkMvInfo[] GetMvInfo()
		{
			return wkBkDst.Info.GetDstToMvInfo( Wtm );
		}



		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
			TasksMv  tasksMv     = (TasksMv)tasks;
			int maxDataChunksToUse = tasksMv.NofFreeWriteDataChunks - (MyTaskMv.MinNofWriteDataChunksPerThread * freeThreads);
			CalcTB   calc        = tasksMv.CalcTB;
			Pieces   pieces      = tasksMv.Pieces;
			moveInfo             = wkBkDst.Info.GetDstToMvInfo( wtm );
			winResToGen          = tasksMv.Step.WinResToGen;
			lsResToGen           = tasksMv.Step.GetLsResToGen(wtm);
			winResWithCountToGen_ForFastBits = (winResToGen.WinIn<MyTaskMv.FastBitsInterval.WinInMaxPlus1) ? (new ResWithCount( winResToGen ).Value) : -1;
			lsValueDst           = (new ResWithCount( winResToGen.HalfMoveToMate )).Value;
			wkBkBits             = wkBkDst.Bits;
			indexPosDst          = new IndexPos( wkBkDst, pieces, !wtm );
			count                = (int)indexPosDst.IndexCount;
			fastCalculation      = ( maxDataChunksToUse >= moveInfo.Length+2 ) && !Config.ForceNonFastCalculation;
			if ( !fastCalculation && !TasksMv.FastCalcDeactivatedPrinted ) {
				TasksMv.FastCalcDeactivatedPrinted = true;
				Message.Text( " Fast calc deactivated " );
			}
			indexPosSrc          = new IndexPos( wkBkDst, pieces,  wtm );
			finalResCountCurrent = finalResToProcessCountCurrent = 0;

			if ( fastCalculation ) {
				GetDataChunkInfo[] info = new GetDataChunkInfo[moveInfo.Length+2];
				wkBkSrc              = new WkBk[moveInfo.Length];
				kDestWithoutMirror   = new Field[moveInfo.Length];
				indexPosSrcMvK       = new IndexPos[moveInfo.Length];
				dataSrcMvK           = new DataChunkWrite[moveInfo.Length];
				fastBitsSrcMvK       = new FastBits[moveInfo.Length];
				info[0]              = new GetDataChunkInfo( wkBkDst,  wtm, true,  true );
				info[1]              = new GetDataChunkInfo( wkBkDst, !wtm, false, true );
				for ( int i=0 ; i<moveInfo.Length ; i++ ) {
					wkBkSrc[i]             = moveInfo[i].WkBkSrc;
					kDestWithoutMirror[i]  = moveInfo[i].KDestWithoutMirror;
					indexPosSrcMvK[i]      = new IndexPos( wkBkSrc[i], pieces,  wtm );
					info[2+i]              = new GetDataChunkInfo( wkBkSrc[i], wtm, true, true );
				}
				calc.TaBasesWrite.GetDataChunks( info, threadIndex );
				dataSrc = info[0].DataChunk;
				dataDst = info[1].DataChunk;
				for ( int i=0 ; i<moveInfo.Length ; i++ ) {
					dataSrcMvK[i] = info[2+i].DataChunk;
					fastBitsSrcMvK[i] = dataSrcMvK[i].GetFastBits();
				}
			}
			tasksMv.NofFreeWriteDataChunks = tasksMv.NofFreeWriteDataChunks - NofDataChunksToUse;
		}


		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			TasksMv tasksMv = (TasksMv)tasks;
			Pieces  pieces  = tasksMv.Pieces;

			if ( fastCalculation ) {
				Mv_MvK( pieces, tasksMv );
			}
			else {
				for ( int i=0 ; i<moveInfo.Length ; i++ )
					MvK( pieces, tasksMv, moveInfo[i].WkBkSrc, moveInfo[i].Mirror, moveInfo[i].KDestWithoutMirror, threadIndex );
				Mv( pieces, tasksMv, threadIndex );
			}
		}


		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			TasksMv  tasksMv     = (TasksMv)tasks;
			CalcTB   calc        = tasksMv.CalcTB;

			if ( fastCalculation ) {
				calc.TaBasesWrite.FreeDataChunk( dataSrc );
				calc.TaBasesWrite.FreeDataChunk( dataDst );
				for ( int i=0 ; i<dataSrcMvK.Length ; i++ )
					calc.TaBasesWrite.FreeDataChunk( dataSrcMvK[i] );
			}
			TasksMv.FinalResCount += finalResCountCurrent;
			TasksMv.FinalResToProcessCount += finalResToProcessCountCurrent;
			tasksMv.NofFreeWriteDataChunks += NofDataChunksToUse;
		}


		private void Mv_MvK( Pieces pieces, TasksMv tasksMv )
		{
			FastBits    potNewPosSrc         = dataSrc.GetFastBits( count );
			FastBits    potNewPosDst         = dataDst.GetFastBits( count );
			DoMvAll     doMvAll              = new DoMvAll( potNewPosSrc, indexPosSrc, dataSrc, winResToGen.WinIn, moveInfo, fastBitsSrcMvK, indexPosSrcMvK, dataSrcMvK, lsResToGen, indexPosDst, tasksMv.GetUpdateSrcResWithLsIndexToInfoArray(wtm) );

			MainLoop( pieces, doMvAll, potNewPosDst, true );
        }


		private void Mv( Pieces pieces, TasksMv tasksMv, int threadIndex )
		{
			CalcTB calc = tasksMv.CalcTB;
			calc.TaBasesWrite.GetDataChunks( out dataSrc, wkBkDst,  wtm, true, true, out dataDst, wkBkDst, !wtm, false, true, threadIndex );
			FastBits  potNewPosDst         = dataDst.GetFastBits( count );
			FastBits  potNewPosSrc         = dataSrc.GetFastBits();
			DoMv doMv = new DoMv( potNewPosSrc, indexPosSrc, dataSrc, winResToGen.WinIn, lsResToGen, indexPosDst, tasksMv.GetUpdateSrcResWithLsIndexToInfoArray(wtm) );

			MainLoop( pieces, doMv, potNewPosDst, true );

			calc.TaBasesWrite.FreeDataChunk( dataSrc );
			calc.TaBasesWrite.FreeDataChunk( dataDst );
		}


		private void MvK( Pieces pieces, TasksMv tasksMv, WkBk wkBkSrc, MirrorType mirrorType, Field kDestWithoutMirror, int threadIndex )
		{
			CalcTB calc = tasksMv.CalcTB;
			IndexPos            indexPosSrc    = new IndexPos( wkBkSrc, pieces,  wtm );
			calc.TaBasesWrite.GetDataChunks( out dataSrc, wkBkSrc,  wtm, true, true, out dataDst, wkBkDst, !wtm, false, false, threadIndex );
			FastBits   potNewPosDst   = dataDst.GetFastBits( count );
			FastBits   potNewPosSrc   = dataSrc.GetFastBits();
			DoMvK doMv = new DoMvK( potNewPosSrc, indexPosSrc, dataSrc, winResToGen.WinIn, mirrorType, lsResToGen, indexPosDst, tasksMv.GetUpdateSrcResWithLsIndexToInfoArray(wtm) );

			MainLoop( pieces, doMv, potNewPosDst, false );

			calc.TaBasesWrite.FreeDataChunk( dataSrc );
			calc.TaBasesWrite.FreeDataChunk( dataDst );
		}


		private void MainLoop( Pieces pieces, DoMvBase doMv, FastBits fastBitsDst, bool removePotNewPosBit )
		{
			indexPosDst.SetToIndex( 0 );
			int       winValueDst       = (new ResWithCount( lsResToGen.HalfMoveToMate )).Value;
			Fields    flds              = indexPosDst.GetFields();
			int       index, oldIndex=0;

			index = fastBitsDst.GetNext();
			while ( index!=-1 ) {
				// FastBit is set. So res is
				//     Wi with WiIn >= lsResToGen.HalfMvToMt.WinIn = new ResWithCount( winValueDst ).Res.WinIn     or
				//     Ls with LsIn >= winResToGen.HalfMvToMt.LsIn

#if DEBUG
				int resValue = dataDst.GetDebug(index,"Mv DstPosToPerformBackMoves",indexPosDst);
#else
				int resValue = dataDst.Get(index);
#endif
				ResWithCount resWithCount = new ResWithCount( resValue );
				Res          res          = resWithCount.Res;
				if ( resValue == winValueDst ) {
					if ( removePotNewPosBit )
						fastBitsDst.Unset( index );
					indexPosDst.ChangeIndex( index - oldIndex, ref flds);
					oldIndex = index;
					BitBrd occFldBits = flds.GetBitBoard(pieces.PieceCount) | wkBkBits;
					doMv.DoMvAndUpdateSrcResWithLs( flds, wtm, occFldBits );
				}
				else if ( res.IsLs ) {                        // early ls res processing (as soon as final)
					if ( removePotNewPosBit )
						fastBitsDst.Unset( index );
					indexPosDst.ChangeIndex( index - oldIndex, ref flds );
					oldIndex = index;
					BitBrd occFldBits = flds.GetBitBoard(pieces.PieceCount) | wkBkBits;
					doMv.DoMvAndUpdateSrcResWithWin( flds, wtm, occFldBits, res.HalfMvAwayFromMateForLs );
				}
				index = fastBitsDst.GetNext();
			}
			finalResCountCurrent          += doMv.FinalResCount;
			finalResToProcessCountCurrent += doMv.FinalResToProcessCount;
		}


		public int NofDataChunksToUse
		{
			get { return fastCalculation ? 2 : (moveInfo.Length+2); }   // See info.txt
		}


		public static int MinNofWriteDataChunksPerThread
		{
			get{ return 2; }
		}


		public override string ToString()
		{
			string s = "Mv WkBkDst=" + wkBkDst.ToString() + "   WkBkSrc: ";
			WkBkMvInfo[] info = GetMvInfo();
			for ( int i=0 ; i<info.Length ; i++ ) {
				s += info[i].WkBkSrc.ToString() + " ";
			}
			return s;
		}

		public static void Finalize( CalcTB calc, bool wtm, int threadIndex )
		{
			if ( PrintInfos )
				GenTbInfo.Gen( calc, threadIndex );
		}
	}
}
