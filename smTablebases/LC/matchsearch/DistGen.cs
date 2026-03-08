﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class DistGen
	{
		private readonly int[] length;
		private readonly int[] lengthIndexLeft;


		// output values
		private int                   maxMatchLengthIndex = -1;
		private int[]                 shortestDist;


		private int                   pos              = -1;
		private int                   currentTwoBytes  = 0;

		// for match length 1
		private int[]                 firstOcc, lastOcc;


		// Bits 0-23:     a pos which holds these properties:
		//                    1) pos is left of current position
		//                    2) the tails of both positions are equal; the amount of characters that match is maximal
		//                    3) for every position that holds (1) and (2) the leftmost is taken
		// Bits 24-31:    the amount of characters that match stored as lengthIndex + 1; e.g. Bits 24-31= 2 => lengthIndex=1
		private int[]                 subId;


        // (length, idLeft, idRight) --> (lastPos, idCombined)
        // To reduce memory usage, a "link" may only be created upon the second occurrence of the combined ID.
        // Specifically, a link is established if:
        //     (1) It is the 1st occurrence of the combined ID AND both partial IDs have already occurred at least twice.
        //     (2) Otherwise, it is established upon the 2nd occurrence.
        private MyHash  combine;   


		private int    countHashTableEntries  = 0;



        public DistGen( int countDataBytes, int[] length )
		{
			firstOcc     = new int[65536];
			lastOcc      = new int[65536];
			subId        = new int[countDataBytes];
			shortestDist = new int[length.Length+1];
			for ( int i=0 ; i<firstOcc.Length ; i++ )
				firstOcc[i] = -1;

			this.length = length;
			lengthIndexLeft = new int[this.length.Length];
			for ( int i=1 ; i<lengthIndexLeft.Length ; i++ )
				lengthIndexLeft[i] = Array.IndexOf( this.length, this.length[i]-this.length[i-1] );
			combine = new MyHash( countDataBytes );
            // regarding the hash fill factor:  in some tests it was around 0.5-0.9.
			// theoretically I could get an upper bound at 2.0 but without verifying it deeply and only for the case of exponentially rising length
        }


        public int CountHashTableEntries
		{
			get { return countHashTableEntries; }
		}


		public int MaxEntriesPerBucket
		{
			get { return combine.MaxEntriesPerBucket; }
		}


		public double UsedBucketsFraction
		{
			get { return ((double)combine.UsedBuckets) / countHashTableEntries; }
		}

			
		public void AddChar( byte c )
		{
			currentTwoBytes = ((currentTwoBytes<<8)&0xffff) | c;
			int   id   = firstOcc[currentTwoBytes];
			if ( ++pos == 0 )
				return;

			if ( id == -1 ) {
				firstOcc[currentTwoBytes]    = lastOcc[currentTwoBytes]  = pos;
				maxMatchLengthIndex          = -1;
				return;
			}
			shortestDist[0]          = pos - lastOcc[currentTwoBytes];
			lastOcc[currentTwoBytes] = pos;

			
			int l=0;
			while( ++l<length.Length ) {
				int lengthIndexLeft  = this.lengthIndexLeft[l];
				int lenRight         = length[l-1];
				int posLeft          = pos - lenRight; 
				int idLeft           = GetId( posLeft, lengthIndexLeft );


				// The heart of LC. Here the matches are searched.
				//
                // To save memory 5 scenarios are distinguished:
                //  
                //     L      L       L         LR (1)      L    R    R    L     LR (2)  ...  LR (6)      >=0 occurrences of left IDs; then combined ID; then any number of left and right ID occurrences and then the combined ID
                //     R   R     R      R       LR (3)      L   R  R   R  L R    LR (4)  ...  LR (6)      analogous, with the roles of L and R reversed
                //     L  R    R    L      L    LR (5)                                   ...  LR (6)      >=1 L's and >=1 R's and then LR
                //
                //   Only at (2), (4) and (5) an entry "combine" will be generated. The position of (1) and (3)
                //   can be reconstructed later!
                //  
                if ( idLeft==posLeft/* (3) */ || id == pos  /* (1) */ ) { 
					break;                                 // case (1), (3)
				}
				else {                                     // case (2), (4), (5) or (6)
					Int64 key = (((long)(uint)l)<<48) | (((long)(uint)idLeft)<<24) | ((long)(uint)id);
					int  idCombined;
					long hashTableValue = combine.Get( key );
					if ( hashTableValue != -1 ) {
						// case (6)
						idCombined      = (int)(hashTableValue&0xffffff);
						shortestDist[l] = pos - (int)(hashTableValue>>32);
						hashTableValue  = (hashTableValue & 0xffffff) | (((long)pos)<<32);
						combine.SetLastAccessed( hashTableValue );
					}
					else {
						if ( id-lenRight>=0 && GetId(id-lenRight,lengthIndexLeft) == idLeft ) {
							// case (2)
							idCombined = id;
						}
						else if ( idLeft+lenRight<pos && GetId(idLeft+lenRight,l-1) == id ) {
							// case (4) 
							idCombined = idLeft + lenRight;
						}
						else {
							// case (5) 
							combine.Add( key, ((long)(uint)pos) | (((long)(uint)pos)<<32) );
							countHashTableEntries++;
							break;
						}
						shortestDist[l] = pos - idCombined;
						combine.Add( key, ((long)(uint)idCombined) | (((long)(uint)pos)<<32) );
						countHashTableEntries++;
					}
					if ( idCombined != id ) {
						subId[idCombined] = id | (l<<24);
						id = idCombined;
					}
				}
			}
			maxMatchLengthIndex = l-1;
			subId[pos] = id | (l<<24);
		}



		public int[] ShortestDist
		{
			get { return shortestDist; }
		}


		public int MaxMatchLengthIndex
		{
			get { return maxMatchLengthIndex; }
		}


		public int Pos
		{
			get { return pos; }
		}


		public int[] LengthArray
		{
			get {  return length; }
		}


		public int GetId( int pos, int lengthIndex )
		{
			while ( (subId[pos] >> 24) > lengthIndex )
				pos = (subId[pos]&0xffffff);
			return pos;		
		}
	}
}
