using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;

namespace LabelVal.V5.Sectors;

public class SectorReport : ISectorReport
{
    public object Original { get; private set; }

    public AvailableDevices Device => AvailableDevices.V5;
    public AvailableRegionTypes RegionType { get; private set; }
    public AvailableSymbologies SymbolType { get; private set; }

    public AvailableStandards Standard { get; private set; }
    public AvailableTables GS1Table { get; private set; }

    public double Top { get; private set; }
    public double Left { get; private set; }
    public double Width { get; private set; }
    public double Height { get; private set; }
    public double AngleDeg { get; private set; }

    public System.Drawing.Point CenterPoint { get; set; }

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

    //Custom to V5
    public double AperturePercentage { get; private set; }
    public double ApertureMils { get; private set; }
    public double Ppi { get; private set; }

    public SectorReport(JObject report, JObject template, AvailableTables desiredTable)
    {
        Original = report;
        GS1Table = desiredTable;

        _ = SetBoudingBox(report);

        //Set Symbology
        _ = SetSymbologyAndRegionType(report);

        DecodeText = report.GetParameter<string>(AvailableParameters.DecodeText, Device, SymbolType);

        _ = SetStandardAndTable(report, template);
        //Set GS1 Data
        _ = SetGS1Data(report);
        //Set XDimension
        _ = SetXdimAndUnits(report, template);
        _ = SetOverallGrade(report, template);
        //Set Aperture
        _ = SetApeture(report, template);

    }

    private bool SetBoudingBox(JObject report)
    {
        (double Top, double Left, double Width, double Height) bb = ConvertBoundingBox(report.GetParameter<JArray>("boundingBox"));
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

        CenterPoint = new System.Drawing.Point((int)Left, (int)Top);

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

        string version = report.GetParameter<string>($"{sym}.version");
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

    private bool SetOverallGrade(JObject report, JObject template)
    {
        string overall = report.GetParameter<string>(AvailableParameters.OverallGrade, Device, SymbolType);
        if (!string.IsNullOrEmpty(overall))
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogWarning($"Could not find: '{AvailableParameters.OverallGrade.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");

            //bool qualityEnabled = report.GetParameter<bool>(AvailableParameters.QuailityEnabled, Device, SymbolType);
            //bool goodquality = report.GetParameter<bool>(AvailableParameters.GoodQuality, Device, SymbolType);
            string apertureS = "00";
            double aperture = double.NaN;
            if (AperturePercentage != double.NaN)
            {
                double ppe = report.GetParameter<double>(AvailableParameters.PPE, Device, SymbolType);
                aperture = ppe * AperturePercentage / 100;
                apertureS = ppe >= 10 ? $"{aperture:N0}" : $"0{aperture:N0}";
            }

            OverallGrade = new OverallGrade(Device, new Grade(AvailableParameters.OverallGrade, Device, double.NaN), $"NaN/{apertureS}/623", $"{aperture:N0}", "600");
            return true;
        }

        return true;
    }

    private bool SetStandardAndTable(JObject report, JObject template)
    {
        int toolSlot = report.GetParameter<int>("toolSlot") - 1;

        AperturePercentage = double.NaN;
        ApertureMils = double.NaN;

        if (template.GetParameter<bool>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15415.enabled"))
        {
            Standard = AvailableStandards.ISO15415;
            AperturePercentage = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15415.aperture");
            ApertureMils = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15415.aperture_mil");
        }
        else
        if (template.GetParameter<bool>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15416.enabled"))
        {
            Standard = AvailableStandards.ISO15416;
            AperturePercentage = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15416.aperture");
            ApertureMils = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15416.aperture_mil");
        }
        else
        if (template.GetParameter<bool>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso29158.enabled"))
        {
            Standard = AvailableStandards.DPM;
            AperturePercentage = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso29158.aperture");
            ApertureMils = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso29158.aperture_mil");
        }

        //Set GS1 last
        if (report.GetParameter<bool>($"gs1Enabled"))
        {
            Standard = AvailableStandards.GS1;
        }

        return true;
    }

    private bool SetGS1Data(JObject report)
    {
        if (Standard != AvailableStandards.GS1)
        {
            Logger.LogInfo("GS1 is not enabled. Skipping GS1 Data.");
            return true;
        }

        string data = report.GetParameter<string>(AvailableParameters.GS1Data, Device, SymbolType);
        bool pass = report.GetParameter<bool>(AvailableParameters.GS1DataStructure, Device, SymbolType);

        List<string> list = [];
        if (!string.IsNullOrEmpty(data))
        {

            string[] spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in spl)
                list.Add($"({str}");
        }
        GS1Results = new GS1Decode(AvailableParameters.GS1Data, Device, SymbolType, pass ? "PASS" : "FAIL", DecodeText, data, pass ? list : null, "");

        return true;
    }

    private bool SetXdimAndUnits(JObject report, JObject template)
    {
        Units = AvailableUnits.Pixels;

        string ppi = template.GetParameter<string>(AvailableParameters.PPI, Device, SymbolType);

        if (ppi == null)
        {
            Ppi = double.NaN;
            Logger.LogInfo($"Could not find: '{AvailableParameters.PPI.GetParameterPath(Device, SymbolType)}' in the Job. {Device}");
        }
        else
            Ppi = ppi.ParseDouble();

        string ppe = report.GetParameter<string>(AvailableParameters.PPE, Device, SymbolType);
        if (ppe == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.PPE.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }

        XDimension = double.IsNaN(Ppi) ? ppe.ParseDouble() : ppe.ParseDouble() * 1000 / Ppi;

        return true;
    }

    private bool SetApeture(JObject report, JObject template)
    {
        if (OverallGrade == null)
        {
            Logger.LogError("OverallGrade is null. Cannot calculate Aperture.");
            return false;
        }

        if (string.IsNullOrEmpty(OverallGrade.Value))
        {
            //Would have to calculate the aperture based on the ppe and the target aperture percentage.
            Aperture = double.NaN;
            return true;
        }

        string[] spl = OverallGrade.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (spl.Length < 2)
        {
            Logger.LogError($"Could not parse: '{OverallGrade.Value}' to get Aperture. {Device}");
            return false;
        }

        Aperture = spl[1].TrimStart('0').ParseDouble();

        return true;
    }

    private OverallGrade GetOverallGrade(string original)
    {
        string[] spl = original.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(AvailableParameters.OverallGrade, Device, spl[0].ParseDouble());
        return new OverallGrade(Device, grade, original, spl[1], spl[2]);
    }
}
