
namespace TBacc
{
	public static class Config
	{
		public  const  int            OptimizeStepInterval                  = 10;
		public  const  int            MaxMen                                = 7;
		public  const  int            MaxNonKMen                            = MaxMen - 2;
		public  const  int            MaxDtm                                = 1097;               // KQPKRBN  WTM wi549
		public  const  bool           ForceNonFastCalculation               = false;

		// for faster access and to access in TBacc define again outside Settings
		public static bool DebugGeneral = false;
		public  static bool           VerifyDoubleBlockLoad, SaveDataChunksAtMd5;
		public  static int            ReadBufferSize                        = 4096;


		public  static int            BlockSize               = 16*1024*1024;   // 16 MB = maximum for LC
		public  const  int            FactorIpSizeDividedBy8  = 16;             // Default: 16    Max Block size incl. recalc positions 
		public  const  int            FactorVirtualPos        = 8;              // Default:  8   
		
		

	}
}
