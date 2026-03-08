﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
    // MatchSearch works only on NON virtual dists. 
    // CodingState/History works only on VIRTUAL dists.
    // All positions and lengths are NON virtual. 
    //
    // ExpDist could also be coded non virtual but this does not seem to lead to better compression ratio.
    //
    // Sequence         dist  ->   virt dist    ->       dist          is unique
    // Sequence    virt dist  ->        dist    ->  virt dist          is not unique   
	//
    public class DistConverter
	{
		private int[] posToVirtualPos;
		private int[] virtualPosToPos;
		//private int[] virtualPosToPosOld;


		public DistConverter( int[] posToVirtualPos, int count )
		{
			this.posToVirtualPos = posToVirtualPos;
			virtualPosToPos = new int[(posToVirtualPos[count-1]+7)>>3];
			
			int bits = 0;
			int virtualPosBound = 7;
			for ( int pos=0 ; pos<count ; pos++ ) {
				int posV = posToVirtualPos[pos];
				while ( posV >= virtualPosBound ) {
					virtualPosToPos[virtualPosBound>>3] = pos | (bits<<24);
					bits = 0;
					virtualPosBound += 8;
				}
				if ( (posV&7) != 7 )
					bits |= 1<<(posV&7);
			}
			if ( (posToVirtualPos[count-1]&7) != 7  )
				virtualPosToPos[virtualPosToPos.Length-1] = count | (bits<<24);



			//virtualPosToPosOld = new int[posToVirtualPos[count-1]+1];
			//int j=0;
			//for ( int i=0 ; i<virtualPosToPosOld.Length ; i++ ) {
			//	virtualPosToPosOld[i] = j;
			//	if ( posToVirtualPos[j] == i )
			//		j++;
			//}				




			// verify
			//int virtualCount = posToVirtualPos[count-1]+1;
			//for ( int i=0 ; i<virtualCount ; i++ ) {
			//	int p    = virtualPosToPos[i>>3];
			//	int b    = p>>(24+(i&7));
			//	p = (p & 0xffffff) - Tools.CountBits127( b );
			//	int pVer = virtualPosToPosOld[i];
			//	if ( p != pVer )
			//		throw new Exception();
			//}

		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos">Non virtual pos</param>
		/// <param name="distToLeft">Virtual distance</param>
		/// <returns></returns>
		public int DistToVirtualDist( int pos, int distToLeft )
		{
			while ( pos <0 || pos-distToLeft <0 ||  pos >= posToVirtualPos.Length || pos - distToLeft >= posToVirtualPos.Length)
				break;
			return posToVirtualPos[pos] - posToVirtualPos[pos-distToLeft];
		}


		public int PosToVirtualPos( int pos )
		{
			return posToVirtualPos[pos];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos">Non virtual pos</param>
		/// <param name="virtualDistToLeft">Virtual distance</param>
		/// <returns></returns>
		public int VirtualDistToDist( int pos, int virtualDistToLeft )
		{
			int virtualPos = posToVirtualPos[pos]-virtualDistToLeft;
			if ( virtualPos < 0 )
				return 0;

			//return pos - virtualPosToPosOld[virtualPos];

			int p    = virtualPosToPos[virtualPos>>3];
			int b    = p>>(24+(virtualPos&7));
			int vPos = (p & 0xffffff) - BitOperations.PopCount((uint)b);

			//if ( virtualPosToPosOld[virtualPos] != vPos )
			//	throw new Exception();

			return pos-vPos;
		}
	}
}
