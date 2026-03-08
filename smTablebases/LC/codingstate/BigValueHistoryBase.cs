﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
	public class BigValueHistoryBase
	{
		protected int[]                values;
		protected int[]                occurence;
		protected int                  countDifferentValues;
		protected int                  firstOccurenceOneIndex;
		protected int                  sumOccurenceTwoOrHigher;   // sum of all occurence which have at least value = 2


		public static bool Compare( BigValueHistoryBase h1, BigValueHistoryBase h2 )
		{
			if ( h1.countDifferentValues != h2.countDifferentValues || h1.sumOccurenceTwoOrHigher != h2.sumOccurenceTwoOrHigher )
				return false;

			for ( int i=0 ; i<h1.countDifferentValues ; i++ ) {
				if ( h1.values[i] != h2.values[i] || h1.occurence[i] != h2.occurence[i] )
					return false;
			}
			return true;
		}


		public int SumOccurenceTwoOrHigher
		{
			get{ return sumOccurenceTwoOrHigher; }
		}


		public int FirstOccurenceOneIndex
		{
			get { return firstOccurenceOneIndex; }
		}


		public int CountDifferentValues
		{
			get { return countDifferentValues; }
		}


		public int GetValue( int index )
		{
			return values[index];
		}


		public int[] Occurence
		{
			get { return occurence; }
		}


		public override string ToString()
		{
			string s="";

			for ( int i=0 ; i<Math.Min(countDifferentValues,20) ; i++ )
				s += values[i].ToString() + "(" + occurence[i] + "), ";

			if ( s.Length > 2 )
				s = s.Remove(s.Length-2,2);

			if ( countDifferentValues > 20 )
				s += "...";

			return s;
		}


		protected void Init( int windowSize, InitValues initValues, int minOccurrence )
		{
			this.countDifferentValues  = initValues.GetCountDifferentValues();
			Array.Clear( values, 0, values.Length );
			Array.Clear( occurence, 0, occurence.Length );

			for ( int i=0 ; i<initValues.GetCountDifferentValues() ; i++ )
				values[i]     = initValues.RankToValue(i);
			for ( int i=0 ; i<windowSize ; i++ )
				occurence[initValues.IndexToRank(i)]++;

			sumOccurenceTwoOrHigher = 0;
			firstOccurenceOneIndex = 0;
			while ( firstOccurenceOneIndex<occurence.Length && occurence[firstOccurenceOneIndex]>=minOccurrence ) {
				sumOccurenceTwoOrHigher += occurence[firstOccurenceOneIndex];
				firstOccurenceOneIndex++;
			}
		}


		/// <summary>
		/// Updates Occurence and restore sorting.
		/// </summary>
		/// <param name="valueToAdd">Value to add</param>
		/// <param name="indexToAdd">Current rank of value to add or -1 if not yet contained.</param>
		/// <param name="valueToRemove">Value to remove</param>
		/// <param name="indexToRemove">Current rank of value to remove.</param>
		protected void UpdateOccurence( int valueToAdd, int indexToAdd, int valueToRemove, int indexToRemove, int minOcc )
		{
			if ( indexToAdd == -1 ) {
				values[countDifferentValues]       = valueToAdd;
				occurence[countDifferentValues]    = 0;
				indexToAdd = countDifferentValues++;
				ValueAdded( valueToAdd, indexToAdd );
			}
#if DEBUG
			int debugAddCountBefore = occurence[indexToAdd];
#endif 
			if ( occurence[indexToAdd] + 1 == occurence[indexToRemove] ) {
				SwapValues( indexToAdd, indexToRemove );
				if ( occurence[countDifferentValues-1] == 0 ) {
					countDifferentValues--;
					ValueRemoved( valueToRemove, indexToAdd/* indexToAdd and indexToRemoved are switched before */ );
				}
				return;				
			}

			occurence[indexToAdd]++;

			// restore sorting; update sumLeft, sumLeftIndex and countDifferentValues
			int indexToAddNew = indexToAdd;
			while ( indexToAddNew>0 && occurence[indexToAddNew-1]<occurence[indexToAdd] )
				indexToAddNew--;
			if ( indexToAddNew != indexToAdd ) {
				Swap( indexToAdd, indexToAddNew );
				if ( indexToAddNew == indexToRemove )   // rare case that added value is switched with removed value
					indexToRemove = indexToAdd;
			}

			if ( occurence[indexToAddNew] == minOcc ) {
				firstOccurenceOneIndex++;
				sumOccurenceTwoOrHigher += minOcc;
			}
			else if ( occurence[indexToAddNew] > minOcc )
				sumOccurenceTwoOrHigher++;


			occurence[indexToRemove]--;

			int indexToRemoveNew = indexToRemove; 
			while ( indexToRemoveNew<countDifferentValues-1 && occurence[indexToRemoveNew+1]>occurence[indexToRemove] )
				indexToRemoveNew++;
			if ( indexToRemoveNew != indexToRemove )
				Swap( indexToRemove, indexToRemoveNew );

			if ( occurence[indexToRemoveNew] == 0 ) {
				countDifferentValues--;
				ValueRemoved( valueToRemove, indexToRemoveNew );
			}
			else if ( occurence[indexToRemoveNew] == minOcc-1 ) {
				sumOccurenceTwoOrHigher -= minOcc;
				firstOccurenceOneIndex--;
			}
			else if ( occurence[indexToRemoveNew] >= minOcc )
				sumOccurenceTwoOrHigher--;

		
#if DEBUG
			int sumVerify = 0 ;
			for ( int i=0 ; i<firstOccurenceOneIndex && i<countDifferentValues ; i++ )  {
				sumVerify += occurence[i];
				if ( occurence[i] == 0 )
					throw new Exception();
			}
			if ( sumOccurenceTwoOrHigher != sumVerify )
				throw new Exception();
			for ( int i=firstOccurenceOneIndex ; i<countDifferentValues ; i++ ) {
				sumVerify += occurence[i];
				if ( occurence[i] == 0 )
					throw new Exception();
			}
			if ( sumVerify != SettingsFix.HistoryDistWindowSize )
				throw new Exception();
			bool addedValueFound = false;
			for ( int i=0 ; i<countDifferentValues ; i++ ) {
				if ( values[i]==valueToAdd ) {
					addedValueFound = true;
					if ( occurence[i] != debugAddCountBefore + 1 )
						throw new Exception();
				}
				if ( i!=0 && occurence[i-1] < occurence[i] )
					throw new Exception();
			}
			if ( !addedValueFound )
				throw new Exception();
#endif
		}


		private void Swap( int i, int j )
		{
			int tmp      = occurence[i];
			occurence[i] = occurence[j];
			occurence[j] = tmp;
			SwapValues( i, j );
		}


		protected virtual void SwapValues( int i, int j )
		{
			int tmp      = values[i];
			values[i]    = values[j];
			values[j]    = tmp;
		}


		public int RankToValue( int rank )
		{
			return values[rank];
		}


		public virtual int ValueToRank( int value, int maxRank )
		{
			throw new Exception();
		}


		protected virtual void ValueAdded( int value, int index )
		{
		}


		protected virtual void ValueRemoved( int value, int index )
		{
		}


#if DEBUG
		public void CalcMd5( MD5 md5 )
		{
			byte[] b = new byte[Math.Max(4*countDifferentValues,12)];
			Buffer.BlockCopy( values, 0, b, 0, 4*countDifferentValues );
			md5.TransformBlock( b, 0, 4*countDifferentValues, b, 0 );
			Buffer.BlockCopy( occurence, 0, b, 0, 4*countDifferentValues );
			md5.TransformBlock( b, 0, 4*countDifferentValues, b, 0 );
			Buffer.BlockCopy( BitConverter.GetBytes( countDifferentValues ), 0, b, 0, 4 );
			Buffer.BlockCopy( BitConverter.GetBytes( firstOccurenceOneIndex ), 0, b, 4, 4 );
			Buffer.BlockCopy( BitConverter.GetBytes( sumOccurenceTwoOrHigher ), 0, b, 8, 4 );
			md5.TransformBlock( b, 0, 12, b, 0 );
		}
#endif

	}
}
