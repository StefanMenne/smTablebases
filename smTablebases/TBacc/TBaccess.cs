﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace TBacc
{
	public static class TBaccess
	{

		public static bool TablebaseAvailable( Pieces p )
		{
			return File.Exists( TaBaRead.GetFilename( (p.IsDoubled ? p.SwitchSides() : p) ) );
		}


		public static Res GetResult( Pieces pieces, Field wk, Field bk, bool wtm, Fields fields, Field enPassantCaptureDest )
		{
			Res res = GetResultWithoutClose( pieces, wk, bk, wtm, fields, enPassantCaptureDest );
			Close();
			return res;
		}


		private static TaBasesRead    lastAccessTaBasesRead    = null;
		private static DataChunkRead  lastAccessDataChunkRead  = null;



	
		public static Res GetResultWithoutClose( Pieces pieces, Field wk, Field bk, bool wtm, Fields fields, Field enPassantCaptureDest )
		{
			WkBk wkBk;
			Pieces piecesOut;
			bool wtmOut;
			long index = GetIndex( pieces, wk, bk, wtm, fields, enPassantCaptureDest, out piecesOut, out wkBk, out wtmOut );

			if ( index == -1L )
				return Res.IllegalPos;

			if ( lastAccessDataChunkRead!=null && lastAccessDataChunkRead.WkBk==wkBk && lastAccessDataChunkRead.Pieces.Index==piecesOut.Index && lastAccessDataChunkRead.Wtm == wtmOut ) {
				return lastAccessDataChunkRead.Get( index );
			}
			else {
				if ( lastAccessDataChunkRead != null )
					lastAccessTaBasesRead.FreeDataChunk( lastAccessDataChunkRead );
				if ( lastAccessTaBasesRead == null || !lastAccessTaBasesRead.Contains(piecesOut) )
					lastAccessTaBasesRead   = TaBasesRead.OpenSingle( piecesOut, 1 );
				lastAccessDataChunkRead = lastAccessTaBasesRead.GetDataChunk( piecesOut, wkBk, wtmOut );
				lastAccessTaBasesRead.LoadDataChunkSingleThreaded( lastAccessDataChunkRead );
				return lastAccessDataChunkRead.Get( index );
			}

		}


		// given parameters
		private static Field lastAccessInWk, lastAccessInBk;
		private static int   lastAccessInPiecesIndex = -1;
		private static bool  lastAccessInWtm;

		// calculated values
		private static WkBk           lastAccessWkBk;
		private static Pieces         lastAccessPieces;
		private static MirrorType     lastAccessMirror;
		private static bool           lastAccessSideSwitch;
		private static IndexPos       lastAccessIndexPos;

		public static long GetIndex( Pieces pieces, Field wk, Field bk, bool wtm, Fields fields, Field enPassantCaptureDest, out Pieces piecesOut, out WkBk wkBkOut, out bool wtmOut )
		{
			if ( lastAccessInPiecesIndex==pieces.Index && lastAccessInWk==wk && lastAccessInBk==bk && lastAccessInWtm==wtm ) {
				piecesOut = lastAccessPieces;
				wkBkOut = lastAccessWkBk;
				wtmOut  = lastAccessInWtm;
				if ( lastAccessSideSwitch ) {
					fields = fields.MirrorOnHorizontal();
					if ( !enPassantCaptureDest.IsNo )
						enPassantCaptureDest = enPassantCaptureDest.Mirror(MirrorType.MirrorOnHorizontal);
					fields = fields.SwitchSides( pieces.CountW, pieces.CountB );
					wtmOut ^= true;
				}
				if ( wkBkOut.IsIllegal )
					return -1L;
			}
			else {
				lastAccessSideSwitch = pieces.IsDoubled;
				wtmOut = lastAccessInWtm = wtm;
				lastAccessInWk = wk;
				lastAccessInBk = bk;
				lastAccessInPiecesIndex = pieces.Index;
				if ( lastAccessSideSwitch ) {    // switch sides
					wk = wk.Mirror(MirrorType.MirrorOnHorizontal);
					bk = bk.Mirror(MirrorType.MirrorOnHorizontal);
					fields = fields.MirrorOnHorizontal();
					if ( !enPassantCaptureDest.IsNo )
						enPassantCaptureDest = enPassantCaptureDest.Mirror(MirrorType.MirrorOnHorizontal);
					lastAccessMirror = MirrorNormalize.WkBkToMirror( bk, wk, pieces );
					lastAccessWkBk = wkBkOut = new WkBk( bk, wk, pieces );
					wtmOut ^= true;
					lastAccessPieces = piecesOut = pieces.SwitchSides();
					fields = fields.SwitchSides( piecesOut.CountW, piecesOut.CountB );
				}
				else {
					lastAccessMirror = MirrorNormalize.WkBkToMirror( wk, bk, pieces );
					lastAccessWkBk = wkBkOut = new WkBk( wk, bk, pieces );
					lastAccessPieces = piecesOut = pieces;
				}
				if ( wkBkOut.IsIllegal )
					return -1L;
				lastAccessIndexPos      = new IndexPos( wkBkOut, piecesOut, wtmOut );
			}
			fields = fields.Mirror( lastAccessMirror );
			if ( enPassantCaptureDest.IsNo ) {
				if ( !lastAccessIndexPos.SetFields( fields ) )
					return -1L;
			}
			else {
				enPassantCaptureDest = enPassantCaptureDest.Mirror( lastAccessMirror );
				Field dblStepDst = EP.GetDblStepDst( enPassantCaptureDest );
				Field capSrc     = EP.GetOneCapSrc( piecesOut, dblStepDst, fields );
				if ( capSrc.IsNo )
					throw new Exception();
				if ( !lastAccessIndexPos.SetFieldsEP( fields, dblStepDst, capSrc ) )
					return -1L;
			}
			return lastAccessIndexPos.GetIndex();
		}



		public static void Close()
		{
			if ( lastAccessDataChunkRead != null ) {
				lastAccessTaBasesRead.FreeDataChunk( lastAccessDataChunkRead );
				lastAccessTaBasesRead.CloseAll( false );
			}
			lastAccessTaBasesRead   = null;
			lastAccessDataChunkRead = null;
			lastAccessIndexPos      = null;
			lastAccessInPiecesIndex     = -1;
		}





		public static Res GetResult( string fen )
		{
			Pieces    pieces;
			Field     wk,bk, ep;
			Fields    flds;
			bool      wtm;

			ParseFen( fen, out pieces, out wk, out bk, out wtm, out flds, out ep );
			return GetResult( pieces, wk, bk, wtm , flds, ep ); 
		}
		

		public static bool TryGetResult( string fen, out Res res )
		{
			Pieces  pieces;
			Field wk, bk, ep;
			Fields flds;
			bool wtm;
			ParseFen( fen, out pieces, out wk, out bk, out wtm, out flds, out ep );
			
			if ( TablebaseAvailable(pieces) ) {
				res = GetResult( pieces, wk, bk, wtm, flds, ep );
				return true;
			}
			else {
				res = Res.No;
				return false;
			}
		}


		private static void ParseFen( string fen, out Pieces pieces, out Field wk, out Field bk, out bool wtm, out Fields flds, out Field ep )
		{
			pieces = Pieces.KK;
			string[] s = fen.Split( ' ' );
			if ( s.Length != 6 )
				throw new Exception();

			string[] line = s[0].Split( '/' );
			if ( line.Length != 8 )
				throw new Exception();

			wk=Field.No;
			bk=Field.No;
			int piecesCnt = 0;
			flds = new Fields();
			for ( int i=0 ; i<Piece.IntToPiece.Length ; i++ ) {
				char pieceChar = Piece.IntToPiece[i].AsCharacter;
				bool wPiece = i<(Piece.IntToPiece.Length/2);
				if ( !wPiece )
					pieceChar = pieceChar.ToString().ToLower()[0];
				if ( s[0].Contains( pieceChar ) ) {
					for ( int y=0 ; y<8 ; y++ ) {
						int x=0;
						string st = line[7-y];
						while ( x!=8 ) {
							char curchar = st[0];
							st = st.Remove( 0, 1 );
							if ( char.IsDigit(curchar) )
								x += int.Parse( curchar.ToString(), CultureInfo.InvariantCulture );
							else {
								if ( curchar == pieceChar ) {
									Field f = new Field( x, y );
									if ( curchar == 'K' )
										wk = f;
									else if ( curchar == 'k' )
										bk = f;
									else { 
										flds = flds.SetNew( piecesCnt++, f );
										pieces = pieces.Add( wPiece, Piece.IntToPiece[i] );
									}
								}
								x++;
							}
						}

					}
				}
			}
			wtm = s[1].ToLower() == "w";
			ep = new Field( s[3] );
		}

		

		public static string GetFilename( string piecesString )
		{
			return Path.Combine( GetDatabaseFolder(piecesString.Length), piecesString + ".bin" );
		}

		public static string GetFilenameLog( string piecesString )
		{
			return Path.Combine(GetDatabaseFolder(piecesString.Length), piecesString + ".log");
		}


		public static string GetDatabaseFolder( int countPieces )
		{
			return Path.Combine( ApplicationPath, countPieces.ToString() );
		}

		public static string DebugFolder
		{
			get{ return Path.Combine( ApplicationPath, "Debug" ); }
		}

		
		public static string ApplicationPath
		{
			get { return AppDomain.CurrentDomain.BaseDirectory; }
		}
	}
}
