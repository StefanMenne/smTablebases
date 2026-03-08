using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	public enum AddPieceType
	{
		Stm,              // side to move;                               possible pin
 		SntmCheck,        // side not to move check giving piece         used also for knight/pawn?? check with dist 1 and orientation Other
		SntmBlck,         // blocking piece                              used also for Stm pieces that are not in the same line
		No
	}


    public struct AddPieceInfo
    {
		public  static AddPieceInfo   Empty, NCheck;
        private static AddPieceInfo[] all;

        private int             distToKstm;
		private AddPieceType      type;


		public AddPieceInfo( int distToKstm, AddPieceType type )
		{
			this.distToKstm      = distToKstm;
			this.type            = type;
		}


        static AddPieceInfo()
        {
			List<AddPieceInfo> list = new List<AddPieceInfo>();

			// generate all possible combinations
			list.Add( Empty = new AddPieceInfo( 100,   AddPieceType.No      )  );            
			list.Add( NCheck  = new AddPieceInfo(   1,   AddPieceType.SntmCheck )  );

			foreach ( int distToKstm in new int[]{ -1, -2, -3, -4, -5, -6, -7, 1, 2, 3, 4, 5, 6, 7 } ) {
				foreach( AddPieceType apt in new AddPieceType[]{ AddPieceType.SntmBlck, AddPieceType.SntmCheck, AddPieceType.Stm } ) {
					if ( ( apt!=AddPieceType.SntmCheck&&Math.Abs(distToKstm)==7 ) )  // no unblockable check  /  irrelevant on border; no pin; no blocking of check possible
						continue;
					list.Add( new AddPieceInfo(distToKstm,apt) ); 
				}
			}
			all = list.ToArray();
        }


		public bool IsEmpty
		{
			get{ return type==AddPieceType.No; }
		}


		public AddPieceType Type
		{
			get{ return type; }
		}


		public int DistToKstm
		{
			get{ return distToKstm; }
		}


		public int Index
		{
			get { 
				return Array.IndexOf<AddPieceInfo>(all, this); 
			}
		}


		public static int Count
		{
			get { return all.Length; }
		}


		public static AddPieceInfo Get( int index )
		{
			return all[index];
		}




		public static AddPieceInfo Get( int distToKstm, AddPieceType type )
		{
			if ( Math.Abs(distToKstm)==7 && type!=AddPieceType.SntmCheck )
				return Empty;   // empty;   irrelevant blocking or Stm piece on the border
			else
				return new AddPieceInfo(distToKstm,type);
		}
    }

}
