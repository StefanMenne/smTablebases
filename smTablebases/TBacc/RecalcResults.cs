using System;
using System.Collections.Generic;
using System.Text;

namespace TBacc
{
    public enum RecalcResults
    {
        Disabled = 0,         // all data will be compressed / stored
        
        ZeroOut = 1,          // recalculateable results will be set to 0 before compression / storage 
                              // and restored after decompression / loading
                              // LC will use this knowledge and might decompress this positions with other then 0 values !
                               
        Remove = 2            // recalculateabele results will be removed before compression / storage
    }
}
