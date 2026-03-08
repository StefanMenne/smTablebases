using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TBacc
{
	public static class Permutation
	{
		public static int[][] GetAll( int n )
		{
			List<int[]> list = new List<int[]>();

			int[] permut = new int[n];
			for ( int i=0 ; i<permut.Length ; i++ )
				permut[i] = i;

			GetAllRec( list, permut, n );

			return list.ToArray();
		}


		private static void GetAllRec( List<int[]> list, int[] perm, int n )
		{
			if ( n==1 )
				list.Add( (int[])perm.Clone() );
			else {
				for ( int i=0 ; i<n-1 ; i++ ) {
					GetAllRec( list, perm, n-1 );
					int swapIndex = (n%2==0) ? i : 0;
					int tmp =  perm[swapIndex];
					perm[swapIndex] = perm[n-1];
					perm[n-1] = tmp;
				}
				GetAllRec( list, perm, n-1 );	 
			}
		}
	}
}
