using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{
	// epIndex    LineNr(y)     Line   (P=WP p=BP)                                             LineNr(y)     Line   (P=WP p=BP)
	//
	//    0           4           Pp......              BTM                                       5           pP......              WTM
	//    1           4           pP......              BTM                                       5           Pp......              WTM
	//    2           4           .Pp.....              BTM                                       5           .pP.....              WTM
	//    3           4           .pP.....              BTM                                       5           .Pp.....              WTM
	//    4           4           ..Pp....              BTM                                       5           ..pP....              WTM
	//    5           4           ..pP....              BTM                                       5           ..Pp....              WTM
	//    6           4           ...Pp...              BTM                                       5           ...pP...              WTM
	//    7           4           ...pP...              BTM                                       5           ...Pp...              WTM
	//    8           4           ....Pp..              BTM                                       5           ....pP..              WTM
	//    9           4           ....pP..              BTM                                       5           ....Pp..              WTM
	//   10           4           .....Pp.              BTM                                       5           .....pP.              WTM
	//   11           4           .....pP.              BTM                                       5           .....Pp.              WTM
	//   12           4           ......Pp              BTM                                       5           ......pP              WTM
	//   13           4           ......pP              BTM                                       5           ......Pp              WTM
	//

	public static class EP
	{
		/// <summary>
		/// Returns overlap field to encode this EP. WkBk is needed because not all Fields are allowed due unblockable check.
		/// Three fields will be used as overlapField to encode EP: DblStpSrc, CapDst or DblStpDst
		/// One of the three might not be available due unblockable check.
		/// Normally encode CapSrc=left with DblStpDst and CapSrc=right with DblStpSrc
		/// If k stands in the special line (only line is checked for simplicity) CapDst is used instead.
		/// </summary>
		public static Field GetOverlap( Field dblStepDst, Field capSrc, WkBk wkbk )
		{
			Field kSntm      = GetDblStepPawnIsW(dblStepDst) ? wkbk.Wk : wkbk.Bk;   // k that causes unblockable check
			Field capDst     = GetCapDst( dblStepDst );
			Field dblStepSrc = GetDblStepSrc( dblStepDst );

			if ( ( (dblStepDst.AsBit | capDst.AsBit | dblStepSrc.AsBit | capSrc.AsBit) & wkbk.Bits ).IsNotEmpty )
				return Field.No;

			if ( capSrc < dblStepDst ) {
				if ( kSntm.Y==capDst.Y )     //   check if kSntm is in CapDstLine; if so unblockable check is possible if pawn Stm is put on field DblStpDst 
					return capDst;
				else 
					return dblStepDst;
			}
			else {
				if ( kSntm.Y == (GetDblStepPawnIsW(dblStepDst)?0:7) )     //   check if kSntm is in CapDstLine; if so unblockable check is possible if pawn Stm is put on field DblStpDst 
					return capDst;
				else 
					return dblStepSrc;
			}			 
		}


		public static void Index14ToFields( int ep14Index, bool wtm, out Field dblStepDst, out Field capSrc )
		{
			dblStepDst = new Field( ((ep14Index+1)>>1), wtm?4:3 );
			capSrc     = new Field( (ep14Index>>1) + 1 - (ep14Index%2), (wtm ? 4 : 3) );
		}


		public static bool GetDblStepPawnIsW( Field dblStepDst )
		{
			return dblStepDst.Y==3;
		}


		public static Field GetCapDst( Field dblStepDst )
		{
			return new Field( GetDblStepPawnIsW(dblStepDst) ? (dblStepDst.Value-8) : (dblStepDst.Value+8) ); 
		}


		public static Field GetDblStepSrc( Field dblStepDst )
		{
			return new Field( GetDblStepPawnIsW(dblStepDst) ? (dblStepDst.Value-16) : (dblStepDst.Value+16) ); 
		}


		public static Field GetCapSrcLeft( Field dblStepDst )
		{
			return new Field( dblStepDst.Value - 1 );
		}


		public static Field GetCapSrcRight( Field dblStepDst )
		{
			return new Field( dblStepDst.Value + 1 );
		}


		public static Field GetDblStepDst( Field capDst )
		{
			return (capDst.Y==5) ? (capDst-8) : (capDst+8) ;
		}


		public static bool CapSrcLeftExist( Field dblStepDst )
		{
			return dblStepDst.X!=0;
		}


		public static bool CapSrcRightExist( Field dblStepDst )
		{
			return dblStepDst.X!=7;
		}


		public static Field GetOneCapSrc( Pieces pieces, Field dblStepDst, Fields fields )
		{
			bool wtm = !GetDblStepPawnIsW( dblStepDst );
			Field f = Field.No;
			for ( int i=0 ; i<pieces.PieceCount ; i++ ) {
				if ( pieces.GetPieceType(i).IsP && pieces.IsW(i)==wtm && (  (EP.CapSrcLeftExist(dblStepDst)&&(fields.Get(i)==EP.GetCapSrcLeft(dblStepDst))) || (EP.CapSrcRightExist(dblStepDst)&&(fields.Get(i)==EP.GetCapSrcRight(dblStepDst))) ) ) {
					// cap src found; but return the non redundant 
					if ( f.IsNo || fields.Get(i)<f )
						f = fields.Get(i);
				}
			}
			return f;
		}
	

		public static bool GetSecondCapSrcAvailable( Field epDblStepDst )
		{
			return epDblStepDst.X!=0 && epDblStepDst.X!=7;
		}


		public static Field GetSecondCapSrc( Field dblStepDst, Field capSrc )
		{
			return dblStepDst+(dblStepDst.Value-capSrc.Value); 
		}


		public static Field[] GetEp( Pieces p, Fields f, bool wtm )
		{
			if ( !p.ContainsWpawnAndBpawn )
				return null;
			List<Field> epList = null;

			for ( int i=p.FirstPiece(wtm) ; i<p.LastPiecePlusOne(wtm) ; i++ ) {
				Field epStm = f.Get(i);
				if ( epStm.IsPawnFourthLine(!wtm) && p.GetPieceType(i).IsP ) {
					for ( int j=p.FirstPiece(!wtm) ; j<p.LastPiecePlusOne(!wtm) ; j++ ) {
						Field epDblStepDst = f.Get(j);
						if ( epDblStepDst.Y == epStm.Y && Math.Abs(epStm.X-epDblStepDst.X)==1 && p.GetPieceType(j).IsP ) {
							if ( epList == null )
								epList = new List<Field>();
							Field capDst = EP.GetCapDst(epDblStepDst);
							if ( !epList.Contains(capDst) )
								epList.Add( capDst );
						}
					}
				}
			}
			return epList==null ? null : epList.ToArray();
		}


		public static bool IsEpPos( Field pawnPosAfterDblStep, bool pawnIsW, Fields pawnsForPossibleCapturing, int sntmPawnsCount )
		{
			if ( !pawnsForPossibleCapturing.IsNo ) {   
				if ( pawnPosAfterDblStep.IsPawnFourthLine(pawnIsW) ) {
					for ( int i=0 ; i<sntmPawnsCount ; i++ ) {
						Field fSntmPawn = pawnsForPossibleCapturing.Get(i);
						if ( fSntmPawn.IsPawnFourthLine(pawnIsW) && Math.Abs(fSntmPawn.X-pawnPosAfterDblStep.X)==1 ) {   // ep would be possible; so prevent double step
							return true;
						}
					}
				}
			}
			return false;
		}
	}
}
