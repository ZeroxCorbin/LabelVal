using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using NHibernate.SqlCommand;

namespace LabelVal.V5.Sectors;

public class SectorReport : ISectorReport
{
    public object Original { get; private set; }

    public AvailableDevices Device => AvailableDevices.V5;
    public AvailableRegionTypes RegionType { get; private set; }
    public AvailableSymbologies SymbolType { get; private set; }

    public AvailableStandards Standard { get; private set;}
    public AvailableTables GS1Table { get;private set; }

    public double Top { get; private set; }
    public double Left { get; private set; }
    public double Width { get; private set; }
    public double Height { get; private set; }
    public double AngleDeg { get; private set; }

    public OverallGrade OverallGrade { get; private set; }

    public double XDimension { get; private set; }
    public double Aperture { get; private set; }
    public AvailableUnits Units { get; private set; }

    public string DecodeText { get; private set; }

    //GS1
    public GS1Decode GS1Results { get; private set; }

    //OCR
    public string Text { get; private set; }
    public double Score { get; private set; }

    //Blemish
    public int BlemishCount { get; private set; }

    //V275 2D module data
    public ModuleData ExtendedData { get; private set; }

    public double Ppi { get; private set; }

    public SectorReport(JObject report, JObject template)
    {
        Original = report;

        //Set Symbology
        SetSymbologyAndRegionType(report);

        DecodeText = report.GetParameter<string>(AvailableParameters.DecodeText, Device, SymbolType);

        SetStandardAndTable(report);
        //Set GS1 Data
        SetGS1Data(report);
        //Set XDimension
        SetXdimAndUnits(report, template);
        //Set Aperture
        SetApeture(report);

        SetOverallGrade(report);

        //Original = v5;

        //RegionType = V5GetType(v5);
        //SymbolType = v5.type.GetSymbology(AvailableDevices.V5);
        //DecodeText = v5.dataUTF8;

        //if (v5.boundingBox != null)
        //    (Top, Left, Width, Height) = ConvertBoundingBox(v5.boundingBox);

        //if (v5.grading != null)
        //{
        //    if (v5.grading.standard == "iso15416")
        //    {
        //        if (v5.grading.iso15416 == null || v5.grading.iso15416.overall == null)
        //        {
        //            OverallGradeString = "No Grade";
        //            if (v5.read)
        //            {
        //                OverallGradeValue = 0;
        //                OverallGradeLetter = "A";
        //            }
        //            else
        //            {
        //                OverallGradeValue = 0;
        //                OverallGradeLetter = "F";
        //            }
        //        }
        //        else
        //        {
        //            OverallGradeString = $"{v5.grading.iso15416.overall.grade:f1}/00/600";
        //            OverallGradeValue = v5.grading.iso15416.overall.grade;
        //            OverallGradeLetter = V5GetLetter(v5.grading.iso15416.overall.letter);
        //        }
        //    }
        //    else if (v5.grading.standard == "iso15415")
        //    {
        //        if (v5.grading.iso15415 == null)
        //        {
        //            OverallGradeString = "No Grade";
        //            if (v5.read)
        //            {
        //                OverallGradeValue = 0;
        //                OverallGradeLetter = "A";
        //            }
        //            else
        //            {
        //                OverallGradeValue = 0;
        //                OverallGradeLetter = "F";
        //            }
        //        }
        //        else
        //        {
        //            if (v5.grading.iso15415.overall != null)
        //            {
        //                OverallGradeString = $"{v5.grading.iso15415.overall.grade:f1}/00/600";
        //                OverallGradeValue = v5.grading.iso15415.overall.grade;
        //                OverallGradeLetter = V5GetLetter(v5.grading.iso15415.overall.letter);
        //            }
        //        }
        //    }
        //    else if (v5.grading.standard == "iso29158")
        //    {
        //        if (v5.grading.format == "grade")
        //        {

        //            OverallGradeString = "";
        //            OverallGradeValue = double.Parse(v5.grading.grade);
        //            OverallGradeLetter = GetLetter(float.Parse(v5.grading.grade));
        //        }
        //        else
        //        {
        //            OverallGradeString = "No Grade";
        //            if (v5.read)
        //            {
        //                OverallGradeValue = 0;
        //                OverallGradeLetter = "A";
        //            }
        //            else
        //            {
        //                OverallGradeValue = 0;
        //                OverallGradeLetter = "F";
        //            }
        //        }
        //    }
        //    else
        //    {
        //        OverallGradeString = "Unkown";
        //        if (v5.read)
        //        {
        //            OverallGradeValue = 0;
        //            OverallGradeLetter = "A";
        //        }
        //        else
        //        {
        //            OverallGradeValue = 0;
        //            OverallGradeLetter = "F";
        //        }
        //    }
        //}
        //else
        //{
        //    OverallGradeString = "Grade Disabled";
        //    if (v5.read)
        //    {
        //        OverallGradeValue = 0;
        //        OverallGradeLetter = "A";
        //    }
        //    else
        //    {
        //        OverallGradeValue = 0;
        //        OverallGradeLetter = "F";
        //    }
        //}
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

    private bool SetSymbologyAndRegionType(JObject report)
    {
        string sym = report.GetParameter<string>(AvailableParameters.Symbology, Device, SymbolType);
        if (sym == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Symbology.GetParameterPath(Device, AvailableSymbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        //string dataBarType = GetParameter(AvailableParameters.DataBarType, report);
        //if (dataBarType != null)
        //    sym = $"DataBar {dataBarType}";

        SymbolType = sym.GetSymbology(Device);

        //Set RegionType
        RegionType = SymbolType.GetSymbologyRegionType(Device);

        if (SymbolType == AvailableSymbologies.Unknown)
        {
            Logger.LogError($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private bool SetOverallGrade(JObject report)
    {
        string overall = report.GetParameter<string>(AvailableParameters.OverallGrade, Device, SymbolType);
        if (!string.IsNullOrEmpty(overall))
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogError($"Could not find: '{AvailableParameters.OverallGrade.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject template)
    {
        string stdString = template.GetParameter<string>(AvailableParameters.Standard, Device, SymbolType);
        string tblString = "1";

        if (stdString == null || stdString.Equals("False"))
        {
            Standard = AvailableStandards.ISO;
            GS1Table = AvailableTables.Unknown;
        }
        else
        {
            Standard = AvailableStandards.GS1;
            GS1Table = tblString.GetTable(Device);
        }

        return true;
    }

    private bool SetGS1Data(JObject report)
    {
        //If a table is not defined, it is not a GS1 symbol Exit
        if (GS1Table == AvailableTables.Unknown)
            return true;

        string data = report.GetParameter<string>(AvailableParameters.GS1Data, Device, SymbolType);
        string pass = report.GetParameter<string>(AvailableParameters.GS1DataStructure, Device, SymbolType);

        if (data != null)
        {
            List<string> list = [];
            string[] spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in spl)
                list.Add($"({str}");

            GS1Results = new GS1Decode(AvailableParameters.GS1Data, Device, SymbolType, pass, DecodeText, data, list, "");
        }

        return true;
    }

    private bool SetXdimAndUnits(JObject report, JObject template)
    {
        Units = AvailableUnits.Mils;

        var ppi = template.GetParameter<string>(AvailableParameters.PPI, Device, SymbolType);

        if (ppi == null)
        {
            Ppi = 0;
            Logger.LogInfo($"Could not find: '{AvailableParameters.PPI.GetParameterPath(Device, SymbolType)}' in the Job. {Device}");
            return true;
        }

        Ppi = ppi.ParseDouble();

        var ppe = report.GetParameter<string>(AvailableParameters.PPE, Device, SymbolType);
        if (ppe == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.PPE.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }

        XDimension = (ppe.ParseDouble() * 1000) / Ppi;

        return true;
    }

    private bool SetApeture(JObject report)
    {
        string aperture = report.GetParameter<string>(AvailableParameters.Aperture, Device, SymbolType);
        if (aperture == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Aperture.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }

        Aperture = aperture.ParseDouble();

        return true;
    }

    private OverallGrade GetOverallGrade(string original)
    {
        string[] spl = original.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(AvailableParameters.OverallGrade, Device, spl[0].ParseDouble());
        return new OverallGrade(Device, grade, original, spl[1], spl[2]);
    }

}
