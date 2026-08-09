namespace IdExtractor.Modules.Extraction.Mrz;

/// <summary>ICAO 9303 machine-readable zone layouts.</summary>
public enum MrzFormat
{
    /// <summary>3 lines x 30 chars — national ID cards and similar.</summary>
    Td1,

    /// <summary>2 lines x 36 chars — older ID cards and visas.</summary>
    Td2,

    /// <summary>2 lines x 44 chars — passports.</summary>
    Td3
}
