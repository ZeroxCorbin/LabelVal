using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Models;
using Mysqlx.Sql;
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

    public SectorReport(JObject report, JObject template)
    {
        Original = report;

        //Set Symbolog
        SetSymbologyAndRegionType(report);


        DecodeText = GetParameter(AvailableParameters.DecodeText, report);

        SetStandardAndTable(template);

        //Set GS1 Data
        SetGS1Data(report);

        SetXdimAndUnits(report);

        //Set Aperture
        SetApeture(report);

        SetOverallGrade(report);
 

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

    private bool SetStandardAndTable(JObject template)
    {
        string stdString = GetParameter(AvailableParameters.Standard, template);
        string tblString = GetParameter(AvailableParameters.GS1Table, template);

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
        var xdim = GetParameter(AvailableParameters.Xdim, report);
        var json = xdim != null;
        xdim ??= GetParameter(AvailableParameters.XDimension, report);
        if (xdim == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Xdim.GetParameterPath(Device, SymbolType)}' or '{AvailableParameters.XDimension.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }

        if(json)
           XDimension = JObject.Parse(xdim)["value"].ToObject<double>();
        else
            XDimension = xdim.ParseDouble();
        
        var unit = GetParameter(AvailableParameters.Units, report);
        if (unit == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Units.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }

        Units = unit.Equals("mil") ? AvailableUnits.Mils : AvailableUnits.MM;

        return true;
    }

    private bool SetApeture(JObject report)
    {
        string aperture = GetParameter(AvailableParameters.Aperture, report);
        if (aperture == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Aperture.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }

        Aperture = aperture.ParseDouble();

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
        var json = JObject.Parse(original);

        string[] spl = json["string"].ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(AvailableParameters.OverallGrade, Device, json["grade"]["value"].Value<double>());
        return new OverallGrade(Device, grade, json["string"].ToString(), spl[1], spl[2]);
    }
}
