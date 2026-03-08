using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public class DataChunkIndex
	{
		public static int Get( int wkBkIndex, int wkBkCount, bool wtm )
		{
			return wtm ? wkBkIndex : (wkBkIndex + wkBkCount); 
		} 


		public static int Get( WkBk wkBk, bool wtm )
		{
			return wtm ? wkBk.Index : (wkBk.Index + wkBk.Count.Index); 
		} 


		public static int GetCount( Pieces p )
		{
			return 2 * WkBk.GetCount( p ).Index; 
		}


		public static int GetCount( bool containsPawn )
		{
			return 2 * WkBk.GetCount( containsPawn ).Index; 
		}


		public static int GetHalfCount( Pieces p )
		{
			return WkBk.GetCount( p ).Index; 
		}


		public static int GetHalfCount( bool containsPawn )
		{
			return WkBk.GetCount( containsPawn ).Index; 
		}


		public static WkBk ToWkBk( int index, Pieces pieces )
		{
			return new WkBk( index%WkBk.GetCount(pieces).Index, pieces );
		}


		public static bool ToWtm( int index, Pieces pieces )
		{
			return index < WkBk.GetCount(pieces).Index;
		}


		public static bool ToWtm( int index, int wkBkCount )
		{
			return index < wkBkCount;
		}


		public static int IndexToFirstIndex( int index, int chunksPerBlock )
		{
			return index - (index%chunksPerBlock); 
		}
	}
}
