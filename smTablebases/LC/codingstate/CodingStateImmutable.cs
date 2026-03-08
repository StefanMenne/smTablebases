﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CodingStateImmutable : CodingStateBase
	{
		private CodingStateImmutablePool                          pool                   = null;


		public CodingStateImmutable() : base( -1 )
		{
		}


		public static void Create( CodingStateImmutablePool pool, CodingStateImmutable instance, ProbabilityArray historyIsLit, Collection<ProbabilityArray> historyType, BigValueHistoryImmutable historyDistance, ProbabilityArray historyLength, Collection<ProbabilityTree> historyLengthTreeLow, Collection<ProbabilityTree> historyLengthTreeMid, ProbabilityTree historyLengthTreeHigh, ProbabilityArray probabilityIsLowIsMidLength, LatestHistory historyLatest, int lastLengthIndex, int pos, Last3CodingsStateIndex last3CodingsStateIndex, Collection<ProbabilityTree> literalTrees, Collection<ProbabilityTree> historyExpDistSlot, ProbabilityTree historyExpDistBits, double probabilityIsRep0S )
		{
			instance.pool                           = pool;
			instance.historyIsLit                   = historyIsLit;
			instance.historyType                    = historyType;
			instance.historyDistance                = historyDistance;
			instance.historyLength                  = historyLength;
			instance.historyLengthTreeLow           = historyLengthTreeLow;
			instance.historyLengthTreeMid           = historyLengthTreeMid;
			instance.historyLengthTreeHigh          = historyLengthTreeHigh;
			instance.probabilityIsLowIsMidLength    = probabilityIsLowIsMidLength;
			instance.HistoryLatest                  = historyLatest;
			instance.rep0AllowedLengthIndexCount  = (lastLengthIndex==-1||lastLengthIndex==pool.LengthIndexCount-1) ? pool.LengthIndexCount : lastLengthIndex;
			instance.pos                            = pos;
			instance.historyLiteralTrees            = literalTrees;
			instance.last3CodingsStateIndex         = last3CodingsStateIndex;
			instance.historyExpDistSlot             = historyExpDistSlot;
			instance.historyExpDistBits             = historyExpDistBits;
			instance.probabilityIsRep0S             = probabilityIsRep0S;
		}


		public void AddCodingItemLiteralReuse( int literalBitCount, int literalValue, int literalProposedValue, int literalTreeIndex )
		{
			historyIsLit = historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), true, SettingsFix.IsLiteralMinProbability, 1D-SettingsFix.IsLiteralMinProbability, true );
			Collection<ProbabilityTree> literalTreesNew = historyLiteralTrees.ChangeItem(literalTreeIndex,true);
			CodingLiteral.GetCodingSizeProbabilityProduct( literalBitCount, literalValue, literalProposedValue, literalTreesNew[literalTreeIndex].Probabilities, false );
			historyLiteralTrees = literalTreesNew;
			this.rep0AllowedLengthIndexCount  = pool.LengthIndexCount;
			last3CodingsStateIndex = last3CodingsStateIndex.CodeLiteral();
			this.pos++;
		}


		public void AddCodingItemRep0SReuse( int dist )
		{
			probabilityIsRep0S = Probability.ChangeProbability( probabilityIsRep0S, true, 0.001, 0.999 );
			historyIsLit                   = historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), false, SettingsFix.IsLiteralMinProbability, 1D-SettingsFix.IsLiteralMinProbability, true );
			historyDistance                = ((BigValueHistoryImmutable)historyDistance).AddValue( dist, true );
			historyType                    = historyType.Clone( true );
			int idx = last3CodingsStateIndex.GetIndexType(pos);
			historyType[idx] = historyType[idx].IncreaseProbabilityKeepSum1Immutable( CodingItemType.Rep0, SettingsFix.HistoryTypeDecreaseProbabilityFactor, SettingsFix.HistoryTypeMinProbability, true );
			pos++;
			rep0AllowedLengthIndexCount  = 0;
			last3CodingsStateIndex         = last3CodingsStateIndex.CodeMatch( CodingItemType.Rep0S, 0 );
		}


		public void AddCodingItemReuse( int type, DistLength distLength, int length, int lengthIndex )
		{
			if ( type==CodingItemType.Rep0 || type==CodingItemType.Rep0S )
				probabilityIsRep0S = Probability.ChangeProbability( probabilityIsRep0S, type==CodingItemType.Rep0S, 0.001, 0.999 );
			historyIsLit                   = historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), false, SettingsFix.IsLiteralMinProbability, 1D-SettingsFix.IsLiteralMinProbability, true );
			historyType                    = historyType.Clone( true );
			int idx = last3CodingsStateIndex.GetIndexType(pos);
			historyType[idx] = historyType[idx].IncreaseProbabilityKeepSum1Immutable( type, SettingsFix.HistoryTypeDecreaseProbabilityFactor, SettingsFix.HistoryTypeMinProbability, true );
			historyDistance                = ((BigValueHistoryImmutable)historyDistance).AddValue( distLength.Dist, true );
			
			if ( historyLength == null ) {
				 historyLengthTreeHigh = AddLength( out historyLengthTreeLow, out historyLengthTreeMid, out probabilityIsLowIsMidLength, lengthIndex, historyLengthTreeLow, historyLengthTreeMid, historyLengthTreeHigh, probabilityIsLowIsMidLength, true, CodingItemType.IsRep(type) );
			}
			else {
				historyLength                  = historyLength.Clone( true );
				historyLength.IncreaseProbabilityKeepSum1( distLength.LengthIndex, ((type==CodingItemType.Rep0)?rep0AllowedLengthIndexCount:historyLength.Probabilities.Length), SettingsFix.HistoryLengthMinProbability );
			}
			
			HistoryLatest                  = HistoryLatest.Add( distLength.Dist );
			rep0AllowedLengthIndexCount  = (distLength.LengthIndex==pool.LengthIndexCount-1) ? pool.LengthIndexCount : distLength.LengthIndex;
			pos                           += length;
			last3CodingsStateIndex         = last3CodingsStateIndex.CodeMatch( type, distLength.LengthIndex );
			if ( type == CodingItemType.ExpDist )
				historyExpDistBits = CodingExpDist.AddCoding( distLength.Dist-1, CodingExpDist.LengthIndexToContextIndex(distLength.LengthIndex), historyExpDistSlot, out historyExpDistSlot, historyExpDistBits, true );
		}


		public CodingStateImmutable AddCodingItemLiteral( int literalBitCount, int literalValue, int literalProposedValue, int literalTreeIndex )
		{
			CodingStateImmutable newInstance = pool.GetInstance();
			HistoryType.ShareInstance();
			Collection<ProbabilityTree> literalTreesNew = historyLiteralTrees.ChangeItem( literalTreeIndex, false );
			CodingLiteral.GetCodingSizeProbabilityProduct( literalBitCount, literalValue, literalProposedValue, literalTreesNew[literalTreeIndex].Probabilities, false );

			CodingStateImmutable.Create( pool, 
												  newInstance, 
												  historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), true, SettingsFix.IsLiteralMinProbability, 1D-SettingsFix.IsLiteralMinProbability, false ),
												  HistoryType,
												  ((BigValueHistoryImmutable)historyDistance).Clone(),
												  ( historyLength==null ? null : historyLength.Clone(false) ),
												  ( historyLengthTreeLow==null ? null : historyLengthTreeLow.Clone(false) ),
												  ( historyLengthTreeMid==null ? null : historyLengthTreeMid.Clone(false) ),
												  ( historyLengthTreeHigh==null ? null : historyLengthTreeHigh.Clone(false) ),
												  ( probabilityIsLowIsMidLength==null ? null : probabilityIsLowIsMidLength.Clone(false) ),
												  HistoryLatest,
												  -1,
												  pos+1,
												  last3CodingsStateIndex.CodeLiteral(),
												  literalTreesNew,
												  historyExpDistSlot.Clone( false ),
												  historyExpDistBits.Clone( false ),
												  probabilityIsRep0S
												);
			return newInstance;		
		}


		public CodingStateImmutable AddCodingItemRep0S( int dist )
		{
			CodingStateImmutable newInstance = pool.GetInstance();
			Collection<ProbabilityArray> historyTypeNew = historyType.Clone( false );
			int idx = last3CodingsStateIndex.GetIndexType(pos);
			historyTypeNew[idx] = historyTypeNew[idx].IncreaseProbabilityKeepSum1Immutable( CodingItemType.Rep0, SettingsFix.HistoryTypeDecreaseProbabilityFactor, SettingsFix.HistoryTypeMinProbability, true );
			CodingStateImmutable.Create( pool, 
												  newInstance, 
												  historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), false, SettingsFix.IsLiteralMinProbability, 1D-SettingsFix.IsLiteralMinProbability, false ),
												  historyTypeNew,
												  ((BigValueHistoryImmutable)historyDistance).AddValue( dist, false ),
												  ( historyLength==null ? null : historyLength.Clone(false) ),
												  ( historyLengthTreeLow==null ? null : historyLengthTreeLow.Clone(false) ),
												  ( historyLengthTreeMid==null ? null : historyLengthTreeMid.Clone(false) ),
												  ( historyLengthTreeHigh==null ? null : historyLengthTreeHigh.Clone(false) ),
												  ( probabilityIsLowIsMidLength==null ? null : probabilityIsLowIsMidLength.Clone(false) ),
												  HistoryLatest,
												  0,
												  pos+1,
												  last3CodingsStateIndex.CodeMatch( CodingItemType.Rep0S, 0 ),
												  historyLiteralTrees.Clone(false),
												  historyExpDistSlot.Clone(false),
          									      historyExpDistBits.Clone(false),
					                              Probability.ChangeProbability( probabilityIsRep0S, true, 0.001, 0.999 )

												 );
			return newInstance;
		}


		public CodingStateImmutable AddCodingItem( int type, DistLength distLength, int length, int lengthIndex )
		{
			CodingStateImmutable newInstance = pool.GetInstance();
			Collection<ProbabilityTree>       historyExpDistSlotNew;
			ProbabilityTree                   historyExpDistBitsNew;
			if ( type == CodingItemType.ExpDist ) {
				historyExpDistBitsNew = CodingExpDist.AddCoding( distLength.Dist-1, CodingExpDist.LengthIndexToContextIndex(distLength.LengthIndex), historyExpDistSlot, out historyExpDistSlotNew, historyExpDistBits, false );
			}
			else {
				historyExpDistSlotNew = historyExpDistSlot.Clone(false);
				historyExpDistBitsNew = historyExpDistBits.Clone(false);
			}

			Collection<ProbabilityArray> historyTypeNew = historyType.Clone( false );
			int idx = last3CodingsStateIndex.GetIndexType(pos);
			historyTypeNew[idx] = historyTypeNew[idx].IncreaseProbabilityKeepSum1Immutable( type, SettingsFix.HistoryTypeDecreaseProbabilityFactor, SettingsFix.HistoryTypeMinProbability, true );
			ProbabilityArray             historyLengthNew        = null;
			Collection<ProbabilityTree>  lengthTreeLowNew=null, lengthTreeMidNew=null;
			ProbabilityTree              lengthTreeHighNew=null;
			ProbabilityArray             probabilityIsLowIsMidLengthNew=null;
			if ( historyLength == null ) {
				lengthTreeHighNew = AddLength( out lengthTreeLowNew, out lengthTreeMidNew, out probabilityIsLowIsMidLengthNew, lengthIndex, historyLengthTreeLow, historyLengthTreeMid, historyLengthTreeHigh, probabilityIsLowIsMidLength, false, CodingItemType.IsRep(type) );
			}
			else {
				historyLengthNew = historyLength.Clone( false );
				historyLengthNew.IncreaseProbabilityKeepSum1( distLength.LengthIndex, ((type==CodingItemType.Rep0)?rep0AllowedLengthIndexCount:pool.LengthIndexCount), SettingsFix.HistoryLengthMinProbability );
			}
			CodingStateImmutable.Create( pool, 
												  newInstance, 
												  historyIsLit.ChangeProbability( GetIsLiteralStateIndex(pos), false, SettingsFix.IsLiteralMinProbability, 1D-SettingsFix.IsLiteralMinProbability, false ),
												  historyTypeNew,
												  ((BigValueHistoryImmutable)historyDistance).AddValue( distLength.Dist, false ),
												  historyLengthNew,
												  lengthTreeLowNew, 
												  lengthTreeMidNew, 
												  lengthTreeHighNew,
												  probabilityIsLowIsMidLengthNew,
												  HistoryLatest.Add( distLength.Dist ),
												  distLength.LengthIndex,
												  pos+length,
												  last3CodingsStateIndex.CodeMatch( type, distLength.LengthIndex ),
												  historyLiteralTrees.Clone(false),
												  historyExpDistSlotNew,
												  historyExpDistBitsNew,
												  ( ( type==CodingItemType.Rep0 || type==CodingItemType.Rep0S ) ? Probability.ChangeProbability( probabilityIsRep0S, type==CodingItemType.Rep0S, 0.001, 0.999 ) :  probabilityIsRep0S )

												 );
			return newInstance;
		}


		private ProbabilityTree AddLength( out Collection<ProbabilityTree> treeLowNew, out Collection<ProbabilityTree> treeMidNew, out ProbabilityArray probabilityIsLowIsMidLengthNew, int length, Collection<ProbabilityTree> treeLow, Collection<ProbabilityTree> treeMid, ProbabilityTree treeHigh, ProbabilityArray probabilityIsLowIsMidLength, bool disposeOldInstances, bool isRep )
		{
			ProbabilityTree treeHighNew = treeHigh.Clone( disposeOldInstances );
			probabilityIsLowIsMidLengthNew = probabilityIsLowIsMidLength.Clone( disposeOldInstances );
			probabilityIsLowIsMidLengthNew[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount] = Probability.ChangeProbability( probabilityIsLowIsMidLengthNew[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount], length<8, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability );

			if ( length < 8 ) {
				treeLowNew = treeLow.ChangeItem( pos&3, disposeOldInstances );
				treeLowNew[pos&3].ChangeTreeProbabilities( length, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, 0, (isRep?-1:6) );
			}
			else {
				probabilityIsLowIsMidLengthNew[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)] = Probability.ChangeProbability( probabilityIsLowIsMidLengthNew[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)], length<16, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability );
				treeLowNew = treeLow.Clone( disposeOldInstances );
			}

			if ( length>=8 && length<16 ) {
				treeMidNew = treeMid.ChangeItem( pos&3, disposeOldInstances );
				treeMidNew[pos&3].ChangeTreeProbabilities( length-8, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, 0, (isRep?-1:6) );
			}
			else {
				treeMidNew = treeMid.Clone( disposeOldInstances );
			}

			if ( length>=16 )
				treeHighNew.ChangeTreeProbabilities( length-16, SettingsFix.HistoryLengthTreeBits, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, 0, (isRep?-1:256) );

			return treeHighNew;
		}


		public void Dispose()
		{
			historyIsLit.Dispose();
			historyType.Dispose();
			((BigValueHistoryImmutable)historyDistance).Dispose();
			if ( historyLength == null ) {
				probabilityIsLowIsMidLength.Dispose();
				historyLengthTreeLow.Dispose();
				historyLengthTreeMid.Dispose();
				historyLengthTreeHigh.Dispose();
			}
			else 
				historyLength.Dispose();
			historyLiteralTrees.Dispose();
			historyExpDistSlot.Dispose();
			historyExpDistBits.Dispose();
			pool.ReuseInstance( this );
		}

	}

}
