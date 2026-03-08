using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace smTablebases
{
	public static class Progress
	{
		public static bool      IsIndeterminate  = false;
		public static long      Value            = -1L;
		public static long      Max              = -1L;

		public static void Reset()
		{
			Value = Max = -1L;
			IsIndeterminate = false;
		}


	}
}
