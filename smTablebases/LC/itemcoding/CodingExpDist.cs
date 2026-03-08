﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CodingExpDist
	{
		// Exp Dist Coding:     for dist <= 27Bit   (virtual dists)
		//  for ExpDistTreeCodingBitCount=4 and ExpDistTreeCount=6
		//   Dist             Dist Bit Length       Slot      TreeOffset                  
		//   0-17             0-5                   0-17   
		//   18-31            5                     18,19         0
		//   32-63            6                     20,21         7
		//   64-127           7                     22,23        22
		//   128-255          8                     24,25        53  
		//   ...              ...                   ...          ...
		//                    23                    54,55        53
		//                    24                    56,57        53
		//                    25                    58,59        53
		//                    26                    60,61        53
		//                    27                    62,63        53
		//
		private int               dataLengthBits;
		private static int[]      treeOffsets         = new int[]{ -1, 6, 21, 52 };
#if DEBUG
		private int[]             countCodings        = new int[28];
		private double[]          sumCostsSlot        = new double[28];
		private double[]          sumCostsDirectBits  = new double[28];
		private double[]          sumCostsBits        = new double[28];
#endif



		public CodingExpDist( int dataLengthBits )
		{
			this.dataLengthBits             = dataLengthBits;
		}


		public static int ExpDistBitsTreeLength
		{
			get{ return 68; }
		}


		public static int ExpDistSlotBitCount
		{
			get{ return 6; }
		}


		public static int ExpDistTreeCodingBitCount
		{
			get{ return 4; }
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="dist">0,1,2,...</param>
		/// <param name="bitsToCode"></param>
		/// <returns></returns>
		private static int DistToSlot( int dist, out int bitsToCode )
		{
			if ( dist < 18 ) {
				bitsToCode = 0;
				return dist;
			}
			else { 
				int bitLength = Tools.ValueToBitCount( dist );
				bitsToCode = bitLength-2;
				//return 2*bitLength + 14 + ((dist>>(bitLength-2))&1);				
				return 2*bitLength + 8 + ((dist>>(bitLength-2))&1);				
			}
		}


		private static int SlotToDistOffset( int slot, out int bitsToCode )
		{
			if ( slot < 18 ) {
				bitsToCode = 0;
				return slot;
			}
			else { 
				bitsToCode = (slot-12)>>1;
				return (2+(slot&1))<<bitsToCode;
			}
		}


		public static int LengthIndexToContextIndex( int lengthIndex )
		{
			return Math.Min( lengthIndex, SettingsFix.ExpDistSlotContextCount-1 );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rangeEncoder"></param>
		/// <param name="slotOccurence"></param>
		/// <param name="dist">0,1,2,...</param>
		/// <param name="tree"></param>
#if DEBUG
		public double Encode( RangeEncoder rangeEncoder, ProbabilityTree treeSlot, int dist, ProbabilityTree tree, out string info )
#else
		public void Encode( RangeEncoder rangeEncoder, ProbabilityTree treeSlot, int dist, ProbabilityTree tree )
#endif
		{
			int    bitsToCode;
			int    slot         = DistToSlot( dist, out bitsToCode );

#if DEBUG
			int    distBitLength         = Tools.ValueToBitCount( dist );
			double costSlot              = treeSlot.CodeAndUpdateTreeProbabilities( rangeEncoder, slot, ExpDistSlotBitCount, 0.001, 0.999 );
			double costDirectBits = 0d, costBits = 0d;


			if ( bitsToCode != 0 ) {
				int valueToCode = dist&((1<<bitsToCode)-1);
				int treeOffset  = treeOffsets[ ( bitsToCode >= 6 ) ? 3 : (bitsToCode-3) ];
				if ( bitsToCode >= 6 ) {
					costDirectBits += rangeEncoder.AddBits( (uint)(valueToCode>>CodingExpDist.ExpDistTreeCodingBitCount), bitsToCode-CodingExpDist.ExpDistTreeCodingBitCount );
					bitsToCode = 4;
				}
				costBits += tree.ReverseCodeAndUpdateTreeProbabilities( rangeEncoder, valueToCode, bitsToCode, treeOffset, SettingsFix.ExpDistProbTreeMinProbability, 1d-SettingsFix.ExpDistProbTreeMinProbability );
			}

			countCodings[distBitLength]++;
			sumCostsSlot[distBitLength]       += costSlot;
			sumCostsDirectBits[distBitLength] += costDirectBits;
			sumCostsBits[distBitLength]       += costBits;
			info = "Dist[" + costSlot.ToString("#0.000") + "; " + costDirectBits.ToString("#0.000") + "; " + costBits.ToString("#0.000") + "]";
			return costSlot + costDirectBits + costBits;
#else
			treeSlot.CodeAndUpdateTreeProbabilities( rangeEncoder, slot, ExpDistSlotBitCount, 0.001, 0.999 );
			if ( bitsToCode != 0 ) {
				int valueToCode = dist&((1<<bitsToCode)-1);
				int treeOffset  = treeOffsets[ ( bitsToCode >= 6 ) ? 3 : (bitsToCode-3) ];
				if ( bitsToCode >= 6 ) {
					rangeEncoder.AddBits( (uint)(valueToCode>>CodingExpDist.ExpDistTreeCodingBitCount), bitsToCode-CodingExpDist.ExpDistTreeCodingBitCount );
					bitsToCode = 4;
				}
				tree.ReverseCodeAndUpdateTreeProbabilities( rangeEncoder, valueToCode, bitsToCode, treeOffset, SettingsFix.ExpDistProbTreeMinProbability, 1d-SettingsFix.ExpDistProbTreeMinProbability );
			}
#endif
		}

		
		public int Decode( RangeDecoder rangeDecoder, ProbabilityTree treeSlot, ProbabilityTree tree )
		{
			int bitsToCode;
		
			int slot = treeSlot.DecodeAndUpdateTreeProbabilities( rangeDecoder, ExpDistSlotBitCount, 0.001, 0.999 );
			int dist = SlotToDistOffset( slot, out bitsToCode );

			if ( bitsToCode != 0 ) {
				int valueToCode = dist&((1<<bitsToCode)-1);
				int treeOffset  = treeOffsets[ ( bitsToCode >= 6 ) ? 3 : (bitsToCode-3) ];
				if ( bitsToCode >= 6 ) {
					dist |= rangeDecoder.GetBits( bitsToCode-CodingExpDist.ExpDistTreeCodingBitCount ) << CodingExpDist.ExpDistTreeCodingBitCount ;
					bitsToCode = 4;
				}
				dist |= tree.ReverseDecodeAndUpdateTreeProbabilities( rangeDecoder, bitsToCode, treeOffset, SettingsFix.ExpDistProbTreeMinProbability, 1d-SettingsFix.ExpDistProbTreeMinProbability );
			}

			return dist;
		}


		public static ProbabilityTree AddCoding( int dist, int expDistContextIndex, Collection<ProbabilityTree> historyExpDistSlotIn, out Collection<ProbabilityTree> historyExpDistSlotOut, ProbabilityTree historyExpDistBits, bool disposeOldInstance )
		{
			int    bitsToCode;
			int    slot                  = DistToSlot( dist, out bitsToCode );
			int    distBitLength         = Tools.ValueToBitCount( dist );

			historyExpDistSlotOut = historyExpDistSlotIn.ChangeItem( expDistContextIndex, disposeOldInstance );
			historyExpDistSlotOut[expDistContextIndex].ChangeTreeProbabilities( slot, ExpDistSlotBitCount, SettingsFix.ExpDistProbTreeMinProbability, 1D-SettingsFix.ExpDistProbTreeMinProbability );
			int    valueToCode           = dist&((1<<bitsToCode)-1);


			if ( bitsToCode == 0 )
				return historyExpDistBits.Clone( disposeOldInstance );
			else { 
				int treeOffset  = treeOffsets[ ( bitsToCode >= 6 ) ? 3 : (bitsToCode-3) ];
				historyExpDistBits = historyExpDistBits.Clone( disposeOldInstance );
				historyExpDistBits.ChangeTreeProbabilitiesReverse( valueToCode, ((bitsToCode==5)?5:Math.Min( bitsToCode, CodingExpDist.ExpDistTreeCodingBitCount)), treeOffset, SettingsFix.ExpDistProbTreeMinProbability, 1d-SettingsFix.ExpDistProbTreeMinProbability );
				return historyExpDistBits;
			}
		}


		public double GetCodingCostsAsProbability( int distToCode, int lengthIndex, Collection<ProbabilityTree> historyExpDistSlot, ProbabilityTree historyExpDistBits )
		{
			int     bitsToCode;
			int     slot             = CodingExpDist.DistToSlot( distToCode, out bitsToCode );
			double  probability      = historyExpDistSlot[LengthIndexToContextIndex(lengthIndex)].GetCodingSizeAsProbability( slot, ExpDistSlotBitCount );
			int     valueToCode      = distToCode&((1<<bitsToCode)-1);

			if ( bitsToCode > 0 ) {
				int treeOffset  = treeOffsets[ ( bitsToCode >= 6 ) ? 3 : (bitsToCode-3) ];
				if ( bitsToCode >= 6 ) {
					probability *=  CodingCosts.ProbabilityFromBitCount( bitsToCode-CodingExpDist.ExpDistTreeCodingBitCount );
					bitsToCode = 4;
				}
				probability *= historyExpDistBits.GetReverseCodingSizeAsProbability( valueToCode, bitsToCode, treeOffset );
			}
			return probability;
		}



#if DEBUG
		public string GetDetailledOutput()
		{
			string s = "Bits      Count         PosSlot           Direct        Data            Sum         AvgSum\r\n";
			int countCodingsSum = 0;
			double sum1=0d, sum2=0d, sum3=0d;
			for ( int distBitLength=0 ; distBitLength<24 ; distBitLength++ ) {
				if ( sumCostsSlot[distBitLength] != 0d )
					s += distBitLength.ToString().PadLeft(2) + countCodings[distBitLength].ToString( "#,###,##0" ).PadLeft(13) + (sumCostsSlot[distBitLength]/8d).ToString( "#,###,###,##0.000" ).PadLeft(16) + (sumCostsDirectBits[distBitLength]/8d).ToString( "#,###,###,##0.000" ).PadLeft(15) + (sumCostsBits[distBitLength]/8d).ToString( "#,###,###,##0.000" ).PadLeft(15) + ((sumCostsSlot[distBitLength]+sumCostsDirectBits[distBitLength]+sumCostsBits[distBitLength])/8d).ToString( "#,###,###,##0.000" ).PadLeft(15) + ((sumCostsSlot[distBitLength]+sumCostsDirectBits[distBitLength]+sumCostsBits[distBitLength])/((double)countCodings[distBitLength])).ToString( "#0.000" ).PadLeft(15) + "   = " + ((sumCostsSlot[distBitLength])/((double)countCodings[distBitLength])).ToString( "#0.000" ).PadLeft(6) + " + " + ((sumCostsDirectBits[distBitLength])/((double)countCodings[distBitLength])).ToString( "#0.000" ).PadLeft(6) + " + " + ((sumCostsBits[distBitLength])/((double)countCodings[distBitLength])).ToString( "#0.000" ).PadLeft(6) + "\r\n";
				countCodingsSum += countCodings[distBitLength];
				sum1 += sumCostsSlot[distBitLength];
				sum2 += sumCostsDirectBits[distBitLength];
				sum3 += sumCostsBits[distBitLength];
			}		
			s += "S " + countCodingsSum.ToString( "#,###,##0" ).PadLeft(13) + (sum1/8d).ToString( "#,###,###,##0.000" ).PadLeft(16) + (sum2/8d).ToString( "#,###,###,##0.000" ).PadLeft(15) + (sum3/8d).ToString( "#,###,###,##0.000" ).PadLeft(15) + ((sum1+sum2+sum3)/8d).ToString( "#,###,###,##0.000" ).PadLeft(15) + ((sum1+sum2+sum3)/countCodingsSum).ToString( "#0.000" ).PadLeft(15) + "   = " + (sum1/countCodingsSum).ToString( "#0.000" ).PadLeft(6) + " + " + (sum2/countCodingsSum).ToString( "#0.000" ).PadLeft(6) + " + " + (sum3/countCodingsSum).ToString( "#0.000" ).PadLeft(6) + "\r\n";
			
			return s;	
		}
#endif

	}
}
