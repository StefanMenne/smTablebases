using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class Coder
	{
		protected byte[]                        data;
		protected int                           dataLengthBits;
		protected int                           dataLength;

		protected RangeCoder                    rangeCoder;
		protected Literal                       literal;
		protected Settings                      settings;
		protected Level                         level;

		protected CodingIntOccurence            codingTypeCoder;
		protected CodingLiteral                 literalCoder;
		protected CodingIntOccurence            historyDistCoder;  
		protected CodingIntOccurence            lengthCoder;
		protected CodingState                   codingState;
		protected LengthInfo                    lengthInfo;
		protected CodingExpDist                 codingExpDist;
		protected DistConverter                 distConverter;

#if DEBUG
		protected CodingIntOccurence            repeatDistCoder;
		public    static bool        Verify      = false;
		protected static Queue<long> verifyQueue = new Queue<long>();
#endif


		protected Coder()
		{		
		}


		protected void Init( RangeCoder rangeCoder, byte[] data, int[] posToVirtualPos, LengthInfo lengthInfo )
		{
			this.literal     = new Literal( data, settings.BytesPerItem, settings.LiteralPosBits, settings.PrevByteHighBits );
			this.data        = data;
			this.rangeCoder  = rangeCoder;
			this.level       = Level.Instances[settings.Level];

			if ( dataLength == 0 )
				return;

			distConverter      = (posToVirtualPos==null) ? null : new DistConverter( posToVirtualPos, dataLength );
			codingTypeCoder    = new CodingIntOccurence( rangeCoder );
			literalCoder       = new CodingLiteral( rangeCoder, literal.Bits );
			historyDistCoder   = new CodingIntOccurence( rangeCoder );
			lengthCoder        = new CodingIntOccurence( rangeCoder );
			codingExpDist      = new CodingExpDist( ( (posToVirtualPos==null) ? dataLengthBits : Tools.ValueToBitCount(posToVirtualPos[dataLength-1]+1) ) );
#if DEBUG
			repeatDistCoder    = new CodingIntOccurence( rangeCoder );
#endif

			this.codingState = new CodingState( dataLengthBits, lengthInfo.LengthIndexCount, literal, lengthInfo, level, CodingExpDist.ExpDistSlotBitCount );
		}


		protected RangeEncoder RangeEncoder
		{
			get { return (RangeEncoder)rangeCoder; }
		}


		protected RangeDecoder RangeDecoder
		{
			get { return (RangeDecoder)rangeCoder; }
		}



	}
}
