using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class MatchGen
	{
		private DistGen  distGen;
		private byte[]   data;
		private int      dataLength;
		private int      dataIndex            = 0;
		private int[][]  dist                 = new int[512][];
		private int[]    maxLengthIndexRight  = new int[512];
		private int[]    maxLengthIndexLeft   = new int[512];


		public MatchGen( byte[] data, int dataLength, int[] length )
		{
			this.data       = data;
			this.dataLength = dataLength;
			distGen         = new DistGen( dataLength, length );
			for ( int i=0 ; i<dist.Length ; i++ )
				dist[i] = new int[distGen.LengthArray.Length];

			for ( int i=0 ; i<256 ; i++ )
				FillData( i );
		}


		public void Shift()
		{
			FillData( (dataIndex++)+256 );
		}


		public byte[] Data
		{
			get { return data; }
		}


		public int DataIndex
		{
			get { return dataIndex; }
		}


		public DistGen DistGen
		{
			get { return distGen; }
		}


		public int MaxMatchIndexLeft
		{
			get { return maxLengthIndexLeft[(dataIndex-1)&511]; }
		}


		public int MaxMatchIndexRight
		{
			get { return maxLengthIndexRight[(dataIndex-1)&511]; }
		}


		public int GetShortestDistLeft( int lengthIndex )
		{ 
			return dist[(dataIndex-1)&511][lengthIndex];
		}


		public int[] GetShortestDistLeft()
		{ 
			return dist[(dataIndex-1)&511];
		}



		public int GetShortestDistRight( int lengthIndex )
		{ 
			int length = distGen.LengthArray[lengthIndex];
			return dist[(dataIndex+length-1)&511][lengthIndex];
		}


		private void FillData( int dataIndex )
		{
			if ( dataIndex < dataLength ) {
				distGen.AddChar( data[dataIndex] );
				maxLengthIndexLeft[dataIndex&511]  = distGen.MaxMatchLengthIndex;
				maxLengthIndexRight[dataIndex&511] = -1;
				for ( int i=0 ; i<=distGen.MaxMatchLengthIndex ; i++ ) {
					int lengthCurrent = distGen.LengthArray[i];
					maxLengthIndexRight[(dataIndex-lengthCurrent)&511]++;
					dist[dataIndex&511][i] = distGen.ShortestDist[i];
				}
			}
		}


		public override string ToString()
		{
			string s = "Left: ";

			for ( int i=0 ; i<=MaxMatchIndexLeft ; i++ )
				s += "  " + distGen.LengthArray[i].ToString() + ":" + GetShortestDistLeft(i).ToString();
			s += "     Right: ";
			for ( int i=0 ; i<=MaxMatchIndexRight ; i++ )
				s += "  " + distGen.LengthArray[i].ToString() + ":" + GetShortestDistRight(i).ToString();
			return s;
		}
	}
}
