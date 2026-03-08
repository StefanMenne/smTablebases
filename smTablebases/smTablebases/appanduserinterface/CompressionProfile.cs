using TBacc;

namespace smTablebases;

public static class CompressionProfile
{
    public static readonly CompressionType DefaultCompressionType = CompressionType.Brotli;
    public static readonly RecalcResults   DefaultRecalcRes = RecalcResults.Remove;
    public static readonly int DefaultBrotliQuality = 6;

    public static bool IsHigh
    {
        get
        {
            bool match = Settings.CompressionType == CompressionType.LC;
            match &= Settings.CompressRecalculateRes == RecalcResults.ZeroOut;
            match &= Settings.LcLevel == 1;
            match &= Settings.LcBytesPerItem == 1;
            match &= Settings.LcLengthSet == 0;
            match &= Settings.LcLiteralPosBits == 0;
            match &= Settings.LcPrevByteHighBits == 3;

            return match;
        }
    }
    public static void SetHigh()
    {
        Settings.CompressionType = CompressionType.LC;
        Settings.CompressRecalculateRes = RecalcResults.ZeroOut;
        Settings.LcLevel = 1;
        Settings.LcBytesPerItem = 1;
        Settings.LcLengthSet = 0;
        Settings.LcLiteralPosBits = 0;
        Settings.LcPrevByteHighBits = 3;
    }
    public static bool IsMedium
    {
        get
        {
            bool match = Settings.CompressionType == CompressionType.LC;
            match &= Settings.CompressRecalculateRes == RecalcResults.ZeroOut;
            match &= Settings.LcLevel == 4;
            match &= Settings.LcBytesPerItem == 1;
            match &= Settings.LcLengthSet == 0;
            match &= Settings.LcLiteralPosBits == 0;
            match &= Settings.LcPrevByteHighBits == 3;
            return match;
        }
    }
    public static void SetMedium()
    {
        Settings.CompressionType = CompressionType.LC;
        Settings.CompressRecalculateRes = RecalcResults.ZeroOut;
        Settings.LcLevel = 4;
        Settings.LcBytesPerItem = 1;
        Settings.LcLengthSet = 0;
        Settings.LcLiteralPosBits = 0;
        Settings.LcPrevByteHighBits = 3;
    }
    public static bool IsLow
    {
        get
        {
            bool match = Settings.CompressionType == CompressionType.Brotli;
            match &= Settings.CompressRecalculateRes == RecalcResults.Remove;
            match &= Settings.BrotliQuality == 6;

            return match;
        }
    }
    public static void SetLow()
    {
        Settings.CompressionType = CompressionType.Brotli;
        Settings.CompressRecalculateRes = RecalcResults.Remove;
        Settings.BrotliQuality = 6;
    }





}
