using System.IO.Compression;
using System.Security.Cryptography;

namespace TBacc
{
    public class Decompressor
    {
        private    byte[]                               bufferIn, bufferOut;
        private    CompressionType                      compressionType = CompressionType.IllegalCompressionType; 
        private    LC.Decoder                           lcDecoder;
        private    int[]                                posToVirtualPos;
        public     RecalcResults                        RecalcRes;



        public Decompressor( int maxUncompressedBytes )
        {
            bufferIn        = new byte[ maxUncompressedBytes + 10 ];
            bufferOut       = new byte[ maxUncompressedBytes      ];
            posToVirtualPos = new int[maxUncompressedBytes];
        }


        public CompressionType CompressionType
        {
            get {  return compressionType; }
            set {
                if ( compressionType != value ) {
                    compressionType = value;
                    lcDecoder = null;
                    if ( compressionType == TBacc.CompressionType.LC )
                        lcDecoder = new LC.Decoder();
                }
            }
        }


        public int[] PosToVirtualPos
        {
            get { return posToVirtualPos; }
        }


        public int Decompress( int compressedDataSize )
        {
            int uncompressedSize = 0;

            switch ( compressionType ) {
                case CompressionType.LC:
                    uncompressedSize = lcDecoder.Decode( bufferIn, bufferOut, (RecalcRes==RecalcResults.ZeroOut)?posToVirtualPos:null );
                    break;


                case CompressionType.Deflate:
                    uncompressedSize = BitConverter.ToInt32( bufferIn, compressedDataSize - 4 );
                    using (MemoryStream msIn = new MemoryStream(bufferIn,0,compressedDataSize-4) ) {
                        using (DeflateStream defStream = new DeflateStream(msIn, CompressionMode.Decompress) ) {
                            if (RecalcRes == RecalcResults.ZeroOut) {
                                int uncompressedVirtualSize = posToVirtualPos[uncompressedSize - 1] + 1;
                                using ExtractionStream extStream = new ExtractionStream(bufferOut, uncompressedVirtualSize, posToVirtualPos);
                                defStream.CopyTo(extStream);
                            }
                            else {
                                defStream.ReadExactly(bufferOut, 0, uncompressedSize);
                            }
                        }
                    }
                    break;
        

                case CompressionType.Brotli:
                    if ( RecalcRes == RecalcResults.ZeroOut ) {
                        using MemoryStream msIn = new MemoryStream(bufferIn, 0, compressedDataSize);
                        using BrotliStream brotliStream = new BrotliStream( msIn, CompressionMode.Decompress );
                        using ExtractionStream extStream = new ExtractionStream(bufferOut, int.MaxValue, posToVirtualPos);
                        brotliStream.CopyTo(extStream);
                        uncompressedSize = extStream.Pos;
                    }
                    else {
                        if ( !BrotliDecoder.TryDecompress( bufferIn[0..compressedDataSize], bufferOut, out uncompressedSize ) )
                            throw new Exception("Brotli decompression failed");
                    }
                    break;


                case CompressionType.NoCompression:
                    uncompressedSize = compressedDataSize;
                    for ( long i=0; i<compressedDataSize ; i++ )
                        bufferOut[i] = bufferIn[i];
                    break;				
                default:
                    throw new Exception();
            }
            
            //File.WriteAllBytes("c:\\A\\tmp\\out_" + uncompressedSize.ToString() + ".bin", bufferOut[0..uncompressedSize]);
            return uncompressedSize;
        }


        public byte[] BufferIn
        {
            get { return bufferIn; }
        }


        public byte[] BufferOut
        {
            get { return bufferOut; }
        }
    }
}
