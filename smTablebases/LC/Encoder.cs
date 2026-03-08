﻿namespace LC
{
	public class Encoder : Coder
	{
		private static int                    finishedEncodingsInHundredth;
		public  string                        Info           = "";                 
#if DEBUG
		public  static bool                   CompLog       = false,     ShowExpDistCosts = false;
		public  static DebugInfo              DebugInfoToCode = null;
		private DebugInfo                     debugInfo;
		private CodingStateImmutablePool      codingStateVerPool;
		private int                           countLiteralCodings=0, countIsRep0Codings=0, countIsRep0SCodings=0, countTypeOtherCodings=0;
		private double                        sumCostsIsLiteral=0d, sumCostsIsRep0=0d, sumCostsIsRep0S=0d, sumCostsTypeOther = 0.0;
#endif
		

		public Encoder()
		{
			settings = Settings.Default;
		}


		public static int FinishedEncodingsInHundredth
		{
			get { return finishedEncodingsInHundredth; }
		}


		public Settings Settings
		{
			get { return settings; }
		}




		public int Encode( byte[] data, int length, byte[] bufferOut, int[] posToVirtualPos=null )
		{
			this.dataLength     = length/settings.BytesPerItem;
			this.dataLengthBits = Tools.ValueToBitCount( dataLength );
			this.lengthInfo     = LengthInfo.Instances[(int)settings.LengthSet];

			// if (posToVirtualPos == null)  {
			// 	posToVirtualPos = new int[length];
			// 	for (int i = 0; i < posToVirtualPos.Length; i++)
			// 		posToVirtualPos[i] = i;
			// }
			
#if DEBUG
			if ( CompLog )
				debugInfo = new DebugInfo( dataLength, data, DebugInfoToCode==null, true, lengthInfo.GetLengthArray(), settings.Level );
#endif
			Init( new RangeEncoder(bufferOut), data, posToVirtualPos, lengthInfo );
#if DEBUG
			double headerBits  = RangeEncoder.AddBits( (uint)settings.AsBits, Settings.BitCount );
			headerBits        += RangeEncoder.AddBits( (uint)dataLengthBits, 5 );
			headerBits        += RangeEncoder.AddBits( (uint)dataLength, dataLengthBits );
#else
			RangeEncoder.AddBits( (uint)settings.AsBits, Settings.BitCount );
			RangeEncoder.AddBits( (uint)dataLengthBits, 5 );
			RangeEncoder.AddBits( (uint)dataLength, dataLengthBits );
#endif
			if ( dataLength != 0 ) { 
				DistLength[]  distLength   = new DistLength[dataLength+1];

#if DEBUG

				if ( DebugInfoToCode != null ) { 
					Log.Line( "USE COMPRESSION-LOG FOR CODING" );
					DebugInfoToCode.GetDistLength( distLength, lengthInfo, true );
					if ( !VerifyDistLength(data,distLength) ) {
						Log.Line( "Illegal coding! Maybe not created with same data." );
						return -1;					
					}
				}
				else {
					if ( settings.LengthSet == LengthSet.Set3_Length_2_3_4____273_noFSC )
						throw new Exception( "Not allowed to use this LengthSet for Encoding." );
					FindShortCoding( distLength, distConverter );
					if ( Verify && !VerifyDistLength(data,distLength) ) 
						throw new Exception( "Coding not correct" );
				}

				if ( CompLog )
					CompLogAnalyze( debugInfo, distLength );

#else
				FindShortCoding( distLength, distConverter );
#endif

				Code( distLength, distConverter, dataLengthBits );

#if DEBUG
				if ( CompLog )
					WriteCompressionLog( -1 );
#endif
			}

#if DEBUG
			double fillBits = 0.0;
			for ( int i=length%settings.BytesPerItem ; i>=1 ; i-- )       // fill not coded bytes if length%BytesPerItem != 0
				fillBits += RangeEncoder.AddBits( ((uint)data[length-i]) | 0x100 , 9 );
			fillBits += RangeEncoder.AddBit( false );
#else
			for ( int i=length%settings.BytesPerItem ; i>=1 ; i-- )       // fill not coded bytes if length%byteserItem != 0
				RangeEncoder.AddBits( ((uint)data[length-i]) | 0x100 , 9 );
			RangeEncoder.AddBit( false );
#endif

			int countBytesTotal = RangeEncoder.Close();

#if DEBUG
			if ( dataLength == 0 )
				return countBytesTotal;
			int characterSize = 39;
			int countAllCodings = countLiteralCodings + countIsRep0Codings;
			string s = "IsLiteral".PadRight(20) + countAllCodings.ToString("###,###,###,##0").PadLeft(10) + (sumCostsIsLiteral/countAllCodings).ToString("#,###,###,##0.000").PadLeft(7) + (sumCostsIsLiteral/8).ToString("#,###,###,##0.000").PadLeft(14) + "\r\n";
			s += "IsRep0".PadRight(20) + countIsRep0Codings.ToString("###,###,###,##0").PadLeft(10) + (sumCostsIsRep0/countIsRep0Codings).ToString("#,###,###,##0.000").PadLeft(7) + (sumCostsIsRep0/8).ToString("#,###,###,##0.000").PadLeft(14) + "\r\n";
			s += "IsRep0S".PadRight(20) + countIsRep0SCodings.ToString("###,###,###,##0").PadLeft(10) + (sumCostsIsRep0S/countIsRep0SCodings).ToString("#,###,###,##0.000").PadLeft(7) + (sumCostsIsRep0S/8).ToString("#,###,###,##0.000").PadLeft(14) + "\r\n";
			s += "TypeOther".PadRight(20) + countTypeOtherCodings.ToString("###,###,###,##0").PadLeft(10) + (sumCostsTypeOther/countTypeOtherCodings).ToString("#,###,###,##0.000").PadLeft(7) + (sumCostsTypeOther/8).ToString("#,###,###,##0.000").PadLeft(14) + "\r\n";
			s += "Literal".PadRight(20) + literalCoder.ToString() + "\r\n";
			s += "Repeat".PadRight(20) + repeatDistCoder.ToString() + "\r\n";
			s += "History".PadRight(20) + historyDistCoder.ToString() + "\r\n";
			if ( ShowExpDistCosts )
				s += codingExpDist.GetDetailledOutput();
			s += "Length".PadRight(20) + lengthCoder.ToString();

			Log.Line( s );
			Log.Line( "Header: ".PadRight(characterSize) + (headerBits/8).ToString("###.000").PadLeft(12) );
			Log.Line( "FillModuloData: ".PadRight(characterSize) + (fillBits/8.0).ToString("##0.000").PadLeft(12) );
			Log.Line( "Padding RangeCoder: ".PadRight(characterSize) + (RangeEncoder.SumPaddingBits/8).ToString("##0.000").PadLeft(12) );
			Log.Line( "Total: ".PadRight(characterSize) + countBytesTotal.ToString("#,###,###,##0.000").PadLeft(12) );

			DebugInfoToCode = null;
#endif

			return countBytesTotal;
		}


#if DEBUG
		private bool VerifyDistLength( byte[] data, DistLength[] distLength )
		{ 
			for ( int i=0 ; i<dataLength ; i+=distLength[i].GetDelta(lengthInfo) ) {
				if ( distLength[i].IsLiteral ) {
				}
				else if ( distLength[i].IsExpDist || CodingItemType.IsRep(distLength[i].Type) ) {
					int virtualDist   = distLength[i].Dist;
					int dist          = distConverter.VirtualDistToDist( i, virtualDist );
					int lengthIndex   = distLength[i].LengthIndex;
					int length        = distLength[i].GetDelta( lengthInfo );
					for ( int j=0 ; j<length ;  j++ ) {
						if ( data[i+j] != data[i+j-dist] )
							return false;
					}			
				}
			}
			return true;
		}
#endif


		private void FindShortCoding( DistLength[] distLength, DistConverter distConverter )
		{

			if (settings.Level == 6)
			{
				ShortCodingSearchFast.Do( data, dataLength, lengthInfo, literal, level, distLength, dataLengthBits, distConverter, ref finishedEncodingsInHundredth );
			}
			else
			{
				ShortCodingSearch scs = new ShortCodingSearch(dataLength, literal, data, dataLengthBits, level,
					lengthInfo, distConverter);
#if DEBUG
				scs.DebugInfo = debugInfo;
#endif
				scs.Do(distLength, dataLengthBits, ref finishedEncodingsInHundredth, out Info);
			}
		}


		private void Code( DistLength[] distLength, DistConverter distConverter, int dataLengthBits )
		{
			int prevMatchVirtualDist = 0;
#if DEBUG
			double costs = 0.0;
			CodingStateImmutableInfo codingStateInfoVer = new CodingStateImmutableInfo( dataLengthBits, lengthInfo, codingExpDist );
			codingStateVerPool = new CodingStateImmutablePool( 2, lengthInfo, literal, dataLengthBits, level, CodingExpDist.ExpDistSlotBitCount );
			int matchCount     = 0;
			int count          = 0;
			int oldIndex       = -1;
			Log.Line( "Encode" );
			double headingCosts = RangeEncoder.PosInBits;


			int[] lengthIndexOccurence = new int[lengthInfo.LengthIndexCount+1];
#endif
			for ( int i=0 ; i<dataLength ; i+=distLength[i].GetDelta(lengthInfo) ) {
#if DEBUG
				if ( CompLog ) {
					debugInfo.Items[i].CostsToHere = costs;
					if ( debugInfo.CostsToHereSCS[i]!=0d && Math.Abs( debugInfo.CostsToHereSCS[i] - costs ) > 0.0001 )
						throw new Exception( "SCS costs do not match Encoding costs! Coding from " + oldIndex.ToString() + " to " + i.ToString() );
				}
				
				costs += EncodeItem( i, distLength[i], distConverter, codingStateInfoVer, ref prevMatchVirtualDist );
				if ( Math.Abs( RangeEncoder.PosInBits - headingCosts - costs ) > 0.01 )
					throw new Exception();
				if ( !distLength[i].IsLiteral ) {
					lengthIndexOccurence[distLength[i].IsRep0S ? 0 : (1+distLength[i].LengthIndex)]++;
					matchCount++;
				}
				count++;

				oldIndex = i;
#else
				EncodeItem( i, distLength[i], distConverter, ref prevMatchVirtualDist );
#endif
			}
#if DEBUG
			Log.Line( "Literals:    " + countLiteralCodings.ToString("#,###,###,###,###0").PadLeft(15) );
			Log.Line( "Rep0S:       " + lengthIndexOccurence[0].ToString("#,###,###,###,###0").PadLeft(15) );
			for ( int i=0 ; i<lengthInfo.LengthIndexCount ; i++ )
				Log.Line( "Length:   " + lengthInfo.IndexToLength(i).ToString().PadLeft(3) + lengthIndexOccurence[i+1].ToString("#,###,###,###,###0").PadLeft(15) );
#endif
		}



#if DEBUG
		public double EncodeItem( int dataIndex, DistLength distLength, DistConverter distConverter, CodingStateImmutableInfo codingStateInfoVer, ref int prevMatchVirtualDist )
#else
		public void EncodeItem( int dataIndex, DistLength distLength, DistConverter distConverter, ref int prevMatchVirtualDist )
#endif
		{
			int     type, historyIndex = -1, distVirtual = -1, lengthIndex = -1, dist = -1;

#if DEBUG
			double costs, costIsLit, costIsRep_Virt=0.0d, costIsRep0=0.0d, costIsRep0S=0.0d, costsDistOrLit=0.0d, costTypeOther=0.0d, costsLength=0.0d;
			string debugItemInfo = "";
			int length = distLength.GetDelta(lengthInfo);
			if ( dataIndex!=-1 && dataIndex<=ShortCodingSearch.DebugDataIndex && ShortCodingSearch.DebugDataIndex<=dataIndex+length )
				Log.Line( "Code " + distLength.ToString() + " from " + dataIndex.ToString() + " to " + (dataIndex+length).ToString() );

			costIsLit = RangeEncoder.AddBit( distLength.IsLiteral, codingState.HistoryIsLit.Probabilities[codingState.GetIsLiteralStateIndex(dataIndex)] );
			sumCostsIsLiteral += costIsLit;
#else
			RangeEncoder.AddBit( distLength.IsLiteral, codingState.HistoryIsLit.Probabilities[codingState.GetIsLiteralStateIndex(dataIndex)] );
#endif
			
			if ( distLength.IsLiteral ) {   // literal
				type = CodingItemType.Literal;
				int prevLit = -1;
				if ( prevMatchVirtualDist != 0 ) {
					int prevMatchDist = (distConverter==null) ? prevMatchVirtualDist : distConverter.VirtualDistToDist(dataIndex,prevMatchVirtualDist);
					if ( prevMatchDist>0 && dataIndex>=prevMatchDist/*can occur with initial values??*/ )
						prevLit = literal.GetFromData(dataIndex-prevMatchDist);
				} 
#if DEBUG
				int ctxIndex= literal.GetContextIndex(dataIndex);
				costsDistOrLit = literalCoder.Encode( literal.GetFromData(dataIndex), prevLit, codingState.LiteralTrees[ctxIndex].Probabilities );
				countLiteralCodings++;
#else
				literalCoder.Encode( literal.GetFromData(dataIndex), prevLit, codingState.LiteralTrees[literal.GetContextIndex(dataIndex)].Probabilities );
#endif
				prevMatchVirtualDist = 0;
				codingState.AddCodingItemLiteral();
			}
			else {
				distVirtual    = distLength.Dist;
				lengthIndex    = distLength.LengthIndex;
				dist           = (distConverter==null||distVirtual==-1) ? distVirtual : distConverter.VirtualDistToDist(dataIndex,distVirtual);
				double probIsRep0;
				double[] probabilities = codingState.GetDistHistoryProbabilities( out probIsRep0 );
				bool isRep0 = distVirtual == codingState.HistoryLatest.Val0;
#if DEBUG
				double probabilityIsRep = codingState.GetProbabilityIsRep();			
				costIsRep0 = RangeEncoder.AddBit( isRep0, probIsRep0 );
				sumCostsIsRep0 += costIsRep0;
				countIsRep0Codings++;
#else
				RangeEncoder.AddBit( isRep0, probIsRep0 );
#endif

				prevMatchVirtualDist = distVirtual;
				if ( isRep0 ) {
					bool isRep0S = distLength.IsRep0S;
#if DEBUG
					costIsRep0S = RangeEncoder.AddBit( isRep0S, codingState.ProbabilityIsRep0S );
					sumCostsIsRep0S += costIsRep0;
					countIsRep0SCodings++;
#else
					RangeEncoder.AddBit( isRep0S, codingState.ProbabilityIsRep0S );
#endif
					if ( isRep0S ) {
						type = CodingItemType.Rep0S;
						lengthIndex = -1;
					}
					else { 
						type = CodingItemType.Rep0;
#if DEBUG
						costsLength         = EncodeLength( type, distLength.LengthIndex, dataIndex );
#else
						EncodeLength( type, distLength.LengthIndex, dataIndex );
#endif
					}
					historyIndex = codingState.CountHistoryDists;
				}
				else if ( CodingItemType.IsRep( distLength.Type ) ) {    // repeat
					type = codingState.HistoryLatest.GetRank( distLength.Dist );

					historyIndex = codingState.CountHistoryDists + type;
#if DEBUG
					if ( type == -1 )
						throw new Exception();
					costTypeOther += repeatDistCoder.EncodeProbabilitySum1( historyIndex, probabilities, codingState.CountHistoryDists+SettingsFix.RepeatCount );// distinguish only for debug purpose
					sumCostsTypeOther += costTypeOther;
					countTypeOtherCodings++;
					costsLength         = EncodeLength( type, distLength.LengthIndex, dataIndex );
#else
					historyDistCoder.EncodeProbabilitySum1( historyIndex, probabilities, codingState.CountHistoryDists+SettingsFix.RepeatCount );
					EncodeLength( type, distLength.LengthIndex, dataIndex );
#endif
				}
				else if ( distLength.IsHist ) {   // history match
					historyIndex = codingState.GetHistoryEntryIndex( distLength.Dist );
					type = CodingItemType.Hist;

#if DEBUG
					if ( historyIndex==-1 || historyIndex>=codingState.HistoryDistance.FirstOccurenceOneIndex )
						throw new Exception();
					double probRepeatOrExpDist = 0.0d;
					for ( int i=0 ; i<SettingsFix.RepeatCount+1 ; i++ )
						probRepeatOrExpDist += probabilities[codingState.CountHistoryDists+i];
					double costRepOrExpDist = Math.Log( 1.0d / probRepeatOrExpDist );
					costTypeOther      += costRepOrExpDist;
					costsDistOrLit      = historyDistCoder.EncodeProbabilitySum1( historyIndex, probabilities, codingState.CountHistoryDists+SettingsFix.RepeatCount );  // distinguish only for debug purpose
					costsDistOrLit     -= costRepOrExpDist;
					sumCostsTypeOther  += costsDistOrLit;
					countTypeOtherCodings++;
					costsLength         = EncodeLength( type, distLength.LengthIndex, dataIndex );
#else
					historyDistCoder.EncodeProbabilitySum1( historyIndex, probabilities, codingState.CountHistoryDists+SettingsFix.RepeatCount );
					EncodeLength( type, distLength.LengthIndex, dataIndex );
#endif			
					prevMatchVirtualDist = distVirtual;
				}
				else {   // exp dist
					type = CodingItemType.ExpDist;
					historyIndex  = codingState.CountHistoryDists + SettingsFix.RepeatCount;
#if DEBUG
					costTypeOther      += historyDistCoder.EncodeProbabilitySum1( historyIndex, probabilities, codingState.CountHistoryDists+SettingsFix.RepeatCount );  // distinguish only for debug purpose
					costsLength         = EncodeLength( type, distLength.LengthIndex, dataIndex );
					costsDistOrLit      = codingExpDist.Encode( RangeEncoder, codingState.HistoryExpDistSlot[CodingExpDist.LengthIndexToContextIndex(distLength.LengthIndex)], distVirtual-1, codingState.HistoryExpDistBits, out debugItemInfo );
#else
					historyDistCoder.EncodeProbabilitySum1( historyIndex, probabilities, codingState.CountHistoryDists+SettingsFix.RepeatCount );
					EncodeLength( type, distLength.LengthIndex, dataIndex );
					codingExpDist.Encode( RangeEncoder, codingState.HistoryExpDistSlot[CodingExpDist.LengthIndexToContextIndex(distLength.LengthIndex)], distVirtual-1, codingState.HistoryExpDistBits );
#endif
					prevMatchVirtualDist = distVirtual;
				}
#if DEBUG
				costIsRep_Virt = Math.Log( 1.0d / ((CodingItemType.IsRep(type)||CodingItemType.Rep0S==type)?probabilityIsRep:(1.0d-probabilityIsRep)), 2.0d );

				if ( type==0 && codingState.HistoryLength!=null && distLength.LengthIndex>=codingState.Rep0AllowedLengthIndexCount )
					throw new Exception( dataIndex.ToString() );
#endif
				if ( type == CodingItemType.Rep0S ) {
					codingState.AddCodingItemRep0S( distLength.Dist, historyIndex );
				}
				else { 
					codingState.AddCodingItemMatch( type, distLength.Dist, distLength.LengthIndex, lengthInfo.IndexToLength(distLength.LengthIndex), historyIndex );
				}
			}

#if DEBUG
			costs = costIsLit + costIsRep0 + costIsRep0S + costTypeOther + costsDistOrLit + costsLength;
			if ( CompLog ) {
				debugInfo.Items[dataIndex].CostIsLiteral   = costIsLit;
				debugInfo.Items[dataIndex].CostIsRep_Virt  = costIsRep_Virt;
				debugInfo.Items[dataIndex].CostIsRep0      = costIsRep0;
				debugInfo.Items[dataIndex].CostIsRep0S     = costIsRep0S;
				debugInfo.Items[dataIndex].CostDistOrLit   = costsDistOrLit;
				debugInfo.Items[dataIndex].CostLength      = costsLength;
				debugInfo.Items[dataIndex].CostTypeOther   = costTypeOther;
				debugInfo.Items[dataIndex].Type            = type;
				debugInfo.Items[dataIndex].Info            = debugItemInfo;
			}
			if ( Verify ) {
				verifyQueue.Enqueue( type );
				verifyQueue.Enqueue( historyIndex );
				verifyQueue.Enqueue( distVirtual );
				verifyQueue.Enqueue( dist );
				verifyQueue.Enqueue( lengthIndex );
				verifyQueue.Enqueue( (long)RangeEncoder.Low );
				verifyQueue.Enqueue( (long)RangeEncoder.Range );
				verifyQueue.Enqueue( RangeEncoder.BufferPos );
			}
			return costs;
#endif
		}


#if DEBUG
		private double EncodeLength( int type, int lengthIndex, int pos )
#else
		private void EncodeLength( int type, int lengthIndex, int pos )
#endif
		{
			bool isRep = CodingItemType.IsRep(type);
			if ( codingState.HistoryLength == null ) {
#if DEBUG
				double cost = RangeEncoder.AddBit( lengthIndex<8, codingState.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount] );
#else
				RangeEncoder.AddBit( lengthIndex<8, codingState.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount] );
#endif
				codingState.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount] = Probability.ChangeProbability( codingState.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount], lengthIndex<8, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability );
				if ( lengthIndex < 8 ) {
#if DEBUG
					cost += codingState.HistoryLengthTreeLow[pos&3].CodeAndUpdateTreeProbabilities( RangeEncoder, lengthIndex, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:6) );
#else
					codingState.HistoryLengthTreeLow[pos&3].CodeAndUpdateTreeProbabilities( RangeEncoder, lengthIndex, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:6) );
#endif
				}
				else { 
#if DEBUG
					cost += RangeEncoder.AddBit( lengthIndex<16, codingState.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)] );
#else
					RangeEncoder.AddBit( lengthIndex<16, codingState.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)] );
#endif
					codingState.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)] = Probability.ChangeProbability( codingState.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)], lengthIndex<16, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability );

					if ( lengthIndex < 16 ) {
#if DEBUG
						cost += codingState.HistoryLengthTreeMid[pos&3].CodeAndUpdateTreeProbabilities( RangeEncoder, lengthIndex-8, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:6) );
#else
						codingState.HistoryLengthTreeMid[pos&3].CodeAndUpdateTreeProbabilities( RangeEncoder, lengthIndex-8, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:6) );
#endif
					}
					else {
#if DEBUG
						cost += codingState.HistoryLengthTreeHigh.CodeAndUpdateTreeProbabilities( RangeEncoder, lengthIndex-16, SettingsFix.HistoryLengthTreeBits, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:256) );
#else
						codingState.HistoryLengthTreeHigh.CodeAndUpdateTreeProbabilities( RangeEncoder, lengthIndex-16, SettingsFix.HistoryLengthTreeBits, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:256) );
#endif
					}
				}
#if DEBUG
				return cost;
#endif
			}
			else { 
#if DEBUG
				return lengthCoder.Encode( lengthIndex, codingState.LengthHistoryProbabilities, (type==0 ? codingState.Rep0AllowedLengthIndexCount : codingState.LengthHistoryProbabilities.Length) );
#else
				lengthCoder.Encode( lengthIndex, codingState.LengthHistoryProbabilities, (type==0 ? codingState.Rep0AllowedLengthIndexCount : codingState.LengthHistoryProbabilities.Length) );
#endif
			}
		}


#if DEBUG
		private void WriteCompressionLog( int splitIndex )
		{
			debugInfo.Finish();
			debugInfo.Write( "comp.bin" );
		}


		private void CompLogAnalyze( DebugInfo debugInfo, DistLength[] distLength )
		{
			for ( int i=0 ; i<dataLength ; i += distLength[i].GetDelta(lengthInfo) ) {
				debugInfo.Items[i] = new DebugInfoItem( debugInfo );
				debugInfo.Items[i].MaxMatchLength = -1; 
				debugInfo.Items[i].MaxMatchDist   = -1;
				debugInfo.Items[i].MatchLength    = (distLength[i].IsLiteral) ? -1 : (distLength[i].GetDelta(lengthInfo));
				debugInfo.Items[i].MatchDist      = (distLength[i].IsLiteral) ? -1 : (distLength[i].Dist);
			}
		}
#endif
	}
}
