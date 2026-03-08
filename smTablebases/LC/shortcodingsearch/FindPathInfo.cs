﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class FindPathInfo
	{
		public CodingCosts                 Costs2;
		public CodingCosts                 MinCostsRight;

		public CodingStateImmutable        History                = null;
		public int                         ReferenceCount         = 0;
		public FindPathInfo                Predecessor            = null;
		public int                         Index                  = -1;

		//
		// The following members are set during searching and referencing backwards
		//
		public int                         BestCostType           = CodingItemType.Literal;
		public DistLength                  BestCostDistLength     = DistLength.Literal;  // Dist is virtual; also used afterwards to contain the last virtual distance
		public int                         HistoryIndex           = -1;


		public FindPathInfo()
		{ 
			Init();
		}


		private void Init()
		{
			Costs2                 = CodingCosts.BigValue;
			MinCostsRight          = CodingCosts.BigValue; 
			BestCostType           = CodingItemType.Literal;
			BestCostDistLength     = DistLength.Literal;
			HistoryIndex           = -1;
			History                = null;
			ReferenceCount         = 0;
			Predecessor            = null;
		}


		public int GetDeltaToBestCostPredecessor( LengthInfo lengthInfo )
		{
			return BestCostDistLength.GetDelta(lengthInfo);
		}


		public void ApplyLiteral( CodingCosts costs, FindPathInfo predecessor )
		{
#if DEBUG
			if ( ShortCodingSearch.PrintDebug( Index-1, 1 ) )
				Log.Line( "Apply Literal to Position " + (Index-1).ToString() + " to FPI=" +  Index.ToString() + " Costs=" + costs.GetBitSize() );
#endif 
			Costs2                 = costs;
			BestCostDistLength     = DistLength.Literal;
			BestCostType           = CodingItemType.Literal;
			ApplyCommon( predecessor );
		}


		public void ApplyRep0S( CodingCosts costs, FindPathInfo predecessor, int dist )
		{
#if DEBUG
			if ( ShortCodingSearch.PrintDebug( Index-1, 1 ) )
				Log.Line( "Apply Rep0S to Position " + (Index-1).ToString() + " to FPI=" +  Index.ToString() + " Costs=" + costs.GetBitSize() );
#endif 
			Costs2                 = costs;
			BestCostDistLength     = DistLength.Rep0SWithDist(dist);
			BestCostType           = CodingItemType.Rep0S;
			ApplyCommon( predecessor );
		}


#if DEBUG
		public void ApplyHistoryMatch( CodingCosts costs, int rank, DistLength distLength, FindPathInfo predecessor, int length, int distNonVirtual, int type )
#else
		public void ApplyHistoryMatch( CodingCosts costs, int rank, DistLength distLength, FindPathInfo predecessor, int type )
#endif
		{
#if DEBUG
			if ( ShortCodingSearch.PrintDebug( Index-length, 1 ) )
				Log.Line( "Apply HistoryMatch for Position " + (Index-length).ToString() + " until " +  (Index-1).ToString() + " to FPI=" + Index.ToString() + " Costs=" + costs.ToString() + " dist=" + distNonVirtual.ToString() + " distVirt=" + distLength.Dist.ToString() );
#endif 
			Costs2              = costs;
			BestCostType        = type;
			BestCostDistLength  = distLength;
			ApplyCommon( predecessor );
		}


#if DEBUG
		public void ApplyExplicitDistMatch( CodingCosts costs2, DistLength distLength, FindPathInfo predecessor, int length, int distNonVirtual )
#else
		public void ApplyExplicitDistMatch( CodingCosts costs2, DistLength distLength, FindPathInfo predecessor )
#endif
		{
#if DEBUG
			if ( ShortCodingSearch.PrintDebug( Index-length, 1 ) )
				Log.Line( "Apply ExplicitMatch for Position " + (Index-length).ToString() + " until " +  (Index-1).ToString() + " to FPI=" + Index.ToString() + " Costs=" + costs2.ToString()  + " dist=" + distNonVirtual.ToString() + " distVirt=" + distLength.Dist.ToString() );
#endif 
			Costs2              = costs2;
			BestCostType        = CodingItemType.ExpDist;
			BestCostDistLength  = distLength;
			ApplyCommon( predecessor );
		}


		private void ApplyCommon( FindPathInfo predecessor )
		{
			if ( this.Predecessor != null ) {
				this.Predecessor.ReferenceCount--;
#if DEBUG
				if ( this.Predecessor.ReferenceCount<0 )
					throw new Exception();
#endif
			}
			this.Predecessor = predecessor;
			this.Predecessor.ReferenceCount++;
		}


		public void SetCodingState( DistConverter distConverter, FindPathInfo predecessor, Literal literal, LengthInfo lengthInfo )
		{
#if DEBUG
			if ( Index == ShortCodingSearch.DebugFpiIndex || (predecessor.Index <= ShortCodingSearch.DebugDataIndex && ShortCodingSearch.DebugDataIndex < Index) )
				Log.Line( "ApplyCodingState from " + predecessor.Index.ToString() + " to FPI at " + Index.ToString() + " Type=" + BestCostType.ToString() );
#endif 

			if ( BestCostType == CodingItemType.Literal ) {
				int dataIndex             = predecessor.Index;
				int literalValue          = literal.GetFromData( dataIndex );
				int distVirtual           = predecessor.BestCostDistLength.IsLiteral ? 0 : predecessor.BestCostDistLength.Dist;
				int literalTreeIndex      = literal.GetContextIndex( dataIndex );
				int literalProposedValue  = -1;
				if ( distVirtual != 0 ) {
					int prevMatchDist = (distConverter==null) ? distVirtual : distConverter.VirtualDistToDist(dataIndex,distVirtual);
					if ( dataIndex>=prevMatchDist )
						literalProposedValue  = literal.GetFromData( dataIndex-prevMatchDist);
				} 

				if ( --Predecessor.ReferenceCount==0 ) {
					History = predecessor.History;
					predecessor.History = null;
					History.AddCodingItemLiteralReuse( literal.Bits, literalValue, literalProposedValue, literalTreeIndex );
				}
				else {
					History = predecessor.History.AddCodingItemLiteral( literal.Bits, literalValue, literalProposedValue, literalTreeIndex );
				}
			}
			else if ( BestCostType == CodingItemType.Rep0S ) {
				if ( --Predecessor.ReferenceCount==0 ) {
					History = predecessor.History;
					predecessor.History = null;
					History.AddCodingItemRep0SReuse( BestCostDistLength.Dist );
				}
				else {
					History = predecessor.History.AddCodingItemRep0S( BestCostDistLength.Dist );
				}
			}
			else {
				if ( --Predecessor.ReferenceCount==0 ) {
					History = predecessor.History;
					predecessor.History = null;
					History.AddCodingItemReuse( BestCostType, BestCostDistLength, lengthInfo.IndexToLength(BestCostDistLength.LengthIndex), BestCostDistLength.LengthIndex );
				}
				else {
					History = predecessor.History.AddCodingItem( BestCostType, BestCostDistLength, lengthInfo.IndexToLength(BestCostDistLength.LengthIndex), BestCostDistLength.LengthIndex );
				}
			}

			Predecessor = null;
		}


		public void SkipCodingState()
		{
			if ( Predecessor != null ) {
				Predecessor.ReferenceCount--;
				Predecessor = null;
			}
		}


		public void Reuse()
		{
			Dispose();
			Init();
		}


		public void Dispose()
		{
#if DEBUG
			if ( ReferenceCount != 0 )
				throw new Exception();
#endif
			if ( History != null )
				History.Dispose();
		}


#if DEBUG
		public override string ToString()
		{
			return "Index=" + Index.ToString("#,###,###,##0") + ", Cost=" + Costs2.ToString() + ", Type=" + BestCostType.ToString() + ", LengthIdx=" + BestCostDistLength.LengthIndex.ToString();
		}

		public static bool PrintDebug( int matchStartIndex, int length )
		{
			if ( matchStartIndex == ShortCodingSearch.DebugFpiIndex )
				return false;
			if ( ShortCodingSearch.DebugDataIndexIsFirstOfMatch )
				return matchStartIndex == ShortCodingSearch.DebugDataIndex;
			else
				return (matchStartIndex == ShortCodingSearch.DebugDataIndex) && (ShortCodingSearch.DebugDataIndex < matchStartIndex+length);
		}
#endif 
	}
}











