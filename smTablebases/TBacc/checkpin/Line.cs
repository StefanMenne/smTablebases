using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TBacc
{
	public enum HalfLineType
	{
		Empty,
		Blck,
		Check,
		Pin
	}

	public struct Line : IComparable<Line>
	{
		private int          left, right;
		private HalfLineType leftType, rightType;

		private static Line[] all;


		
		public Line( int l, int r, HalfLineType lt, HalfLineType rt )
		{
			left      = l;
			right     = r;
			leftType  = lt;
			rightType = rt;
		}

		
		static Line()
		{
			List<Line> list = new List<Line>();

			for ( int l=0 ; l<=7 ; l++ ) {
				for ( int r=0 ; r+l<=7 ; r++ ) {
					foreach ( HalfLineType lt in Enum.GetValues(typeof(HalfLineType)) ) {
						if ( (l==0) != (lt==HalfLineType.Empty) )
							continue;
						if ( l==1 && lt==HalfLineType.Pin )
							continue;
						foreach ( HalfLineType rt in Enum.GetValues(typeof(HalfLineType)) ) {
							if ( (r==0) != (rt==HalfLineType.Empty) )
								continue;
							if ( r==1 && rt==HalfLineType.Pin )
								continue;
							if ( l+r==7 && (lt==HalfLineType.Blck||rt==HalfLineType.Blck) )
								continue;
							list.Add( new Line(l,r,lt,rt) );
						}
					}
				}
			}
#if DEBUG
			if ( list.Count != 165 )
				throw new Exception();
#endif
			list.Sort();
			all = list.ToArray();
		}


		public void Reset()
		{
			left = right = 0;
			leftType = rightType = HalfLineType.Empty;
		}

		public bool IsCheckRight
		{
			get { return rightType == HalfLineType.Check; }
		}

		public bool IsCheckLeft
		{
			get { return leftType == HalfLineType.Check; }
		}

		public bool IsPinRight
		{
			get { return rightType == HalfLineType.Pin; }
		}

		public bool IsPinLeft
		{
			get { return leftType == HalfLineType.Pin; }
		}

		public int Left
		{
			get { return left; }
		}

		public int Right
		{
			get { return right; }
		}

		public void AddSntmBlck( int distToKstm )
		{
			HalfLineType t       = (distToKstm>0) ? rightType : leftType;
			int          dist    = (distToKstm>0) ? right : left;
			int          distAbs = Math.Abs(distToKstm);
			
			if ( t==HalfLineType.Empty || distAbs<dist ) {
				t    = HalfLineType.Blck;
				dist = distAbs; 
			}

			if ( distToKstm<0 ) {
				left     = dist;
				leftType = t;
			}
			else {
				right = dist;
				rightType = t;
			}

			if ( left+right==7 ) {
				if ( leftType==HalfLineType.Blck ) {
					left = 0;
					leftType = HalfLineType.Empty;
				}
				else if ( rightType==HalfLineType.Blck ) {
					right = 0;
					rightType = HalfLineType.Empty;
				}
			}
			else if ( left+right>7 ) {
				left = right = 0;
				leftType = rightType = HalfLineType.Empty;
			}

#if DEBUG
			if ( Config.DebugGeneral && Array.IndexOf<Line>(all,this) == -1 )
				throw new Exception();
#endif
		}


		public void AddSntmCheck( int distToKstm )
		{
			HalfLineType t       = (distToKstm>0) ? rightType : leftType;
			int          dist    = (distToKstm>0) ? right : left;
			int          distAbs = Math.Abs(distToKstm);
			
			if ( t==HalfLineType.Empty || distAbs<dist ) {
				t    = HalfLineType.Check;
				dist = distAbs; 
			}

			if ( distToKstm<0 ) {
				left     = dist;
				leftType = t;
			}
			else {
				right = dist;
				rightType = t;
			}

			if ( left+right==7 ) {
				if ( leftType==HalfLineType.Blck ) {
					left = 0;
					leftType = HalfLineType.Empty;
				}
				else if ( rightType==HalfLineType.Blck ) {
					right = 0;
					rightType = HalfLineType.Empty;
				}
			}
			else if ( left+right>7 ) {
				left = right = 0;
				leftType = rightType = HalfLineType.Empty;
			}

#if DEBUG
			if ( Config.DebugGeneral && Array.IndexOf<Line>(all,this) == -1 )
				throw new Exception();
#endif
		}

		public void AddStm( int distToKstm )
		{
			HalfLineType t       = (distToKstm>0) ? rightType : leftType;
			int          dist    = (distToKstm>0) ? right : left;
			int          distAbs = Math.Abs(distToKstm);
			
			if ( t==HalfLineType.Check && distAbs<dist ) {
				t    = HalfLineType.Pin;
			}
			else if ( t==HalfLineType.Pin && distAbs<dist ) {
				t    = HalfLineType.Empty;
				dist = 0;
			}

			if ( distToKstm<0 ) {
				left     = dist;
				leftType = t;
			}
			else {
				right = dist;
				rightType = t;
			}

			if ( left+right==7 ) {
				if ( leftType==HalfLineType.Blck ) {
					left = 0;
					leftType = HalfLineType.Empty;
				}
				else if ( rightType==HalfLineType.Blck ) {
					right = 0;
					rightType = HalfLineType.Empty;
				}
			}

#if DEBUG
			if ( Config.DebugGeneral && Array.IndexOf<Line>(all,this) == -1 )
				throw new Exception();
#endif
		}

		public int Index
		{
			get { return Array.IndexOf<Line>(all, this); }
		}

		public bool IsCheck
		{
			get{ return leftType==HalfLineType.Check || rightType==HalfLineType.Check; }
		}


		public bool IsDblCheck
		{
			get { return leftType == HalfLineType.Check && rightType == HalfLineType.Check; }
		}

		private int CompareValue
		{
			get{
				if ( IsDblCheck )
					return 3;
				else if ( IsCheck )
					return 2;
				else if ( leftType==HalfLineType.Empty && rightType== HalfLineType.Empty )
					return 0;
				else 
					return 1;
			}
		}

		public int CompareTo( Line other )
		{
			return CompareValue.CompareTo( other.CompareValue );
		}

		public override string ToString()
		{
			return leftType.ToString() + " " + left.ToString() + " / " + rightType.ToString() + " " + right.ToString();
		}

		public static Line Get(int index)
		{
			return all[index];
		}

		public static int Count
		{
			get { return all.Length; }
		}
	}
}
