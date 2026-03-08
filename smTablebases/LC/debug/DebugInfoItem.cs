﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC
{
#if !RELEASEFINAL
	public class DebugInfoItem
	{
		private const bool showMaxMatch       = false;

		public DebugInfo  DebugInfo;

		public int  MaxMatchLength;
		public int  MaxMatchDist;

		public double CostsToHere;

		public double CostIsLiteral;     
		public double CostIsRep_Virt;    // virtual for LC    independent from all other costs(not to be added to overall sum)
		public double CostIsRep0;
		public double CostIsRep0S;
		public double CostTypeOther;     // all other costs for type 
		public string Info;

		public double CostDistOrLit;
		public double CostLength;

		public int  MatchLength;
		public int  MatchDist;

		public int  Type;  
		public int  HistoryIndex;


		public DebugInfoItem( DebugInfo debugInfo )
		{
			this.DebugInfo = debugInfo;
			Info = "";
		}


		public DebugInfoItem( BinaryReader br, DebugInfo debugInfo )
		{
			this.DebugInfo     = debugInfo;
			MaxMatchLength     = br.ReadInt32();
			MaxMatchDist       = br.ReadInt32();
			CostsToHere        = br.ReadDouble();
			CostIsLiteral      = br.ReadDouble();
			CostIsRep_Virt     = br.ReadDouble();
			CostIsRep0         = br.ReadDouble();
			CostIsRep0S        = br.ReadDouble();
			CostTypeOther      = br.ReadDouble();
			CostDistOrLit      = br.ReadDouble();
			CostLength         = br.ReadDouble();
			MatchLength        = br.ReadInt32();
			MatchDist          = br.ReadInt32();
			Type               = br.ReadInt32();
			HistoryIndex       = br.ReadInt32();
			Info               = br.ReadString();
		}


		public double CostSum
		{
			get { 
				return CostIsLiteral + (IsLc ? 0d : CostIsRep_Virt) + CostIsRep0 + CostIsRep0S + CostTypeOther + CostDistOrLit + CostLength; 
			}
		}


		public bool IsLc
		{
			get { return DebugInfo.IsLc; }
		}


		public void ToMaxPath()
		{
			MatchLength = MaxMatchLength;
			MatchDist   = MaxMatchDist;
		}


		public void Write( BinaryWriter bw )
		{
			bw.Write( MaxMatchLength );
			bw.Write( MaxMatchDist );
			bw.Write( CostsToHere );
			bw.Write( CostIsLiteral );
			bw.Write( CostIsRep_Virt );
			bw.Write( CostIsRep0 );
			bw.Write( CostIsRep0S );
			bw.Write( CostTypeOther );
			bw.Write( CostDistOrLit );
			bw.Write( CostLength );
			bw.Write( MatchLength );
			bw.Write( MatchDist );
			bw.Write( Type );
			bw.Write( HistoryIndex );
			bw.Write( Info );
		}


		public int DeltaChoosen
		{
			get { return (MatchLength>=2) ? MatchLength : 1; }
		}


		public int DeltaMax
		{
			get { return (MaxMatchLength>=2) ? MaxMatchLength : 1; }
		}


		public bool IsExpDist
		{
			get { return Type==CodingItemType.ExpDist; }
		}


		public bool IsRep
		{
			get { return CodingItemType.IsRep(Type); }
		}


		public bool IsRep0S
		{
			get { return Type == CodingItemType.Rep0S; }
		}


		public string ToString( int indexWidth, bool showEstimatedCosts, double costsToHereSCS )
		{
			string s = "";
#pragma warning disable 0162
			if ( showMaxMatch ) { 
				s += MaxMatchLength.ToString().PadLeft(4) + " ";
				s += ((MaxMatchLength>=2)?MaxMatchDist.ToString("###,###,###,##0"):"-").PadLeft(indexWidth) + " ";
			}
			s += CostsToHere.ToString( "0.000" ).PadLeft(20) + " ";
			if ( showEstimatedCosts )
				s += (costsToHereSCS-CostsToHere).ToString("0.000").PadLeft(20) + " ";
			s += GetInfoString( ' ' );
#pragma warning restore 0162
			
			return s;
		}

		public override string ToString()
		{
			if ( MatchLength == -1 )
				return "Literal";
			else
				return "dist=" + MatchDist.ToString() + "   length=" + MatchLength.ToString() ;
		}


		public string ToShortLogString( int indexWidth, char splitCharacter )
		{
            return CostsToHere.ToString("#,###,##0.000").PadLeft(15) + " " + GetInfoString( splitCharacter );
		}


		private string GetInfoString( char splitCharacter )
		{
			string s;

			if ( Type == CodingItemType.Literal )					
				s = "Lite";
			else if ( Type == CodingItemType.Rep0S )
				s = "RepS";
			else if ( Type == CodingItemType.ExpDist )
				s = "ExpD";
			else if ( CodingItemType.IsRep(Type) )
				s = "Rep" + Type.ToString();
			else if ( Type == CodingItemType.Hist )
				s = "H" + HistoryIndex.ToString( "000" );
			else
				throw new Exception();
			//double costSum = costTypeSum + costDistOrLit + costLength;

			s += "[" + (CostSum).ToString("0.000").PadLeft(6) + "]" + " Type[" + CostIsLiteral.ToString("0.000");
			if ( Type != CodingItemType.Literal ) {
				s += ";" + CostIsRep_Virt.ToString("0.000") ;
				s += ";" + CostIsRep0.ToString("0.000") + ";";
				if  ( CostIsRep0S != 0d ) 
					s += CostIsRep0S.ToString("0.000");
				s += ";";
				if ( CostTypeOther != 0d )
					s += CostTypeOther.ToString("##0.000");
			}
			else
				s += ";;;";
			s += "]";

			if ( Type == CodingItemType.ExpDist )
				s += splitCharacter;

			if ( CostDistOrLit != 0d )
				s += (Type==CodingItemType.Literal ? " Data[" : (" Dist=" + (MatchDist).ToString() + "[")) + CostDistOrLit.ToString( "#,##0.000" ) + "]";
			else if ( CodingItemType.IsRep(Type) || Type==CodingItemType.Rep0S )
				s += " Dist=" + (MatchDist).ToString();
			if ( CostLength != 0d )
				s += " Len=" + MatchLength.ToString() + "[" + CostLength.ToString( "#,##0.000" ) + "]";

			return s + " " + Info;
		}



		public static string GetHeadingString( int indexWidth, bool showEstimatedCosts )
		{
			string s = "C ";
#pragma warning disable 0162
			if ( showMaxMatch )
				s += "MaxMatch" + new string( ' ', indexWidth );
			s += "              Costs";
			if ( showEstimatedCosts )
				s += "   Delta Estim. Costs";
			s += " PathInfo       ";
#pragma warning restore 0162

			return s;
		}
	}


#endif
}
