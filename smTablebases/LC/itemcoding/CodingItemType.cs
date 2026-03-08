using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public static class CodingItemType
	{
		public const int     Rep0    = 0;
		public const int     Rep1    = 1;
		public const int     Rep2    = 2;
		public const int     Rep3    = 3;
		public const int     Hist    = SettingsFix.RepeatCount;
		public const int     ExpDist = SettingsFix.RepeatCount+1;
		public const int     Literal = SettingsFix.RepeatCount+2;
		public const int     Rep0S   = SettingsFix.RepeatCount+3;

		public const int     Count   = SettingsFix.RepeatCount+4;




		public static bool IsRep( int type )
		{
			return type < SettingsFix.RepeatCount;
		}
	}
}
