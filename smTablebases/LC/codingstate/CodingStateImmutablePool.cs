using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class CodingStateImmutablePool
	{
		private Stack<CodingStateImmutable>                    instances;
		private BigValueHistoryImmutablePool                   poolDist;
		private int                                            lengthIndexCount;    
		private int                                            count;


		public CodingStateImmutablePool( int count, LengthInfo lengthInfo, Literal literal, int dataLengthBits, Level level, int expDistSlotCount )
		{
			lengthIndexCount = lengthInfo.LengthIndexCount;
			poolDist         = new BigValueHistoryImmutablePool( SettingsFix.HistoryDistWindowSize, count, level.MinHistoryDistOccurence, SettingsFix.HistoryDistInitValues );
			instances = new Stack<CodingStateImmutable>( count );
			for ( int i=0 ; i<count ; i++ ) 
				instances.Push( new CodingStateImmutable() );
			this.count  = count;
		}


		public CodingStateImmutable CreateEmpty( Literal literal, LengthInfo lengthInfo )
		{
			CodingStateImmutable             csi                       = GetInstance();
			Collection<ProbabilityArray>     historyType               = ProbabilityArray.CreateFirstCollection( count, Last3CodingsStateIndex.IndexCountType, SettingsFix.HistoryTypeCount, true );
			Collection<ProbabilityTree>      literalProbabilityTree    = ProbabilityTree.CreateFirstCollection( count, literal.ContextCount, 3<<literal.Bits );
			ProbabilityArray                 isLitProbabilityArray     = ProbabilityArray.CreateFirst( count, Last3CodingsStateIndex.IndexCountIsLiteral, false );
			ProbabilityTree                  expDistBits               = ProbabilityTree.CreateFirst( count, CodingExpDist.ExpDistBitsTreeLength );
			Collection<ProbabilityTree>      expDistSlot               = ProbabilityTree.CreateFirstCollection( count, SettingsFix.ExpDistSlotContextCount, ProbabilityTree.GetProbabilityVariablesCount(CodingExpDist.ExpDistSlotBitCount) );
			ProbabilityArray                 historyLength             = null;
			ProbabilityArray                 historyIsLowIsMidLength   = null;
			Collection<ProbabilityTree>      historyLengthTreeLow      = null;
			Collection<ProbabilityTree>      historyLengthTreeMid      = null;
			ProbabilityTree                  historyLengthTreeHigh     = null;
			if ( lengthInfo.LengthIndexCount >= 13 ) {
				historyIsLowIsMidLength = ProbabilityArray.CreateFirst( count, 8, false );
				historyLengthTreeLow    = ProbabilityTree.CreateFirstCollection( count, SettingsFix.HistoryLengthIsLowIsMidContextCount, 14 );
				historyLengthTreeMid    = ProbabilityTree.CreateFirstCollection( count, SettingsFix.HistoryLengthIsLowIsMidContextCount, 14 );
				historyLengthTreeHigh   = ProbabilityTree.CreateFirst( count, 2*256 );
			}
			else
				historyLength     = ProbabilityArray.CreateFirst( count, lengthInfo.LengthIndexCount, true );
			CodingStateImmutable.Create( this, csi, isLitProbabilityArray, historyType, poolDist.CreateEmpty(), historyLength, historyLengthTreeLow, historyLengthTreeMid, historyLengthTreeHigh, historyIsLowIsMidLength, LatestHistory.Initial, -1, 0, Last3CodingsStateIndex.Init, literalProbabilityTree, expDistSlot, expDistBits, 0.5D );
			return csi;
		}


		public int LengthIndexCount
		{
			get { return lengthIndexCount; }
		}

		
		public CodingStateImmutable GetInstance()
		{
			return instances.Pop();
		}


		public void ReuseInstance( CodingStateImmutable inst )
		{
			instances.Push( inst );
		}
	}
}
