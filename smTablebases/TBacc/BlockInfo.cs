using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public class BlockInfo
	{ 
		// CountBytesVirtual >= CountBytes >= CountBytesWithoutRecalculateable
		public int              CountBytesVirtual;                   // only for information; size using IndexPos64
		public int              CountBytes;                          // only for information; size using IndexPos; later more bytes will be set to virtual
		public int              CountBytesWithoutRecalculateable;    // only for information; all bytes that will not be set to virtual
	
		public int              BoundReason;           // Reason why next chunk could not be included in this block
		                                               // 0 : CountBytesWithoutRecalculateable
													   // 1 : CountBytes
													   // 2 : CountBytesVirtual
													   // 3 : last
	}
}
