﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LC
{
	public class Decoder : Coder
	{
#if DEBUG
		public byte[] DataVerify = null;
#endif


		public Decoder() : base()
		{
		}


		public int Decode( byte[] dataIn, byte[] dataOut, int[] posToVirtualPos=null )
		{
			RangeDecoder               rangeDecoder            = new RangeDecoder( dataIn );
			settings = Settings.FromBits( rangeDecoder.GetBits( Settings.BitCount ) );
			this.lengthInfo     = LengthInfo.Instances[(int)settings.LengthSet];
			dataLengthBits = (int)rangeDecoder.GetBits(5);
			dataLength     = (int)rangeDecoder.GetBits(dataLengthBits);
			
			// if (posToVirtualPos == null)  {
			// 	posToVirtualPos = new int[dataLength];
			// 	for (int j = 0; j < posToVirtualPos.Length; j++)
			// 		posToVirtualPos[j] = j;
			// }
			
			Init( rangeDecoder, dataOut, posToVirtualPos, lengthInfo );

#if DEBUG
			Log.Line( "Decode" );
#endif

			int i=0;
			int prevMatchVirtualDist = 0;

			while ( i<dataLength ) {
				DistLength distLength = DecodeItem( i, lengthInfo.MinMatchLength, ref prevMatchVirtualDist );
				if ( !distLength.IsLiteral ) {
					int source         = settings.BytesPerItem*(i-distLength.Dist);
					int length         = distLength.GetDelta(lengthInfo) * settings.BytesPerItem;
					int dest           = settings.BytesPerItem * i;
					while ( length-- != 0 )
						data[dest++] = data[source++];
#if DEBUG
					if (DataVerify != null) { 
						length         = distLength.GetDelta(lengthInfo) * settings.BytesPerItem;
						dest           = settings.BytesPerItem * i;
						while ( length-- != 0 ) {
							if ( data[dest] != DataVerify[dest] )
								throw new Exception();
						}
					}
#endif
				}
				int delta = distLength.GetDelta(lengthInfo);
				i += delta;
			}

			int additionalBytes = 0;                // read additional bytes if length%bytesPerItem != 0
			while ( rangeDecoder.GetBit() )
				data[dataLength*settings.BytesPerItem+additionalBytes++] = (byte)rangeDecoder.GetBits( 8 );
#if DEBUG
			DataVerify = null;
#endif
			return dataLength * settings.BytesPerItem + additionalBytes;
		}


		public DistLength DecodeItem( int dataPos, int minMatchLength, ref int prevMatchVirtualDist )
		{
			DistLength distLength;
			int        distVirtual = -1, historyIndex = -1, lengthIndex = -1, dist = -1, type;
			
			if ( RangeDecoder.GetBit( codingState.HistoryIsLit.Probabilities[codingState.GetIsLiteralStateIndex(dataPos)] ) ) {   // Literal
				type = CodingItemType.Literal;
				int prevLit = -1;
				if ( prevMatchVirtualDist != 0 ) {
					int prevMatchDist = (distConverter==null) ? prevMatchVirtualDist : distConverter.VirtualDistToDist(dataPos,prevMatchVirtualDist);
					if ( prevMatchDist > 0 )
						prevLit = literal.GetFromData(dataPos-prevMatchDist);
				} 
				int ctxIndex = literal.GetContextIndex(dataPos);
				int l = literalCoder.Decode( prevLit, codingState.LiteralTrees[ctxIndex].Probabilities );
				literal.WriteToData( dataPos, l );
				distLength   = DistLength.Literal;
				prevMatchVirtualDist = 0;
				codingState.AddCodingItemLiteral();
			}
			else {
				double probIsRep0;
				double[] probabilities = codingState.GetDistHistoryProbabilities( out probIsRep0 );
				bool isRep0  = RangeDecoder.GetBit( probIsRep0 );
				bool isRep0S = false;
		
				if ( isRep0 ) {
					historyIndex = codingState.CountHistoryDists;
					isRep0S = RangeDecoder.GetBit( codingState.ProbabilityIsRep0S );
				}
				else { 
					historyIndex = historyDistCoder.DecodeProbabilitySum1( probabilities, codingState.CountHistoryDists+SettingsFix.RepeatCount );
				}


				bool isExpDist = historyIndex == codingState.CountHistoryDists + SettingsFix.RepeatCount;
				if ( isExpDist ) {   // ExpDist
					type         = CodingItemType.ExpDist;
					lengthIndex  = DecodeLength( type, dataPos );
					distVirtual  = codingExpDist.Decode( RangeDecoder, codingState.HistoryExpDistSlot[CodingExpDist.LengthIndexToContextIndex(lengthIndex)], codingState.HistoryExpDistBits ) + 1;
					dist         = (distConverter==null) ? distVirtual : distConverter.VirtualDistToDist( dataPos, distVirtual );
				}
				else if ( historyIndex >= codingState.CountHistoryDists ) {    // repeat
					type         = historyIndex - codingState.CountHistoryDists;
					distVirtual  = codingState.HistoryLatest.GetVal( type );
					dist         = (distConverter==null) ? distVirtual : distConverter.VirtualDistToDist(dataPos,distVirtual);
					if ( isRep0S )
						type = CodingItemType.Rep0S;
					else { 
						lengthIndex = DecodeLength( type, dataPos );
					}
				}
				else {    // His
					type         = CodingItemType.Hist;
					distVirtual  = codingState.GetHistoryDistance( historyIndex );
					dist         = (distConverter==null) ? distVirtual : distConverter.VirtualDistToDist(dataPos,distVirtual);
					lengthIndex  = DecodeLength( type, dataPos );
				}


				prevMatchVirtualDist = distVirtual;
				if ( isRep0S ) {
					codingState.AddCodingItemRep0S( distVirtual, historyIndex );
					distLength = DistLength.Rep0SWithDist( dist );
				}
				else {
					codingState.AddCodingItemMatch( type, distVirtual, lengthIndex , lengthInfo.IndexToLength(lengthIndex), historyIndex );
					distLength = new DistLength( dist, lengthIndex, type );
				}
			}

#if DEBUG
			if ( Verify && verifyQueue.Count!=0  ) {
				int distNonVirtual = (distConverter==null||dist==-1) ? dist : distConverter.VirtualDistToDist(dataPos,dist);
				int             typeVerify            = (int)verifyQueue.Dequeue();
				int             historyIndexVerify    = (int)verifyQueue.Dequeue();
				int             distVirtualVerify     = (int)verifyQueue.Dequeue();
				int             distVerify            = (int)verifyQueue.Dequeue();
				int             lengthIndexVerify     = (int)verifyQueue.Dequeue();
				if ( type!=typeVerify || historyIndex!=historyIndexVerify || distVirtual!=distVirtualVerify || dist != distVerify || lengthIndex != lengthIndexVerify )
					throw new Exception( "Error at pos=" + dataPos );

				UInt64          lowVerify             = (UInt64)verifyQueue.Dequeue();
				UInt64          rangeVerify           = (UInt64)verifyQueue.Dequeue();
				int             bufferPosVerify       = (int)verifyQueue.Dequeue();
				UInt64 low       = RangeDecoder.Low;
				UInt64 range     = RangeDecoder.Range;
				int    bufferPos = RangeDecoder.BufferPosVerify;

				if ( low!=lowVerify || range!=rangeVerify || bufferPos!=bufferPosVerify )
					throw new Exception( "RangeCoder Mismatch at pos=" + dataPos );
			}
#endif

			return distLength;
		}




		private int DecodeLength( int type, int pos )
		{
			bool isRep = CodingItemType.IsRep(type);
			if ( codingState.HistoryLength == null ) {
				bool isLow = RangeDecoder.GetBit(codingState.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount]);
				codingState.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount] = Probability.ChangeProbability( codingState.ProbabilityIsLowIsMidLength[pos%SettingsFix.HistoryLengthIsLowIsMidContextCount], isLow, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability );
				if ( isLow ) { 
					return codingState.HistoryLengthTreeLow[pos&3].DecodeAndUpdateTreeProbabilities( RangeDecoder, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:6) );
				}
				else { 
					bool isMid = RangeDecoder.GetBit(codingState.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)]);
					codingState.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)] = Probability.ChangeProbability( codingState.ProbabilityIsLowIsMidLength[SettingsFix.HistoryLengthIsLowIsMidContextCount+(pos%SettingsFix.HistoryLengthIsLowIsMidContextCount)], isMid, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability );
					if ( isMid ) 
						return 8+codingState.HistoryLengthTreeMid[pos&3].DecodeAndUpdateTreeProbabilities( RangeDecoder, 3, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:6) );
					else 
						return 16+codingState.HistoryLengthTreeHigh.DecodeAndUpdateTreeProbabilities( RangeDecoder, SettingsFix.HistoryLengthTreeBits, SettingsFix.HistoryLengthMinProbability, 1D-SettingsFix.HistoryLengthMinProbability, (isRep?-1:256) );
				}
			}
			else { 
				return lengthCoder.Decode( codingState.LengthHistoryProbabilities, ((type==0)?codingState.Rep0AllowedLengthIndexCount : codingState.LengthHistoryProbabilities.Length) );
			}
		}

	}
}
