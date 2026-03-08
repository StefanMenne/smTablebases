﻿//#define DEBUG_GET_PROBABILITIES
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CodingState : CodingStateBase
	{
		private double[]                distProbability         = new double[(SettingsFix.HistoryDistWindowSize>>1)+SettingsFix.RepeatCount];
		private int[]                   repeatHistoryIndices    = new int[SettingsFix.RepeatCount];
		private int                     countHistoryDists       = -1;


		public CodingState( int dataLengthBits, int lengthIndexCount, Literal literal, LengthInfo lengthInfo, Level level, int expDistSlotBitCount ) : base( lengthIndexCount )
		{
			historyIsLit             = ProbabilityArray.CreateFirst( 1, Last3CodingsStateIndex.IndexCountIsLiteral, false );
			historyDistance          = new BigValueHistory( SettingsFix.HistoryDistWindowSize, SettingsFix.HistoryDistInitValues, level.MinHistoryDistOccurence );
			historyType              = ProbabilityArray.CreateFirstCollection( 1, Last3CodingsStateIndex.IndexCountType, SettingsFix.HistoryTypeCount, true );
			if ( lengthInfo.LengthIndexCount >= 13 ) {
				probabilityIsLowIsMidLength = ProbabilityArray.CreateFirst( 1, 8, false );
				historyLengthTreeLow  = ProbabilityTree.CreateFirstCollection( 1, SettingsFix.HistoryLengthIsLowIsMidContextCount, 14 );
				historyLengthTreeMid  = ProbabilityTree.CreateFirstCollection( 1, SettingsFix.HistoryLengthIsLowIsMidContextCount, 14 );
				historyLengthTreeHigh = ProbabilityTree.CreateFirst( 1, 2*256 );
			}
			else
				historyLength    = ProbabilityArray.CreateFirst( 1, lengthIndexCount, true );
			historyLiteralTrees      = ProbabilityTree.CreateFirstCollection( 1, literal.ContextCount, 3<<literal.Bits );
			UpdateRepeatHistoryIndices();
			historyExpDistSlot       = ProbabilityTree.CreateFirstCollection( 1, SettingsFix.ExpDistSlotContextCount, ProbabilityTree.GetProbabilityVariablesCount(CodingExpDist.ExpDistSlotBitCount) );
			historyExpDistBits       =  ProbabilityTree.CreateFirst( 1, CodingExpDist.ExpDistBitsTreeLength );
		}


		public void AddCodingItemLiteral()
		{
			historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), true, SettingsFix.IsLiteralMinProbability, 1d-SettingsFix.IsLiteralMinProbability );
			if ( historyLength != null )
				rep0AllowedLengthIndexCount = LengthHistoryProbabilities.Length;
			last3CodingsStateIndex = last3CodingsStateIndex.CodeLiteral();
			pos++;
		}


		public void AddCodingItemRep0S( int dist, int historyIndex )
		{
			probabilityIsRep0S = Probability.ChangeProbability( probabilityIsRep0S, true, 0.001, 0.999 );
			historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), false, SettingsFix.IsLiteralMinProbability, 1d-SettingsFix.IsLiteralMinProbability );
			((BigValueHistory)historyDistance).AddValue( dist );
			historyType[last3CodingsStateIndex.GetIndexType(pos)].IncreaseProbabilityKeepSum1( CodingItemType.Rep0, SettingsFix.HistoryTypeDecreaseProbabilityFactor, SettingsFix.HistoryTypeMinProbability );
			pos++;
			UpdateRepeatHistoryIndices();
			rep0AllowedLengthIndexCount = 0;
			last3CodingsStateIndex = last3CodingsStateIndex.CodeMatch( CodingItemType.Rep0S, 0 );
		}



		public void AddCodingItemMatch( int type, int distVirtual, int lengthIndex, int length, int historyIndex )
		{
			if ( type==CodingItemType.Rep0 || type==CodingItemType.Rep0S )
				probabilityIsRep0S = Probability.ChangeProbability( probabilityIsRep0S, type==CodingItemType.Rep0S, 0.001, 0.999 );
			historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), false, SettingsFix.IsLiteralMinProbability, 1d-SettingsFix.IsLiteralMinProbability );
			((BigValueHistory)historyDistance).AddValue( distVirtual );	
			HistoryLatest = HistoryLatest.Add( distVirtual );
			if ( historyLength != null ) {    // historyLengthTree is already updated while coding
				historyLength.IncreaseProbabilityKeepSum1( lengthIndex, ((type==0)?rep0AllowedLengthIndexCount:LengthHistoryProbabilities.Length), SettingsFix.HistoryLengthMinProbability );
				rep0AllowedLengthIndexCount = (lengthIndex==LengthHistoryProbabilities.Length-1) ? LengthHistoryProbabilities.Length/*allow max length again*/ : lengthIndex;
			}
			historyType[last3CodingsStateIndex.GetIndexType(pos)].IncreaseProbabilityKeepSum1( type, SettingsFix.HistoryTypeDecreaseProbabilityFactor, SettingsFix.HistoryTypeMinProbability );
			pos += length;
			UpdateRepeatHistoryIndices();
			last3CodingsStateIndex = last3CodingsStateIndex.CodeMatch( type, lengthIndex );
		}


		public double[] LengthHistoryProbabilities
		{
			get{ return historyLength.Probabilities; }
		}


		/// <summary>
		/// 
		/// +-------------+-------------+-------------+-------------+-------------+-------------+
		/// |   rep0      |   rep1      |   rep2      |   rep3      |   Hist      |   ExpDist   |     all are sum to 1  
		/// | ---------   | ---------   | ---------   | ---------   | ---------   | ---------   |
		/// | repOccSum   | repOccSum   | repOccSum   | repOccSum   | repOccSum   | repOccSum   |             repOccSum = rep0 + rep1 + rep2 + rep3 + Hist + ExpDist
		/// +-------------+-------------+-------------+-------------+-------------+-------------+
		///     R0              R1            R2             R3             H            E
		///                                                          
		///                                                          H = H0 + H1 + ... + Hn
		///                           ||  ||
		///                       \\  ||  ||  //                              Hist               Hist[i]
		///                         \\||  ||//                        Hi = -------------  *  ---------------               ( sumLeft = Hist0 + Hist1 + ... + Histn )
		///                           \    /                                repOccSum            sumLeft
		///                             \/
		/// 
		/// 
		/// probabilityIsRep0    probabilities
		/// +----------------+   +-------------+-------------+      +-------------+-------------+-------------+-------------+-------------+-------------+
		/// |                |   |             |             |      |             |             |             |             |             |             |     all are sum to 1  
		/// |    R0          |   |    H0       |    H1       | .... |   Hn        |     0       |  R1 + Hi    | R2 + Hi     |  R3 + Hi    |      Exp    |
		/// |                |   |             |             |      |             |   Empty     |  (Hi := 0)  |  (Hi := 0)  |   (Hi := 0) |             |
		/// +----------------+   +-------------+-------------+      +-------------+-------------+-------------+-------------+-------------+-------------+
		///                             0                         countHistoryDists-1                                                   countHistoryDist+4
		/// </summary>
		public double[] GetDistHistoryProbabilities( out double probabilityIsRep0 )
		{
			double[]   typeProbabilities = historyType[last3CodingsStateIndex.GetIndexType(pos)].Probabilities;
			int        sumLeft           = historyDistance.SumOccurenceTwoOrHigher;
			double     factorHist        = ((double)typeProbabilities[CodingItemType.Hist]) / (sumLeft);

#if DEBUG_GET_PROBABILITIES
			double sumVer = 0d;
			for ( int i=0 ; i<typeProbabilities.Length ; i++ ) {
				if ( i==CodingItemType.Hist && countHistoryDists!=0 ) {
					for ( int j=0 ; j<countHistoryDists ; j++ ) {
						sumVer += factorHist * historyDistance.Occurence[j];
					}
				}
				else 
					sumVer += typeProbabilities[i];
			}
			if ( Math.Abs( sumVer - 1d ) > 0.00001 )
				throw new Exception();
#endif
			probabilityIsRep0 = typeProbabilities[0];
			int hisIdxRep0 = repeatHistoryIndices[0];
			if ( hisIdxRep0 != -1 )
				probabilityIsRep0 += factorHist * historyDistance.Occurence[hisIdxRep0];
	
			double correctionCutRep0Factor = 1.0d / (1.0d - probabilityIsRep0);
			
			factorHist *= correctionCutRep0Factor;
			for ( int i=0 ; i<countHistoryDists ; i++ ) 
				distProbability[i] = factorHist * historyDistance.Occurence[i];
	
			double factorRepeatCorrected = correctionCutRep0Factor;
			for ( int i=0 ; i<repeatHistoryIndices.Length ; i++ ) {
				int histIndex = repeatHistoryIndices[i];
				
				distProbability[countHistoryDists+i] = factorRepeatCorrected * typeProbabilities[i];
				if ( histIndex != -1 ) {
					distProbability[countHistoryDists+i] += distProbability[histIndex];
					distProbability[histIndex] = 0.0d;
				}
			}

			distProbability[countHistoryDists + SettingsFix.RepeatCount] = correctionCutRep0Factor * typeProbabilities[CodingItemType.ExpDist];
			if ( countHistoryDists == 0 )   // add history-probabilty as ExpDist-probability
				distProbability[countHistoryDists + SettingsFix.RepeatCount] += correctionCutRep0Factor * ((double)typeProbabilities[CodingItemType.Hist]); 
			distProbability[countHistoryDists] = 0.0d;

#if DEBUG_GET_PROBABILITIES
			sumVer = 0d;
			for ( int i=0 ; i<countHistoryDists+SettingsFix.RepeatCount+1 ; i++ )
				sumVer += distProbability[i];
			if ( Math.Abs( sumVer - 1d ) > 0.00001 )
				throw new Exception();
#endif
	
			return distProbability;
		}



		public int GetHistoryDistance( int historyIndex )
		{
			return historyDistance.RankToValue( historyIndex );
		}


		public int GetRepeatIndex( int dist )
		{
			return HistoryLatest.GetRank( dist );
		}


		public int GetHistoryEntryIndex( int dist )
		{
			return historyDistance.ValueToRank( dist, historyDistance.FirstOccurenceOneIndex );
		}


		public int CountHistoryDists
		{
			get { return countHistoryDists; }
		}


		private void UpdateRepeatHistoryIndices()
		{
			countHistoryDists = historyDistance.FirstOccurenceOneIndex;
			for ( int i=0 ; i<repeatHistoryIndices.Length ; i++ ) {
				int dist = HistoryLatest.GetVal( i );
				repeatHistoryIndices[i] = historyDistance.ValueToRank(dist,countHistoryDists);
			}
		}


#if DEBUG
		public double GetProbabilityIsRep()
		{
			double []   occ             = historyType[last3CodingsStateIndex.GetIndexType(pos)].Probabilities;
			return ((double)(1.0D - occ[CodingItemType.Hist] - occ[CodingItemType.ExpDist]));
		}
#endif 

	}
}
