using LabelVal.Sectors.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using V275_REST_lib.Models;

namespace LabelVal.LVS_95xx.Sectors;

public class Report : IReport
{
    public string Type { get; set; }
    public string SymbolType { get; set; }

    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AngleDeg { get; set; }

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

    public Report(List<string> report)
    {
        Type = report.Find((e) => e.StartsWith("Cell size")) == null ? "verify1D" : "verify2D";

        foreach (string data in report)
        {
            if (!data.Contains(','))
                continue;

            string[] spl1 = [data[..data.IndexOf(',')], data[(data.IndexOf(',') + 1)..]];
            if (spl1[0].StartsWith("Symbology"))
            {
                SymbolType = L95xxGetSymbolType(spl1[1]);

                //verify1D
                if (SymbolType == "dataBar")
                {
                    string item = report.Find((e) => e.StartsWith("DataBar"));
                    if (item != null)
                    {
                        string[] spl2 = item.Split(',');

                        if (spl2.Length != 2)
                            continue;

                        SymbolType += spl2[1];
                    }
                }
                continue;
            }

            if (spl1[0].StartsWith("Decoded"))
            {
                DecodeText = spl1[1];
                continue;
            }

            //Verify2D
            if (spl1[0].StartsWith("Cell size"))
            {
                XDimension = L95xxParseFloat(spl1[1]);
                continue;
            }

            //Verify1D
            if (spl1[0].StartsWith("Xdim"))
            {
                XDimension = L95xxParseFloat(spl1[1]);
                continue;
            }

            if (spl1[0].StartsWith("Overall"))
            {
                string[] spl2 = spl1[1].Split('/');

                if (spl2.Length < 3) continue;

                OverallGradeValue = L95xxGetGrade(spl2[0]).value;// new Report_InspectSector_Common.Overallgrade() { grade = GetGrade(spl2[0]), _string = spl1[1] };
                OverallGradeString = spl1[1];
                OverallGradeLetter = L95xxGetGrade(spl2[0]).letter;

                Aperture = L95xxParseFloat(spl2[1]);
                continue;
            }
        }
    }

    private static string L95xxGetLetter(float value) =>
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

    private static float L95xxParseFloat(string value)
    {
        string digits = new(value.Trim().TakeWhile("0123456789.".Contains).ToArray());

        return float.TryParse(digits, out float val) ? val : 0;
    }
    private static Report_InspectSector_Common.Grade L95xxGetGrade(string data)
    {
        float tmp = L95xxParseFloat(data);

        return new Report_InspectSector_Common.Grade()
        {
            value = tmp,
            letter = L95xxGetLetter(tmp)
        };
    }

    private static string L95xxGetSymbolType(string value) =>
        value.Contains("Code 128")
        ? "code128"
        : value.Contains("UPC-A")
        ? "upcA"
        : value.Contains("UPC-B")
        ? "upcB"
        : value.Contains("EAN-13")
        ? "ean13"
        : value.Contains("EAN-8")
        ? "ean8"
        : value.Contains("DataBar")
        ? "dataBar"
        : value.Contains("Code 39")
        ? "code39"
        : value.Contains("Code 93")
        ? "code93"
        : value.StartsWith("GS1 QR")
        ? "qrCode"
        : value.StartsWith("Micro")
        ? "microQrCode"
        : value.Contains("Data Matrix")
        ? "dataMatrix"
        : value.Contains("Aztec")
        ? "aztec"
        : value.Contains("Codabar") ? "codaBar" : value.Contains("ITF") ? "i2of5" : value.Contains("PDF417") ? "pdf417" : "";
}
