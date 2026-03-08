using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
    [Flags]
    public enum MirrorType
    {
        None                = 0x00,
        MirrorOnHorizontal  = 0x01,
        MirrorOnVertical    = 0x02,
        MirrorOnDiagonal    = 0x04
    }
}
