﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class SettingsFix
	{
		// General
		public    static int                   MaxBlockSizeBits                 = 24; // Max File size
		public    static int                   MaxVirtualDistBits               = MaxBlockSizeBits + 3;
		public    static int                   MaxBlockSize                         { get{ return 1<<MaxBlockSizeBits;} }


		// IsLiteral
		public    const int                    IsLiteralPosBits                 = 2;     // 0 allowed
		public    const int                    IsLiteralLengthIndices           = 3;     // 0 not allowed
		public    const double                 IsLiteralMinProbability           = 0.01;


		// Literal
		public    const double                 LiteralTreeMinProbability        = 0.01;
		public    const double                 LiteralTreeMaxProbability        = 1d-LiteralTreeMinProbability;


		// CodingItemType
		public    const int                    CodingItemTypePosBits                 = 0;    // 0,1,2,...        
		public    const int                    RepeatCount                            = 4;
		public    const int                    HistoryTypeCount                       = RepeatCount + 2;  // one for "History"; one for "Exp Dist"; CodingItemType-values will be stored
		public    const double                 HistoryTypeDecreaseProbabilityFactor   = 31D/32D;
		public    const double                 HistoryTypeMinProbability              = 0.01D;


		// Length
		public    const double                 HistoryLengthMinProbability           = 0.01;
		public    const int                    HistoryLengthTreeBits                 = 8;
		public    const int                    HistoryLengthIsLowIsMidContextCount   = 4;


		// Dist
		public    const int                    HistoryDistWindowSize                  = 1024; 
		public    static readonly InitValues   HistoryDistInitValues                  = new InitValues( new int[]{ 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7, 1, 8, 1, 9, 1, 10, 1, 11, 1, 12, 1, 13, 1, 14, 1, 15, 1, 16, 1 } );
		public    const double                 ExpDistProbTreeMinProbability          = 0.001;
		public    const int                    ExpDistBitLengthWindowSize             = 256;
		public    const int                    ExpDistSlotContextCount                = 4;
	

		static SettingsFix()
		{
			if ( HistoryDistWindowSize<8 )
				throw new Exception();

		}

	}
}
