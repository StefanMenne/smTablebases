using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using Avalonia.Controls;
using TBacc;


namespace smTablebases
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class Settings
    {
        private static Settings instance = new Settings();

        private bool                        initialized                                  = false;
        private int                         tbIndex                                      = 0;
        private Pieces                      pieces                                       = Pieces.Instances[1];
        private TbInfoList?                 tbInfo                                       = null;
        private bool                        track                                        = false;
        private string                      trackText                                    = "KPKP C2 A6 A1 B7 wtm";
        private int                         threadCount                                  = System.Environment.ProcessorCount;
        private int                         threadCountCompression                       = 99;
        private StopType                    stopType                                     = StopType.Never;
        private int                         memoryMb                                     = 3000;
        private string                      tmpFolder                                    = "";
        private CompressionType             compressionType                              = CompressionProfile.DefaultCompressionType;
        private bool                        showResIndexCount                            = false;
        private bool                        verifyJoinUnjoin                             = true;
        private int                         readBuffer                                   = 4 * 1024;
        private int                         readWriteTmpBuffer                           = 4 * 1024;
        private int                         writeBuffer                                  = 4 * 1024;
        private bool                        saveUncompressedBlocks                       = false;
        private bool                        saveDataChunksAtMd5                          = false;
        private bool                        genAllPieceGroupReorderings                    = false;
        private bool                        largeObjectHeapCompaction                    = true;
        private double                      windowTop, windowLeft, windowHeight, windowWidth;
        private WindowState                 windowState;


        // Compression
        private RecalcResults               compByRecalcRes                               = CompressionProfile.DefaultRecalcRes;
        private int                         compDataLength                                = -1;
        private bool                        compLog                                       = false;
        private int                         lcLevel                                       = 1;
        private int                         lcBytesPerItem                                = 1;
        private int                         lcLengthSet                                   = 0;
        private int                         lcLiteralPosBits                              = 0;
        private int                         lcPrevByteHighBits                            = 3;
        private bool                        lcCompareCodingStateWithCodingStateImmutable  = true;
        private bool                        lcShowDetailedExpDistCosts                    = false;
        private System.IO.Compression.CompressionLevel deflateCompressionLevel            = System.IO.Compression.CompressionLevel.SmallestSize;
        private int                         brotliQuality                                 = CompressionProfile.DefaultBrotliQuality;


        public static int TbIndex
        {
            get { return instance.tbIndex; }
            set{ instance.pieces = Pieces.Instances[ (instance.tbIndex = value) + 1 ]; }
        }

        public static Pieces PiecesSrc => instance.pieces;

        public static bool VerifyJoinUnjoin
        {
            get { return instance.verifyJoinUnjoin; }
            set { instance.verifyJoinUnjoin = value; }
        }
        public static int ReadBuffer
        {
            get { return instance.readBuffer; }
            set { Config.ReadBufferSize = instance.readBuffer = value; }
        }
        public static int ReadWriteTmpBuffer
        {
            get { return instance.readWriteTmpBuffer; }
            set { instance.readWriteTmpBuffer = value; }
        }
        public static int WriteBuffer
        {
            get { return instance.writeBuffer; }
            set { instance.writeBuffer = value; }
        }
        public static int CompDataLength
        {
            get { return instance.compDataLength; }
            set { instance.compDataLength = value; }
        }
        public static bool CompLog
        {
            get { return instance.compLog; }
            set { instance.compLog = value; }
        }
        public static int LcLevel
        {
            get { return instance.lcLevel; }
            set { instance.lcLevel = value; }
        }
        public static int LcBytesPerItem
        {
            get { return instance.lcBytesPerItem; }
            set { instance.lcBytesPerItem = value; }
        }
        public static int LcLengthSet
        {
            get { return instance.lcLengthSet; }
            set { instance.lcLengthSet = value; }
        }
        public static int LcLiteralPosBits
        {
            get { return instance.lcLiteralPosBits; }
            set { instance.lcLiteralPosBits = value; }
        }
        public static int LcPrevByteHighBits
        {
            get { return instance.lcPrevByteHighBits; }
            set { instance.lcPrevByteHighBits = value; }
        }
        public static bool LcCompareCodingStateWithCodingStateImmutable
        {
            get { return instance.lcCompareCodingStateWithCodingStateImmutable; }
            set { instance.lcCompareCodingStateWithCodingStateImmutable = value; }
        }
        public static bool SaveUncompressedBlocks
        {
            get { return instance.saveUncompressedBlocks; }
            set { instance.saveUncompressedBlocks = value; }
        }
        public static bool SaveDataChunksAtMd5
        {
            get { return instance.saveDataChunksAtMd5; }
            set { Config.SaveDataChunksAtMd5 = instance.saveDataChunksAtMd5 = value; }
        }
        public static bool GenAllPieceGroupReorderings
        {
            get { return instance.genAllPieceGroupReorderings; }
            set { instance.genAllPieceGroupReorderings = value; }
        }
        public static bool LargeObjectHeapCompaction
        {
            get { return instance.largeObjectHeapCompaction; }
            set { instance.largeObjectHeapCompaction = value; }
        }
        public static bool LcShowDetailedExpDistCosts
        {
            get { return instance.lcShowDetailedExpDistCosts; }
            set { instance.lcShowDetailedExpDistCosts = value; }
        }

        public static System.IO.Compression.CompressionLevel DeflateCompressionLevel
        {
            get { return instance.deflateCompressionLevel; }
            set { instance.deflateCompressionLevel = value; }
        }


        public static int BrotliQuality
        {
            get { return instance.brotliQuality; }
            set { instance.brotliQuality = value; }
        }


        public static void LoadXml( string file )
        {
            try {
                if ( File.Exists(file) ) {
                    instance.Load( File.ReadAllText(file) );
                }
            }
            catch
            {
                instance = new Settings();
            }

            Config.ReadBufferSize        = instance.readBuffer;
            Config.SaveDataChunksAtMd5   = instance.saveDataChunksAtMd5;
        }


        private Settings()
        {
            tbInfo = new TbInfoList();
        }

        private void Load( string xmlString )
        {
            try
            {
                using StringReader sr = new StringReader(xmlString);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load( sr );
                XmlNode? root = xmlDoc.DocumentElement;
                foreach (XmlNode node in root!.ChildNodes ) {
                    if ( node.Name == "TablebaseIndex" ) {
                        tbIndex = int.Parse( node.InnerText, CultureInfo.InvariantCulture );
                        pieces = Pieces.Instances[tbIndex + 1];
                    }
                    else if ( node.Name == "TbInfoList" ) {
                        tbInfo = new TbInfoList( node );
                    }
                    else if ( node.Name == "Track" ) {
                        track = bool.Parse( node.InnerText );
                    }
                    else if ( node.Name == "TrackText" ) {
                        trackText = node.InnerText;
                    }
                    else if ( node.Name == "ThreadCount" )
                    {
                        threadCount = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if ( node.Name == "ThreadCountCompression" )
                    {
                        threadCountCompression = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "CompressionType")
                    {
                        compressionType = CompressionTypeStrings.Parse( node.InnerText );
                    }
                    else if (node.Name == "StopType")
                    {
                        stopType = Enum.Parse<StopType>(node.InnerText, ignoreCase: true);
                    }
                    else if ( node.Name == "Memory" )
                    {
                        memoryMb = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if ( node.Name == "TmpFolder" )
                    {
                        tmpFolder = node.InnerText;
                    }
                    else if ( node.Name == "WindowTop" )
                    {
                        windowTop = double.Parse( node.InnerText, CultureInfo.InvariantCulture );
                    }
                    else if ( node.Name == "WindowLeft" )
                    {
                        windowLeft = double.Parse( node.InnerText, CultureInfo.InvariantCulture );
                    }
                    else if ( node.Name == "WindowHeight" )
                    {
                        windowHeight = double.Parse( node.InnerText, CultureInfo.InvariantCulture );
                    }
                    else if ( node.Name == "WindowWidth" )
                    {
                        windowWidth = double.Parse( node.InnerText, CultureInfo.InvariantCulture );
                    }
                    else if ( node.Name == "WindowState" )
                    {
                        windowState = Enum.Parse<WindowState>(node.InnerText, ignoreCase: true);
                    }
                    else if ( node.Name == "ShowResIndexCount" ) {
                        showResIndexCount = bool.Parse( node.InnerText );
                    }
                    else if ( node.Name == "CompressRecalculateRes" ) {
                        compByRecalcRes = Enum.Parse<RecalcResults>( node.InnerText, ignoreCase: true );
                    }
                    else if (node.Name == "VerifyJoinUnjoin")
                    {
                        verifyJoinUnjoin = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "ReadBuffer")
                    {
                        readBuffer = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "ReadWriteTmpBuffer")
                    {
                        readWriteTmpBuffer = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "WriteBuffer")
                    {
                        writeBuffer = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "CompDataLength")
                    {
                        compDataLength = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "CompLog")
                    {
                        compLog = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "LcLevel")
                    {
                        lcLevel = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "LcBytesPerItem")
                    {
                        lcBytesPerItem = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "LcLengthSet")
                    {
                        lcLengthSet = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "LcLiteralPosBits")
                    {
                        lcLiteralPosBits = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "LcPrevByteHighBits")
                    {
                        lcPrevByteHighBits = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                    else if (node.Name == "LcCompareCodingStateWithCodingStateImmutable")
                    {
                        lcCompareCodingStateWithCodingStateImmutable = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "SaveUncompressedBlocks")
                    {
                        saveUncompressedBlocks = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "SaveDataChunksAtMd5")
                    {
                        saveDataChunksAtMd5 = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "GenAllPieceGroupReorderings")
                    {
                        genAllPieceGroupReorderings = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "LargeObjectHeapCompaction")
                    {
                        largeObjectHeapCompaction = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "LcShowDetailedExpDistCosts")
                    {
                        lcShowDetailedExpDistCosts = bool.Parse(node.InnerText );
                    }
                    else if (node.Name == "DeflateCompressionLevel")
                    {
                        deflateCompressionLevel = Enum.Parse<System.IO.Compression.CompressionLevel>( node.InnerText, ignoreCase: true );
                    }
                    else if (node.Name == "BrotliQuality")
                    {
                        brotliQuality = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }

                }
                sr.Close();
            }
            catch (Exception ex){
                MsgBox.Show( "Failed parsing settings.\r\n\r\n"+ ex.ToString() , "ERROR" );
            }

            initialized = true;
        }

        public static bool IsInitialized
        {
            get{ return instance.initialized; }
        }

        private string ToXml()
        {
            using StringWriter sw = new StringWriter();
            using (XmlTextWriter xmlWriter = new XmlTextWriter(sw) ) {
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                xmlWriter.WriteStartElement( "Settings" );
                xmlWriter.WriteStartElement( "TbInfoList" );
                tbInfo!.ToXml( xmlWriter );
                xmlWriter.WriteEndElement();
                xmlWriter.WriteElementString( "TablebaseIndex", tbIndex.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "Track", track.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "TrackText", trackText );
                xmlWriter.WriteElementString( "ThreadCount", threadCount.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "ThreadCountCompression", threadCountCompression.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "StopType", stopType.ToString() );
                xmlWriter.WriteElementString( "Memory", memoryMb.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "TmpFolder", tmpFolder );
                xmlWriter.WriteElementString( "CompressionType", CompressionTypeStrings.Get(compressionType) );
                xmlWriter.WriteElementString( "WindowTop", windowTop.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "WindowLeft", windowLeft.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "WindowHeight", windowHeight.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "WindowWidth", windowWidth.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "WindowState", windowState.ToString() );
                xmlWriter.WriteElementString( "ShowResIndexCount", showResIndexCount.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "CompressRecalculateRes", compByRecalcRes.ToString() );
                xmlWriter.WriteElementString( "VerifyJoinUnjoin", verifyJoinUnjoin.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "ReadBuffer", readBuffer.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "ReadWriteTmpBuffer", readWriteTmpBuffer.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "WriteBuffer", writeBuffer.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "CompDataLength", compDataLength.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "CompLog", compLog.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "LcLevel", lcLevel.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "LcBytesPerItem", lcBytesPerItem.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "LcLengthSet", lcLengthSet.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "LcLiteralPosBits", lcLiteralPosBits.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "LcPrevByteHighBits", lcPrevByteHighBits.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "lcCompareCodingStateWithCodingStateImmutable", lcCompareCodingStateWithCodingStateImmutable.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "SaveUncompressedBlocks", saveUncompressedBlocks.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteElementString( "SaveDataChunksAtMd5", saveDataChunksAtMd5.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "GenAllPieceGroupReorderings", genAllPieceGroupReorderings.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "LargeObjectHeapCompaction", largeObjectHeapCompaction.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "LcShowDetailedExpDistCosts", lcShowDetailedExpDistCosts.ToString(CultureInfo.InvariantCulture) );
                xmlWriter.WriteElementString( "DeflateCompressionLevel", deflateCompressionLevel.ToString() );

                xmlWriter.WriteElementString("BrotliQuality", brotliQuality.ToString(CultureInfo.InvariantCulture));

                xmlWriter.WriteEndElement();
            }
            sw.Close();
            return sw.ToString();
        }



        public static MenuItem GetBoolMenuItem( string header, string settingName, string? toopTip=null )
        {
            MenuItem mi = new MenuItem { Header = header };
            if ( toopTip != null )
                ToolTip.SetTip( mi, toopTip );

            mi.ToggleType = MenuItemToggleType.CheckBox;
            PropertyInfo pi = typeof( Settings ).GetProperty( settingName, BindingFlags.Public | BindingFlags.Static )!;
            mi.IsChecked = (bool) pi.GetValue( null )!;
            mi.Click += (_, _) => {
                pi.SetValue(null, mi.IsChecked);
            };
            return mi;
        }


        public static MenuItem GetIntMenuItem( string header, string settingName, string? tooltip, string[]? menuItemText, params int[] items )
        {
            MenuItem miTop = new MenuItem();
            PropertyInfo pi = typeof(Settings).GetProperty(settingName, BindingFlags.Public | BindingFlags.Static)!;
            miTop.Header = header;
            ToolTip.SetTip( miTop, tooltip );
            for ( int i=0 ; i<items.Length ; i++ ) {
                int currentValue = items[i];
                MenuItem mi = new MenuItem();
                if ( menuItemText==null )
                    mi.Header = currentValue.ToString( "###,###,###,##0", CultureInfo.InvariantCulture );
                else
                    mi.Header = menuItemText[i];
                mi.Tag = currentValue;
                mi.ToggleType = MenuItemToggleType.Radio;
                mi.Click += delegate{ pi.SetValue(null,currentValue); };
                miTop.Items.Add( mi );
            }
            miTop.SubmenuOpened += delegate{
                int val = (int)pi.GetValue(null)!;
                foreach ( MenuItem mi in miTop.Items! )
                    mi!.IsChecked = ((int)(mi.Tag!)) == val;
            };
            return miTop;
        }


        public static MenuItem GetEnumMenuItem( string header, string settingName, string tooltip, Type enumeration )
        {
            PropertyInfo pi = typeof(Settings).GetProperty(settingName, BindingFlags.Public | BindingFlags.Static)!;
            MenuItem miTop = new MenuItem { Header = header };
            ToolTip.SetTip( miTop, tooltip );
            string[] names = Enum.GetNames( enumeration );

            for ( int i=0 ; i<names.Length ; i++ ) {
                int item = i;
                MenuItem mi = new MenuItem { Header = names[i], Tag = i, ToggleType = MenuItemToggleType.Radio };
                mi.Click += delegate{ pi.SetValue(null, item ); };
                miTop.Items.Add( mi );
            }
            miTop.SubmenuOpened += delegate{
                int val = (int) pi.GetValue( null )!;
                foreach ( MenuItem mi in miTop.Items! )
                    mi!.IsChecked = ((int)(mi.Tag!)) == val;
            };
            return miTop;
        }


        public static string CompressionSettingsString
        {
            get{
                string s = CompressionTypeStrings.Get(CompressionType);
                return s;
            }
        }


        public static string TrackText
        {
            get{ return instance.trackText; }
            set{ instance.trackText = value; }
        }

        public static bool Track
        {
            get{ return instance.track; }
            set{ instance.track = value; }
        }


        public static int ThreadCount
        {
            get { return instance.threadCount; }
            set { instance.threadCount = value; }
        }

        public static int ThreadCountCompression
        {
            get { return instance.threadCountCompression; }
            set { instance.threadCountCompression = value; }
        }

        public static CompressionType CompressionType
        {
            get { return instance.compressionType; }
            set { instance.compressionType = value; }
        }

        public static StopType StopType
        {
            get { return instance.stopType; }
            set { instance.stopType = value; }
        }


        public static string TmpFolder
        {
            get { return instance.tmpFolder; }
            set { instance.tmpFolder = value; }
        }


        public static double WindowHeight
        {
            get { return instance.windowHeight; }
            set { instance.windowHeight = value; }
        }


        public static double WindowWidth
        {
            get { return instance.windowWidth; }
            set { instance.windowWidth = value; }
        }


        public static double WindowLeft
        {
            get { return instance.windowLeft; }
            set { instance.windowLeft = value; }
        }


        public static double WindowTop
        {
            get { return instance.windowTop; }
            set { instance.windowTop = value; }
        }


        public static WindowState WindowState
        {
            get { return instance.windowState; }
            set { instance.windowState = value; }
        }

        public static bool ShowResIndexCount
        {
            get { return instance.showResIndexCount; }
            set { instance.showResIndexCount = value; }
        }

        public static RecalcResults CompressRecalculateRes
        {
            get { return instance.compByRecalcRes; }
            set { instance.compByRecalcRes = value; }
        }


        public static int MemoryMb
        {
            get { return instance.memoryMb; }
            set {
                if ( value>=10 )
                    instance.memoryMb = value;
            }
        }

        public static TbInfoList TbInfo
        {
            get{ return instance.tbInfo!; }
        }


        public static void SaveXml( string file )
        {
            File.WriteAllText( file, instance.ToXml() );
        }

        public static string GetCompressionSettingsStr()
        {
            string s = $"RecalcRes={CompressRecalculateRes.ToString()}   " + CompressionTypeStrings.Get( CompressionType );

            if ( CompressionType == TBacc.CompressionType.Deflate )
                s += " (" + DeflateCompressionLevel.ToString() + ")";
            else if ( CompressionType == TBacc.CompressionType.Brotli )
                s += " (quality=" + BrotliQuality.ToString(CultureInfo.InvariantCulture) + ")";

            return s;
        }

    }
}
