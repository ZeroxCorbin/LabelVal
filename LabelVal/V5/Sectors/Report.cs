using LabelVal.Sectors.Interfaces;
using System;

namespace LabelVal.V5.Sectors;

public class Report : IReport
{
    public string Type { get; set; }
    public string SymbolType { get; set; }
    public string DecodeText { get; set; }
    public string Text { get; set; }
    public int BlemishCount { get; set; }
    public double Score { get; set; }
    public double XDimension { get; set; }
    public double Aperture { get; set; }

    public string OverallGradeString { get; set; }
    public double OverallGradeValue { get; set; }
    public string OverallGradeLetter { get; set; }

    public Gs1results GS1Results { get; set; }
    public string FormattedOut { get; set; }
    public ModuleData ExtendedData { get; set; }

    public Report(V5_REST_Lib.Models.ResultsAlt.Decodedata v5)
    {
        Type = V5GetType(v5);
        SymbolType = V5GetSymbology(v5.type);
        DecodeText = v5.dataUTF8;

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
