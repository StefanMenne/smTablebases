using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TBacc;

namespace smTablebases
{
	public static class TbInfoFileList
	{
		private static TbInfoFile[] Items = new TbInfoFile[Pieces.Count];
		private static string       header;


		static TbInfoFileList()
		{
			Load();
		}


		public static TbInfoFile Get( Pieces pieces )
		{
			return Items[pieces.Index];
		}


		public static TbInfoFile Current
		{
			get{ return Get( Settings.PiecesSrc ); }
		}


		public static string GetPieceGroupReordering( Pieces p, bool wtm )
		{
			string s = wtm ? TbInfoFileList.Get( p ).PieceGroupReoredStringWtm : TbInfoFileList.Get( p ).PieceGroupReoredStringBtm;
			if ( s==null || s.Length<2 )
				return PieceGroupReorder.Get( p, wtm, PieceGroupReorderType.CompressionOptimized ).GetString(p);
			else
				return s;
		}


		private static void Load()
		{
			int index = 1;
			try {
				if ( File.Exists( App.Md5AndOtherInfosFile ) ) {
					header = "";
					string[] lines = File.ReadAllLines( App.Md5AndOtherInfosFile );
					for ( int i=0 ; i<lines.Length ; i++ ) {
						if ( lines[i].Trim().Length==0 )
							continue;
						else if ( lines[i].StartsWith( "//" ) )
							header += lines[i] + "\r\n";
						else if ( index < Items.Length ) {
							Items[index] = new TbInfoFile( lines[i].Substring(10), index );
							index++;
						}
					}
				}
			}
			catch {
				Message.Text( "Failed to load \"" + App.Md5AndOtherInfosFile + "\"" );
			}
			while ( index < Items.Length ) {
				Items[index] = new TbInfoFile( "", index );
				index++;
			}
		}


		public static void Save()
		{
			StringBuilder sb = new StringBuilder( header );

			for ( int i=1 ; i<Items.Length ; i++ )
				sb.AppendLine( Pieces.FromIndex(i).ToString().PadRight(10) + Items[i].ToString() );

			File.WriteAllText( App.Md5AndOtherInfosFile, sb.ToString() );
		}

	}
}
