using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using V275_REST_Lib.Models;

namespace LabelVal.V275.Sectors;

public class SectorReport : ISectorReport
{
    public object Original { get; private set; }

    public AvailableDevices Device => AvailableDevices.V275;
    public AvailableRegionTypes RegionType { get; private set; }
    public AvailableSymbologies SymbolType { get; private set; }

    public AvailableStandards Standard { get; private set; }
    public AvailableTables GS1Table { get; private set; }

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

    public SectorReport(JObject report)
    {
        Original = report;

        //Set Symbology
        if (!SetSymbologyAndRegionType(report))
            return;

        DecodeText = GetParameter(AvailableParameters.DecodeText, report);

        if (!SetStandardAndTable(report))
            return;

        //Set XDimension
        if (!SetXdimAndUnits(report))
            return;

        //Set Aperture
        if (!SetApeture(report))
            return;

        if (!SetOverallGrade(report))
            return;

        //if (sector.type is "verify1D" or "verify2D" && sector.gradingStandard != null)
        //    Standard = sector.gradingStandard.enabled ? AvailableStandards.GS1 : AvailableStandards.ISO;

        //if (Standard == AvailableStandards.GS1)
        //    GS1Table = sector.gradingStandard.tableId.GetTable(AvailableDevices.V275);

        //OverallGradeString = report["data"]["overallGrade"]["string"].ToString();
        //OverallGradeValue = report["data"]["overallGrade"]["grade"]["value"].ToObject<double>();
        //OverallGradeLetter = report["data"]["overallGrade"]["grade"]["letter"].ToString();

        //if (report["data"]["gs1Results"] != null)
        //{
        //    List<string> fld = [];
        //    foreach (JProperty f in report["data"]["gs1Results"]["fields"])
        //        fld.Add($"({f.Name}) {f.Value.ToString().Trim('{', '}', ' ')}");
        //    GS1Results = new Gs1Results
        //    {
        //        Validated = report["data"]["gs1Results"]["validated"].ToObject<bool>(),
        //        Input = report["data"]["gs1Results"]["input"].ToString(),
        //        FormattedOut = report["data"]["gs1Results"]["formattedOut"].ToString(),
        //        Fields = fld,
        //        Error = report["data"]["gs1Results"]["error"].ToString()
        //    };
        //}

        if (report["data"]["extendedData"] != null)
            ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(report["data"]["extendedData"]));

        //switch (report)
        //{
        //    case Report_InspectSector_OCR:
        //        Report_InspectSector_OCR ocr = (Report_InspectSector_OCR)report;

        //        SymbolType = AvailableSymbologies.OCR;
        //        Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

        //        Top = ocr.top;
        //        Left = ocr.left;
        //        Width = ocr.width;
        //        Height = ocr.height;
        //        AngleDeg = 0;

        //        Text = ocr.data.text;
        //        Score = ocr.data.score;
        //        break;

        //    case Report_InspectSector_OCV:
        //        Report_InspectSector_OCV ocv = (Report_InspectSector_OCV)report;

        //        SymbolType = AvailableSymbologies.OCV;
        //        Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

        //        Top = ocv.top;
        //        Left = ocv.left;
        //        Width = ocv.width;
        //        Height = ocv.height;
        //        AngleDeg = 0;

        //        Text = ocv.data.text;
        //        Score = ocv.data.score;
        //        break;

        //    case Report_InspectSector_Blemish:
        //        Report_InspectSector_Blemish blem = (Report_InspectSector_Blemish)report;

        //        SymbolType = AvailableSymbologies.Blemish;
        //        Type = SymbolType.GetSymbologyRegionType(AvailableDevices.V275);

        //        Top = blem.top;
        //        Left = blem.left;
        //        Width = blem.width;
        //        Height = blem.height;
        //        AngleDeg = 0;

        //        BlemishCount = blem.data.blemishCount;
        //        break;
        //}
    }

    private bool SetSymbologyAndRegionType(JObject report)
    {
        string sym = GetParameter(AvailableParameters.Symbology, report);
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
        string overall = GetParameter(AvailableParameters.OverallGrade, report);
        if (overall != null)
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogError($"Could not find: '{AvailableParameters.OverallGrade.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject report)
    {
        string stdString = GetParameter(AvailableParameters.Standard, report);
        string tblString = GetParameter(AvailableParameters.GS1Table, report);

        if (stdString == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Standard.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        Standard = stdString.GetStandard(Device);

        if (Standard == AvailableStandards.Unknown)
        {
            Logger.LogError($"Could not determine standard from: '{stdString}' {Device}");
            return false;
        }

        GS1Table = tblString.GetTable(Device);

        //If a table is not defined, it is not a GS1 symbol Exit
        if (GS1Table == AvailableTables.Unknown)
            return true;

        string data = GetParameter(AvailableParameters.GS1Data, report);
        string pass = GetParameter(AvailableParameters.GS1DataStructure, report);

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

    private bool SetXdimAndUnits(JObject report)
    {
        string xdim = GetParameter(AvailableParameters.CellSize, report);
        xdim ??= GetParameter(AvailableParameters.Xdim, report);
        if (xdim == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.CellSize.GetParameterPath(Device, SymbolType)}' or '{AvailableParameters.Xdim.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        string[] split = xdim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2)
        {
            Logger.LogError($"Could not parse: '{xdim}' to get XDimension. {Device}");
            return false;
        }
        XDimension = split[0].ParseDouble();
        if (split[1].Equals("mils"))
            Units = AvailableUnits.Mils;
        else if (split[1].Equals("mm"))
            Units = AvailableUnits.Millimeters;
        else
        {
            Logger.LogError($"Could not determine units from: '{xdim}' {Device}");
            return false;
        }
        return true;
    }

    private bool SetApeture(JObject report)
    {
        string aperture = GetParameter(AvailableParameters.Aperture, report);
        if (aperture != null)
        {
            //GetParameter returns: Reference number 12 (12 mil)
            string[] split = aperture.Split('(', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                Logger.LogError($"Could not parse: '{aperture}' to get Aperture. {Device}");
                return false;
            }
            Aperture = split[1].ParseDouble();
        }
        else
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Aperture.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }
    private string GetParameter(AvailableParameters parameter, JObject report)
    {
        string path = parameter.GetParameterPath(Device, SymbolType);
        string[] parts = path.Split('.');
        JObject current = report;
        for (int i = 0; i < parts.Length; i++)
        {
            if (current[parts[i]] is null)
                return null;
            if (i == parts.Length - 1)
                return current[parts[i]].ToString();
            current = (JObject)current[parts[i]];
        }
        return null;
    }


    private OverallGrade GetOverallGrade(string original)
    {
        string data = original.Replace("DPM", "");
        string[] spl = data.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(AvailableParameters.OverallGrade, Device, spl[0]);
        return new OverallGrade(Device, grade, original, spl[1], spl[2]);
    }
}
