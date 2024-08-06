using LabelVal.Sectors.Interfaces;
using System;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.V5.Sectors;

public class Report : IReport
{
    public string Type { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

    //Verify1D, Verify2D
    public string SymbolType { get; set; }
    public double XDimension { get; set; }
    public double Aperture { get; set; }
    public string Units { get; set; }

    public string DecodeText { get; set; }

    public string OverallGradeString { get; set; }
    public double OverallGradeValue { get; set; }
    public string OverallGradeLetter { get; set; }

    //GS1
    public Gs1results GS1Results { get; set; }

    //OCR
    public string Text { get; set; }
    public double Score { get; set; }

    //Blemish
    public int BlemishCount { get; set; }

    //V275 2D module data
    public ModuleData ExtendedData { get; set; }

    public Report(V5_REST_Lib.Models.ResultsAlt.Decodedata v5)
    {
        Type = V5GetType(v5);
        SymbolType = V5GetSymbology(v5.type);
        DecodeText = v5.dataUTF8;

        // Create the Rect
        Rect rect = new(v5.x - v5.width / 2, v5.y - v5.height / 2, v5.width, v5.height);

        // Create the RotateTransform
        RotateTransform rotateTransform = new(v5.angleDeg, v5.x, v5.y);

        // Apply the rotation to the Rect
        Point topLeft = rotateTransform.Transform(new Point(rect.Left, rect.Top));
        Point topRight = rotateTransform.Transform(new Point(rect.Right, rect.Top));
        Point bottomLeft = rotateTransform.Transform(new Point(rect.Left, rect.Bottom));
        Point bottomRight = rotateTransform.Transform(new Point(rect.Right, rect.Bottom));

        // Calculate the new bounding box
        double newLeft = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        double newTop = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        double newRight = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        double newBottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));

        // Update the properties
        Top = newTop;
        Left = newLeft;
        Width = newRight - newLeft;
        Height = newBottom - newTop;
        AngleDeg = v5.angleDeg;

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
                    OverallGradeString = $"{v5.grading.iso15415.overall.grade:f1}/00/600";
                    OverallGradeValue = v5.grading.iso15415.overall.grade;
                    OverallGradeLetter = V5GetLetter(v5.grading.iso15415.overall.letter);
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

    private static string V5GetSymbology(string type) => type == "Datamatrix" ? "DataMatrix" : type;
    private static string V5GetType(V5_REST_Lib.Models.ResultsAlt.Decodedata Report) =>
        Report.Code128 != null
        ? "verify1D"
        : Report.Datamatrix != null
        ? "verify2D"
        : Report.QR != null ? "verify2D" : Report.PDF417 != null ? "verify1D" : Report.UPC != null ? "verify1D" : "unknown";

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
}
