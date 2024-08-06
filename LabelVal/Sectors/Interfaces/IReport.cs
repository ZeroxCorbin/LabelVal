using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LabelVal.Sectors.Interfaces;

public class ModuleData
{
    public int[] ModuleModulation { get; set; }
    public int[] ModuleReflectance { get; set; }

    public int QuietZone { get; set; }

    public int NumRows { get; set; }
    public int NumColumns { get; set; }

    public double CosAngle0 { get; set; }
    public double CosAngle1 { get; set; }

    public double SinAngle0 { get; set; }
    public double SinAngle1 { get; set; }

    public double DeltaX { get; set; }
    public double DeltaY { get; set; }

    public double Xne { get; set; }
    public double Yne { get; set; }

    public double Xnw { get; set; }
    public double Ynw { get; set; }

    public double Xsw { get; set; }
    public double Ysw { get; set; }
}

public class Gs1results
{
    public bool Validated { get; set; }
    public string Input { get; set; }
    public string FormattedOut { get; set; }
    public List<string> Fields { get; set; }
    public string Error { get; set; }
}


public interface IReport
{
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

    //GS1
    Gs1results GS1Results { get; set; }

    //OCR\OCV
    string Text { get; set; }
    double Score { get; set; }

    //Blemish
    int BlemishCount { get; set; }

    //V275 2D module data
    ModuleData ExtendedData { get; set; }
}
