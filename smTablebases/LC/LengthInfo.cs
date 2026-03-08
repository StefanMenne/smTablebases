using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class LengthInfo
	{
		private readonly LengthSet     lengthSet;
		private MatchGenInfo           matchGenInfo;
		private readonly int[]         indexToLength;
		private int                    minMatchLength, maxMatchLength;


		public static LengthInfo[] Instances = new LengthInfo[]{
			new LengthInfo( LengthSet.Set0_Length_2_4_8_16_32_64_128_256,        MatchGenInfo.Length_2_4_8_16_32_64_128_256,          new int[] { 2, 4, 8, 16, 32, 64, 128, 256 }                                                  ),
			new LengthInfo( LengthSet.Set1_Length_2_3_4_8_16_32_64_128,          MatchGenInfo.Length_2_3_4_8_16_32_64_128,            new int[] { 2, 3, 4, 8, 16, 32, 64, 128 }                                                    ),
			new LengthInfo( LengthSet.Set2_Length_2_3_4_5_8_16_32_64,            MatchGenInfo.Length_2_3_4_5_8_16_32_64,              new int[] { 2, 3, 4, 5, 8, 16, 32, 64 }                                                      ),
			new LengthInfo( LengthSet.Set3_Length_2_3_4____273_noFSC,            MatchGenInfo.Length_2_4_8_16_32_64_128_256,          CreateLengthArray()                                                                          ),
			new LengthInfo( LengthSet.Set4_Length_2_3_4____273_RAM,              MatchGenInfo.Length_2_3_4____273_RAM,                CreateLengthArray()                                                                          ),
			new LengthInfo( LengthSet.Set5_Length_2_3__9_10_16_32_64_128_256,    MatchGenInfo.Length_2_3__9_10_16_32_64_128_256,      new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 16, 32, 64, 128, 256 }                                   ),
			new LengthInfo( LengthSet.Set6_Length_2_3__16_17_32_64_128_256,      MatchGenInfo.Length_2_3__16_17_32_64_128_256,        new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 ,11 ,12 ,13, 14, 15, 16, 17, 32, 64, 128, 256 }       )
		};


#if DEBUG
		static LengthInfo()
		{
		}
#endif

		private static int[] CreateLengthArray()
		{
			int[] array = new int[272];
			for ( int i=0 ; i<array.Length ; i++ )
				array[i] = 2+i;
			return array;
		}



		public LengthInfo( LengthSet lengthSet, MatchGenInfo matchGenInfo, int[] length )
		{
			this.lengthSet      = lengthSet;
			this.indexToLength  = length;
			this.minMatchLength = indexToLength[0];
			this.maxMatchLength = indexToLength[indexToLength.Length-1];
			this.matchGenInfo   = matchGenInfo;
		}


		public MatchGenInfo MatchGenInfo
		{
			get { return matchGenInfo; }
		}


		public LengthSet LengthSet
		{
			get { return lengthSet; }
		}


		public int LengthIndexCount
		{
			get { return indexToLength.Length; }
		}


		public int MinMatchLength
		{
			get { return minMatchLength; }
		}


		public int MaxMatchLength
		{
			get { return maxMatchLength; }
		}


		public int IndexToLength( int index )
		{
			return indexToLength[index];
		}


		public int[] GetLengthArray()
		{
			return indexToLength;
		}

	}
}
