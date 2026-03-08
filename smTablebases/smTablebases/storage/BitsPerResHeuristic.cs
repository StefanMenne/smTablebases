using TBacc;

namespace smTablebases
{
	public class BitsPerResHeuristic
	{
		public static int Get( bool wtm, int pieceIndex )
		{
			Pieces     pieces = Pieces.FromIndex( pieceIndex );
			TbInfoFile info   = TbInfoFileList.Get(pieces);
			int        maxDtm = info.MaxDtmHm;

			if ( maxDtm == -1 )
				maxDtm = Config.MaxDtm;

			int winResultCount   = ((maxDtm+1)/2);
			int mvCountBound     = pieces.GetMvCountBound( wtm );
			int lsResultCount    = ((maxDtm/2)+1/*Init*/) * (mvCountBound+1/*FinalResults*/);
			int otherResultCount = 5;   // mate; stale mate; draw; illegal

			if ( pieces.GetPieceCount(!wtm) == 0 )    // opponent has no piece => cannot lose
				lsResultCount = 0;                // all results will be set to draw in Init

			return Tools.Log2ForAnyNumber( winResultCount + lsResultCount + otherResultCount -1 ) + 1;
		}
	}
}
