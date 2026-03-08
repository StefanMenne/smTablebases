using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;


namespace LC
{
#if !RELEASEFINAL
	public class DebugInfo
	{
		public delegate bool   Choose( DebugInfoItem left, DebugInfoItem right );
		public delegate double Sum( DebugInfoItem left, DebugInfoItem right );


		public  bool            IsLc; 
		public  int             Level;
		public  byte[]          Character;
		public  double[]        CostsToHereSCS;
		public  int[]           Length;

		private DebugInfoItem[] info;
		private bool            estimatedCostsAvailable;


		public DebugInfo( int count, byte[] character, bool estimatedCostsAvailable, bool isLc, int[] length, int level )
		{
			info = new DebugInfoItem[count];
			CostsToHereSCS = new double[count];
			Character = character;
			Length = length;
			Level = level;
			this.estimatedCostsAvailable = estimatedCostsAvailable;
			this.IsLc = isLc;
		}


		public DebugInfoItem this[int index]
		{
			get { return info[index]; }
			set { info[index]=value;  }
		}


		public DebugInfoItem[] Items
		{
			get {  return info; }
		}


		public void AddPath( bool[] path )
		{
			for ( int i=0 ; i<path.Length ; i++ ) 
				path[i] |= Items[i]!=null;
		}


		public void Finish()
		{
		}


		public void Write( string filename )
		{
			using( FileStream fs = new FileStream( filename, FileMode.Create ) ) {
				using ( BinaryWriter bw = new BinaryWriter( fs ) ) {
					bw.Write( info.Length );
					bw.Write( Length.Length );
					for ( int i=0 ; i<Length.Length ; i++ )
						bw.Write( Length[i] );
					bw.Write( estimatedCostsAvailable );
					bw.Write( IsLc );
					bw.Write( Level );
					for ( int i=0 ; i<info.Length ; i++ ) {
						bw.Write( Character[i] );
						bw.Write( info[i]!=null );
						if ( info[i]!=null  )
							info[i].Write( bw );
						bw.Write( CostsToHereSCS[i] );
					}
				}
			}
			Log.Line( "Write to " + Path.GetFileName(filename) );
		}


		public static DebugInfo Read( string filename )
		{
			using( FileStream fs = new FileStream( filename, FileMode.Open ) ) {
				using ( BinaryReader br = new BinaryReader( fs ) ) {
					int count = br.ReadInt32();
					byte[] characters = new byte[count];
					int[] length = new int[br.ReadInt32()];
					for ( int i=0 ; i<length.Length ; i++ )
						length[i] = br.ReadInt32();
					DebugInfo info = new DebugInfo( count, characters, br.ReadBoolean(), br.ReadBoolean(), length, br.ReadInt32() );
					for ( int i=0 ; i<info.info.Length ; i++ ) {
						characters[i] = br.ReadByte();
						if ( br.ReadBoolean() )
							info[i] = new DebugInfoItem( br, info );
						info.CostsToHereSCS[i] = br.ReadDouble();
					}
					return info;		
				}
			}
		}


		public void WriteLog( string filename )
		{
			MatchGen matchGen = new MatchGen( Character, Character.Length, Length );
			StreamWriter streamWriter = new StreamWriter( filename );
			int indexWidth = Items.Length.ToString("###,###,###,##0").Length;

			streamWriter.WriteLine(  "Index".PadLeft(indexWidth)+ " CH " + DebugInfoItem.GetHeadingString(indexWidth,estimatedCostsAvailable) );

			for ( int i=0 ; i<Items.Length ; i++ ) {
				string s = i.ToString("###,###,###,##0").PadLeft(indexWidth) + " " + Character[i].ToString("X2") + " ";
				if ( Items[i] != null )
					s += Items[i].ToString( indexWidth, estimatedCostsAvailable, CostsToHereSCS[i] );	
				if ( i!=0 )
					s += "                     " + matchGen.ToString();
				streamWriter.WriteLine( s );
				matchGen.Shift();
			}
			streamWriter.Close();
			streamWriter.Dispose();
			Log.Line( "Log written to " + Path.GetFileName(filename) );
		}


		public void WriteShortLog( string filename, bool[] path=null )
		{
			if ( path == null ) {
				path = new bool[Items.Length];
				AddPath( path );
			}
			StreamWriter streamWriter = new StreamWriter( filename );
			int indexWidth = Items.Length.ToString("###,###,###,##0").Length;

			streamWriter.WriteLine(  "Index".PadLeft(indexWidth)+ " CH " + DebugInfoItem.GetHeadingString(indexWidth,estimatedCostsAvailable) );

			for ( int i=0 ; i<Items.Length ; i++ ) {
				if ( path[i] ) {
					if ( Items[i] != null )
						streamWriter.WriteLine( i.ToString("###,###,###,##0").PadLeft(indexWidth) + " " + Items[i].ToShortLogString( indexWidth, ' ' ) + " " );	
					else
						streamWriter.WriteLine( i.ToString("###,###,###,##0").PadLeft(indexWidth) );	
				}
			}
			streamWriter.Close();
			streamWriter.Dispose();
			Log.Line( "Log written to " + Path.GetFileName(filename) );
		}



		private double GetCost( int i )
		{
			if ( info[i] != null )
				return info[i].CostsToHere;

			int prev = GetNextPath( i, -1 );
			int next = GetNextPath( i,  1 );

			if ( next>=info.Length )
				return 0.0;

			double costPrev = info[prev].CostsToHere;
			double costNext = info[next].CostsToHere;
			return  costPrev + (i-prev) * (costNext-costPrev) / (next-prev);
		}


		private int GetNextPath( int current, int delta )
		{
			while ( current<info.Length && info[current]==null )
				current += delta;
			return current;
		}




		public void GetDistLength( DistLength[] distLength, LengthInfo lengthInfo, bool forceExpDistCoding )
		{
			int j=0;
			bool literalsUsedForMatches = false;

			int dist = 1, distOld = 1;
			LatestHistory latestHistory = LatestHistory.Initial;
			for ( int i=0 ; i<Items.Length ;  ) {
				if ( Items[i].MatchLength == -1 ) {
					distLength[j++] = DistLength.Literal;
					i+=Items[i].DeltaChoosen;
				}
				else {
					int length = 0;
					distOld        = dist;
					dist           = Items[i].MatchDist;
					bool isExpDist = Items[i].IsExpDist;

					while ( i<Items.Length && Items[i].MatchLength!=-1 && Items[i].MatchDist==dist && ( (!forceExpDistCoding) || isExpDist || (!Items[i].IsExpDist) ) ) {   // add all length with same dist
						length += Items[i].MatchLength;
						i+=Items[i].DeltaChoosen;					
					}

					if ( length == 1 && distOld != dist )
						throw new Exception( "Rep0S not possible" );

					while ( length > 1 ) {
						int lengthIndex=lengthInfo.LengthIndexCount;
						bool found = false;
						while ( --lengthIndex>=0 ) {
							int currentLength = lengthInfo.IndexToLength( lengthIndex );
							if ( currentLength <= length ) {
								if ( forceExpDistCoding && isExpDist ) {
									distLength[j] = DistLength.ExpDist( dist, lengthIndex );
								}							
								else if ( latestHistory.Contains( dist ) ) {
									distLength[j] = new DistLength( dist, lengthIndex, latestHistory.GetRank( dist ) );
								}
								else { 
									distLength[j] = DistLength.ExpDist( dist, lengthIndex );																				
								}
								latestHistory = latestHistory.Add( dist );
								j += currentLength;
								length -= currentLength;
								found = true;
								break;
							}
						}
						if ( !found ) {
							while ( length > 0 ) {
								distLength[j++] = DistLength.Literal;
								length--;
								literalsUsedForMatches = true;
							}
						}
					}
					
					if ( length == 1 )
						distLength[j++] = DistLength.Rep0SWithDist( dist );
				}
			}

			if ( literalsUsedForMatches ) {
				Log.Line( "LITERAL'S USED FOR MATCHES. This happens when compression log used different LengthSet" );
			}
		}




		public static void Compare( DebugInfo info1, DebugInfo info2, string outFile )
		{
			if ( info1.Items.Length != info2.Items.Length ) {
				Log.Line( "Amount items does not match" );
				return;
			}
			Log.Line( "Write Log to " + Path.GetFileName(outFile) );

			bool[] path = new bool[info1.Items.Length];
			info1.AddPath( path );
			info2.AddPath( path );

			StreamWriter streamWriter = new StreamWriter( outFile );
			int indexWidth = info1.Items.Length.ToString("###,###,###,##0").Length;
			int maxLength  = 85; 

			streamWriter.WriteLine( "Type[IsLit;IsRep_Virt;IsRep0;IsRep0S;Other]" );

			string left2 = null, right2 = null;
			for ( int i=0 ; i<info1.Items.Length ; i++ ) {
				string line = (MarkLine(info1,info2,i) ? '~' : ' ' ) + i.ToString("###,###,###,##0").PadLeft(indexWidth) + " ";
				double costDelta = info1.GetCost(i) - info2.GetCost(i);
				line += costDelta.ToString( "#,###,##0.000").PadLeft(12) + " ";

				string left="", right="";
				if ( info1.Items[i]!=null ) {
					string[] a = info1.Items[i].ToShortLogString(indexWidth,'*').Split('*');
					left = a[0];
					if ( a.Length>1 )
						left2 = "                   " + a[1];			
				}
				else if ( left2 != null ) {
					left = left2;
					left2 = null;
				}
				if ( info2.Items[i]!=null ) {
					string[] a = info2.Items[i].ToShortLogString(indexWidth,'*').Split('*');
					right = a[0];
					if ( a.Length>1 )
						right2 = "                   " + a[1];			
				}
				else if ( right2 != null ) {
					right = right2;
					right2 = null;
				}
				streamWriter.WriteLine( line + left.PadRight(maxLength) + right );
			}
			streamWriter.Close();
			streamWriter.Dispose();

			double sumLeft, sumRight;
			GetSum( info1, info2, false, delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null; }, delegate(DebugInfoItem left, DebugInfoItem right){ return left.CostSum; }, out sumLeft, out sumRight );
			Log.Line( "" );
			Log.Line( "                                                Left / First                 Right / Second");
			PrintAllCostTypes( info1, info2, "All",           (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null; }                        );
			Log.Line( "" );
			PrintAllCostTypes( info1, info2, "Literals",      (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.MatchLength==-1; } );
			PrintAllCostTypes( info1, info2, "Matches",       (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.MatchLength!=-1; } ); 
			PrintAllCostTypes( info1, info2, "MatchesRep0S",  (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.IsRep0S; }         );
			PrintAllCostTypes( info1, info2, "MatchesRep",    (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.IsRep; }           );
			PrintAllCostTypes( info1, info2, "MatchesHis",    (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.Type==CodingItemType.Hist; } );
			PrintAllCostTypes( info1, info2, "MatchesExp",    (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.Type==CodingItemType.ExpDist; }  );
			Log.Line( "" );
			PrintAllCostTypes( info1, info2, "Literals same pos",             (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.MatchLength==-1 && other.MatchLength==-1; } );
			PrintAllCostTypes( info1, info2, "Literals other",                (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.MatchLength==-1 && !( other!=null && other.MatchLength==-1); } );
			PrintAllCostTypes( info1, info2, "MatchesRep0S same pos",         (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.IsRep0S && other.IsRep0S; }                 );
			PrintAllCostTypes( info1, info2, "MatchesRep0S other",            (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.IsRep0S && !(other!=null && other.IsRep0S); }                 );
			PrintAllCostTypes( info1, info2, "MatchesRep same pos/length",    (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.IsRep && other.IsRep && cur.MatchLength==other.MatchLength; } );
			PrintAllCostTypes( info1, info2, "MatchesRep other",              (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.IsRep && !(other.IsRep && cur.MatchLength==other.MatchLength); } );
			PrintAllCostTypes( info1, info2, "MatchesHis same pos/length",    (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.Type==CodingItemType.Hist && other.Type==CodingItemType.Hist && cur.MatchLength==other.MatchLength; } );
			PrintAllCostTypes( info1, info2, "MatchesHis other",              (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.Type==CodingItemType.Hist && !(other.Type==CodingItemType.Hist && cur.MatchLength==other.MatchLength); } );
			PrintAllCostTypes( info1, info2, "MatchesExp same pos/length",    (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.Type==CodingItemType.ExpDist && other.Type==CodingItemType.ExpDist && cur.MatchLength==other.MatchLength; } );
			PrintAllCostTypes( info1, info2, "MatchesExp other",              (Choose)delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && other!=null && cur.Type==CodingItemType.ExpDist && !( other.Type==CodingItemType.ExpDist && cur.MatchLength==other.MatchLength); } );
			Log.Line( "" );

			Log.Line( "Sum in Bytes                  Literals             " + PrintSumOrGetNull(info1,info2,delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.Type==CodingItemType.Literal; }, delegate(DebugInfoItem left, DebugInfoItem right){ return left.CostSum; }, false, false, 0.125d ) );
			Log.Line( "                              Rep0S                " + PrintSumOrGetNull(info1,info2,delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.Type==CodingItemType.Rep0S; }, delegate(DebugInfoItem left, DebugInfoItem right){ return left.CostSum; }, false, false, 0.125d ) );
			Log.Line( "                              Rep                  " + PrintSumOrGetNull(info1,info2,delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && CodingItemType.IsRep(cur.Type); }, delegate(DebugInfoItem left, DebugInfoItem right){ return left.CostSum; }, false, false, 0.125d ) );
			Log.Line( "                              Hist                 " + PrintSumOrGetNull(info1,info2,delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.Type==CodingItemType.Hist; }, delegate(DebugInfoItem left, DebugInfoItem right){ return left.CostSum; }, false, false, 0.125d ) );
			Log.Line( "                              Exp                  " + PrintSumOrGetNull(info1,info2,delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null && cur.Type==CodingItemType.ExpDist; }, delegate(DebugInfoItem left, DebugInfoItem right){ return left.CostSum; }, false, false, 0.125d ) );
			Log.Line( "                              Overall              " + PrintSumOrGetNull(info1,info2,delegate(DebugInfoItem cur, DebugInfoItem other){ return cur!=null; }, delegate(DebugInfoItem left, DebugInfoItem right){ return left.CostSum; }, false, false, 0.125d ) );

			Log.Line( "Done" );

		}


		private static void PrintAllCostTypes( DebugInfo info1, DebugInfo info2, string name, Choose choose )
		{
			object[] o = new object[]{
			       "Total",      (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostSum; },
			       "IsLiteral",  (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostIsLiteral; },
			       "IsRep0",     (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostIsRep0; },
			       "IsRep0S",    (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostIsRep0S; },
			       "TypeOther",  (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostTypeOther; },
			       "DistOrLit",  (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostDistOrLit; },
			       "Length",     (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostLength; },
			       "IsRep_Virt", (Sum)delegate(DebugInfoItem current, DebugInfoItem other){ return current.CostIsRep_Virt; },
			};

	
			Log.Line( name.PadRight(30) + "Count            " + PrintCount(info1,info2,choose) );
			for ( int i=0 ; i<o.Length ; i+=2 ) {
				string s = PrintSumOrGetNull(info1,info2,choose,(Sum)o[i+1]);
				if ( s != null )
					Log.Line( "                              " + ((string)o[i]).PadRight(20) + " " + s );
			}
		}


		private static string PrintSumOrGetNull( DebugInfo info1, DebugInfo info2, Choose choose, Sum sum, bool average=true, bool returnNullIfBothSumsAreIsNull=true, double factor=1.0d )
		{
			double left, right;
			GetSum( info1, info2, average, choose, sum, out left, out right );
			return (left==0d && right==0d && returnNullIfBothSumsAreIsNull)?null:((left*factor).ToString("#,###,###,##0.000").PadLeft(12) + "         " + (right*factor).ToString("#,###,###,##0.000").PadLeft(12));
		}

		
		private static string PrintCount( DebugInfo info1, DebugInfo info2, Choose choose )
		{
			int left, right;
			GetCount( info1, info2, choose, out left, out right );
			return left.ToString("#,###,###,##0").PadLeft(12) + "         " + right.ToString("#,###,###,##0").PadLeft(12);
		}


		private static void GetCount( DebugInfo info1, DebugInfo info2, Choose choose, out int left, out int right )
		{
			left = 0;
			right = 0;
			for ( int i=0 ; i<info1.Items.Length ; i++ ) {
				if ( choose( info1.Items[i], info2.Items[i] ) )
					left++;
				if ( choose( info2.Items[i], info1.Items[i] ) )
					right++;
			}
		}


		private static void GetSum( DebugInfo info1, DebugInfo info2, bool average, Choose choose, Sum sum, out double left, out double right )
		{
			left = 0.0d;
			right = 0.0d;
			int countLeft = 0, countRight = 0;
			for ( int i=0 ; i<info1.Items.Length ; i++ ) {
				if ( choose( info1.Items[i], info2.Items[i] ) ) {
					countLeft++;
					left += sum(info1.Items[i], info2.Items[i]);
				}
				if ( choose( info2.Items[i], info1.Items[i] ) ) {
					countRight++;
					right += sum(info2.Items[i], info1.Items[i]);
				}
			}
			if ( average && countLeft!=0 )
				left = left / countLeft;
			if ( average && countRight!=0 )
				right = right / countRight;
		}


		private static bool MarkLine( DebugInfo info1, DebugInfo info2, int index )
		{
			return (info1.Items[index]!=null) != (info2.Items[index]!=null);
		}

	}
#endif
}
