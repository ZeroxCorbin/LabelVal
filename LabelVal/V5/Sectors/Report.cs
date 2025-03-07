using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;

namespace LabelVal.V5.Sectors;

public class Report : IReport
{
    public object Original { get; set; }
    public AvailableRegionTypes Type { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    public AvailableSymbologies SymbolType { get; set; }
    public double XDimension { get; set; }
    public double Aperture { get; set; }
    public string Units { get; set; }

    public string DecodeText { get; set; }

    public string OverallGradeString { get; set; }
    public double OverallGradeValue { get; set; }
    public string OverallGradeLetter { get; set; }

    public AvailableStandards? Standard { get; set; }
    public AvailableTables? GS1Table { get; set; }

    //GS1
    public Gs1Results GS1Results { get; set; }

    //OCR
    public string Text { get; set; }
    public double Score { get; set; }

    //Blemish
    public int BlemishCount { get; set; }

    //V275 2D module data
    public ModuleData ExtendedData { get; set; }

    public Report(V5_REST_Lib.Models.ResultsAlt.Decodedata v5)
    {
        Original = v5;

        Type = V5GetType(v5);
        SymbolType = v5.type.GetSymbology(AvailableDevices.V5);
        DecodeText = v5.dataUTF8;

        if (v5.boundingBox != null)
            (Top, Left, Width, Height) = ConvertBoundingBox(v5.boundingBox);

        if (v5.grading != null)
        {
            if (v5.grading.standard == "iso15416")
            {
                if (v5.grading.iso15416 == null || v5.grading.iso15416.overall == null)
                {
                    OverallGradeString = "No Grade";
                    if (v5.read)
                    {
                        OverallGradeValue = 0;
                        OverallGradeLetter = "A";
                    }
                    else
                    {
                        OverallGradeValue = 0;
                        OverallGradeLetter = "F";
                    }
                }
                else
                {
                    OverallGradeString = $"{v5.grading.iso15416.overall.grade:f1}/00/600";
                    OverallGradeValue = v5.grading.iso15416.overall.grade;
                    OverallGradeLetter = V5GetLetter(v5.grading.iso15416.overall.letter);
                }
            }
            else if (v5.grading.standard == "iso15415")
            {
                if (v5.grading.iso15415 == null)
                {
                    OverallGradeString = "No Grade";
                    if (v5.read)
                    {
                        OverallGradeValue = 0;
                        OverallGradeLetter = "A";
                    }
                    else
                    {
                        OverallGradeValue = 0;
                        OverallGradeLetter = "F";
                    }
                }
                else
                {
                    if (v5.grading.iso15415.overall != null)
                    {
                        OverallGradeString = $"{v5.grading.iso15415.overall.grade:f1}/00/600";
                        OverallGradeValue = v5.grading.iso15415.overall.grade;
                        OverallGradeLetter = V5GetLetter(v5.grading.iso15415.overall.letter);
                    }
                }
            }
            else if (v5.grading.standard == "iso29158")
            {
                if (v5.grading.format == "grade")
                {

                    OverallGradeString = "";
                    OverallGradeValue = double.Parse(v5.grading.grade);
                    OverallGradeLetter = GetLetter(float.Parse(v5.grading.grade));
                }
                else
                {
                    OverallGradeString = "No Grade";
                    if (v5.read)
                    {
                        OverallGradeValue = 0;
                        OverallGradeLetter = "A";
                    }
                    else
                    {
                        OverallGradeValue = 0;
                        OverallGradeLetter = "F";
                    }
                }
            }
            else
            {
                OverallGradeString = "Unkown";
                if (v5.read)
                {
                    OverallGradeValue = 0;
                    OverallGradeLetter = "A";
                }
                else
                {
                    OverallGradeValue = 0;
                    OverallGradeLetter = "F";
                }
            }
        }
        else
        {
            OverallGradeString = "Grade Disabled";
            if (v5.read)
            {
                OverallGradeValue = 0;
                OverallGradeLetter = "A";
            }
            else
            {
                OverallGradeValue = 0;
                OverallGradeLetter = "F";
            }
        }
    }
    public static (double Top, double Left, double Width, double Height) ConvertBoundingBox(V5_REST_Lib.Models.ResultsAlt.Boundingbox[] corners)
    {
        if (corners.Length != 4)
        {
            throw new ArgumentException("Bounding box must have exactly 4 corners.");
        }

        double minX = corners.Min(point => point.x);
        double minY = corners.Min(point => point.y);
        double maxX = corners.Max(point => point.x);
        double maxY = corners.Max(point => point.y);

        double width = maxX - minX;
        double height = maxY - minY;

        return (minY, minX, width, height);
    }
    private static string V5GetSymbology(string type) => type == "Datamatrix" ? "DataMatrix" : type;
    private static AvailableRegionTypes V5GetType(V5_REST_Lib.Models.ResultsAlt.Decodedata Report) =>
        Report.Code128 != null
        ? AvailableRegionTypes._1D
        : Report.Datamatrix != null
        ? AvailableRegionTypes._2D
        : Report.QR != null ? AvailableRegionTypes._2D : Report.PDF417 != null ? AvailableRegionTypes._1D : Report.UPC != null ? AvailableRegionTypes._1D : AvailableRegionTypes._1D;

    private static string V5GetLetter(int grade) =>
        grade switch
        {
            65 => "A",
            66 => "B",
            67 => "C",
            68 => "D",
            70 => "F",
            _ => throw new NotImplementedException(),
        };

    private static string GetLetter(float value) =>
    value == 4.0f
    ? "A"
    : value is <= 3.9f and >= 3.0f
    ? "B"
    : value is <= 2.9f and >= 2.0f
    ? "C"
    : value is <= 1.9f and >= 1.0f
    ? "D"
    : value is <= 0.9f and >= 0.0f
    ? "F"
    : "F";

}
