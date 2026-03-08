


namespace TBacc
{

	public enum MultistepUnblockableCheck
	{
		Q_HorDiagUnblock             =  0,   //  e.g. KQQQK   for piece Grp Q
		Q_HorUnblock                 =  1,   //  e.g. KQQRK   for Piece Grp Q
		Q_DiagUnblock                =  2,   //  e.g. KQBBK   for Piece Grp Q
		Q_HorUnblockLine1and8        =  3,   //  e.g. KQPKP   for Piece Grp Q
		R_HorUnblock                 =  4,   //  e.g. KQRK    for Piece Grp R
		R_HorUnblockLine1and8        =  5,   //  e.g  KQRPK   for Piece Grp R
		B_DiagUnblock                =  6,   //  e.g. KQQBBBK for Piece Grp B

		// only one step unblockable check
		Q1                           =  7,   
		R1                           =  8,
		B1                           =  9,
		N1                           = 10,
		P1                           = 11,

		// no unblockable check; only overlapping removed
		Q0                           = 12,   
		R0                           = 13,
		B0                           = 14,
		N0                           = 15,
		P0                           = 16,
	}


	public class IdxFldTables : IEqualityComparer<IdxFldTables>
	{
		public byte[] IndexToField;
		public byte[] FieldToIndex;
		public BitBrd NotAllowedFields;

		bool IEqualityComparer<IdxFldTables>.Equals( IdxFldTables a, IdxFldTables b )
		{
			if ( a.NotAllowedFields.Value != b.NotAllowedFields.Value )
				return false;
			for ( int i=0 ; i<a.FieldToIndex.Length ; i++ ) {
				if ( a.FieldToIndex[i] != b.FieldToIndex[i] )
					return false;
			}
			return true;				
		}


		int IEqualityComparer<IdxFldTables>.GetHashCode( IdxFldTables a )
		{
			return a.NotAllowedFields.Value.GetHashCode();
		}
	}



	/// <summary>
	/// Contains index to fld and fld to index tables.
	/// Unblockable check and overlapping with one K are removed.
	/// </summary>
	public static class PieceGroupIndexTables
	{
		public const int IndexReorderType   = 0;    // stored in TB header; for future use

		private static readonly int reuseCounter = 0;



		// memory consumption: (1806 * 17 * 2) * 64Byte * 2 = 7.859.712 Byte
		// default tables:  ensures that if indices are sorted fields are also sorted
		private static byte[,,][]     fieldToIndexTables    = new byte[1806,17,2][];
		private static byte[,,][]     indexToFieldTables    = new byte[1806,17,2][];            
		

		// tables for compression; memory consumption: another 7.859.712 bytes
		// if indices sorted fields might not be sorted
		// indices are sorted for better compression; neighbour indices will lead to similar positions
		private static byte[,,][]     fieldToIndexTablesComp    = new byte[1806,17,2][];
		private static byte[,,][]     indexToFieldTablesComp    = new byte[1806,17,2][];            

		private static byte[] identity64 = new byte[64];

		static PieceGroupIndexTables()
		{
			// first piece indicates the piece for the PieceGroup
			// the second piece gives the type of moves where check cannot be blocked by any piece  
			Piece[] unblockableCheckEnumToPieceCheck1AndCheckX = new Piece[]{ 
				Piece.Q,  Piece.Q,  Piece.Q,              // Q_HorDiagUnblock
				Piece.Q,  Piece.Q,  Piece.R,              // Q_HorUnblock    
				Piece.Q,  Piece.Q,  Piece.B,              // Q_DiagUnblock     
				Piece.Q,  Piece.Q,  Piece.R,              // Q_HorUnblockLine1and8  
				Piece.R,  Piece.R,  Piece.R,              // R_HorUnblock           
				Piece.R,  Piece.R,  Piece.R,              // R_HorUnblockLine1and8        
				Piece.B,  Piece.B,  Piece.B,              // B_DiagUnblock                
				Piece.Q,  Piece.Q,  Piece.K,              // Q1                           
				Piece.R,  Piece.R,  Piece.K,              // R1                           
				Piece.B,  Piece.B,  Piece.K,              // B1                           
				Piece.N,  Piece.N,  Piece.K,              // N1                           
				Piece.PW, Piece.PW, Piece.K,              // P1                                                   
				Piece.Q,  Piece.K,  Piece.K,              // Q0         
				Piece.R,  Piece.K,  Piece.K,              // R0                           
				Piece.B,  Piece.K,  Piece.K,              // B0                           
				Piece.N,  Piece.K,  Piece.K,              // N0                           
				Piece.PW, Piece.K,  Piece.K,              // P0                                                   
			};

			Field[] fTmp = new Field[64], fSpider = new Field[64], fSnake = new Field[64], fB = new Field[64];
			Field[] fKnightTour = new Field[]{ Field.A1,Field.B3,Field.A5,Field.B7,Field.D8,Field.C6,Field.E5,Field.C4,Field.E3,Field.D5,Field.C3,Field.E4,Field.C5,Field.D3,Field.F4,Field.H3,Field.G1,Field.E2,Field.C1,Field.A2,Field.B4,Field.A6,Field.B8,Field.D7,Field.F8,Field.E6,Field.G7,Field.H5,Field.G3,Field.H1,Field.F2,Field.D1,Field.B2,Field.A4,Field.B6,Field.A8,Field.C7,Field.E8,Field.D6,Field.F5,Field.D4,Field.F3,Field.G5,Field.H7,Field.F6,Field.G8,Field.E7,Field.C8,Field.A7,Field.B5,Field.A3,Field.B1,Field.D2,Field.F1,Field.H2,Field.G4,Field.H6,Field.F7,Field.H8,Field.G6,Field.H4,Field.G2,Field.E1,Field.C2};
			GetFieldsSpider( fSpider );
			GetFieldsSnake( fSnake );
			GetFieldsB( fB );
			Dictionary<IdxFldTables,IdxFldTables> dict = new Dictionary<IdxFldTables,IdxFldTables>( (IEqualityComparer<IdxFldTables>)(new IdxFldTables()) );  // to save memory; reuse same tables
			for ( WkBk wkBk=WkBk.First(true) ; wkBk<wkBk.Count ; wkBk++ ) {
				for ( int i=0 ; i<17 ; i++ ) {
					MultistepUnblockableCheck m = (MultistepUnblockableCheck)i;
					foreach( bool pIsW in Tools.BoolArray ) {
						Piece pType     = unblockableCheckEnumToPieceCheck1AndCheckX[3*((int)m)  ];
						Piece pTypeCheck1 = unblockableCheckEnumToPieceCheck1AndCheckX[3*((int)m)+1];
						if ( pTypeCheck1.IsP && !pIsW ) // table always contains PW
							pTypeCheck1 = Piece.PB;
						Piece pTypeCheckX = unblockableCheckEnumToPieceCheck1AndCheckX[3*((int)m)+2];
						Field k  = ( pIsW ? (wkBk.Bk) : (wkBk.Wk) );
						Field k2 = ( pIsW ? (wkBk.Wk) : (wkBk.Bk) );
						
						BitBrd mvBitsX        = pTypeCheckX.IsK ? BitBrd.Empty : pTypeCheckX.GetCapBackBits(k);
						BitBrd mvBits1        = pTypeCheck1.GetCapBackBitsInclProm(k) & (BitBrd.GetFldNeighbour(k)|BitBrd.GetMvsN(k));
						if ( m==MultistepUnblockableCheck.Q_HorUnblockLine1and8 || m==MultistepUnblockableCheck.R_HorUnblockLine1and8 ) {
							if ( k.Y == 7 )
								mvBitsX = BitBrd.BorderTop;
							else if ( k.Y == 0 )
								mvBitsX = BitBrd.BorderBottom;
							else
								mvBitsX = BitBrd.Empty;
						}
						mvBitsX &= MoveCheck.PiecePosAnotherPiecePos_To_FieldsBehindInverse( k, k2 );
						BitBrd notAllowedFlds = wkBk.Bits;						

						if ( m < MultistepUnblockableCheck.Q0 )
							notAllowedFlds |= mvBitsX|mvBits1;
						if ( pType.IsP ) {
							notAllowedFlds |= BitBrd.Line1AndLine8;
						}
						IdxFldTables idxFldTables = CalcPieceToIndexAndIndexToPieceTable( notAllowedFlds );
						if ( dict.ContainsKey(idxFldTables) ) { 
							idxFldTables = dict[idxFldTables];
							reuseCounter++;
						}
						else
							dict.Add(idxFldTables,idxFldTables);
						fieldToIndexTables[wkBk.Index, i, (pIsW?0:1)] = idxFldTables.FieldToIndex;
						indexToFieldTables[wkBk.Index, i, (pIsW?0:1)] = idxFldTables.IndexToField;
						idxFldTables = CalcPieceToIndexAndIndexToPieceTableComp( notAllowedFlds, wkBk, pType, pIsW, m<MultistepUnblockableCheck.Q0, fTmp, fSnake, fSpider, fB, fKnightTour );
						if ( dict.ContainsKey(idxFldTables) ) { 
							idxFldTables = dict[idxFldTables];
							reuseCounter++;
						}
						else
							dict.Add(idxFldTables,idxFldTables);
						fieldToIndexTablesComp[wkBk.Index, i, (pIsW?0:1)] = idxFldTables.FieldToIndex;
						indexToFieldTablesComp[wkBk.Index, i, (pIsW?0:1)] = idxFldTables.IndexToField;
					}
				}
			}
			for ( int i=0 ; i<identity64.Length ; i++ )
				identity64[i] = (byte)i;
		}


		public static string GetPrecalcInfo()
		{
			int tablesCount = 1806 * 17 * 2 * 2;
			return "PieceGroupIndexTables:   array entries: " + tablesCount.ToString() + "   allocated instances: " + (tablesCount-reuseCounter).ToString();
		}


		private static IdxFldTables CalcPieceToIndexAndIndexToPieceTable( BitBrd illFields )
		{
			byte[]     pieceToIndex     = new byte[64];
			List<byte> indexToPieceList = new List<byte>(64);

			for ( Field f = Field.A1 ; f<Field.Count ; f++ ) {
				if ( illFields.ContainsNot(f) ) {
					pieceToIndex[f.Value] = (byte)indexToPieceList.Count;
					indexToPieceList.Add( (byte)f.Value );
				}
				else 
					pieceToIndex[ f.Value ] = (byte)255;
			}

			return new IdxFldTables(){ NotAllowedFields=illFields,IndexToField=indexToPieceList.ToArray(), FieldToIndex=pieceToIndex };
		}


		private static IdxFldTables CalcPieceToIndexAndIndexToPieceTableComp( BitBrd illFields, WkBk wkBk, Piece pieceType, bool pIsW, bool pieceIsStm, Field[] fields, Field[] fSnake, Field[] fSpider, Field[] fB, Field[] fKnightTour )
		{
			byte[]     pieceToIndex     = new byte[64];
			List<byte> indexToPieceList = new List<byte>(64);

			GetFields( fields, wkBk, pieceType, pIsW, pieceIsStm, fSnake, fSpider, fB, fKnightTour );

			for ( int i=0 ; i<fields.Length ; i++ ) {
				Field f = fields[i];
				if ( illFields.ContainsNot(f) ) {
					pieceToIndex[f.Value] = (byte)indexToPieceList.Count;
					indexToPieceList.Add( (byte)f.Value );
				}
				else 
					pieceToIndex[ f.Value ] = (byte)255;
			}
			return new IdxFldTables(){ NotAllowedFields=illFields,IndexToField=indexToPieceList.ToArray(), FieldToIndex=pieceToIndex };
		}


		/// <summary>
		/// Get 64 Fields that are used for the order. Illegal fields will be removed later.
		/// </summary>
		private static void GetFields( Field[] f, WkBk wkBk, Piece pieceType, bool pIsW, bool pieceIsStm, Field[] fSnake, Field[] fSpider, Field[] fB, Field[] fKnightTour )
		{
			int      count   = 0;
			BitBrd   fld     = new BitBrd();
			Field    kOpp    = pIsW ? wkBk.Bk : wkBk.Wk;
			Field    kOwn    = pIsW ? wkBk.Wk : wkBk.Bk;

			BitBrd   kOppMv  = Piece.K.GetMvBits(kOpp);                         // 8 neighbour fields of opposite k
			BitBrd   kk      = Piece.K.GetMvBits(kOpp) & Piece.K.GetMvBits(kOwn); // fields covered by both k


			if ( pieceIsStm ) {
				if ( pieceType.IsQ ) {
					// this optimization saves only 0.1% for all 3,4,5-Men Tablebases
					// for 3 and 4 men Tablebases the size even increases
					// for most 5 men tablebase the size decreases
					// it seems as bigger the file as bigger is the benefit 
					AddFieldsThatCanMvTo( kOpp, pieceType, f, ref count, ref fld );                         // Q gives check
					AddFieldsThatCanMvTo( kk, pieceType, f, ref count, ref fld );            // Q can mv directly beside opp k and cannot be captured
					AddFieldsThatCanMvTo( kOppMv&~kk, pieceType, f, ref count, ref fld );    // Q controls k-neighbour fields
					AddFields( BitBrd.All, f, ref count, ref fld );                     // add remaining fields
				}
				//else if ( pieceType.IsR ) {
					//AddFields( BitBrd.All, f, ref count, ref fld );
					
					//even more worse than snake
					//AddFields( fSpider, f, ref count, ref fld );
					
					//not better than default ordering; nearly every file is a little bit bigger					
					//AddFields( fSnake, f, ref count, ref fld );
					
					//not better than default ordering
					//for ( int i=0 ; i<8 ; i++ ) {
					//	if ( kOpp.X + i < 8 )
					//		AddFields( BitBrd.Row(kOpp.X+i), f, ref count, ref fld );
					//	if ( kOpp.X >= i )
					//		AddFields( BitBrd.Row(kOpp.X-i), f, ref count, ref fld );
					//	if ( kOpp.Y + i < 8 )
					//		AddFields( BitBrd.Line(kOpp.Y+i), f, ref count, ref fld );
					//	if ( kOpp.Y >= i )
					//		AddFields( BitBrd.Line(kOpp.Y-i), f, ref count, ref fld );
					//}
				//}
				else if ( pieceType.IsB ) {
					AddFields( fB, f, ref count, ref fld );
				}
				//else if ( pieceType.IsN ) {
				//	AddFields( BitBrd.All, f, ref count, ref fld );

					//AddFields( fKnightTour, f, ref count, ref fld );

					//AddFieldsThatCanMvTo( kOpp,           pieceType, f, ref count, ref fld );      // N gives check
					//AddFieldsThatCanMvTo( BitBrd.Center,  pieceType, f, ref count, ref fld );      // N can mv center (4 fields)
					//AddFieldsThatCanMvTo( BitBrd.Center2, pieceType, f, ref count, ref fld );      // N can mv center2 (12 fields)
					//AddFields( Field.A1.AsBit | Field.A8.AsBit | Field.H1.AsBit | Field.H1.AsBit, f, ref count, ref fld );  // add remaining 4 fields

					//AddFieldsThatCanMvTo( BitBrd.Center,  pieceType, f, ref count, ref fld );      // N can mv center (4 fields)
					//AddFieldsThatCanMvTo( BitBrd.Center2, pieceType, f, ref count, ref fld );      // N can mv center2 (12 fields)
					//AddFields( Field.A1.AsBit | Field.A8.AsBit | Field.H1.AsBit | Field.H1.AsBit, f, ref count, ref fld );  // add remaining 4 fields
				//}
				else if ( pieceType.IsP ) {   // for P most important is distance to promotion; same reordering for Stm and nonStm
					AddFields( fSnake, f, ref count, ref fld );
				}
				else {
					AddFields( BitBrd.All, f, ref count, ref fld );
				}
			}
			else {
				// piece is not to move. 
				//if ( pieceType.IsQ ) {
					// same optimizations as stm does not improve size
					//AddFields( BitBrd.All, f, ref count, ref fld );                        // add remaining fields
				//}
				//else if ( pieceType.IsR ) {
					//for ( int i=0 ; i<8 ; i++ ) {
					//	if ( kOpp.X + i < 8 )
					//		AddFields( BitBrd.Row(kOpp.X+i), f, ref count, ref fld );
					//	if ( kOpp.X >= i )
					//		AddFields( BitBrd.Row(kOpp.X-i), f, ref count, ref fld );
					//	if ( kOpp.Y + i < 8 )
					//		AddFields( BitBrd.Line(kOpp.Y+i), f, ref count, ref fld );
					//	if ( kOpp.Y >= i )
					//		AddFields( BitBrd.Line(kOpp.Y-i), f, ref count, ref fld );
					//}
				//}
				//else if ( pieceType.IsB ) {
				//	AddFields( fB, f, ref count, ref fld );
				//}
				//else if ( pieceType.IsN ) {
					//AddFields( fKnightTour, f, ref count, ref fld );

					//AddFieldsThatCanMvTo( kOpp,           pieceType, f, ref count, ref fld );      // N gives check
					//AddFieldsThatCanMvTo( BitBrd.Center,  pieceType, f, ref count, ref fld );      // N can mv center (4 fields)
					//AddFieldsThatCanMvTo( BitBrd.Center2, pieceType, f, ref count, ref fld );      // N can mv center2 (12 fields)
					//AddFields( Field.A1.AsBit | Field.A8.AsBit | Field.H1.AsBit | Field.H1.AsBit, f, ref count, ref fld );  // add remaining 4 fields

					//AddFieldsThatCanMvTo( BitBrd.Center,  pieceType, f, ref count, ref fld );      // N can mv center (4 fields)
					//AddFieldsThatCanMvTo( BitBrd.Center2, pieceType, f, ref count, ref fld );      // N can mv center2 (12 fields)
					//AddFields( Field.A1.AsBit | Field.A8.AsBit | Field.H1.AsBit | Field.H1.AsBit, f, ref count, ref fld );  // add remaining 4 fields
				//}
				//else 
				if ( pieceType.IsP ) {   // for P most importand is distance to promotion; same reordering for Stm and nonStm
					AddFields( fSnake, f, ref count, ref fld );
				}
				else {
					AddFields( BitBrd.All, f, ref count, ref fld );
				}
			}


#if DEBUG
			//BitBrd bb = new BitBrd();
			//for ( int i=0 ; i<f.Length ; i++ )
			//	bb |= f[i].AsBit;
			//if ( bb != BitBrd.All )
			//	throw new Exception();
#endif 
		}


		/// <summary>
		/// -------\
		/// /------/
		/// \------\
		/// /------/
		/// \------\
		/// /------/
		/// \------\
		/// -------/
		/// </summary>
		/// <param name="f"></param>
		private static void GetFieldsSnake( Field[] f )
		{
			int count = 0;
			for ( int y=0 ; y<8 ; y++ ) {
				for ( int x=0 ; x<8 ; x++ ) {
					f[count++] = new Field( ((y%2==0)?(7-x):x), y );
				}
			}
		}


		private static void GetFieldsB( Field[] f )
		{
			// reordering for b
			Field[] fa = new Field[]{ Field.A1, Field.B2, Field.C3, Field.D4, Field.H8, Field.G7, Field.F6, Field.E5, Field.B8, Field.C7, Field.D6, Field.H2, Field.G3, Field.F4, Field.H6, Field.G5, Field.C1, Field.D2, Field.E3, Field.G1, Field.A7, Field.B6, Field.C5, Field.F8, Field.E7, Field.A3, Field.B4, Field.A5, Field.E1, Field.F2, Field.H4, Field.D8 };
			for ( int i=0 ; i<fa.Length ; i++ ) {
				f[i]    = fa[i];
				f[32+i] = fa[i].Mirror(MirrorType.MirrorOnVertical);
			}

			/*
			// Simple but not as good as manually created above
			for ( int x=0 ; x<8 ; x+=2 ) {
				for (int y = 0; y < 8; y++) {
					f[count]      = new Field( (x+y)%8,     y );
					f[32+count++] = new Field( 7-((x+y)%8), y );
				}
			}
			*/
		}




		/// <summary>
		/// +-------+
		/// |+-----+|
		/// ||+---+||
		/// |||+-+|||
		/// |||++||||
		/// ||+--+|||
		/// |+----+||
		/// +------+|
		/// --------+
		/// </summary>
		/// <param name="f"></param>
		private static void GetFieldsSpider( Field[] f )
		{
			// spirale
			int count = 0;
			Field current = Field.A1;
			f[count++] = current;
			for ( int dir=0 ; dir<15 ; dir++ ) {
				do {
					current = current + new int[]{1,8,-1,-8}[dir%4];
					f[count++] = current;
				} while ( current.X+current.Y!=7 && (current.Y-current.X)!=(1-(current.X/4)) );
			}
		}


		private static void AddFieldsThatCanMvTo( Field dst, Piece pieceType, Field[] f, ref int count, ref BitBrd addedFields )
		{
			for ( int dir=0 ; dir<pieceType.Delta.Length ; dir++ ) {
				Field c = dst;
				while ( !Piece.IsMvToOutside(c,pieceType.DeltaX[dir],pieceType.DeltaY[dir]) ) {
					c = c + pieceType.Delta[dir];
					if ( addedFields.ContainsNot(c) ) {
						f[count++] = c;
						addedFields |= c;
					}						
				}
			}
		}


		private static void AddFieldsThatCanMvTo( BitBrd dst, Piece pieceType, Field[] f, ref int count, ref BitBrd addedFields )
		{
			while( dst.IsNotEmpty ) {
				Field  ff = dst.LowestField;
				AddFieldsThatCanMvTo( ff, pieceType, f, ref count, ref addedFields );
				dst=dst.XorField(ff);
			}
		}


		private static void AddFields( BitBrd mvToAdd, Field[] allFields, ref int count, ref BitBrd allFieldsBitBrd )
		{
			mvToAdd          = mvToAdd & ~allFieldsBitBrd;
			allFieldsBitBrd |= mvToAdd;

			while ( mvToAdd.IsNotEmpty ) {
                Field fCurr  = mvToAdd.LowestField;
				allFields[count++]  = fCurr;
				mvToAdd=mvToAdd.XorField(fCurr);
			}
		}


		private static void AddFields( Field[] fToAdd, Field[] allFields, ref int count, ref BitBrd allFieldsBitBrd )
		{
			for ( int i=0 ; i < fToAdd.Length ; i++ ) {
				Field fta = fToAdd[i];
				if ( allFieldsBitBrd.ContainsNot(fta) ) {
					allFields[count++] = fta;
					allFieldsBitBrd |= fta;			
				}
			}
		}


		public static void Get( Pieces p, WkBk wkBk, Piece pieceType, bool pieceIsW, bool wtm, out byte[] indexToField, out byte[] fieldToIndex )
		{
			MultistepUnblockableCheck m = GetMultistepUnblockableCheck( p, pieceType, pieceIsW, wtm );
			
			indexToField = indexToFieldTables[wkBk.Index, (int)m, (pieceIsW ? 0 : 1)];
			fieldToIndex = fieldToIndexTables[wkBk.Index, (int)m, (pieceIsW ? 0 : 1)];
		}


		public static void GetComp( Pieces p, WkBk wkBk, Piece pieceType, bool pieceIsW, bool wtm, out byte[] indexToField, out byte[] fieldToIndex )
		{
			MultistepUnblockableCheck m = GetMultistepUnblockableCheck( p, pieceType, pieceIsW, wtm );
			
			indexToField = indexToFieldTablesComp[wkBk.Index, (int)m, (pieceIsW ? 0 : 1)];
			fieldToIndex = fieldToIndexTablesComp[wkBk.Index, (int)m, (pieceIsW ? 0 : 1)];
		}


		public static void Get64( Pieces p, WkBk wkBk, Piece pieceType, bool pieceIsW, bool wtm, out byte[] indexToField, out byte[] fieldToIndex )
		{
			indexToField = identity64;
			fieldToIndex = identity64;
		}


		private static MultistepUnblockableCheck GetMultistepUnblockableCheck( Pieces p, Piece piece, bool pieceIsW, bool wtm )
		{
			bool qStm = false, rStm = false, bStm=false, nStm_qSntm_rSntm_bSntm_nSntm = false, pStm_pSntm=false;

			if ( pieceIsW != wtm )
				return MultistepUnblockableCheck.Q0 + piece.AsInt3-1;

			for ( int i=0 ; i<p.PieceCount ; i++ ) {
				bool crntPieceIsW =  i<p.CountW;
				Piece crntP = p.GetPieceType(i);

				if ( crntPieceIsW == pieceIsW ) {
					qStm |= crntP.IsQ;
					rStm |= crntP.IsR;
					bStm |= crntP.IsB;
					nStm_qSntm_rSntm_bSntm_nSntm |= crntP.IsN;
					pStm_pSntm |= crntP.IsP;
				}
				else {
					nStm_qSntm_rSntm_bSntm_nSntm |= crntP.IsQ||crntP.IsR||crntP.IsB||crntP.IsN;
					pStm_pSntm |= crntP.IsP;
				}
			}


			if ( nStm_qSntm_rSntm_bSntm_nSntm || (rStm&&bStm) || (bStm&&pStm_pSntm) )
				return GetMultistepUnblockableCheck1( piece );
			else if ( piece.IsQ ) {
				if ( pStm_pSntm )
					return MultistepUnblockableCheck.Q_HorUnblockLine1and8;
				else {
					if ( bStm )
						return MultistepUnblockableCheck.Q_DiagUnblock;
					else if ( rStm )
						return MultistepUnblockableCheck.Q_HorUnblock;
					else if ( !bStm && !rStm )
						return MultistepUnblockableCheck.Q_HorDiagUnblock;
					else
						throw new Exception();
				}
			}
			else if ( piece.IsR ) {
				if ( pStm_pSntm )
					return MultistepUnblockableCheck.R_HorUnblockLine1and8;
				else 
					return MultistepUnblockableCheck.R_HorUnblock;
			}
			else if ( piece.IsB ) {
				return MultistepUnblockableCheck.B_DiagUnblock;
			}
			else 
				return GetMultistepUnblockableCheck1( piece );
		}


		private static MultistepUnblockableCheck GetMultistepUnblockableCheck1( Piece p )
		{
			return (MultistepUnblockableCheck)(p.AsInt3 + 6);
		}
    }
}
