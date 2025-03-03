using LabelVal.Sectors.Classes;

namespace LabelVal.Sectors.Interfaces;

public interface IReport
{
    object Original { get; set; }

    string Type { get; set; }
    string SymbolType { get; set; }
    double XDimension { get; set; }
    double Aperture { get; set; }
    string Units { get; set; }

    double Top { get; set; }
    double Left { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    double AngleDeg { get; set; }

    string OverallGradeString { get; set; }
    double OverallGradeValue { get; set; }
    string OverallGradeLetter { get; set; }

    string DecodeText { get; set; }

    StandardsTypes Standard { get; set; }
    Gs1TableNames GS1Table { get; set; }

    //GS1
    Gs1Results GS1Results { get; set; }

    //OCR\OCV
    string Text { get; set; }
    double Score { get; set; }

    //Blemish
    int BlemishCount { get; set; }

    //V275 2D module data
    ModuleData ExtendedData { get; set; }
}

