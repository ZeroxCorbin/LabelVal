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

        SetBoudingBox(report);

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
    }

    private bool SetBoudingBox(JObject report)
    {
        var bb = ConvertBoundingBox(report.GetParameter<JArray>("boundingBox"));
        if (bb == (0, 0, 0, 0))
        {
            Logger.LogError("Could not find the bounding box.");
            return false;
        }

        Top = bb.Top;
        Left = bb.Left;
        Width = bb.Width;
        Height = bb.Height;
        AngleDeg = report.GetParameter<double>("angleDeg");
  
        return true;
    }
    public static (double Top, double Left, double Width, double Height) ConvertBoundingBox(JArray corners)
    {
        if (corners == null)
        {
            Logger.LogError("Could not find the bounding box.");
            return (0, 0, 0, 0);
        }

        if (corners.Count != 4)
        {
            throw new ArgumentException("Bounding box must have exactly 4 corners.");
        }

        double minX = corners.Min(point => point["x"].Value<double>());
        double minY = corners.Min(point => point["y"].Value<double>());
        double maxX = corners.Max(point => point["x"].Value<double>());
        double maxY = corners.Max(point => point["y"].Value<double>());

        double width = maxX - minX;
        double height = maxY - minY;

        return (minY, minX, width, height);
    }

    private bool SetSymbologyAndRegionType(JObject report)
    {
        string sym = report.GetParameter<string>(AvailableParameters.Symbology, Device, SymbolType);
        if (sym == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Symbology.GetParameterPath(Device, AvailableSymbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }


        var version = report.GetParameter<string>($"{sym}.version");
        if (version != null)
            sym = version;

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
            Logger.LogWarning($"Could not find: '{AvailableParameters.OverallGrade.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");

            bool qualityEnabled = report.GetParameter<bool>(AvailableParameters.QuailityEnabled, Device, SymbolType);
            bool goodquality = report.GetParameter<bool>(AvailableParameters.GoodQuality, Device, SymbolType);

            var ppe = report.GetParameter<double>(AvailableParameters.PPE, Device, SymbolType);

            OverallGrade = new OverallGrade(Device, new Grade(AvailableParameters.OverallGrade, Device, (qualityEnabled && goodquality) ? 4.0 : !qualityEnabled ? double.NaN : 0), $"0/{ppe * 2}/623", $"{ppe * 2}", "600");
            return true;
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
