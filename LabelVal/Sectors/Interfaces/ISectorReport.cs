using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;

namespace LabelVal.Sectors.Interfaces;

public interface ISectorReport
{
    object Original { get; }

    AvailableDevices Device { get; }
    AvailableRegionTypes RegionType { get; }
    AvailableSymbologies SymbolType { get; }

    AvailableStandards Standard { get; }
    AvailableTables GS1Table { get; }

    OverallGrade OverallGrade { get; }

    double XDimension { get; }
    double Aperture { get; }
    AvailableUnits Units { get; }

    double Top { get; }
    double Left { get; }
    double Width { get; }
    double Height { get; }
    double AngleDeg { get; }

    string DecodeText { get; }

    //GS1
    GS1Decode GS1Results { get; }

    //OCR\OCV
    string Text { get; }
    double Score { get; }

    //Blemish
    int BlemishCount { get; }

    //V275 2D module data
    ModuleData ExtendedData { get; }
}

