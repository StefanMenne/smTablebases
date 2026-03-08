using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class ProbabilityArray : Immutable<ProbabilityArray>
	{
		protected double[] probabilities;

	
		public static ProbabilityArray CreateFirst( int countInstances, int countArrayEntries, bool sum1 )
		{
			Stack<ProbabilityArray> stackArray = new Stack<ProbabilityArray>( countInstances );
			for ( int i=0 ; i<countInstances ; i++ )																										
				stackArray.Push( new ProbabilityArray( countArrayEntries, stackArray ) );			
			ProbabilityArray first = stackArray.Pop();
			first.InitFirst( sum1 );
			return first;
		}


		public static Collection<ProbabilityArray> CreateFirstCollection( int countCollections, int countArrays, int countArrayEntries, bool sum1 )
		{
			Stack<Collection<ProbabilityArray>> stackCollection = new Stack<Collection<ProbabilityArray>>( countCollections );
			Stack<ProbabilityArray>             stackArray      = new Stack<ProbabilityArray>( countCollections * countArrays );
		
			for ( int i=0 ; i<countCollections ; i++ ) 
				stackCollection.Push( new Collection<ProbabilityArray>( countArrays, stackCollection ) );
			for ( int i=0 ; i<countCollections*countArrays ; i++ )																										
				stackArray.Push( new ProbabilityArray( countArrayEntries, stackArray ) );

			Collection<ProbabilityArray> first = stackCollection.Pop();
			for ( int i=0 ; i<first.Count ; i++ ) {
				first[i] = stackArray.Pop();
				first[i].InitFirst( sum1 );
			}
			return first;
		} 


		public ProbabilityArray( int countArrayEntries, Stack<ProbabilityArray> stack ) : base( stack )
		{
			probabilities = new double[countArrayEntries];
		}


		public static bool Compare( ProbabilityArray tree1, ProbabilityArray tree2 )
		{ 
			if ( tree1.probabilities.Length != tree2.probabilities.Length )
				return false;
			
			for ( int i=0 ; i<tree1.probabilities.Length ; i++ ) {
				if ( tree1.probabilities[i] != tree2.probabilities[i] )
					return false;
			}

			return true;
		}


		public void InitFirst( bool sum1 )
		{
			double f = sum1 ? (1.0d / probabilities.Length) : (0.5D);

			for ( int i=0 ; i<probabilities.Length ; i++ )
				probabilities[i] = f;
		}


		public double[] Probabilities
		{
			get { return probabilities; }
		}


		public double this[int index]
		{
			get { return probabilities[index]; }
			set { probabilities[index] = value; }
		}


		public void ChangeProbability( int index, bool increase, double minProbability, double maxProbability )
		{
			probabilities[index] = Probability.ChangeProbability( probabilities[index], increase, minProbability, maxProbability );
		}


		public ProbabilityArray ChangeProbability( int index, bool increase, double minProbability, double maxProbability, bool disposeOldInstance )
		{
			ProbabilityArray array = Clone( disposeOldInstance );
			array.ChangeProbability( index, increase, minProbability, maxProbability );
			return array;
		}


		public ProbabilityArray IncreaseProbabilityKeepSum1Immutable( int index, double decreaseFactor, double minProbability, bool disposeOldInstance )
		{
			ProbabilityArray array = Clone( disposeOldInstance );
			array.IncreaseProbabilityKeepSum1( index, decreaseFactor, minProbability );
			return array;
		}


		public void IncreaseProbabilityKeepSum1( int index, double decreaseFactor, double minProbability )
		{
			double sum = 1D;

			for ( int i=0 ; i<probabilities.Length ; i++ ) {
				probabilities[i] *= decreaseFactor;
				if ( probabilities[i] < minProbability )
					probabilities[i] = minProbability;
				sum -= probabilities[i];
			}
			probabilities[index] += sum;

#if DEBUG
			sum = 0D;
			for ( int i=0 ; i<probabilities.Length ; i++ )
				sum += probabilities[i];
			if ( Math.Abs( 1D - sum ) > 0.0000001 )
				throw new Exception();
#endif
		}


		public void IncreaseProbabilityKeepSum1( int indexToIncrease, int countToDecrease, double minProbability )
		{
			LC.Probability.IncreaseOneProbabilityKeepSum1( probabilities, indexToIncrease, countToDecrease, minProbability );
		}


		public override void CopyFieldsTo( ProbabilityArray dst )
		{
			Array.Copy( probabilities, dst.probabilities, probabilities.Length );
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
