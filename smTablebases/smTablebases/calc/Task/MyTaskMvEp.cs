using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
	public sealed class MyTaskMvEp : MyTaskPieces
	{
		private long        finalResCountCurrent, finalResToProcessCountCurrent;


		public MyTaskMvEp( CalcTB calc, WkBk wkBk, Pieces pieces, bool wtm ) : base( calc, wkBk, pieces, wtm )
		{
		}

		public override void PrepareCalcWithoutThreading( Tasks tasks, int freeThreads, int threadIndex )
		{
		}



		public override void FinishCalcWithoutThreading( Tasks tasks )
		{
			TasksMvEp.FinalResCount += finalResCountCurrent;
			TasksMvEp.FinalResToProcessCount += finalResToProcessCountCurrent;
		}

		public override void Do( Tasks tasks, int threadIndex, bool singleThreaded )
		{
			TasksMvEp tasksMvEp = (TasksMvEp)tasks;
			bool afterCapMoves = tasksMvEp.AfterCap;
			Step step = tasksMvEp.Step;
			TransferResFromNonEpToEp( calc, pieces, wtm, wkBk, (afterCapMoves ? Res.IsMt : step.WinResToGen ), step.GetLsResToGen(wtm), afterCapMoves, threadIndex );
		}

		public override bool IsMvEp
		{
			get{ return true; }
		}


		private void TransferResFromNonEpToEp( CalcTB calc, Pieces pieces, bool wtm, WkBk wkBk, Res winResToGen, Res lsResToGen, bool afterCaptureMoves, int threadIndex )
		{
			IndexPos             indexPosPawnBesidePawnEP   = new IndexPos( wkBk, pieces,   wtm );
			IndexPos             indexPosPawnBesidePawnNoEP = new IndexPos( wkBk, pieces,   wtm );
			IndexPos             indexPosBoforePawnDblStep  = new IndexPos( wkBk, pieces,  !wtm );    // before double step of pawn


			long                    count                    = indexPosPawnBesidePawnEP.IndexCount;
			DataChunkWrite          dataPawnBesidePawn, dataBeforePawnDblStep;
			calc.TaBasesWrite.GetDataChunks( out dataPawnBesidePawn, wkBk, wtm, true, false, out dataBeforePawnDblStep, wkBk, !wtm, true, true, threadIndex );
			FastBits                fastBits                 = dataBeforePawnDblStep.GetFastBits();
			Res                     winResToGenInNextStep    = lsResToGen.HalfMoveAwayFromMate;

			EpEnumerateInfo epEnumerateInfo = indexPosPawnBesidePawnEP.SetToFirstEpPos();

			do {
				long i = indexPosPawnBesidePawnEP.GetIndex();

#if DEBUG
				ResWithCount resEp = new ResWithCount( dataPawnBesidePawn.GetDebug(i,"EP",null) );
#else
				ResWithCount resEp = new ResWithCount( dataPawnBesidePawn.Get(i) );
#endif

				if ( resEp.IsIllegalPos )
					continue;

				if ( afterCaptureMoves || resEp.IsWinOrDraw || resEp.IsUnknown ) {
					// Copy Result from non ep pos to ep pos
					Field epSntmDblStepDst, epSntmCapSrc;
					Fields fPawnBesidePawn = indexPosPawnBesidePawnEP.GetFieldsEP( out epSntmDblStepDst, out epSntmCapSrc );
					if ( !indexPosPawnBesidePawnNoEP.SetSortedFields( fPawnBesidePawn ) )
						continue;
#if DEBUG
					ResWithCount resNoEp = new ResWithCount( dataPawnBesidePawn.GetDebug( indexPosPawnBesidePawnNoEP.GetIndex(), "MvEp ResPawnBesidePawn", null ) );
#else
					ResWithCount resNoEp = new ResWithCount( dataPawnBesidePawn.Get( indexPosPawnBesidePawnNoEP.GetIndex() ) );
#endif
					if ( !resNoEp.IsStaleMate ) {
						resEp = resEp.CombineAndCopyMoveCounter( resNoEp );
#if DEBUG
						dataPawnBesidePawn.SetDebug( indexPosPawnBesidePawnEP, i, resEp.Value, "TransEp resNoEp=", resNoEp, VerifyResType.VerifyAlways );
#else
						dataPawnBesidePawn.Set( i, resEp.Value );
#endif


						if ( (!resEp.IsWinOrDraw&&!resEp.IsUnknown) || (resEp.IsWinOrDraw&&resEp.Res.Value==winResToGen.Value)  ) {
							// its Final result; so apply to predecessor by going back double step of pawn
							// Transfer of results from non EP to EP pos is done maybe several times but this code is only executed once
							Fields f = fPawnBesidePawn;

							f = f.SetNew( f.IndexOf(epSntmDblStepDst), epSntmDblStepDst + (wtm?16:-16) );
							if ( indexPosBoforePawnDblStep.SetFields( f ) ) {
								long posBeforeDblStepPawnIndex = indexPosBoforePawnDblStep.GetIndex();
#if DEBUG
								ResWithCount resBeforeDblStepPawn = new ResWithCount(dataBeforePawnDblStep.GetDebug(posBeforeDblStepPawnIndex,"MvEp ResBeforeDblStepPawn",null));
#else
								ResWithCount resBeforeDblStepPawn = new ResWithCount(dataBeforePawnDblStep.Get(posBeforeDblStepPawnIndex));
#endif
								if ( resBeforeDblStepPawn.IsIllegalPos ) {
#if DEBUG
									dataPawnBesidePawn.SetDebug( indexPosPawnBesidePawnEP, i, ResWithCount.IllegalPos.Value, "TransEp ill ep pos", null, VerifyResType.VerifyAlways );
#else
									dataPawnBesidePawn.Set( i, ResWithCount.IllegalPos.Value );    // invalid due src position(before pawn dbl step) is invalid
#endif
									fastBits.Unset( posBeforeDblStepPawnIndex );
								}
								else if ( resBeforeDblStepPawn.IsUnknown || resBeforeDblStepPawn.IsWinOrDraw ) {
									bool isFinalAndNotDraw = resBeforeDblStepPawn.IsFinal && !resBeforeDblStepPawn.IsDraw;
									bool isFinal = resBeforeDblStepPawn.IsFinal;
									bool isDraw = resBeforeDblStepPawn.IsDraw;

                                    resBeforeDblStepPawn = resBeforeDblStepPawn.CombineAndDecrementMoveCounter(resEp.Res.HalfMoveAwayFromMate);

									if ( !isFinal && resBeforeDblStepPawn.IsFinal )
										finalResCountCurrent++;
									if ( resBeforeDblStepPawn.IsFinal && !resBeforeDblStepPawn.IsDraw && (!isFinal||isDraw) )
										finalResToProcessCountCurrent++;

#if DEBUG
									dataBeforePawnDblStep.SetDebug( indexPosBoforePawnDblStep, posBeforeDblStepPawnIndex, resBeforeDblStepPawn.Value, "TransEp(EP result is final)", indexPosPawnBesidePawnEP, VerifyResType.VerifyAlways );
#else
									dataBeforePawnDblStep.Set( posBeforeDblStepPawnIndex, resBeforeDblStepPawn.Value );
#endif


									if ( ((resBeforeDblStepPawn.IsWin&&resBeforeDblStepPawn.Res.WinIn>=winResToGenInNextStep.WinIn&&resBeforeDblStepPawn.Res.WinIn<TasksMvEp.FastBitsInterval.WinInMaxPlus1) || (resBeforeDblStepPawn.IsLose&&!resBeforeDblStepPawn.IsUnknown) ) )
										fastBits.Set( posBeforeDblStepPawnIndex );
								}
							}
							else {
#if DEBUG
								dataPawnBesidePawn.SetDebug( indexPosPawnBesidePawnEP, i, ResWithCount.IllegalPos.Value, "TransEp (EP result is final) Illegal", null, VerifyResType.VerifyAlways );
#else
								dataPawnBesidePawn.Set( i, ResWithCount.IllegalPos.Value );    // invalid due to unblockable check
#endif
							}
						}
					}
				}
			} while( indexPosPawnBesidePawnEP.NextEpPos(ref epEnumerateInfo) );

			calc.TaBasesWrite.FreeDataChunk( dataPawnBesidePawn );
			calc.TaBasesWrite.FreeDataChunk( dataBeforePawnDblStep );
		}


	}
}

