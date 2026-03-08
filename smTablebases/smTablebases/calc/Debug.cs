using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;

namespace smTablebases
{
#if DEBUG

	public enum VerifyResType
	{
		VerifyAlways,
		VerifyFinals,
		DontVerify
	}


	public sealed class Debug
	{
		/// <summary>
		/// Called when DataChunkWrite.GetResult is called for the tracked position.
		/// </summary>
		public static void GetResult( int result, string text )
		{
			ResWithCount res = new ResWithCount(result);
			Message.Line("   DBG: Get " + text + "  res=" + res.ToString());
		}

		/// <summary>
		/// Called when DataChunkWrite.SetResult is called for the tracked position.
		/// </summary>
		public static void SetResult( int result, string text )
		{
			ResWithCount res = new ResWithCount(result);
			Message.Line("   DBG: Set " + text + "  res=" + res.ToString());
		}

		/// <summary>
		/// Called when DataChunkWrite.SetFastBit is called for the tracked position.
		/// </summary>
		public static void SetOrUnsetFastBit( bool value )
		{
			Message.Line("   DBG: Set Fast Bit to " + value.ToString() );
		}







		public static void IsPotentialNewPosBits( bool processed )
		{
			if ( processed )
				Message.Line( "   DBG: IsPotentialNewPos=true  position is processed" );
			else
				Message.Line( "   DBG: IsPotentialNewPos=false position is skipped" );
		}


		// position to track
		private  static   long             trackIndex           = -1L;
		private  static   WkBk             trackWkBk            = WkBk.First(false);
		private  static   bool             trackWtm             = false;
		public   static   bool             TrackPosition                     = false;


		public static void Init( CalcTB calc, Pieces pToCalc, string trackText )
		{
			trackIndex = -1;
			TrackPosition = false;
			if ( trackText == null )
				return;
			UserPos userPos = ParsePos( trackText );
			if ( userPos.Pieces.Index != pToCalc.Index )
				return;

			IndexPos idxPos = userPos.ToIndexPos( out trackIndex );
			if (idxPos == null) {
				Message.Line( "   DBG: Pos not found." );
				return;
			}
			trackWkBk  = idxPos.WkBk;
			trackWtm   = userPos.Wtm;
			TrackPosition = true;
			TaBaWrite taBaWrite = calc.TaBasesWrite.TaBaWrite;

			for ( int i=0 ; i<taBaWrite.DataChunkCount ; i++ )
				taBaWrite.GetDataChunk( i ).TrackIndex = -1;
			taBaWrite.GetDataChunk( trackWtm, trackWkBk ).TrackIndex = (int)TrackIndex;

			Message.Line( "   DBG: Pos = " + userPos.ToString() + "     Unmirrored = " + userPos.Pos.ToString() );
			Message.Line( "   DBG: index = " + trackIndex.ToString( "#,###,###,##0" ) );
		}


		public static long TrackIndex
		{
			get{ return trackIndex; }
		}


		public static WkBk TrackWkBk
		{
			get { return trackWkBk; }
		}


		public static bool TrackWtm
		{
			get { return trackWtm; }
		}


		public static UserPos ParsePos( string text )
		{
			while ( text.Contains("  ") )
				text = text.Replace( "  ", " " );
			string[] s = text.Trim().Split( ' ' );
			Pieces pieces = Pieces.FromString( s[0] );
			Field[] fields = new Field[pieces.PieceCount + 2];
			for (int i = 0; i < fields.Length; i++)
				fields[i] = new Field(s[i + 1]);
			bool wtm = s[pieces.PieceCount + 3].ToLower() == "wtm";
			Field ep = Field.No;
			if (s.Length == pieces.PieceCount + 5)
				ep = new Field(s[pieces.PieceCount + 4]);
			UserPos userPos = new UserPos(pieces, fields, wtm,ep);

			return userPos;
		}
	}
#endif
}
