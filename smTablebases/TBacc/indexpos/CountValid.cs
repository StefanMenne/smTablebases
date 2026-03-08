using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBacc
{
	public static class CountValid
	{

		/// <summary>
		/// Counts the amount of valid positions starting from firstIndex. There are two abort criteria:
		///       - validCountBound is reached    =>    validCountBound is the return value
		///       - maxIndexCount is reached  
		/// 
		/// </summary>
		public static long GetValidCount( IndexPos indexPos, long firstIndex, long maxIndexCount, long validCountBound, out long indexCount, out int reason )
		{
			long             countIndices = indexPos.IndexCount, step = 16384, count = 0, indexEp=-1;
			IndexEnumerator  enumValid = new IndexEnumerator( indexPos, null, false );
			IndexPos         indexPosEp = null;
			CheckAndPin      cpStm=null, cpSntm=null;
	
			enumValid.Reset(firstIndex,false);
			reason = -1;
			if ( indexPos.WPawnAndBPawn ) {
				indexPosEp  = new IndexPos( indexPos );
				indexEp     = firstIndex - 1;
				cpStm       = new CheckAndPin( indexPos.WkBk, indexPos.Pieces,  indexPos.Wtm );
				cpSntm      = new CheckAndPin( indexPos.WkBk, indexPos.Pieces, !indexPos.Wtm );
				indexPosEp.GetNextEpIndex( ref indexEp );						
			}


			while ( step >= 128 ) {
				if ( enumValid.EndReached )
					reason = 3;
				else if ( enumValid.IndexSrc+step>=firstIndex+maxIndexCount )
					reason = 2;
				else if ( count+step>=validCountBound )
					reason = 0;
				else
					reason = -1;

				if ( reason != -1 )
					step >>= 7;
				else { 
					count += enumValid.CountValid( enumValid.IndexSrc+step-65 );
					if ( indexPos.WPawnAndBPawn ) {
						long indexEpMax = enumValid.IndexSrc;
						while ( indexEp!=-1 && indexEp<indexEpMax ) {
							indexPosEp.SetToIndex( indexEp );
							if ( indexPosEp.GetIsValidEpPos( cpStm, cpSntm ) )
								count++;
							indexPosEp.GetNextEpIndex( ref indexEp );
						}
					}
				}
			}
			if ( reason == 3 )
				indexCount = countIndices - firstIndex;
			else if ( reason == -1 ) 
				throw new Exception();
			else
				indexCount = enumValid.IndexSrc - firstIndex;
			
			return count;
		}


		public static long GetValidCount( IndexPos indexPos )
		{
			IndexEnumerator enumValid = new IndexEnumerator( indexPos, null, false );
			enumValid.Reset( 0, true );
			long count = enumValid.CountValid();

			if ( indexPos.WPawnAndBPawn ) {
				long indexCount = indexPos.IndexCount;
				CheckAndPin cpStm = new CheckAndPin( indexPos.WkBk, indexPos.Pieces, indexPos.Wtm ), cpSntm = new CheckAndPin( indexPos.WkBk, indexPos.Pieces, !indexPos.Wtm );
				for ( long i=0 ; i<indexCount ; i++ ) {
					if ( indexPos.GetIsEp(i) ) {
						indexPos.SetToIndex( i );
						if ( indexPos.GetIsValidEpPos( cpStm, cpSntm ) )
							count++;
					}
				}
			}

			return count;
		}

	}
}
