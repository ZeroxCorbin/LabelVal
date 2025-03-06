using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using V275_REST_Lib.Models;

namespace LabelVal.LVS_95xx.Sectors;

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

    public Report(FullReport report)
    {
        Original = report;

        Type = GetParameter("Cell size", report.ReportData) == null ? AvailableRegionTypes.Type1D : AvailableRegionTypes.Type2D;

        Top = report.Report.Y1;
        Left = report.Report.X1;
        Width = report.Report.SizeX;
        Height = report.Report.SizeY;
        AngleDeg = 0;

        string sym = GetParameter("Symbology", report.ReportData);
        if (sym == null)
            return;

        if (sym.Contains("DataBar"))
        {
           var type = GetParameter("DataBar type", report.ReportData);
            if (type != null)
                sym = $"DataBar {type}";
        }

        SymbolType = sym.GetSymbology(AvailableDevices.L95);

        if(SymbolType == AvailableSymbologies.Unknown)
            return;

        XDimension = Type == AvailableRegionTypes.Type2D
            ? (double)ParseFloat(GetParameter("Cell size", report.ReportData))
            : SymbolType != AvailableSymbologies.PDF417 ? (double)ParseFloat(GetParameter("Xdim", report.ReportData)) : (double)ParseFloat(GetParameter("XDim", report.ReportData));
        Aperture = ParseFloat(GetParameter("Overall", report.ReportData).Split('/')[1]);
        Units = "mil";

        DecodeText = report.Report.DecodedText.Replace("#", "");

        OverallGradeValue = GetGrade(GetParameter("Overall", report.ReportData)).value;
        OverallGradeString = report.Report.OverallGrade.Replace("DPM", "");
        OverallGradeLetter = GetGrade(GetParameter("Overall", report.ReportData)).letter;

        var stdString = GetParameter("Application standard", report.ReportData);
        var tblString = GetParameter("GS1 Table", report.ReportData);

        Standard = stdString.GetStandard(AvailableDevices.L95);
        if(Standard is AvailableStandards.GS1 or AvailableStandards.GS1_1D_Report or AvailableStandards.GS1_2D_Report or AvailableStandards.GS1NTIN)
            GS1Table = tblString.GetTable(AvailableDevices.L95);

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

}
