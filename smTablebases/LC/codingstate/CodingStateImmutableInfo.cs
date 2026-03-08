﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CodingStateImmutableInfo
	{
		private CodingStateImmutable codingState;
		private double               typeCodingProbabilityHistory;
		private int[]                repeatHistoryIndices = new int[SettingsFix.RepeatCount];
		private int                  historyDistsCount, codingItemTypeStateIndex; 
		private double               factorNoRepeat, probabilityExpDist;
		private CodingExpDist        codingExpDist;


		public CodingStateImmutableInfo( int dataLengthBits, LengthInfo lengthInfo, CodingExpDist codingExpDist )
		{
			this.codingExpDist = codingExpDist;
		}


		public void Init( CodingStateImmutable csi, int pos )
		{
			this.codingState         = csi;
			historyDistsCount        = csi.HistoryDistance.FirstOccurenceOneIndex; 
			codingItemTypeStateIndex = csi.GetCodingItemTypeStateIndex( pos );

			double[] probabilities = csi.HistoryType[codingItemTypeStateIndex].Probabilities;
			int countLatestToFind = SettingsFix.RepeatCount;

			for ( int i=0 ; i<SettingsFix.RepeatCount ; i++ )
				repeatHistoryIndices[i] = -1;

			for ( int i=0 ; i<historyDistsCount ; i++ ) {
				int dist        = csi.HistoryDistance.GetValue(i);
				if ( csi.HistoryLatest.Contains( dist ) ) {
					int repeatIndex = csi.HistoryLatest.GetRank( dist );
					repeatHistoryIndices[repeatIndex] = i;
					if ( --countLatestToFind == 0 )
						break;
				}
			}

			factorNoRepeat      = ((double)(probabilities[SettingsFix.RepeatCount])) / (codingState.HistoryDistance.SumOccurenceTwoOrHigher);
			probabilityExpDist  = ((double)probabilities[SettingsFix.RepeatCount+1]);
			if ( historyDistsCount == 0 )
				probabilityExpDist += ((double)probabilities[SettingsFix.RepeatCount]);
			typeCodingProbabilityHistory = GetCodingItemTypeProbability(false, csi);
		}


		public int HistoryDistsCount
		{
			get { return historyDistsCount; }
		}


		public int CodingItemTypeStateIndex
		{
			get { return codingItemTypeStateIndex; }
		}


		public int RepeatTypeToHistoryIndex( int repeatType )
		{
			return repeatHistoryIndices[repeatType];
		}


		public double Repeat0LengthCodingSizeProbability( CodingStateImmutable history, int lengthIndex, int rep0LengthIndexBound )
		{
			double sum = 0d;   
			for ( int lenIdx=0 ; lenIdx<rep0LengthIndexBound ; lenIdx++ )
				sum += history.HistoryLength.Probabilities[lenIdx];
			return history.HistoryLength.Probabilities[lengthIndex] / sum;
		}


		public double DistanceHistoryIndexToProbability( int historyIndex, int repeatType )
		{
			double res = typeCodingProbabilityHistory;
			double probability = ( (historyIndex == -1) ? 0.0d : (factorNoRepeat * codingState.HistoryDistance.Occurence[historyIndex]) );
			if ( repeatType != -1 ) {
				probability += codingState.HistoryType[codingItemTypeStateIndex].Probabilities[repeatType];
				if ( repeatType == CodingItemType.Rep0 )
					res *= ( 1.0d - codingState.ProbabilityIsRep0S );
			}
			res *= probability;

			return res;
		}


		public double GetRep0SProbability()
		{
			int historyIndex = RepeatTypeToHistoryIndex( CodingItemType.Rep0 );
			double probability = ( (historyIndex == -1) ? 0.0d : (factorNoRepeat * codingState.HistoryDistance.Occurence[historyIndex]) );
			probability += codingState.HistoryType[codingItemTypeStateIndex].Probabilities[0];
			return probability * typeCodingProbabilityHistory * codingState.ProbabilityIsRep0S;
		}


		public double CodeExpDistProbability( int distToCode, int lengthIndex )
		{
			return GetCodingItemTypeProbability(false,codingState) * probabilityExpDist * codingExpDist.GetCodingCostsAsProbability( distToCode, lengthIndex, codingState.HistoryExpDistSlot, codingState.HistoryExpDistBits );
		}
		

		public double LengthToCodingSizeProbability( int type, int lengthIndex, int pos, CodingStateImmutable history )
		{
			bool isRep = CodingItemType.IsRep(type);
			if ( history.HistoryLength == null ) {
				double costsAsProbability = Probability.Get( history.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount], lengthIndex<8 );
				if ( lengthIndex<8 )
					costsAsProbability *= (double)history.HistoryLengthTreeLow[pos&3].GetCodingSizeAsProbability( lengthIndex, 3, (isRep?-1:6) );
				else { 
					costsAsProbability *= Probability.Get( history.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)], lengthIndex<16 );
					if ( lengthIndex<16 )
						costsAsProbability *= (double)history.HistoryLengthTreeMid[pos&3].GetCodingSizeAsProbability( lengthIndex-8, 3, (isRep?-1:6) );
					else 
						costsAsProbability *= (double)history.HistoryLengthTreeHigh.GetCodingSizeAsProbability( lengthIndex-16, 8, (isRep?-1:256) );
				}			
				return costsAsProbability;
			}
			else
				return (double)history.HistoryLength.Probabilities[lengthIndex];
		}
		

		public double LiteralItemTypeProbability( CodingStateImmutable history )
		{
			return (double)GetCodingItemTypeProbability( true, history );
		}


		public double GetLiteralCodingProbabilityProduct( Literal literal, DistConverter distConverter, int dataIndex, int distProposedValueVirtual )
		{
			int literalValue          = literal.GetFromData( dataIndex );
			int literalTreeIndex      = literal.GetContextIndex( dataIndex );
			int literalProposedValue  = -1;
			if ( distProposedValueVirtual != 0 ) {
				int prevMatchDist = (distConverter==null) ? distProposedValueVirtual : distConverter.VirtualDistToDist(dataIndex,distProposedValueVirtual);
				if ( dataIndex>=prevMatchDist )
					literalProposedValue  = literal.GetFromData( dataIndex-prevMatchDist );
			} 
			return (double)CodingLiteral.GetCodingSizeProbabilityProduct( literal.Bits, literalValue, literalProposedValue, codingState.LiteralTrees[literalTreeIndex].Probabilities, true );
		}


		public double GetCodingItemTypeProbability( bool isLiteral, CodingStateImmutable history )
		{
			int index = history.GetIsLiteralStateIndex(history.Pos);
			return Probability.Get( history.HistoryIsLit.Probabilities[index], isLiteral );
		}
	}
}
