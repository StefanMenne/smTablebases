using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public static class MirrorNormalize
	{
		public static MirrorType WkBkToMirror( Field wk, Field bk, Pieces p )
		{
			return WkBkToMirror( wk, bk, p.ContainsPawn );
		}

		public static MirrorType WkBkToMirror( Field wk, Field bk, bool pawn )
		{
			MirrorType m = MirrorType.None;
			Fields f = new Fields( wk, bk );
            
			if ( wk.X >= 4 ) {
				m |= MirrorType.MirrorOnVertical;
				f = f.MirrorOnVertical();
			}
			if ( !pawn ) {
				if ( wk.Y >= 4 ) {
					m |= MirrorType.MirrorOnHorizontal;
					f = f.MirrorOnHorizontal();
				}
				Field wkNew = f.Get(0);
				Field bkNew = f.Get(1);
				if ( (wkNew.Y > wkNew.X) || ( wkNew.Y==wkNew.X && bkNew.Y>bkNew.X ) ) 
					m |= MirrorType.MirrorOnDiagonal;
			}
			return m;
		}




    }
}
