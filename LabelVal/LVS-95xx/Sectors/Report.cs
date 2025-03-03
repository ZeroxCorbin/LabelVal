using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using V275_REST_Lib.Models;

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

    public StandardsTypes Standard { get; set; }
    public Gs1TableNames GS1Table { get; set; }

    //GS1
    public Gs1Results GS1Results { get; set; }

    //OCR
    public string Text { get; set; }
    public double Score { get; set; }

    //Blemish
    public int BlemishCount { get; set; }

    //V275 2D module data
    public ModuleData ExtendedData { get; set; }

    public Report(FullReport report)
    {
        Type = GetParameter("Cell size", report.ReportData) == null ? "verify1D" : "verify2D";

        Top = report.Report.Y1;
        Left = report.Report.X1;
        Width = report.Report.SizeX;
        Height = report.Report.SizeY;
        AngleDeg = 0;

        string sym = GetParameter("Symbology", report.ReportData);
        if (sym == null)
            return;

        SymbolType = GetSymbolType(sym);

        XDimension = Type == "verify2D"
            ? (double)ParseFloat(GetParameter("Cell size", report.ReportData))
            : sym != "PDF417" ? (double)ParseFloat(GetParameter("Xdim", report.ReportData)) : (double)ParseFloat(GetParameter("XDim", report.ReportData));
        Aperture = ParseFloat(GetParameter("Overall", report.ReportData).Split('/')[1]);
        Units = "mil";

        DecodeText = report.Report.DecodedText.Replace("#", "");

        OverallGradeValue = GetGrade(GetParameter("Overall", report.ReportData)).value;
        OverallGradeString = report.Report.OverallGrade.Replace("DPM", "");
        OverallGradeLetter = GetGrade(GetParameter("Overall", report.ReportData)).letter;

        Standard = GetStandard(GetParameter("Application standard", report.ReportData));
        GS1Table = GetGS1Table(GetParameter("GS1 Table", report.ReportData));

        string res = GetParameter("GS1 Data", report.ReportData, true);
        if (res != null)
        {
            string err = GetParameter("GS1 Data Structure", report.ReportData);

            List<string> list = [];
            string[] spl = res.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in spl)
                list.Add($"({str}");

            GS1Results = new Gs1Results()
            {
                Validated = true,
                Input = report.Report.DecodedText.Replace("#", "^"),
                FormattedOut = res,
                Error = err,
                Fields = list
            };
        }
    }
    private string GetParameter(string key, List<ReportData> report, bool equal = false) => report.Find((e) => equal ? e.ParameterName.Equals(key) : e.ParameterName.StartsWith(key))?.ParameterValue;

    private string[] GetKeyValuePair(string key, List<string> report)
    {
        string item = report.Find((e) => e.StartsWith(key));

        //if it was not found or the item does not contain a comma.
        return item?.Contains(',') != true ? null : [item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]];
    }
    private List<string[]> GetMultipleKeyValuePairs(string key, List<string> report)
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
    private static float ParseFloat(string value)
    {
        string digits = new(value.Trim().TakeWhile("0123456789.".Contains).ToArray());

        return float.TryParse(digits, out float val) ? val : 0;
    }
    private static Report_InspectSector_Common.Grade GetGrade(string data)
    {
        data = data.Replace("DPM", "");
        float tmp = ParseFloat(data);

        return new Report_InspectSector_Common.Grade()
        {
            value = tmp,
            letter = GetLetter(tmp)
        };
    }

    private static StandardsTypes GetStandard(string value) =>
        value.StartsWith("GS1")
        ? StandardsTypes.GS1
        : value.StartsWith("ISO")
        ? StandardsTypes.ISO15415_15416
        : value.Contains("29158")
        ? StandardsTypes.ISO29158
        : StandardsTypes.Unsupported;
    private static Gs1TableNames GetGS1Table(string value) =>
        string.IsNullOrEmpty(value) ? Gs1TableNames.Unsupported :
        value.StartsWith("Table 1") ? Gs1TableNames._1 :
        value.StartsWith("Table 1.1") ? Gs1TableNames._1_8200 :
        value.StartsWith("Table 2") ? Gs1TableNames._2 :
        value.StartsWith("Table 3") ? Gs1TableNames._3 :
        value.StartsWith("Table 4") ? Gs1TableNames._4 :
        value.StartsWith("Table 5") ? Gs1TableNames._5 :
        value.StartsWith("Table 6") ? Gs1TableNames._6 :
        value.StartsWith("Table 7.1") ? Gs1TableNames._7_1 :
        value.StartsWith("Table 7.2") ? Gs1TableNames._7_2 :
        value.StartsWith("Table 7.3") ? Gs1TableNames._7_3 :
        value.StartsWith("Table 7.4") ? Gs1TableNames._7_4 :
        value.StartsWith("Table 8") ? Gs1TableNames._8 :
        value.StartsWith("Table 9") ? Gs1TableNames._9 :
        value.StartsWith("Table 10") ? Gs1TableNames._10 :
        value.StartsWith("Table 11") ? Gs1TableNames._11 :
        value.StartsWith("Table 12") ? Gs1TableNames._12_1 :
        value.StartsWith("Table 12.2") ? Gs1TableNames._12_2 :
        value.StartsWith("Table 12.3") ? Gs1TableNames._12_3
        : Gs1TableNames.Unsupported;
    private static string GetSymbolType(string value) =>
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
        : value.Contains("QR")
        ? "qrCode"
        : value.StartsWith("Micro")
        ? "microQrCode"
        : value.Contains("Data Matrix")
        ? "dataMatrix"
        : value.Contains("Aztec")
        ? "aztec"
        : value.Contains("Codabar")
        ? "codaBar"
        : value.Contains("ITF")
        ? "i2of5"
        : value.Contains("PDF417")
        ? "pdf417"
        : "";
}
