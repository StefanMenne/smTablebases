using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public enum PieceGroupReorderType
	{
		NoReordering,               // no reordering at all; only used for md5; incompatible with EP handling which assumes that pawn piece groups are with highest weight
		PawnHeighestWeight,          // Default for calculation
		Random,                     // Random for testing purpose
		CompressionOptimized        // Compression optimized
	}



	/// <summary>
	///                                                              43210
	/// OrigIndex:     Index inside the original order e.g.        KkprPRQ
	/// WeightIndex:   Index inside the weight sorted order e.g.   KkpPrRQ
	///                                                      -- lower Weight -->
	/// </summary>
	public class PieceGroupReorder
	{
		public const int ReorderType   = 0;   // stored in TB; for future use

		private int[] origIndexToWeightIndex;
		private int[] weightIndexToOrigIndex;


		private static readonly int[][] pieceTypeToLowWeightPriority = new int[][]{
			//  Results for Compression optimization:
			//  Only 4,5 men tablebases were checked. It was only checked which PieceGroup should be the one with lowest
			//  weight.  
			//
			//  e.G. p>R means that R should be used as piecegroup with lowest weight => KkpR  (upper letters is w)
			//
			//  Some comparisons are very unique but some very vague. The vagues are written by >=
			//
			//  q > B >= r >= b > n > Q >= p >= R > P >= N    
			//
			//  The PieceGroups with more than two pieces are more volatile. And are added to the above equations.
			//  PieceGroups with >=3 pieces were not checked and assumed to be similar to 2 pieces.
			//
			//  qq > rr > bb > q > QQ > B > r > b > BB > n > NN > Q > RR > p > R > P > nn > PP > N > pp
			//  19   18   17  16   15  14  13  12   11  10    9   8    7   6   5   4    3    2   1    0
			//
			//           one piece                                           two ore more pieces
			//           WK  WQ  WR  WB  WN  WP  BK  BQ  BR  BB  BN  BP     WK  WQ  WR  WB  WN  WP  BK  BQ  BR  BB  BN  BP
			new int[] {  -1,  0,  1,  2,  3,  4, -1,  5,  6,  7,  8,  9,    -1,  0,  1,  2,  3,  4, -1,  5,  6,  7,  8,  9  },    // No reordering 
			new int[] {  -1,  0,  1,  2,  3,  8, -1,  4,  5,  6,  7,  9,    -1,  0,  1,  2,  3,  8, -1,  4,  5,  6,  7,  9  },    // Pawn Highest Weight 
			new int[] {  -1,  8,  5, 14,  1,  4, -1, 16, 13, 12, 10,  6,    -1, 15,  7, 11,  9,  2, -1, 19, 18, 17,  3,  0  },    // Optimized for Compression WTM (BTM via switch sides)
			new int[] {  99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,    99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99 }     // All random 
		};


		private PieceGroupReorder( int[] origIndexToWeightIndex )
		{
			this.origIndexToWeightIndex = origIndexToWeightIndex;
			weightIndexToOrigIndex = new int[origIndexToWeightIndex.Length];
			for ( int i=0 ; i<weightIndexToOrigIndex.Length ; i++ )
				weightIndexToOrigIndex[origIndexToWeightIndex[i]] = i;
		}


		public static string GetString( Pieces pieces, bool wtm, PieceGroupReorderType type )
		{
			PieceGroupReorder reordering = Get( pieces, wtm, type );
			return reordering.GetString( pieces );
		}


		public static string[] GetAllStrings( Pieces pieces )
		{
			PieceGroupInfo pgi = pieces.GetPieceGroupInfo();
			int[][] perm = Permutation.GetAll( pgi.Count );
			string[] s = new string[perm.Length];
			for ( int i=0 ; i<s.Length ; i++ )
				s[i] = new PieceGroupReorder( perm[i] ).GetString(pieces);
			return s;
		}


		public string GetString( Pieces pieces )
		{
			PieceGroupInfo pgi = pieces.GetPieceGroupInfo();
			int pieceGroupCountW = pgi.CountW;

			string s = "Kk";
			for ( int i=pgi.Count-1 ; i>=0 ; i-- ) {
				int index = weightIndexToOrigIndex[i];
				s += (index<pieceGroupCountW) ? (new string( pgi.GetPiece(index).AsCharacter, pgi.GetPieceCount(index) ).ToUpper()) : (new string( pgi.GetPiece(index).AsCharacter, pgi.GetPieceCount(index) ).ToLower());
			}

			return s;
		}
		
		public static PieceGroupReorder GetFromInt( Pieces pieces, int value )
		{
			PieceGroupInfo pgi = pieces.GetPieceGroupInfo();
			int pieceGroupCountW = pgi.CountW;

			int[] origIndexToWeightIndex = new int[pgi.Count];
			for ( int i=0 ; i<origIndexToWeightIndex.Length ; i++ ) {
				origIndexToWeightIndex[i] = value & 7;
				value >>= 3;
			}
			return new PieceGroupReorder( origIndexToWeightIndex );
		}

		
		public static PieceGroupReorder Get( Pieces pieces, bool wtm, PieceGroupReorderType type )
		{
			PieceGroupInfo pgi = pieces.GetPieceGroupInfo();
			int pieceGroupCountW = pgi.CountW;
			int[] reorder = new int[pgi.Count];

			int[] weightPriority;
			bool switchSides = false;
			if ( type == PieceGroupReorderType.NoReordering )
				weightPriority = pieceTypeToLowWeightPriority[0];
			else if ( type == PieceGroupReorderType.PawnHeighestWeight )
				weightPriority = pieceTypeToLowWeightPriority[pieces.ContainsWpawnAndBpawn?1:0];
			else if ( type == PieceGroupReorderType.CompressionOptimized ) {
				weightPriority = pieceTypeToLowWeightPriority[2];
				switchSides = !wtm;
			}
			else if ( type == PieceGroupReorderType.Random ) {
				weightPriority = (int[])pieceTypeToLowWeightPriority[3].Clone();
				Random rnd = new Random( wtm ? (pieces.Index) : (pieces.Index+99999) );
				for ( int i=0 ; i<weightPriority.Length ; i++ ) {
					if ( weightPriority[i] != 99 )
						continue;
					bool valid = false;
					do {
						weightPriority[i] = rnd.Next( weightPriority.Length );
						valid = true;
						for ( int j=0 ; j<weightPriority.Length ; j++ )
							valid &= weightPriority[j]==99 || j==i || ( weightPriority[j] != weightPriority[i] );
					} while( !valid );
				}
			}
			else throw new Exception();


			for ( int i=0 ; i<reorder.Length ; i++ )
				reorder[i] = weightPriority[ ((pgi.GetPieceCount(i)>1)?12:0) + pgi.GetPiece(i).GetAsInt(switchSides^(i<pieceGroupCountW)) ];

			int idx = 0;
			for ( int i=0 ; i<reorder.Length ; i++ ) {
				int lowest = int.MaxValue;
				for ( int j=0 ; j<reorder.Length ; j++ ) {
					if ( reorder[j]>=idx && reorder[j]<lowest )
						lowest = reorder[j];
				}
				for ( int j=0 ; j<reorder.Length ; j++ ) {
					if ( reorder[j] == lowest )
						reorder[j] = idx;
				}					
				idx++;
			}				

	
			return new PieceGroupReorder( reorder );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="pieces"></param>
		/// <param name="s">e.G. KkQnRp</param>
		/// <returns></returns>
		public static PieceGroupReorder GetFromString( Pieces pieces, string s )
		{
			string origString = s;
			try{
				PieceGroupInfo pgi = pieces.GetPieceGroupInfo();
				int pieceGroupCountW = pgi.CountW;
				int[] origIndexToWeightIndex = new int[pgi.Count];

				if ( s.StartsWith("kk",StringComparison.OrdinalIgnoreCase) )
					s = s.Substring(2);

				int weightIndex = 0;
				while ( s.Length != 0 ) {
					char c = s[s.Length-1];

					int i=(char.IsUpper(c)?0:pieceGroupCountW);
					while ( pgi.GetPiece(i).AsCharacter != char.ToUpper(c) )
						i++;
					origIndexToWeightIndex[i] = weightIndex++;
					s = s.Replace( c.ToString(), "" );
				}

				return new PieceGroupReorder( origIndexToWeightIndex );
			}
			catch {
				throw new Exception( "Invalid PieceGroupReorderingString: " + origString + " for Pieces: " + pieces.ToString() );
			}
		}


		public int OrigIndexToWeightIndex( int index )
		{
			return origIndexToWeightIndex[index];
		}


		public int WeightIndexToOrigIndex( int index )
		{
			return weightIndexToOrigIndex[index];
		}


		public int ToInteger()
		{
			int value = 0;

			for ( int i=origIndexToWeightIndex.Length-1 ; i>=0 ; i-- ) {
				value <<= 3;
				value  |= origIndexToWeightIndex[i];
			}
			return value;
		}



	}
}
