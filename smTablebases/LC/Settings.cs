using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public enum LengthSet
	{
		Set0_Length_2_4_8_16_32_64_128_256,
		Set1_Length_2_3_4_8_16_32_64_128,
		Set2_Length_2_3_4_5_8_16_32_64,
		Set3_Length_2_3_4____273_noFSC,
		Set4_Length_2_3_4____273_RAM,
		Set5_Length_2_3__9_10_16_32_64_128_256,
		Set6_Length_2_3__16_17_32_64_128_256,
	}


	public class Settings
	{
		public static Settings Default = new Settings();


		// if 2: literals are 2 byte long; all distances and length are coded in 2 bytes steps
		// 16 bit data you should code with BytesPerItem=2 and LiteralPosBits=0 (or maybe try BytesPerItem=1 and LiteralPosBits=1)
		public    int                    BytesPerItem                     = 1;  // 1,2
		

		// LiteralPosBits specifies the amount of bits of independent coded literals
		// if e.g. LiteralPosBits>=1: literals on even byte positions are coded independent of 'odd bytes'
		// if bytesPerItem=2 only LiteralPosBits=0 is allowed
		public    int                    LiteralPosBits                   = 0;  // 0, 1 or 2


		// PrevByteHighBits specifies the amount of bits of independent coded literals
		// if e.g. PrevByteHighBits=2, then the two highest bits from the previous coded byte are used to choose
		// an individual Probability Tree for coding this literal
		public    int                    PrevByteHighBits                = 3;  // 0...8


		public    LengthSet              LengthSet                        = LengthSet.Set0_Length_2_4_8_16_32_64_128_256;
		

		// 0 is slowest and highest compression rate
		public    int                    Level                            = 2;  


		public Settings()
		{
		}


		public static int GetMaxMatchLength( int minMatchLength )
		{
			return minMatchLength<<7;
		}


		public static Settings FromBits( int bits )
		{
			return new Settings(){ BytesPerItem=(bits&255)+1, LiteralPosBits=((bits>>8)&7), LengthSet=(LengthSet)((bits>>11)&15), PrevByteHighBits=((bits>>15)&15), Level=((bits>>19)&7) };
		}


		public int AsBits
		{
			get { return (BytesPerItem-1) | (LiteralPosBits<<8) | (((int)LengthSet)<<11) | (PrevByteHighBits<<15) | (Level<<19); }
		}


		public static int BitCount
		{
			get { return 22; }
		}

	}
}
