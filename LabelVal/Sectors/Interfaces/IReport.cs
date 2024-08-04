using Newtonsoft.Json;

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
    public Fields Fields { get; set; }
    public string Error { get; set; }
}

public class Fields
{
    [JsonProperty("01")]
    public string _01 { get; set; }
    [JsonProperty("90")]
    public string _90 { get; set; }
    [JsonProperty("10")]
    public string _10 { get; set; }
}

public interface IReport
{
    string Type { get; set; }
    string SymbolType { get; set; }

    double Top { get; set; }
    double Left { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    double AngleDeg { get; set; }

    string Text { get; set; }
    string DecodeText { get; set; }

    string FormattedOut { get; set; }

    double Score { get; set; }
    double XDimension { get; set; }
    double Aperture { get; set; }

    string OverallGradeString { get; set; }
    double OverallGradeValue { get; set; }
    string OverallGradeLetter { get; set; }

    int BlemishCount { get; set; }

    Gs1results GS1Results { get; set; }
    ModuleData ExtendedData { get; set; }
}
