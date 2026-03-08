using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smTablebases
{
	public abstract class MyTaskWtm : MyTask
	{
		protected   bool                           wtm;


		public MyTaskWtm( CalcTB calcTb, bool wtm ) : base( calcTb )
		{
			this.wtm   = wtm;
		}
	


		public bool Wtm
		{
			get{ return wtm; }
		}

	}
}
