﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LC
{
    public class ShortCodingSearch
    {	
        private MatchGen                  matchGen;

        private Literal                   literal;
        private byte[]                    data;
        private LengthInfo                lengthInfo;
        private DistConverter             distConverter;
        private Level                     level;
        private int                       indexCost, indexHist, length, blockId, distVirtual, dist, repeatType;
        private int                       dataLength;
        private int                       maxLengthIndex, countDists, rep0DistVirtual, lengthIndex;
        private FindPathInfo              fpiLength=null, fpiCurrent;
        private ShortCodingSearchWindow   searchWindow; 
        private CodingStateImmutable      codingState;
        private double                    probabilityLengthCoding; 
        private CodingStateImmutableInfo  codingStateInfo;
        private int                       dataPos;
        private SmallArraySort            smallArraySort          = new SmallArraySort( SettingsFix.RepeatCount );

        private static double hashFillFactorN = 0.0D;

#if DEBUG
        public DebugInfo          DebugInfo;
        public static int         DebugDataIndex = -1;
        public static bool        DebugDataIndexIsFirstOfMatch = true;   // to reduce output if first index is known
        public static int         DebugFpiIndex = 8655285;                  // E.g. for length=4 and dataIndex=10 the match is stored at FpiIndex=14 
#endif

        public ShortCodingSearch( int dataLength, Literal literal, byte[] data, int dataLengthBits, Level level, LengthInfo lengthInfo, DistConverter distConverter )
        {
            this.dataLength           = dataLength;
            this.literal              = literal;
            this.data                 = data;
            this.lengthInfo           = lengthInfo;
            this.level                = level;
            this.distConverter        = distConverter;
            CodingExpDist codingExpDist = new CodingExpDist(( (distConverter==null) ? dataLengthBits : Tools.ValueToBitCount(distConverter.PosToVirtualPos(dataLength-1)+1) ) );
            this.codingStateInfo      = new CodingStateImmutableInfo( dataLengthBits, lengthInfo, codingExpDist );
            searchWindow            = new ShortCodingSearchWindow( lengthInfo.MaxMatchLength, literal, lengthInfo, dataLengthBits, level, CodingExpDist.ExpDistSlotBitCount );
        }

        
        

        
        

        public void Do( DistLength[] distLength, int dataLengthBits, ref int finishedEncodingsInHundredth, out string info )
        {
            matchGen = new MatchGen( data, dataLength, lengthInfo.GetLengthArray() );

            // dataPos = 0        handling
            fpiCurrent = searchWindow.Current;
#if DEBUG
            if ( Encoder.CompLog )
                DebugInfo.CostsToHereSCS[0] = fpiCurrent.Costs2.GetBitSize();
#endif
            codingState     = fpiCurrent.History;
            codingStateInfo.Init( codingState, 0 );
            CodingCosts costsLiteralDataPos0 = fpiCurrent.Costs2.Add( codingStateInfo.LiteralItemTypeProbability(codingState) * codingStateInfo.GetLiteralCodingProbabilityProduct( literal, distConverter, fpiCurrent.Index, 0 ) );
            searchWindow.GetSuccessor( 1 ).ApplyLiteral( costsLiteralDataPos0, fpiCurrent );


            int incProgressIndexCount = (dataLength-100)/100, nextIncProgressIndex = incProgressIndexCount;   // progress output
            for ( dataPos=1 ; dataPos<dataLength ; dataPos++ ) {
                if ( dataPos == nextIncProgressIndex ) {                                                    // progress output
                    nextIncProgressIndex += incProgressIndexCount;
                    Interlocked.Increment( ref finishedEncodingsInHundredth );
                }

                matchGen.Shift();
                searchWindow.MoveRight();
                fpiCurrent = searchWindow.Current;		
                
                if ( level.CheckToSkip( fpiCurrent.Costs2, fpiCurrent.MinCostsRight ) ) {
                    fpiCurrent.SkipCodingState();
                    continue;
                }

                FindPathInfo fpiPredecessor = searchWindow.GetPredecessor(fpiCurrent.GetDeltaToBestCostPredecessor(lengthInfo));
                fpiCurrent.SetCodingState( distConverter, fpiPredecessor, literal, lengthInfo );
#if DEBUG
                if ( Encoder.CompLog )
                    DebugInfo.CostsToHereSCS[dataPos] = fpiCurrent.Costs2.GetBitSize();
#endif
                distLength[dataPos] = fpiCurrent.BestCostDistLength; 

                codingState     = fpiCurrent.History;
                rep0DistVirtual = codingState.HistoryLatest.Val0;
                codingStateInfo.Init( codingState, dataPos );

                maxLengthIndex    = matchGen.MaxMatchIndexRight;
                bool tryLiteralCoding = true;
                if ( maxLengthIndex != -1 ) {
                    CodingCosts            currentCosts2         = fpiCurrent.Costs2;
                    smallArraySort.Update( codingState.HistoryType[codingStateInfo.CodingItemTypeStateIndex].Probabilities );
        

                    // dist history coding
                    // for speedup assume history coding always shorter than explicit dist coding
                    // coding with lower rank is shorter than higher rank except the four repeat codings!					
                    indexCost=0; indexHist=-1; length=-1; blockId=-1; distVirtual=-1; dist=-1; repeatType=-1;
                    countDists = SettingsFix.RepeatCount + codingState.HistoryDistance.FirstOccurenceOneIndex;

                    for ( lengthIndex = Math.Max( 0, maxLengthIndex+1-level.LengthTryCount ) ; lengthIndex<=maxLengthIndex ; lengthIndex++ ) {
                        length            = lengthInfo.IndexToLength( lengthIndex );
                        fpiLength         = searchWindow.GetSuccessor( length );
                        blockId           = matchGen.DistGen.GetId(dataPos+length-1, lengthIndex );
                        bool forbidRep0   = lengthIndex>=fpiCurrent.History.Rep0AllowedLengthIndexCount;

                        NextDist( forbidRep0 );

                        if ( indexCost<countDists ) {    // found history match for this length
                            if ( lengthIndex>level.TryLiteralMaxHistoryLengthIndex )
                                tryLiteralCoding = false;
                            int type = (repeatType==-1) ? CodingItemType.Hist : repeatType;
                            probabilityLengthCoding = (fpiCurrent.History.HistoryLength!=null && repeatType == 0 && fpiCurrent.History.Rep0AllowedLengthIndexCount != lengthInfo.LengthIndexCount) ? codingStateInfo.Repeat0LengthCodingSizeProbability(codingState, lengthIndex, fpiCurrent.History.Rep0AllowedLengthIndexCount) : codingStateInfo.LengthToCodingSizeProbability(type,lengthIndex,dataPos,codingState);
                            CodingCosts costs2 = currentCosts2.Add( codingStateInfo.DistanceHistoryIndexToProbability(indexHist,repeatType) * probabilityLengthCoding );

                            if ( costs2 < fpiLength.Costs2 ) { 
#if DEBUG
                                fpiLength.ApplyHistoryMatch( costs2, indexHist, new DistLength(distVirtual,lengthIndex,type), fpiCurrent, length, dist, type );
#else
                                fpiLength.ApplyHistoryMatch( costs2, indexHist, new DistLength(distVirtual,lengthIndex,type), fpiCurrent, type );
#endif
                                searchWindow.UpdateMinCostsRight( length, costs2 );
                            }
                        }
                        else {  // no history match; use explicit dist 
                            int       shortestDist        = matchGen.GetShortestDistRight( lengthIndex );
                            probabilityLengthCoding       = codingStateInfo.LengthToCodingSizeProbability( CodingItemType.ExpDist, lengthIndex, dataPos, codingState );
                            int       shortestDistVirtual = (distConverter==null) ? shortestDist : distConverter.DistToVirtualDist( dataPos, shortestDist );
                            CodingCosts  costs2          = currentCosts2.Add( probabilityLengthCoding * codingStateInfo.CodeExpDistProbability(shortestDistVirtual-1,lengthIndex) );

                            if ( costs2 < fpiLength.Costs2 && shortestDistVirtual!=rep0DistVirtual ) { 
#if DEBUG
                                fpiLength.ApplyExplicitDistMatch( costs2, new DistLength( shortestDistVirtual, lengthIndex, CodingItemType.ExpDist ), fpiCurrent, length, shortestDist );
#else
                                fpiLength.ApplyExplicitDistMatch( costs2, new DistLength( shortestDistVirtual, lengthIndex, CodingItemType.ExpDist ), fpiCurrent );
#endif
                                searchWindow.UpdateMinCostsRight( length, costs2 );
                            }
                        }
                    }
                }
                if ( tryLiteralCoding ) { 
                    FindPathInfo fpiLiteral   = searchWindow.GetSuccessor( 1 );

                    // Rep0
                    dist = (distConverter==null) ? rep0DistVirtual : distConverter.VirtualDistToDist( dataPos, rep0DistVirtual );
                    if ( dist>0 && data[dataPos] == data[dataPos-dist] ) {
                        CodingCosts costsRep0S = fpiCurrent.Costs2.Add( codingStateInfo.GetRep0SProbability() );
                        if ( costsRep0S<fpiLiteral.Costs2 )
                            fpiLiteral.ApplyRep0S( costsRep0S, fpiCurrent, rep0DistVirtual );				
                    }
                        

                    // literal
                    CodingCosts costsLiteral = fpiCurrent.Costs2.Add( codingStateInfo.LiteralItemTypeProbability(codingState) * codingStateInfo.GetLiteralCodingProbabilityProduct( literal, distConverter, fpiCurrent.Index, (fpiCurrent.BestCostDistLength.IsLiteral?0:fpiCurrent.BestCostDistLength.Dist) ) );
                    if ( costsLiteral<fpiLiteral.Costs2 )
                        fpiLiteral.ApplyLiteral( costsLiteral, fpiCurrent );
                }

            }	

            searchWindow.MoveRight();
            distLength[dataLength] = searchWindow.Current.BestCostDistLength;
            searchWindow.Current.SkipCodingState();
#if DEBUG
            Log.Line( "Estimated coding length: " + (searchWindow.Current.Costs2.GetBitSize()/8).ToString("#,###,###,##0.000") );
#endif
            searchWindow.Dispose();

            ReversePath( distLength );

            double hashFillFactorNcurrent = ((double)matchGen.DistGen.CountHashTableEntries) / ((double)dataLength);
            hashFillFactorN = Math.Max( hashFillFactorN, hashFillFactorNcurrent );
            info = matchGen.DistGen.MaxEntriesPerBucket.ToString().PadLeft(2) + " " + matchGen.DistGen.UsedBucketsFraction.ToString("0.000") + " " + hashFillFactorNcurrent.ToString("0.000") + "/" + hashFillFactorN.ToString("0.000");
        }


        private void NextDist( bool forbidRep0 )
        {
            indexCost--;
            while ( ++indexCost < countDists ) {
                if ( indexCost < SettingsFix.RepeatCount ) {
                    repeatType = smallArraySort.RankToValueIndex(indexCost);
                    if ( repeatType==0 && forbidRep0 )
                        continue;
                    indexHist = codingStateInfo.RepeatTypeToHistoryIndex( repeatType );
                    distVirtual = codingState.HistoryLatest.GetVal( repeatType );
                }
                else {
                    repeatType = -1;
                    indexHist = indexCost - SettingsFix.RepeatCount;
                    distVirtual  = codingState.HistoryDistance.GetValue( indexHist );
                    if ( distVirtual == rep0DistVirtual )
                        continue;
                }
                dist         = (distConverter==null) ? distVirtual : distConverter.VirtualDistToDist( dataPos, distVirtual );
                if ( dist==0 )
                    continue;

                int leftIndex    = dataPos - dist;
                if ( leftIndex>=0 && data[leftIndex]==data[dataPos]/* for speed only */ && blockId==matchGen.DistGen.GetId(leftIndex+length-1,lengthIndex)  /* blockIds[leftIndex]==blockId*/   /*same as: matchGen.IsMatch(i,leftIndex,lengthIndex)*/ )  
                    break;   // dist found
            }
        }


        private void ReversePath( DistLength[] distLength )
        {
            // switch backward references to forward references
            DistLength lastDistLength = DistLength.Literal;
            for ( int i=dataLength ; i >= 0 ; i-=lastDistLength.GetDelta(lengthInfo) ) {
                DistLength newDistLength = distLength[i];
                distLength[i] = lastDistLength;
#if DEBUG
                if ( i <= DebugDataIndex && DebugDataIndex < i+distLength[i].GetDelta(lengthInfo) )
                    Log.Line( "Final Path contains From " + i.ToString() + " to " + (i+distLength[i].GetDelta(lengthInfo)-1).ToString() + " : " + distLength[i].ToString() );		
#endif

                lastDistLength = newDistLength;
            }
        }

#if DEBUG
        public static bool PrintDebug( int matchStartIndex, int length )
        {
            if ( matchStartIndex+length == ShortCodingSearch.DebugFpiIndex )
                return false;
            if ( ShortCodingSearch.DebugDataIndexIsFirstOfMatch )
                return matchStartIndex == ShortCodingSearch.DebugDataIndex;
            else
                return matchStartIndex <= ShortCodingSearch.DebugDataIndex && ShortCodingSearch.DebugDataIndex < matchStartIndex+length;
        }
#endif 


    }
}
