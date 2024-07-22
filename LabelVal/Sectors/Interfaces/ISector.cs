using System.ComponentModel;

namespace LabelVal.Sectors.Interfaces;

public enum StandardsTypes
{
    [Description("None")]
    None,
    [Description("Unsupported")]
    Unsupported, //Unsupported table
    [Description("ISO/IEC 15415 & 15416")]
    ISO15415_15416,
    [Description("ISO/IEC 15415")]
    ISO15415,
    [Description("ISO/IEC 15416")]
    ISO15416,
    [Description("GS1")]
    GS1,
    [Description("OCR / OCV")]
    OCR_OCR,
}

public enum GS1TableNames
{
    [Description("None")]
    None,
    [Description("Unsupported")]
    Unsupported, //Unsupported table
    [Description("1")]
    _1, //Trade items scanned in General Retail POS and NOT General Distribution.
    [Description("1.8200")]
    _1_8200, //AI (8200)
    [Description("2")]
    _2, //Trade items scanned in General Distribution.
    [Description("3")]
    _3, //Trade items scanned in General Retail POS and General Distribution.
    [Description("4")]
    _4,
    [Description("5")]
    _5,
    [Description("6")]
    _6,
    [Description("7.1")]
    _7_1,
    [Description("7.2")]
    _7_2,
    [Description("7.3")]
    _7_3,
    [Description("7.4")]
    _7_4,
    [Description("8")]
    _8,
    [Description("9")]
    _9,
    [Description("10")]
    _10,
    [Description("11")]
    _11,
    [Description("12.1")]
    _12_1,
    [Description("12.2")]
    _12_2,
    [Description("12.3")]
    _12_3
}

public interface ISector
{
    ITemplate Template { get; }
    IReport Report { get; }

    bool IsWarning { get; }
    bool IsError { get; }

    StandardsTypes DesiredStandard { get; }
    GS1TableNames DesiredGS1Table { get; }

    StandardsTypes Standard { get; }
    GS1TableNames GS1Table { get; }

    bool IsGS1Standard { get; }
    bool IsWrongStandard { get; }

    ISectorDifferences SectorDifferences { get; }
}