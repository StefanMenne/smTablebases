﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class BigValueHistoryImmutablePool
	{
		public const int oldestNewestQueueSize = 10;

		private Stack<BigValueHistoryImmutable>  instances;
		private Stack<int[]>                     intArrays, intArraysOldestNewestQueue;
		private int                              windowSize;
		private InitValues                       initValues;
		private int                              minOccurence;



		public BigValueHistoryImmutablePool( int windowSize, int poolSize, int minOccurence, InitValues initValues )
		{
			this.windowSize   = windowSize;
			this.initValues   = initValues;
			this.minOccurence = minOccurence;
			instances = new Stack<BigValueHistoryImmutable>( poolSize );
			intArrays = new Stack<int[]>( 3*poolSize ); 
			intArraysOldestNewestQueue = new Stack<int[]>( poolSize );
			for ( int i=0 ; i<3*poolSize ; i++ )
				intArrays.Push( new int[windowSize+1] ); // +1 because removing oldest item and then adding new item is done in one step
			int oldestNewestQueueSizeCurrent = Math.Min( oldestNewestQueueSize, windowSize>>1 );   // otherwise extra handling would be necessary when copying
			for ( int i=0 ; i<poolSize ; i++ ) {
				intArraysOldestNewestQueue.Push( new int[oldestNewestQueueSizeCurrent] );
				instances.Push( new BigValueHistoryImmutable() );
			}
		}


		public int WindowSize
		{ 
			get {  return windowSize; }
		}


		public int MinOccurence
		{ 
			get {  return minOccurence; }
		}


		public BigValueHistoryImmutable GetInstance()
		{
			return instances.Pop();
		}


		public void ReuseInstance( BigValueHistoryImmutable inst )
		{
			instances.Push( inst );
		}


		public BigValueHistoryImmutable CreateEmpty()
		{
			return BigValueHistoryImmutable.CreateEmpty( this, minOccurence, initValues );
		}


		public int[] GetIntArray()
		{
			return intArrays.Pop();
		}


		public int[] GetIntArrayForOldestNewestQueue()
		{
			return intArraysOldestNewestQueue.Pop();
		}


		public void ReuseIntArray( int[] array )
		{
			intArrays.Push( array );
		}


		public void ReuseIntArrayForOldestNewestQueue( int[] array )
		{
			intArraysOldestNewestQueue.Push( array );
		}

	}
}
