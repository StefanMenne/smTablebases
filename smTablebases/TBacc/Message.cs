﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace TBacc
{
    public static class Message
    {
        private static int          charInLine = 0;
        private static int          spaceToAdd = 0;
        public static StringBuilder MessagesStringBuilder = new StringBuilder();
        public static StringBuilder LogStringBuilder = new StringBuilder();

        
        public static void Line( int offset, string line )
        {
            Line( new string( ' ', Math.Max(offset-charInLine,0) )  + line );
        }

        public static void Text( int offset, string text )
        {
            Text( new string( ' ', Math.Max(offset-charInLine,0) ) + text );
        }

        
        public static void Clear()
        {
            lock (MessagesStringBuilder)
            {
                MessagesStringBuilder.Clear();
            }
        }
        
        public static void Line()
        {
            Line( "" );
        }

        public static void Line( string line )
        {
            Text( line + "\r\n" );
            charInLine = 0;
        }

        public static void Text( string txt )
        {
            if ( spaceToAdd!=0 ) {
                string s = new string( ' ', spaceToAdd ) + txt ;
                spaceToAdd = 0;
                Text( s );
                return;
            }
            //Thread.Sleep(30000);
            lock (MessagesStringBuilder)
            {
                MessagesStringBuilder.Append(txt);
            }
            charInLine += txt.Length;
        }

        public static void InsertDebugLine( string text )
        {
            int cil = charInLine;
            if ( cil != 0 )
                Line( "" );
            if ( spaceToAdd!=0 ) {
                cil = spaceToAdd;
                spaceToAdd = 0;
            }
            Line( text );
            if ( cil != 0 )
                spaceToAdd = cil;
        }


        public static void AddLogLine( string line )
        {
            lock (LogStringBuilder)
            {
                if ( LogStringBuilder.Length > 10000 )
                    LogStringBuilder.Remove( 0, 5000 );
                LogStringBuilder.Append( line + Environment.NewLine );
            }
        }
    }
}
