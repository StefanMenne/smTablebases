using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TBacc;
using System.IO;



namespace smTablebases
{
	//
	// Some tests for different functionalities.
	// Surely not all are up to date and still working.
	//
	public static class Test
	{
		private static Random random = new Random( 0 );

		public static void Do()
		{
			Message.Line( "Test started" );

			//IndexEnumerate_TestPerformance();
			//IndexEnumerate_TestIncSrcIndex();
			//IndexEnumerate_TestReorder();
			CountValid();
			//TestChunkBlockSplitterWriteCreation();
			//EPTest2();
			//TestPieceGroup2();
			//PerformanceChangeIndex();
			//PerformanceEnumeratePieceGroups();
			//PerformanceEnumerateFields();
			//SnappyTest();
			//Array12BitTest();
			//EPTestSequentiell();
			//EPTest();
			//EPTestSequentiellSpeed();
			//EPTestSpeed();

			Message.Line( "Test finished" );
		}


		private static void TestChunkBlockSplitterWriteCreation()
		{
			for ( int i=1 ; i<Pieces.Count ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );

				PieceGroupReorder pgrWtm = PieceGroupReorder.Get( pieces,  true, PieceGroupReorderType.Random );
				PieceGroupReorder pgrBtm = PieceGroupReorder.Get( pieces, false, PieceGroupReorderType.Random );
				ChunkBlockSplitterWrite chunkBlockSplitter = new ChunkBlockSplitterWrite( pieces, Config.BlockSize, pgrWtm, pgrBtm, true );
			}
		}



		private static void CountValid()
		{
			bool fast = true;
			for ( int i=1 ; i<Pieces.Count ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );

				foreach( bool wtm in Tools.BoolArray ) {
					for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
						if ( fast )
							wkBk = new WkBk( random.Next(WkBk.GetCount(pieces).Index), pieces );
						PieceGroupReorder pgr           = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
						IndexPos        indexPos      = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress, pgr );

						long count1        = TBacc.CountValid.GetValidCount( indexPos );



						long indexCount  = indexPos.IndexCount;
						long count2        = 0;
						for ( int j=0 ; j<indexCount ; j++ ) {
							if ( IsValid(indexPos,j) ) {
								count2++;
							}
						}



//	IndexEnumerator enumValid = new IndexEnumerator( indexPos );
//	enumValid.Reset( 0, true );
//	long count = 0;
//	do {
//		count++;
//		ver[enumValid.IndexSrc] |= 2;
//	} while ( enumValid.NextValid() );
//
//	if ( indexPos.WPawnAndBPawn ) {
//		CheckPin cpStm = new CheckPin( indexPos.WkBk, indexPos.Pieces, indexPos.Wtm ), cpSntm = new CheckPin( indexPos.WkBk, indexPos.Pieces, !indexPos.Wtm );
//		for ( long j=0 ; j<indexCount ; j++ ) {
//			if ( indexPos.GetIsEp(j) ) {
//				indexPos.SetToIndex( j );
//				Field epDblStepDst, epCapSrc;
//				Pos pos = Pos.FromIndexPosEp( indexPos, out epDblStepDst, out epCapSrc );
//				if ( pos.GetIsValid(indexPos.Wtm,epDblStepDst,cpStm,cpSntm) && !indexPos.IsRedundandEpPos() ) {
//					bool isValid = true;
//					for ( int k=0 ; k<pos.Pieces.PieceCount ; k++ ) {
//						if ( pos.Pieces.GetPieceType(k).IsP && pos.GetPiecePos(k).IsLine0Or7 )
//							isValid = false;
//					}
//					for ( int k=0 ; k<indexPos.PieceGroupCount ; k++ ) {
//						Fields f = indexPos.GetPieceGroup(k).GetFields();
//						if ( f.IsNo )
//							isValid = false;
//					}
//
//					if ( isValid ) {
//						count++;
//						ver[j] |= 2;
//					}
//				}
//			}
//		}
//	}

						if ( count1 != count2 ) {
							throw new Exception();
						}


						if ( fast )
							break;
					}
				}
			}
		}


		private static void IndexEnumerate_TestPerformance()
		{
			long dummy = 0;

			for ( int i=1 ; i<10 ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );

				foreach( bool wtm in Tools.BoolArray ) {
					for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
						PieceGroupReorder pgr           = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
						IndexPos        indexPos      = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress, pgr );
						IndexEnumerator enumValid     = new IndexEnumerator( indexPos );
						if ( !enumValid.GetIsValid() )
							enumValid.NextValid();
						do {
							dummy ^= enumValid.Fields.Bits;
						} while ( enumValid.NextValid() );
					}
				}
			}
			Message.Line( dummy.ToString( "X2" ) );
		}


		private static void IndexEnumerate_TestNextValid()
		{
			bool    fast                 = true;
			bool    skip                 = true;

			for ( int i=1 ; i<Pieces.Count ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );

				foreach( bool wtm in Tools.BoolArray ) {
					for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
						if ( fast )
							wkBk = new WkBk( random.Next(WkBk.GetCount(pieces).Index), pieces );
						PieceGroupReorder                   pgr           = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
						//IndexPos                          indexPos      = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress64, pgr );
						//IndexPos                          indexPosVer   = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress64, pgr );
						IndexPos                          indexPos      = new IndexPos( wkBk, pieces, wtm );
						IndexPos                          indexPosVer   = new IndexPos( wkBk, pieces, wtm );
						IndexEnumerator    enumValid     = new IndexEnumerator( indexPos );
						long indexCount = indexPosVer.IndexCount;
						long skipCount = skip ? random.Next( (int)indexCount ) : 0;
						enumValid.Reset( skipCount, true );
						long index, indexVer=skipCount;

						do {
							index = indexPos.GetIndex();

							if ( index != enumValid.IndexSrc )
								throw new Exception();
							for ( ; indexVer<index ; indexVer++ ) {
								if ( IsValid( indexPosVer, indexVer ) )
									throw new Exception();
							}
							if ( !IsValid( indexPosVer, indexVer ) )
								throw new Exception();
							indexVer++;

						} while ( enumValid.NextValid() );

						for ( ; indexVer<indexCount ; indexVer++ ) {
							if ( IsValid( indexPos, indexVer ) )
								throw new Exception();
						}
						if ( fast )
							break;
					}
				}
			}
		}


		private static void IndexEnumerate_TestIncSrcIndex()
		{
			bool    fast = true, skip = true;
			Random random = new Random( 0 );

			for ( int i=1 ; i<Pieces.Count ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );

				foreach( bool wtm in Tools.BoolArray ) {
					for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
						if ( fast )
							wkBk = new WkBk( random.Next(WkBk.GetCount(pieces).Index), pieces );
						PieceGroupReorder                   pgr                 = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
						IndexPos                          indexPos            = new IndexPos( wkBk, pieces, wtm );
						IndexPos                          indexPosVer         = new IndexPos( wkBk, pieces, wtm );
						IndexEnumerator                   indexEnumerator     = new IndexEnumerator( indexPos );
						long indexCount = indexPosVer.IndexCount;
						long skipCount = skip ? random.Next( (int)indexCount ) : 0;
						indexEnumerator.Reset( skipCount, false );

						for ( long j=skipCount ; j<indexCount ; j++ ) {
							Fields fld    = indexPos.GetFields();
							Fields fldVer = indexEnumerator.Fields;
							if ( Fields.Compare(fld,fldVer,indexPos.Pieces.PieceCount) != 0 )
								throw new Exception();
							indexEnumerator.IncSrcIndex();
						}

						if ( fast )
							break;
					}
				}
			}
		}


		private static CheckAndPin   cpStm, cpSntm;
		private static IndexPos cpIp;
		private static bool IsValid( IndexPos ip, long index )
		{
			ip.SetToIndex( index );
			if ( ip.GetIsEp() ) {
				if ( cpIp==null || cpIp!=ip ) {
					cpIp   = ip;
					cpStm  = new CheckAndPin( ip.WkBk, ip.Pieces,  ip.Wtm );
					cpSntm = new CheckAndPin( ip.WkBk, ip.Pieces, !ip.Wtm );
				}
				return ip.GetIsValidEpPos( cpStm, cpSntm );
			}
			else {
				return ip.GetIsValid();
			}
		}


		private static void IndexEnumerate_TestReorder()
		{
			bool fast = true;
			Random random = new Random( 0 );

			for ( int i=1 ; i<Pieces.Count ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );

				foreach( bool wtm in Tools.BoolArray ) {
					for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {

						if ( fast )
							wkBk = new WkBk( random.Next(WkBk.GetCount(pieces).Index), pieces );

						IndexPos indexPosCalc       = new IndexPos( wkBk, pieces, wtm );
						PieceGroupReorder pgr = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
						IndexPos            indexPosComp64     = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress, pgr );
						IndexEnumerator     indexEnumerator    = new IndexEnumerator( indexPosComp64, indexPosCalc );

						long indexCount = indexPosComp64.IndexCount;

						for ( long j=0 ; j<indexCount ; ) {
							if ( indexEnumerator.IndexPosDstValid ) {
								if ( indexEnumerator.IndexDst != indexPosCalc.GetIndex() )
									throw new Exception();
							}

							int r = random.Next(3);
							if ( r==0 ) {
								indexEnumerator.IncSrcIndex();
								j++;
							}
							else if ( r==1 ) {
								int d = random.Next( 20 );
								indexEnumerator.AddToSrcIndex( d );
								j += d;
							}
							else if ( r==2 ) {
								if ( indexEnumerator.NextValid() )
									j = indexEnumerator.IndexSrc;
								else
									j = int.MaxValue;
							}
						}
						if ( fast )
							break;
					}
				}
			}
		}



		private static void PerformanceIndexPosReorderEnumeratorValid()
		{
			long dummy = 0;

			for ( int i=1 ; i<50 ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );

				foreach( bool wtm in Tools.BoolArray ) {
					for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
						PieceGroupReorder pgr = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
						IndexPos indexPosCalc       = new IndexPos( wkBk, pieces, wtm );
						IndexPos indexPosComp64     = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress, PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random ) );

						IndexEnumerator    indexEnumerator    = new IndexEnumerator( indexPosComp64, indexPosCalc );
						long indexCount = indexPosComp64.IndexCount;

						for ( long j=0 ; j<indexCount ; j++ ) {
							if ( indexEnumerator.IndexPosDstValid ) {
//								indexPosCalc.SetToIndex( dstIndex );
								Fields fld = indexPosCalc.GetFields();
								dummy ^= fld.Bits;
							}
							indexEnumerator.IncSrcIndex();
						}
					}
				}
			}
			Message.Line( dummy.ToString( "X2" ) );
		}


		//private static void PerformanceIndexReorderEnumerator()
		//{
		//	long dummy = 0;
		//
		//	for ( int i=1 ; i<50 ; i++ ) {
		//		Pieces pieces = Pieces.FromIndex( i );
		//		Message.Line( pieces.ToString() );
		//
		//		foreach( bool wtm in Tools.BoolArray ) {
		//			for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
		//				PieceGroupReorder pgr = PieceGroupReorder.Get( picees, wtm, PieceGroupReorderType.Random );
		//				IndexPos indexPosCalc       = new IndexPos( wkBk, pieces, wtm );
		//				IndexPos indexPosComp64     = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress64, PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random ) );
		//
		//				IndexReorderEnumerator    indexEnumerator    = new IndexReorderEnumerator( indexPosComp64, indexPosCalc );
		//				long indexCount = indexPosComp64.IndexCount;
		//
		//				for ( long j=0 ; j<indexCount ; j++ ) {
		//					long dstIndex = indexEnumerator.TryGetDstIndex();
		//					if ( dstIndex != -1 ) {
		//						indexPosCalc.SetToIndex( dstIndex );
		//						Fields fld = indexPosCalc.GetFields();
		//						dummy ^= fld.Bits;
		//					}
		//					indexEnumerator.IncSrcIndex();
		//				}
		//			}
		//		}
		//	}
		//	Message.Line( dummy.ToString( "X2" ) );
		//}


//		private static void PerformanceIndexPosReorderEnumerator()
//		{
//			long dummy = 0;
//
//			for ( int i=1 ; i<50 ; i++ ) {
//				Pieces pieces = Pieces.FromIndex( i );
//				Message.Line( pieces.ToString() );
//
//				foreach( bool wtm in Tools.BoolArray ) {
//					for ( WkBk wkBk=WkBk.First(pieces) ; wkBk<wkBk.Count ; wkBk++ ) {
//						PieceGroupReorder pgr = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
//						IndexPos indexPosCalc       = new IndexPos( wkBk, pieces, wtm );
//						IndexPos indexPosComp64     = new IndexPos( wkBk, pieces, wtm, IndexPosType.Compress64, PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random ) );
//
//						IndexPosReorderEnumerator    indexEnumerator    = new IndexPosReorderEnumerator( indexPosComp64, indexPosCalc );
//						long indexCount = indexPosComp64.IndexCount;
//
//						for ( long j=0 ; j<indexCount ; j++ ) {
//							if ( indexEnumerator.IndexPosDstValid ) {
//								indexPosCalc.SetToIndex( dstIndex );
//								Fields fld = indexPosCalc.GetFields();
//								dummy ^= fld.Bits;
//							}
//							indexEnumerator.IncSrcIndex();
//						}
//					}
//				}
//			}
//			Message.Line( dummy.ToString( "X2" ) );
//		}





		private static void TestPieceGroup2()
		{
			for ( int i=0 ; i<Pieces.Count ; i++ ) {
				Pieces pieces = Pieces.FromIndex( i );
				Message.Line( pieces.ToString() );
				PieceGroupInfo pgi = pieces.GetPieceGroupInfo();

				if ( pgi.GetPieceCount(0) == 2 ) {
					PieceGroup2 pg2 = new PieceGroup2( pieces, WkBk.First(pieces), 2, pgi.GetPiece(0), pgi.CountW!=0, true, 0, IndexPosType.Calc );

					for ( int j=0 ; j<pg2.IndexCount ; j++ ) {
						pg2.Index = j;
						Fields f = pg2.GetFields();
						pg2.SetFields( f );
						if ( pg2.Index != j )
							throw new Exception();
					}
				}
			}
		}



/*
		private static void EPTestSequentiell()
		{
			Pieces pieces = Pieces.FromString( "KPPKP" );
			for ( WkBk wkbk=WkBk.First(pieces) ; wkbk<wkbk.Count ; wkbk++ ) {
				Message.Line( wkbk.ToString() );
				foreach( bool wtm in Tools.BoolArray ) {
					IndexPos indexPos = new IndexPos( wkbk, pieces, wtm );
					long count = indexPos.IndexCount;

					long lastIndex = -1;
					EpEnumerateInfoSequentiell info = indexPos.SetToFirstEpPosSequentiell();

					do {
						long index = indexPos.GetIndex();
						if ( !indexPos.GetIsEp(index) )
							throw new Exception();
						while ( ++lastIndex != index ) {
							if ( indexPos.GetIsEp(lastIndex) )
								throw new Exception();
						}
					} while( indexPos.NextEpPosSequentiell(ref info) );

					while ( ++lastIndex != count ) {
						if ( indexPos.GetIsEp(lastIndex) )
							throw new Exception();
					}
				}
			}
		}
*/


/*
		private static void EPTestSequentiellSpeed()
		{
			Pieces pieces = Pieces.FromString( "KPPKP" );
			for ( WkBk wkbk=WkBk.First(pieces) ; wkbk<wkbk.Count ; wkbk++ ) {
				Message.Line( wkbk.ToString() );
				foreach( bool wtm in Tools.BoolArray ) {
					IndexPos indexPos = new IndexPos( wkbk, pieces, wtm );
					long count = indexPos.IndexCount;

					EpEnumerateInfoSequentiell info = indexPos.SetToFirstEpPosSequentiell();
					do {
					} while( indexPos.NextEpPosSequentiell(ref info) );
				}
			}
		}
*/

		private static void EPTest2()
		{
			bool fast = true;
			Random random = new Random( 101 );

			for ( int p=1 ; p<Pieces.Count ; p++ ) {
				Pieces pieces = Pieces.FromIndex( p );
				if ( !pieces.ContainsWpawnAndBpawn )
					continue;
				Message.Line( "Test for " + pieces.ToString() );
				foreach( bool wtm in Tools.BoolArray ) {
					for ( WkBk wkbk=WkBk.First(pieces) ; wkbk<wkbk.Count ; wkbk++ ) {
						if ( fast )
							wkbk = new WkBk( random.Next(WkBk.GetCount(pieces).Index), pieces );

						//IndexPos indexPos = new IndexPos( wkbk, pieces, wtm );
						PieceGroupReorder pgr           = PieceGroupReorder.Get( pieces, wtm, PieceGroupReorderType.Random );
						IndexPos indexPos = new IndexPos( wkbk, pieces, wtm, IndexPosType.Compress, pgr );
						long count = indexPos.IndexCount;
						Fields f = indexPos.GetFields();
						long cnt1 = 0L, cnt2 = 0L;
						Message.Line( wkbk.ToString() );
						for ( long i=0 ; i<count ; i++ ) {
							if ( indexPos.GetIsEp() )
								cnt1++;
							indexPos.IncIndex( ref f );
						}
						Message.Line( wkbk.ToString() );
						for ( long i=0 ; i<count ; i++ ) {
							if ( indexPos.GetIsEp(i) )
								cnt2++;
							indexPos.IncIndex( ref f );
						}
						Message.Line( wkbk.ToString() );
						if ( cnt1 != cnt2 ) {
							indexPos.SetToIndex( 0 );
							f = indexPos.GetFields();
							for ( long i=0 ; i<count ; i++ ) {
								bool b  = indexPos.GetIsEp();
								bool b2 = indexPos.GetIsEp(i);
								if ( b != b2 )
									throw new Exception();
								indexPos.IncIndex( ref f );
							}
						}
						if ( fast )
							break;
					}
				}
			}
		}



/*
		private static void EPTest()
		{
			Pieces pieces = Pieces.FromString( "KQPKPPP" );
			Message.Line( "Test for " + pieces.ToString() );
			for ( WkBk wkbk=WkBk.First(pieces) ; wkbk<wkbk.Count ; wkbk++ ) {
				Message.Line( wkbk.ToString() );
				foreach( bool wtm in Tools.BoolArray ) {
					IndexPos indexPos = new IndexPos( wkbk, pieces, wtm );
					long count = indexPos.IndexCount;
					long countEp = 0;

					for ( long i=0 ; i<count ; i++ ) {
						if ( indexPos.GetIsEp(i) )
							countEp++;
					}

					EpEnumerateInfo info = indexPos.SetToFirstEpPos();
					do {
						long index = indexPos.GetIndex();
						if ( !indexPos.GetIsEp(index) )
							throw new Exception();
						countEp--;
					} while( indexPos.NextEpPos(ref info) );

					if ( countEp != 0 )
						throw new Exception();
				}
			}
		}
*/


/*
		private static void EPTestSpeed()
		{
			Pieces pieces = Pieces.FromString( "KPPKP" );
			for ( WkBk wkbk=WkBk.First(pieces) ; wkbk<wkbk.Count ; wkbk++ ) {
				Message.Line( wkbk.ToString() );
				foreach( bool wtm in Tools.BoolArray ) {
					IndexPos indexPos = new IndexPos( wkbk, pieces, wtm );
					long count = indexPos.IndexCount;

					EpEnumerateInfo info = indexPos.SetToFirstEpPos();
					do {
					} while( indexPos.NextEpPos(ref info) );
				}
			}
		}
*/



/*
		private static void PerformanceEnumeratePieceGroups()
		{
			// verify
			for ( int i=0 ; i<ListDB.Count ; i++ ) {
				Pieces p = ListDB.IndexToPieces(i);
				foreach( bool b in Tools.BoolArray ) {
					IndexPos indexPos = new IndexPos( WkBk.First(p), p, b );
					int pgIndices = indexPos.PieceGroupsToMvIndices;
					for ( int k=indexPos.FirstPieceGroup(b) ; k!=-1 ; k=indexPos.NextPieceGroup(k) ) {
						if ( k != (pgIndices&15) )
							throw new Exception();
						pgIndices >>= 4;
					}
					if ( pgIndices != 15 )
						throw new Exception();
				}
			}

			long counter = 0;
			int loops = 100000;
			Stopwatch sw = new Stopwatch();
			sw.Start();
			for ( int i=0 ; i<ListDB.Count ; i++ ) {
				Pieces p = ListDB.IndexToPieces(i);
				foreach( bool b in Tools.BoolArray ) {
					IndexPos indexPos = new IndexPos( WkBk.First(p), p, b );
					for ( int j=0 ; j<loops ; j++ ) {
						int pgIndices = indexPos.PieceGroupsToMvIndices;
						while( pgIndices != 15 ) {
							int curPG = pgIndices&15;
							pgIndices >>= 4;
							counter += curPG;
						}
					}
				}
			}
			sw.Stop();
			Message.Line( "IndicesInt: " + sw.ElapsedMilliseconds.ToString("###,##0") + " ms    " + counter.ToString() );

			sw.Reset();
			counter = 0;
			sw.Start();
			for ( int i=0 ; i<ListDB.Count ; i++ ) {
				Pieces p = ListDB.IndexToPieces(i);
				foreach( bool b in Tools.BoolArray ) {
					IndexPos indexPos = new IndexPos( WkBk.First(p), p, b );
					for ( int j=0 ; j<loops ; j++ ) {
						for ( int k=indexPos.FirstPieceGroup(b) ; k!=-1 ; k=indexPos.NextPieceGroup(k) ) {
							counter += k;
						}
					}
				}
			}
			sw.Stop();
			Message.Line( "FirstNext:  " + sw.ElapsedMilliseconds.ToString("###,##0") + " ms    " + counter.ToString() );
		}
*/


/*
		private static void PerformanceChangeIndex()
		{
			Pieces p = new Pieces( "KQRBNQ" );
			Random rnd = new Random();
			Stopwatch sw = new Stopwatch();
			IndexPos indexPos = new IndexPos( WkBk.First(p), p, true );
			indexPos.SetToIndex( 0 );
			long oldIndex = 0, newIndex = 10;
			Fields f = indexPos.GetFields();

			sw.Start();
			while ( newIndex < indexPos.IndexCount ) {
				indexPos.ChangeIndex( newIndex-oldIndex, ref f );
				oldIndex = newIndex;
				newIndex += rnd.Next( 10 ) + 1;
			}
			sw.Stop();
			Message.Line( sw.ElapsedMilliseconds.ToString("###,##0") + " ms    " + f.ToString() );
		}
 */


	}
}
