using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class ProbabilityTree : Immutable<ProbabilityTree>
	{
		protected double[]      probabilities;
		private   int           index;
	

		public static int GetProbabilityVariablesCount( int bitCount )
		{
			return 1<<bitCount;
		}


		public static ProbabilityTree CreateFirst( int countInstances, int countArrayEntries )
		{
			Stack<ProbabilityTree> stackArray = new Stack<ProbabilityTree>( countInstances );
			for ( int i=0 ; i<countInstances ; i++ )																										
				stackArray.Push( new ProbabilityTree( countArrayEntries, stackArray ) );			
			ProbabilityTree first = stackArray.Pop();
			first.InitFirst();
			return first;
		}


		public static Collection<ProbabilityTree> CreateFirstCollection( int countCollections, int countTrees, int countProbabilityVariables )
		{
			Stack<Collection<ProbabilityTree>> stackCollection = new Stack<Collection<ProbabilityTree>>( countCollections );
			Stack<ProbabilityTree>             stackArray      = new Stack<ProbabilityTree>( countCollections * countTrees );
		
			for ( int i=0 ; i<countCollections ; i++ ) 
				stackCollection.Push( new Collection<ProbabilityTree>( countTrees, stackCollection ) );
			for ( int i=0 ; i<countCollections*countTrees ; i++ )																										
				stackArray.Push( new ProbabilityTree( countProbabilityVariables, stackArray ) );

			Collection<ProbabilityTree> first = stackCollection.Pop();
			for ( int i=0 ; i<first.Count ; i++ ) {
				first[i] = stackArray.Pop();
				first[i].InitFirst();
			}
			return first;
		} 




		public ProbabilityTree( int countArrayEntries, Stack<ProbabilityTree> stack ) : base( stack )
		{
			probabilities = new double[countArrayEntries];
		}


		public static bool Compare( ProbabilityTree tree1, ProbabilityTree tree2 )
		{ 
			if ( tree1.probabilities.Length != tree2.probabilities.Length )
				return false;
			
			for ( int i=0 ; i<tree1.probabilities.Length ; i++ ) {
				if ( tree1.probabilities[i] != tree2.probabilities[i] )
					return false;
			}

			return true;
		}


		public void InitFirst()
		{
			for ( int i=0 ; i<probabilities.Length ; i++ )
				probabilities[i] = 0.5D;
		}


		public double[] Probabilities
		{
			get { return probabilities; }
		}


		//public ProbabilityTree IncreaseProbabilityImmutable( int index, double decreaseFactor, double minProbability, bool disposeOldInstance )
		//{
		//	ProbabilityTree a = Clone( disposeOldInstance );
		//	a.IncreaseProbability( index, decreaseFactor, minProbability );
		//	return a;
		//}


		public override void CopyFieldsTo( ProbabilityTree dst )
		{
			Array.Copy( probabilities, dst.probabilities, probabilities.Length );
		}


		public void ChangeTreeProbabilities( int value, int bitCount, double minProbability, double maxProbability, int startBitIndex = 0, int probArrayOffset = 0 )
		{
			Reset( startBitIndex );
			for ( int i=startBitIndex ; i<bitCount ; i++ )
				AddBit( ((value>>(bitCount-i-1))&1)==1, minProbability, maxProbability, probArrayOffset );
		}


		public void ChangeTreeProbabilitiesReverse( int value, int bitCount, int probArrayOffset, double minProbability, double maxProbability )
		{
			Reset( 0 );
			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit = ((value>>i)&1)==1;
				AddBit( bit, minProbability, maxProbability, probArrayOffset );
			}
		}


#if DEBUG
		public double CodeAndUpdateTreeProbabilities( RangeEncoder re, int value, int bitCount, double minProbability, double maxProbability, int probArrayOffset=0 )
#else
		public void CodeAndUpdateTreeProbabilities( RangeEncoder re, int value, int bitCount, double minProbability, double maxProbability, int probArrayOffset=0 )
#endif
		{
#if DEBUG
			double codingSize = 0d; 
#endif
			Reset( 0 );
			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit = ((value>>(bitCount-i-1))&1)==1;
#if DEBUG
				codingSize += re.AddBit( bit, GetProbability(probArrayOffset) );
#else
				re.AddBit( bit, GetProbability(probArrayOffset) );
#endif
				AddBit( bit, minProbability, maxProbability, probArrayOffset );
			}
#if DEBUG
			return codingSize;
#endif
		}


#if DEBUG
		public double ReverseCodeAndUpdateTreeProbabilities( RangeEncoder re, int value, int bitCount, int probArrayOffset, double minProbability, double maxProbability )
#else
		public void ReverseCodeAndUpdateTreeProbabilities( RangeEncoder re, int value, int bitCount, int probArrayOffset, double minProbability, double maxProbability )
#endif
		{
#if DEBUG
			double codingSize = 0d; 
#endif
			Reset( 0 );
			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit = ((value>>i)&1)==1;
#if DEBUG
				codingSize += re.AddBit( bit, GetProbability(probArrayOffset) );
#else
				re.AddBit( bit, GetProbability(probArrayOffset) );
#endif
				AddBit( bit, minProbability, maxProbability, probArrayOffset );
			}
#if DEBUG
			return codingSize;
#endif
		}

		public int DecodeAndUpdateTreeProbabilities( RangeDecoder rd, int bitCount, double minProbability, double maxProbability, int probArrayOffset = 0 )
		{
			int value = 0;
			Reset( 0 );
			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit = rd.GetBit( GetProbability(probArrayOffset) );
				AddBit( bit, minProbability, maxProbability, probArrayOffset );
				value = (value<<1) | (bit ? 1 : 0);
			}
			return value;
		}


		public int ReverseDecodeAndUpdateTreeProbabilities( RangeDecoder rd, int bitCount, int probArrayOffset, double minProbability, double maxProbability )
		{
			int value = 0;
			Reset( 0 );
			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit = rd.GetBit( GetProbability(probArrayOffset) );
				AddBit( bit, minProbability, maxProbability, probArrayOffset );
				if ( bit )
					value |= (1<<i);
			}
			return value;
		}


		public double GetCodingSizeAsProbability( int value, int bitCount, int probArrayOffset=0 )
		{
			double codingSize = 1d;
			Reset( 0 );
			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit = ((value>>(bitCount-i-1))&1)==1;
				codingSize *= bit ? GetProbability(probArrayOffset) : (1d-GetProbability(probArrayOffset));
				MoveOneBit( bit );
			}
			return codingSize;
		}

		public double GetReverseCodingSizeAsProbability( int value, int bitCount, int probArrayOffset = 0 )
		{
			double codingSize = 1d;
			Reset( 0 );
			for ( int i=0 ; i<bitCount ; i++ ) {
				bool bit = ((value>>i)&1)==1;
				codingSize *= bit ? GetProbability(probArrayOffset) : (1d-GetProbability(probArrayOffset));
				MoveOneBit( bit );
			}
			return codingSize;
		}




		private void Reset( int skipCount )
		{
			index = 1<<skipCount;
		}


		private void AddBit( bool bit, double minProbability, double maxProbability, int probArrayOffset=0 )
		{
			double d = probabilities[probArrayOffset+index];
			probabilities[probArrayOffset+index] = LC.Probability.ChangeProbability( d, bit, minProbability, maxProbability );
			MoveOneBit( bit );
		}


		private void MoveOneBit( bool bit )
		{
			index = (index<<1) | (bit ? 1 : 0);
		}


		private double Probability
		{
			get { return probabilities[index]; }
		}


		private double GetProbability( int probArrayOffset )
		{
			return probabilities[index+probArrayOffset];
		}


#if DEBUG
		public override void CalcMd5( MD5 md5 )
		{
			GCHandle handle = GCHandle.Alloc( probabilities, GCHandleType.Pinned );
			try {
				IntPtr pointer = handle.AddrOfPinnedObject();
				byte[] b = new byte[probabilities.Length*Marshal.SizeOf(typeof(double))];
				Marshal.Copy( pointer, b, 0, b.Length );
				md5.TransformBlock( b, 0, b.Length, b, 0 );
			}
			finally {
				if ( handle.IsAllocated )
					handle.Free();
			}
		}
#endif


	}
}
