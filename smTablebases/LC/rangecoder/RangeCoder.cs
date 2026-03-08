using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class RangeCoder
	{
		protected const UInt64   rangeShiftTreshold = 0x0000010000000000UL; 
		protected const UInt64   lowShiftTreshold   = 0x0000FF0000000000UL;             
		protected const UInt64   lowCarryTreshold   = 0x0001000000000000UL;
		protected const UInt64   lowMask            = 0x000000FFFFFFFFFFUL;

		protected byte[]         buffer;
		protected int            bufferPos          = 0;

		protected UInt64         range              = 0x0000FFFFFFFFFFFFUL;
	
		
		protected RangeCoder( byte[] buffer )
		{
			this.buffer = buffer;
		}

#if DEBUG
		public UInt64 Range
		{
			get { return range; }
		}


		public int BufferPos
		{
			get { return bufferPos; }
		}
#endif


	}
}
