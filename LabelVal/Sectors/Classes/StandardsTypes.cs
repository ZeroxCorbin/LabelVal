using System.ComponentModel;

namespace LabelVal.Sectors.Classes;

public enum StandardsTypes
{
    [Description("None")]
    None,
    [Description("Unsupported")]
    Unsupported, //Unsupported table
    [Description("ISO/IEC 15415 & 15416")]
    ISO15415_15416,
    [Description("ISO/IEC 15415 (2D)")]
    ISO15415,
    [Description("ISO/IEC 15416 (1D)")]
    ISO15416,
    [Description("GS1")]
    GS1,
    [Description("OCR/OCV")]
    OCR_OCV,
    [Description("ISO/IEC 29158 (DPM)")]
    ISO29158,
}
