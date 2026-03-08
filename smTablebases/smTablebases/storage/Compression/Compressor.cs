using System;
using System.IO;
using System.IO.Compression;
using TBacc;


namespace smTablebases
{
    public class Compressor
    {
        public     string                                   Info;
        private    byte[]                                   bufferIn, bufferOut;
        private    int[]                                    posToVirtualPos;
        private    CompressionType                          compressionType;
        private    LC.Encoder                               lcEncoder;
        public     volatile object                          Tag;
        private    int                                      bufferOutDataLength;
        private    long                                     uncompressedBytesSum=0;
        private    System.IO.Compression.CompressionLevel   deflateCompressionLevel;
        private    int                                      brotliQuality;
        private    RecalcResults                            recalcResults;



        public int[] PosToVirtualPos
        {
            get { return posToVirtualPos; }
            set { posToVirtualPos = value; }
        }


        public Compressor( CompressionType type, int maxUncompressedBytes )
        {
            this.compressionType      = type;
            bufferIn  = new byte[ maxUncompressedBytes      ];
            bufferOut = new byte[ maxUncompressedBytes + 10 ];

            recalcResults = Settings.CompressRecalculateRes;
            switch ( compressionType ) {
                case CompressionType.LC:
                    int bytesPerItem = Settings.LcBytesPerItem;
                    lcEncoder = new LC.Encoder();
                    lcEncoder.Settings.BytesPerItem = bytesPerItem;
                    lcEncoder.Settings.LiteralPosBits = Settings.LcLiteralPosBits;
                    lcEncoder.Settings.Level = Settings.LcLevel;
                    lcEncoder.Settings.LengthSet = (LC.LengthSet)Settings.LcLengthSet;
                    lcEncoder.Settings.PrevByteHighBits = Settings.LcPrevByteHighBits;
                    break;
                case CompressionType.Deflate:
                    deflateCompressionLevel = Settings.DeflateCompressionLevel;
                    break;
                case CompressionType.Brotli:
                    brotliQuality = Settings.BrotliQuality;
                    break;
                default:
                    break;
            }
        }


        public int Compress( int count )
        {
            uncompressedBytesSum += count;
            Info = "";
            int countBytes;
            //File.WriteAllBytes("c:\\A\\tmp\\in_" + count.ToString() + ".bin", bufferIn[0..count]);
            switch ( compressionType ) {


                case CompressionType.LC:
                    countBytes = lcEncoder.Encode( bufferIn, count, bufferOut, (recalcResults==RecalcResults.ZeroOut) ?  posToVirtualPos : null );
                    Info = lcEncoder.Info;
                    break;


                case CompressionType.Deflate:
                    using( MemoryStream msOut = new MemoryStream( bufferOut, 0, bufferOut.Length ) ) {
                        using ( DeflateStream defStream = new DeflateStream( msOut, deflateCompressionLevel, true ) ) {
                            if ( recalcResults == RecalcResults.ZeroOut ) {
                                using InjectionStream inj = new InjectionStream(bufferIn, count, posToVirtualPos);
                                inj.CopyTo(defStream);
                            }
                            else {
                                defStream.Write(bufferIn, 0, count);
                            }
                        }
                        countBytes = (int)msOut.Position;
                    }
                    Array.Copy( BitConverter.GetBytes(count), 0, bufferOut, countBytes, 4 );
                    countBytes += 4;
                    break;


                case CompressionType.Brotli:
                    if (recalcResults == RecalcResults.ZeroOut) {
                        using InjectionStream inj = new InjectionStream(bufferIn, count, posToVirtualPos);
                        byte[] tmp = new byte[inj.Length];
                        int cnt = inj.Read( tmp, 0, tmp.Length );
                        if ( cnt != tmp.Length )
                            throw new Exception( "Unexpected length" );
                        if ( !BrotliEncoder.TryCompress( tmp, bufferOut, out countBytes, brotliQuality, 24 ) )
                            throw new Exception("Brotli compression failed");
                    }
                    else {
                        if ( !BrotliEncoder.TryCompress( bufferIn[0..count], bufferOut, out countBytes, brotliQuality, 24 ) )
                            throw new Exception( "Brotli compression failed" );
                    }
                    break;


                case CompressionType.NoCompression:
                    for ( long i=0; i<count ; i++ )
                        bufferOut[i] = bufferIn[i];
                    countBytes = count;
                    break;

                default:
                    throw new Exception();
            }

            return bufferOutDataLength=countBytes;
        }


        public long UncompressedBytesSum
        {
            get { return uncompressedBytesSum; }
        }


        public byte[] BufferIn
        {
            get { return bufferIn; }
        }

        public byte[] BufferOut
        {
            get { return bufferOut; }
        }

        public int BufferOutDataLength
        {
            get { return bufferOutDataLength; }
        }
    }
}
