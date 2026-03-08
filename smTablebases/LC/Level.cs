using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
    public class Level
    {
        public static Level[] Instances = new Level[] {
            //   Level 6 uses different algorithm; most values are ignored
            //                        Delta  min 
            //                        Cost   hist
            //                        Bound  dist
            //                        Bits   occ
            /* 0 Max     */ new Level(  16,     2 ){  LengthTryCount=8,   TryLiteralMaxHistoryLengthIndex=8  },
            /* 1 Default */ new Level(  16,     2 ){  LengthTryCount=6,   TryLiteralMaxHistoryLengthIndex=1  },
            /* 2         */ new Level(   4,     3 ){  LengthTryCount=6,   TryLiteralMaxHistoryLengthIndex=0  },
            /* 3         */ new Level(   0,     4 ){  LengthTryCount=5,   TryLiteralMaxHistoryLengthIndex=0  },
            /* 4         */ new Level(  -4,     4 ){  LengthTryCount=4,   TryLiteralMaxHistoryLengthIndex=0  },
            /* 5         */ new Level(  -8,     6 ){  LengthTryCount=4,   TryLiteralMaxHistoryLengthIndex=0  },
            /* 6         */ new Level(   0,     10 ){ LengthTryCount=2,   TryLiteralMaxHistoryLengthIndex=0  },
        };
    
        
        public double   DeltaCostBound;                  // Cut calculation if delta to best coding exceeds
        public int      MinHistoryDistOccurence;         // Dist Occurence count when dists can be coded via history 
        public int      LengthTryCount;                  // Amount of lengthIndices to search for matches
        public int      TryLiteralMaxHistoryLengthIndex; // Try literal coding only if no history match of given length is available
        private bool    negativeDeltaCostBound = false;
        

        private Level( int deltaCostBoundInBits, int minHistoryDistOccurence )
        {
            if (deltaCostBoundInBits < 0)
            {
                negativeDeltaCostBound = true;
                this.DeltaCostBound = CodingCosts.ProbabilityFromBitCount(-deltaCostBoundInBits);
            }
            else
            {
                negativeDeltaCostBound = false;
                this.DeltaCostBound = CodingCosts.ProbabilityFromBitCount(deltaCostBoundInBits);
            }

            this.MinHistoryDistOccurence  = minHistoryDistOccurence;
        }


        public bool CheckToSkip( CodingCosts costsCurrent, CodingCosts costsFutureBest )
        {
            return negativeDeltaCostBound ? (costsCurrent.Add(DeltaCostBound) > costsFutureBest):
                (costsCurrent > costsFutureBest.Add(DeltaCostBound));
        }
        
    }
}
