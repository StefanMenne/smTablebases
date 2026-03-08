using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public abstract class CodingStateBase
	{
		protected     ProbabilityArray                   historyIsLit                    = null;
		protected     Collection<ProbabilityArray>       historyType                     = null;
		protected     Collection<ProbabilityTree>        historyLiteralTrees             = null;
		protected     Collection<ProbabilityTree>        historyExpDistSlot              = null;
		protected     ProbabilityTree                    historyExpDistBits              = null;
		protected     ProbabilityArray                   historyLength                   = null;
		protected     ProbabilityArray                   probabilityIsLowIsMidLength     = null;
		protected     Collection<ProbabilityTree>        historyLengthTreeLow            = null;
		protected     Collection<ProbabilityTree>        historyLengthTreeMid            = null;
		protected     ProbabilityTree                    historyLengthTreeHigh           = null;
		protected     BigValueHistoryBase                historyDistance                 = null;

		public        LatestHistory                      HistoryLatest                   = LatestHistory.Initial;
		protected     Last3CodingsStateIndex             last3CodingsStateIndex          = Last3CodingsStateIndex.Init;
		protected     double                             probabilityIsRep0S              = 0.5D;
		protected     int                                rep0AllowedLengthIndexCount;
		protected     int                                pos                             = 0;

#if DEBUG
		public static bool Compare( CodingState cs, CodingStateImmutable csi )
		{
			bool equal = (cs.pos==csi.pos);
			equal &= cs.last3CodingsStateIndex==csi.last3CodingsStateIndex;
			equal &= cs.probabilityIsRep0S==csi.probabilityIsRep0S;
			equal &= ProbabilityArray.Compare( cs.historyIsLit, csi.HistoryIsLit );
			equal &= LatestHistory.Compare( cs.HistoryLatest, csi.HistoryLatest );
			for ( int i=0 ; i<cs.historyType.Count ; i++ )
				equal &= ProbabilityArray.Compare( cs.historyType[i], csi.historyType[i] );
			if ( csi.historyLength == null ) {
				for ( int i=0 ; i<cs.historyLengthTreeLow.Count ; i++ )
					equal &= ProbabilityTree.Compare( cs.historyLengthTreeLow[i], csi.historyLengthTreeLow[i] );
				for ( int i=0 ; i<cs.historyLengthTreeLow.Count ; i++ )
					equal &= ProbabilityTree.Compare( cs.historyLengthTreeLow[i], csi.historyLengthTreeLow[i] );
				for ( int i=0 ; i<cs.historyLengthTreeMid.Count ; i++ )
					equal &= ProbabilityTree.Compare( cs.historyLengthTreeMid[i], csi.historyLengthTreeMid[i] );
				equal &= ProbabilityTree.Compare( cs.historyLengthTreeHigh, csi.historyLengthTreeHigh );
			}
			else { 
				equal &= ProbabilityArray.Compare( cs.historyLength, csi.historyLength );
				equal &= (cs.Rep0AllowedLengthIndexCount==csi.Rep0AllowedLengthIndexCount);
			}
			equal &= BigValueHistoryBase.Compare( cs.historyDistance, csi.historyDistance );
			for ( int i=0 ; i<cs.historyLiteralTrees.Count ; i++ )
				equal &=  ProbabilityTree.Compare( cs.historyLiteralTrees[i], csi.LiteralTrees[i] );
			for ( int i=0 ; i<cs.historyExpDistSlot.Count ; i++ )
				equal &= ProbabilityTree.Compare( cs.historyExpDistSlot[i], csi.historyExpDistSlot[i] );
			equal &= ProbabilityTree.Compare( cs.historyExpDistBits, csi.historyExpDistBits );
			return equal;
		}
#endif


		public ProbabilityArray HistoryIsLit
		{
			get { return historyIsLit; }
		}


		public Collection<ProbabilityArray> HistoryType
		{
			get { return historyType; }
		}


		public Collection<ProbabilityTree> LiteralTrees
		{
			get { return historyLiteralTrees; }
		}


		public Collection<ProbabilityTree> HistoryExpDistSlot
		{
			get { return historyExpDistSlot; }
		}


		public ProbabilityTree HistoryExpDistBits
		{
			get { return historyExpDistBits; }
		}


		public ProbabilityArray HistoryLength
		{
			get { return historyLength; }
		}


		public Collection<ProbabilityTree> HistoryLengthTreeLow
		{
			get { return historyLengthTreeLow; }
		}
		
		
		public Collection<ProbabilityTree> HistoryLengthTreeMid
		{
			get { return historyLengthTreeMid; }
		}

	
		public ProbabilityTree HistoryLengthTreeHigh
		{
			get { return historyLengthTreeHigh; }
		}

		
		public ProbabilityArray ProbabilityIsLowIsMidLength
		{
			get { return probabilityIsLowIsMidLength; }
			set { probabilityIsLowIsMidLength = value; }
		}


		public BigValueHistoryBase HistoryDistance
		{
			get { return historyDistance; }
		}


		public int GetCodingItemTypeStateIndex( int pos )
		{
			return last3CodingsStateIndex.GetIndexType( pos );
		}


		public int GetIsLiteralStateIndex( int pos )
		{
			return last3CodingsStateIndex.GetIndexIsLiteral( pos );
		}


		public CodingStateBase( int lengthIndexCount )
		{
			rep0AllowedLengthIndexCount = lengthIndexCount;
		}


		public int Rep0AllowedLengthIndexCount
		{
			get { return rep0AllowedLengthIndexCount; }
		}


		public int Pos
		{
			get { return pos; }
		}


		public double ProbabilityIsRep0S
		{
			get { return probabilityIsRep0S; }
		}


#if DEBUG
		public byte[] CalcMD5()
		{
			MD5 md5 = MD5.Create();

			historyLiteralTrees.CalcMd5( md5 );
			historyIsLit.CalcMd5( md5 );
			historyType.CalcMd5( md5 );
			historyDistance.CalcMd5( md5 );

			if ( historyLength == null ) {
				probabilityIsLowIsMidLength.CalcMd5( md5 );
				historyLengthTreeLow.CalcMd5( md5 );
				historyLengthTreeMid.CalcMd5( md5 );
				historyLengthTreeHigh.CalcMd5( md5 );
			}
			else 
				historyLength.CalcMd5( md5 );
			historyExpDistSlot.CalcMd5( md5 );
			historyExpDistBits.CalcMd5( md5 );
			
			byte[] tmp2 = BitConverter.GetBytes( probabilityIsRep0S );
			md5.TransformBlock( tmp2, 0, tmp2.Length, tmp2, 0 );

			md5.TransformFinalBlock( new byte[0], 0, 0 );
			return md5.Hash;
		}
#endif
	}
}
