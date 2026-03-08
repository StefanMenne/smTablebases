﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class ShortCodingSearchWindow : CircularBuffer<FindPathInfo>
	{
		//
		// SearchWindow:
		//
		//   0     1     2     3    ...     n-1    n    n+1     n+2    ....    2*n    
		//                                        /\
		//                                       current
		//  <---------------------------------->        <------------------------->
		//   already processed;                          current applies costs here
		//   have histories which are used to
		//   create history of current                       no history

		private int                            n;

		private CodingStateImmutablePool   pool;


		public ShortCodingSearchWindow( int n, Literal literal, LengthInfo lengthInfo, int dataLengthBits, Level level, int expDistSlotCount ) : base( 2*n+1 )
		{
			this.n        = n;
			pool          = new CodingStateImmutablePool( n+1, lengthInfo, literal, dataLengthBits, level, expDistSlotCount );

			for ( int i=0 ; i<n ; i++ )
				AddAtEnd( new FindPathInfo() );   // add empty items
			AddAtEnd( new FindPathInfo(){ Costs2=CodingCosts.Null, History=pool.CreateEmpty(literal,lengthInfo) } );
			for ( int i=0 ; i<n ; i++ )
				AddAtEnd( new FindPathInfo() );   // add empty items

			for ( int i=0 ; i<=n ; i++ )
				this[i+n].Index = i;
		}


		public FindPathInfo Current
		{
			get{ return this[n]; }
		}


		public FindPathInfo GetSuccessor( int stepsToRight )
		{	
			return this[n+stepsToRight];
		}


		public FindPathInfo GetPredecessor( int stepsToLeft )
		{	
			return this[n-stepsToLeft];
		}


		public void UpdateMinCostsRight( int rightPos, CodingCosts costsRight )
		{
			while( --rightPos!=0 && this[rightPos+n].MinCostsRight>costsRight )
				this[rightPos+n].MinCostsRight = costsRight;
		}


		public void MoveRight()
		{
			FindPathInfo fpi = GetAndRemoveFirst();
			fpi.Reuse();
			AddAtEnd( fpi );
			this[this.Count-1].Index = this[this.Count-2].Index + 1;
		}


		public void Dispose()
		{
			while ( Count > 0 )
				GetAndRemoveFirst().Dispose();
		}


#if DEBUG
		public FindPathInfo Get( int index )
		{
			for ( int i=0 ; i<this.Count ; i++ ) {
				if ( this[i].Index == index )
					return this[i];
			}
			return null;
		}


		public override string ToString()
		{
			string s = "";
			for ( int i=-5 ; i<=5 ; i++ ) {
				 s += this[n+i].ToString() + "\r\n";
				if ( i==-1 || i==0 )
					s += "--------------------\r\n";
			}
			return s;
		}
#endif




	}
}
