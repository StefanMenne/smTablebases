using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Windows;


namespace TBacc
{
    public sealed class Storage
    {
        private FileStream        fs;
        private string            filename;

        public Storage( string filename, bool create, int readWriteTmpBufferSize )
        {
            this.filename    = filename;

            if ( create )
                fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, readWriteTmpBufferSize, FileOptions.RandomAccess);
            else {
                fs = new FileStream( filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, readWriteTmpBufferSize, FileOptions.RandomAccess );
            }
        }


        public long Position
        {
            get{ return fs.Position; }
            set{ fs.Seek( value, SeekOrigin.Begin ); }
        }

        public void SaveInt( long byteOffset, int value )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            WriteInt( value );
        }

        public void Save( long byteOffset, byte[] buffer, long countArrayItemsToWrite )
        {
            if ( byteOffset != -1 )
                fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                fs.WriteByte( buffer[i] );
        }



        public void Save( long byteOffset, int[] buffer, long countArrayItemsToWrite )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                WriteInt( buffer[i] );
        }


        public void Save( long byteOffset, long[] buffer, long countArrayItemsToWrite )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                WriteLong( buffer[i] );
        }


        public void Save( long byteOffset, uint[] buffer, long countArrayItemsToWrite )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                WriteUInt( buffer[i] );
        }

        public int Get( long index )
        {
            fs.Seek( index, SeekOrigin.Begin );
            return ReadInt();
        }


        public int LoadInt( long byteOffset )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            return ReadInt();
        }


        public void Load( long byteOffset, byte[] buffer, long countArrayItemsToWrite )
        {
            if ( byteOffset != -1L )
                fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                 buffer[i] = (byte)fs.ReadByte();
        }


        public void Load( long byteOffset, Int16[] buffer, long countArrayItemsToWrite )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                 buffer[i] = ReadInt16();
        }


        public void Load( long byteOffset, int[] buffer, long countArrayItemsToWrite )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                 buffer[i] = ReadInt();
        }

        public void Load( long byteOffset, long[] buffer, long countArrayItemsToWrite )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                 buffer[i] = ReadLong();
        }


        public void Load( long byteOffset, uint[] buffer, long countArrayItemsToWrite )
        {
            fs.Seek( byteOffset, SeekOrigin.Begin );
            for ( int i=0 ; i<countArrayItemsToWrite ; i++ )
                 buffer[i] = ReadUInt();
        }


        public void Close( bool dontSaveToDisk )
        {
            fs.Close();
            fs.Dispose();
        }


        private Int16 ReadInt16()
        {
            return (Int16)(fs.ReadByte() | (fs.ReadByte()<<8)) ;
        }


        public int ReadInt()
        {
            return fs.ReadByte() | (fs.ReadByte()<<8) | (fs.ReadByte()<<16) | (fs.ReadByte()<<24);
        }

        private uint ReadUInt()
        {
            return (uint)fs.ReadByte() | (uint)(fs.ReadByte()<<8) | (uint)(fs.ReadByte()<<16) | (uint)(fs.ReadByte()<<24);
        }

        public void WriteInt( int val )
        {
            fs.WriteByte( (byte) (val&0xff)       );
            fs.WriteByte( (byte) ((val>>8)&0xff)  );
            fs.WriteByte( (byte) ((val>>16)&0xff) );
            fs.WriteByte( (byte) ((val>>24)&0xff) );
        }

        private void WriteUInt( uint val )
        {
            fs.WriteByte( (byte) (val&0xff)       );
            fs.WriteByte( (byte) ((val>>8)&0xff)  );
            fs.WriteByte( (byte) ((val>>16)&0xff) );
            fs.WriteByte( (byte) ((val>>24)&0xff) );
        }

        public long ReadLong()
        {
            return ((long)(uint)fs.ReadByte()) | (((long)(uint)fs.ReadByte())<<8) | (((long)fs.ReadByte())<<16) | (((long)fs.ReadByte())<<24) | (((long)fs.ReadByte())<<32) | (((long)fs.ReadByte())<<40) | (((long)fs.ReadByte())<<48) | (((long)fs.ReadByte())<<56); 
        }


        public void WriteLong( long val )
        {
            fs.WriteByte( (byte) (val&0xff)       );
            fs.WriteByte( (byte) ((val>>8)&0xff)  );
            fs.WriteByte( (byte) ((val>>16)&0xff) );
            fs.WriteByte( (byte) ((val>>24)&0xff) );
            fs.WriteByte( (byte) ((val>>32)&0xff) );
            fs.WriteByte( (byte) ((val>>40)&0xff) );
            fs.WriteByte( (byte) ((val>>48)&0xff) );
            fs.WriteByte( (byte) ((val>>56)&0xff) );
        }
    }
}