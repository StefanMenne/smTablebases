using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TBacc;
using System.Security.Cryptography;
using System.IO;

namespace smTablebases
{
	public class TasksMd5 : TasksTaBaRead
	{
		public   TaBasesRead     TaBasesRead;
		public   Pieces          Pieces;
		public   static byte[]   Hash;

		private  byte[][]        buffer;


		public TasksMd5( Pieces pieces ) : base( null )
		{
			this.Pieces = pieces;
		}


		public override int ThreadCount
		{
			get { return Settings.ThreadCount; }
		}


		public byte[] GetBuffer( int threadIndex )
		{
			return buffer[threadIndex];
		}



		public override MyTask[] Init( int threadCount )
		{
			try {
				TaBasesRead = TaBasesRead.OpenSingle( Pieces, Settings.ThreadCount );
			}
			catch ( Exception ex ) { 
				Message.Line( ex.Message );
				return new MyTaskMd5[0];
			}
			if ( TaBasesRead==null )
				return new MyTaskMd5[0];
			List<MyTask> list = new List<MyTask>();
			foreach ( bool wtm in Tools.BoolArray )
				for ( WkBk wkBk = WkBk.First(Pieces); wkBk < wkBk.Count; wkBk++ )
					list.Add( new MyTaskMd5( wkBk, Pieces, wtm) );
			NumerizeSteps(list);
			Hash = new byte[ 16*list.Count ];
			buffer = new byte[threadCount][];
			for ( int i=0 ; i<buffer.Length ; i++ )
				buffer[i] = new byte[65536];

			tasks = list.ToArray();
			GenerateInfo( tasks, threadCount );
			return tasks;
		}



		public override void FinishedAllTasks( bool aborted )
		{
			base.FinishedAllTasks( aborted );

			if ( aborted ) {
				TaBasesRead.CloseAll( false );
				return;
			}


			if ( TaBasesRead == null ) {
				Hash = null; 
			}
			else{
				MD5 md5 = MD5.Create();
				Hash = md5.ComputeHash(Hash);


				TaBasesRead.CloseAll( false );
			}		
		}
	}
}
