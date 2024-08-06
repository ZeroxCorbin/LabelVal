using ControlzEx.Standard;
using LabelVal.Sectors.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using V275_REST_lib.Models;

namespace LabelVal.LVS_95xx.Sectors;

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

    public Report(List<string> report)
    {
        Type = report.Find((e) => e.StartsWith("Cell size")) == null ? "verify1D" : "verify2D";

        string[] sym = GetKeyValuePair("Symbology", report);
        SymbolType = L95xxGetSymbolType(sym[1]);
        XDimension = Type == "verify2D"
            ? (double)L95xxParseFloat(GetKeyValuePair("Cell size", report)[1])
            : (double)L95xxParseFloat(GetKeyValuePair("Xdim", report)[1]);
        Aperture = L95xxParseFloat(GetKeyValuePair("Overall", report)[1].Split('/')[1]);
        Units = "mil";

        DecodeText = GetKeyValuePair("Decoded", report)[1];
        
        OverallGradeValue = L95xxGetGrade(GetKeyValuePair("Overall", report)[1]).value;
        OverallGradeString = GetKeyValuePair("Overall", report)[1];
        OverallGradeLetter = L95xxGetGrade(GetKeyValuePair("Overall", report)[1]).letter;

        bool isGS1 = sym[1].StartsWith("GS1");
        string error = null;
        if (isGS1)
        {
            var res = GetMultipleKeyValuePair("GS1 Data", report);
            foreach(var str in res)
            {
                if (str[0].Equals("GS1 Data"))
                    continue;
                else
                    error = str[1];
            }

            var list = new List<string>();
            var spl = GetKeyValuePair("GS1 Data,", report)[1].Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach(var str in spl)
                list.Add($"({str}");
            
            GS1Results = new Gs1results()
            {
                Validated = true,
                Input = GetKeyValuePair("Decoded", report)[1],
                FormattedOut = GetKeyValuePair("GS1 Data,", report)[1],
                Error = error,
                Fields = list
            };
        }
    }

    private string[] GetKeyValuePair(string key, List<string> report)
    {
        string item = report.Find((e) => e.StartsWith(key));

        //if it was not found or the item does not contain a comma.
        return item?.Contains(',') != true ? null : ([item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]]);
    }
    private List<string[]> GetMultipleKeyValuePair(string key, List<string> report)
    {
        List<string> items = report.FindAll((e) => e.StartsWith(key));

        if (items == null || items.Count == 0)
            return null;

        List<string[]> res = [];
        foreach (string item in items)
        {
            if (!item.Contains(','))
                continue;

            res.Add([item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]]);
        }
        return res;
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
