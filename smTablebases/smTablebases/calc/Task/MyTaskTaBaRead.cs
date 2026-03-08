using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBacc;

namespace smTablebases
{
	public abstract class MyTaskTaBaRead : MyTaskWtm
	{
		protected TmpBlockStorage tmpBlockStorage = null;


		public MyTaskTaBaRead( CalcTB calc, bool wtm ) : base( calc, wtm )
		{
		}


		public TmpBlockStorage TmpBlockStorage
		{
			get { return tmpBlockStorage; }
			set { tmpBlockStorage = value; }
		}


		public abstract bool WtmTaBaRead
		{
			get;
		} 


		public abstract WkBk WkBkTaBaRead
		{
			get;
		} 


		public abstract Pieces PiecesTaBaRead
		{
			get;
		} 


		protected void LoadDataChunk( TaBasesRead taBasesRead, DataChunkRead dataChunk, int threadIndex )
		{
			taBasesRead.LoadDataChunk( taBasesRead.GetTaBa( dataChunk.Pieces ), dataChunk, threadIndex, tmpBlockStorage );
		}
	}
}
