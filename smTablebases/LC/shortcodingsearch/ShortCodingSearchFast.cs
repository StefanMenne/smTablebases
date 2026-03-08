using System.Runtime.CompilerServices;

namespace LC;


public class ShortCodingSearchFast
{
    public static void Do(byte[] data, int dataLength, LengthInfo lengthInfo, Literal literal, Level level,
        DistLength[] distLength, int dataLengthBits, DistConverter distConverter, ref int finishedEncodingsInHundredth)
    {
        MatchGen matchGen = new MatchGen(data, dataLength, lengthInfo.GetLengthArray());
        CodingState codingState = new CodingState(dataLengthBits, lengthInfo.LengthIndexCount, literal, lengthInfo,
            level, CodingExpDist.ExpDistSlotBitCount);

        // dataPos = 0        handling
        distLength[0] = DistLength.Literal;
        codingState.AddCodingItemLiteral();
        matchGen.Shift();

        int incProgressIndexCount = (dataLength-100)/100, nextIncProgressIndex = incProgressIndexCount;   // progress output
        for (int dataPos = 1; dataPos < dataLength;)
        {
            if ( dataPos >= nextIncProgressIndex ) {                                                    // progress output
                nextIncProgressIndex += incProgressIndexCount;
                Interlocked.Increment( ref finishedEncodingsInHundredth );
            }
            int maxLengthIndex = matchGen.MaxMatchIndexRight;
            bool codingPerformed = false;
            if (maxLengthIndex != -1)
            {
                int maxLength = lengthInfo.IndexToLength(maxLengthIndex);
                int shortestDist = matchGen.GetShortestDistRight(maxLengthIndex);
                if (shortestDist <= dataPos)
                {
                    int blockId = matchGen.DistGen.GetId(dataPos + maxLength - 1, maxLengthIndex);

                    if (FindRep(codingState, matchGen, distConverter, distLength, data, dataPos, maxLength,
                            maxLengthIndex, blockId ))
                    {
                        dataPos += maxLength;
                        codingPerformed = true;
                    }
                    else if (maxLengthIndex >= 1)
                    {
                        int lengthIndex2 = maxLengthIndex - 1;
                        int length2 = lengthInfo.IndexToLength(lengthIndex2);
                        int blockId2 = matchGen.DistGen.GetId(dataPos + length2 - 1, lengthIndex2 );

                        if (FindRep(codingState, matchGen, distConverter, distLength, data, dataPos, length2,
                                lengthIndex2, blockId2 ) )
                        {
                            dataPos += length2;
                            codingPerformed = true;
                        }
                    }
                    
                    
                    if (!codingPerformed) {
                        if (FindHis(codingState, matchGen, distConverter, distLength, data, dataPos, maxLength,
                                maxLengthIndex, blockId ))
                        {
                            dataPos += maxLength;
                            codingPerformed = true;
                        }
                    }
                    
                    int shortestDistVirtual = (distConverter == null) ? shortestDist : distConverter.DistToVirtualDist(dataPos, shortestDist);
                    int distRep0Virtual = codingState.HistoryLatest.Val0;
                    if (!codingPerformed && distRep0Virtual != shortestDistVirtual)
                    {
                        distLength[dataPos] =
                            new DistLength(shortestDistVirtual, maxLengthIndex, CodingItemType.ExpDist);
                        codingState.AddCodingItemMatch(CodingItemType.ExpDist, shortestDistVirtual, maxLengthIndex,
                            maxLength, -1);
                        for (int i = 0; i < maxLength; i++)
                        {
                            matchGen.Shift();
                        }

                        dataPos += maxLength;
                        codingPerformed = true;
                    }

                    
                }
            }

            if (!codingPerformed) {              // rep0s
                int rep0DistVirtual = codingState.HistoryLatest.Val0;
                int rep0Dist = (distConverter==null) ? rep0DistVirtual : distConverter.VirtualDistToDist( dataPos, rep0DistVirtual );
                if ( rep0Dist>0 && data[dataPos] == data[dataPos-rep0Dist] ) {
                    distLength[dataPos] = DistLength.Rep0SWithDist( rep0DistVirtual );
                    codingState.AddCodingItemRep0S( rep0DistVirtual, codingState.GetHistoryEntryIndex( rep0DistVirtual ) );
                    codingPerformed = true;
                    matchGen.Shift();
                    dataPos++;
                }
            }
            
            
            if (!codingPerformed)
            {
                distLength[dataPos] = DistLength.Literal;
                codingState.AddCodingItemLiteral();
                matchGen.Shift();
                dataPos++;
            }
        }

    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FindRep( CodingState codingState, MatchGen matchGen, DistConverter distConverter, DistLength[] distLength, byte[] data, int dataPos, int length, int lengthIndex, int blockId )
    {
        int rep0maxLengthIndex = codingState.Rep0AllowedLengthIndexCount;
        int distRep0Virtual = codingState.HistoryLatest.Val0;
 
        for (int rep = 0; rep < 4; rep++)
        {
            if (rep == 0 && lengthIndex >= rep0maxLengthIndex)
                continue;

            int distRepVirtual = codingState.HistoryLatest.GetVal(rep);
            if (distRepVirtual == distRep0Virtual)
                continue;
            int distRep = (distConverter == null)
                ? distRepVirtual
                : distConverter.VirtualDistToDist(dataPos, distRepVirtual);
            if (distRep == 0)
                continue;

            int leftIndex = dataPos - distRep;
            if (leftIndex >= 0 && data[leftIndex] == data[dataPos] /* for speed only */ &&
                blockId == matchGen.DistGen.GetId(leftIndex + length - 1, lengthIndex))
            {
                distLength[dataPos] = new DistLength(distRepVirtual, lengthIndex, rep);
                codingState.AddCodingItemMatch(rep, distRepVirtual, lengthIndex, length, -1);
                for (int i = 0; i < length; i++)
                    matchGen.Shift();
                return true;
            }
        }

        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FindHis(CodingState codingState, MatchGen matchGen, DistConverter distConverter,
        DistLength[] distLength, byte[] data, int dataPos, int length, int lengthIndex, int blockId )
    {
        int countHistDists = codingState.HistoryDistance.FirstOccurenceOneIndex;
        int distRep0Virtual = codingState.HistoryLatest.Val0;
        for ( int histIndex=0 ; histIndex<countHistDists ; histIndex++ ) {
            int histDistVirtual = codingState.HistoryDistance.GetValue(histIndex);
            if ( histDistVirtual==distRep0Virtual )
                continue;
            int histDist = (distConverter==null) ? histDistVirtual : distConverter.VirtualDistToDist( dataPos, histDistVirtual );
            if (histDist != 0)  {
                int leftIndex = dataPos - histDist;
                if ( leftIndex>=0 && data[leftIndex]==data[dataPos] /* for speed only */ && blockId==matchGen.DistGen.GetId( leftIndex+length-1, lengthIndex ) ) {
                    distLength[dataPos] = new DistLength(histDistVirtual, lengthIndex, CodingItemType.Hist);
                    codingState.AddCodingItemMatch( CodingItemType.Hist, histDistVirtual, lengthIndex, length, histIndex );
                    for (int i = 0; i < length; i++)
                        matchGen.Shift();
                    return true;
                }
            }
        }
        return false;
    }



}