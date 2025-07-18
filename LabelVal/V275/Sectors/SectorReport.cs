using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate.SqlCommand;
using System.Collections.ObjectModel;
using System.Drawing;

namespace LabelVal.V275.Sectors;

public class SectorReport : ISectorReport
{
    public JObject Original { get; private set; }

    public Devices Device => Devices.V275;
    public AvailableRegionTypes RegionType { get; private set; }
    public Symbologies SymbolType { get; private set; }

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

    public Point CenterPoint { get; private set; }

    public ObservableCollection<IParameterValue> Parameters { get; } = [];

    public SectorReport(JObject report, ISectorTemplate secTemplate)
    {
        Original = report;

        Top = report.GetParameter<double>("top");
        Left = report.GetParameter<double>("left");
        Width = report.GetParameter<double>("width");
        Height = report.GetParameter<double>("height");
        AngleDeg = report.GetParameter<double>("angle");

        CenterPoint = new System.Drawing.Point((int)(Left + (Width / 2)), (int)(Top + (Height / 2)));

        //Set Symbolog
        _ = SetSymbologyAndRegionType(report);

        DecodeText = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.DecodeText, Device, SymbolType);

        _ = SetStandardAndTable((JObject)secTemplate.Original);

        //Set GS1 Data
        _ = SetGS1Data(report);

        _ = SetXdimAndUnits(report);

        //Set Aperture
        _ = SetApeture(report);

        _ = SetOverallGrade(report);

        //foreach (AvailableParameters parameter in Params.CommonParameters)
        //{
        //    try
        //    {
        //        AddParameter(parameter, SymbolType, Parameters, report, (JObject)secTemplate.Original);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Logger.LogError(ex, $"Error processing parameter: {parameter}");
        //    }
        //}

        if (report["data"]["extendedData"] != null)
            ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(report["data"]["extendedData"]));
    }

    private bool SetSymbologyAndRegionType(JObject report)
    {
        string sym = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Symbology, Device, Symbologies.Unknown);
        if (sym == null)
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Symbology.GetParameterPath(Device, Symbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        //string dataBarType = GetParameter(AvailableParameters.DataBarType, report);
        //if (dataBarType != null)
        //    sym = $"DataBar {dataBarType}";

        SymbolType = sym.GetSymbology(Device);

        //Set RegionType
        RegionType = SymbolType.GetSymbologyRegionType(Device);

        if (SymbolType == Symbologies.Unknown)
        {
            Logger.LogError($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private bool SetOverallGrade(JObject report)
    {
        JObject overall = report.GetParameter<JObject>(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, SymbolType);
        if (overall != null)
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.OverallGrade.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject template)
    {
        bool isGs1 = template.GetParameter<bool>(BarcodeVerification.lib.Common.Parameters.Standard, Device, SymbolType);

        if (!isGs1)
        {
            Standard = AvailableStandards.ISO;
            GS1Table = AvailableTables.Unknown;
        }
        else
        {
            Standard = AvailableStandards.GS1;
            GS1Table = template.GetParameter<string>(Parameters.GS1Table, Device, SymbolType).GetTable(Device);
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

        string data = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, SymbolType);
        bool pass = report.GetParameter<bool>(BarcodeVerification.lib.Common.Parameters.GS1DataStructure, Device, SymbolType);

        List<string> list = [];
        if (!string.IsNullOrEmpty(data))
        {
            string[] spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in spl)
                list.Add($"({str}");
        }
        GS1Results = new GS1Decode(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, SymbolType, pass ? "PASS" : "FAIL", DecodeText, data, list , "");
        return true;
    }

    private bool SetXdimAndUnits(JObject report)
    {
        JObject xdim = report.GetParameter<JObject>(BarcodeVerification.lib.Common.Parameters.Xdim, Device, SymbolType);
        XDimension = xdim != null
            ? xdim.GetParameter<double>("value")
            : report.GetParameter<double>(BarcodeVerification.lib.Common.Parameters.XDimension, Device, SymbolType);

        string unit = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Units, Device, SymbolType);
        if (unit == null)
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Units.GetParameterPath(Device, SymbolType)}' in ReportData. {Device}");
            return false;
        }

        Units = unit.Equals("mil") ? AvailableUnits.Mils : AvailableUnits.MM;

        return true;
    }

    private bool SetApeture(JObject report)
    {
        Aperture = report.GetParameter<double>(BarcodeVerification.lib.Common.Parameters.Aperture, Device, SymbolType);

        return true;
    }

    private OverallGrade GetOverallGrade(JObject json)
    {

        string[] spl = json["string"].ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, json["grade"]["value"].Value<double>());
        return new OverallGrade(Device, grade, json["string"].ToString(), spl[1], spl[2]);
    }


    private void AddParameter(Parameters parameter, Symbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report, JObject template)
    {
        Type type = parameter.GetParameterDataType(Device, theSymbology);

        if (type == typeof(GradeValue))
        {
            GradeValue gradeValue = GetGradeValue(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Device, SymbolType)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(Grade))
        {
            Grade grade = GetGrade(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Device, SymbolType)));

            if (grade != null)
            {
                target.Add(grade);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, report.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = parameter is BarcodeVerification.lib.Common.Parameters.Standard or BarcodeVerification.lib.Common.Parameters.GS1Table
                ? GetValueString(parameter, template.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)))
                : GetValueString(parameter, report.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)));
            if (valueString != null) { target.Add(valueString); return; }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(ValuePassFail))
        {
            ValuePassFail valuePassFail = GetValuePassFail(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Device, SymbolType)));
            if (valuePassFail != null) { target.Add(valuePassFail); return; }
        }
        else if (type == typeof(OverallGrade))
        {
            target.Add(OverallGrade);

        }
        else if (type == typeof(Custom))
        {

        }

        target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(Device, SymbolType)}' missing or parse issue.");
    }


    private GradeValue GetGradeValue(Parameters parameter, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        Grade grade = new(parameter, Device, gradeValue["grade"].ToString());
        string value = gradeValue["value"].ToString();
        return new GradeValue(parameter, Device, SymbolType, grade, value);
    }

    private Grade GetGrade(Parameters parameter, JObject grade)
    {
        if (grade is null)
            return null;
        string value = grade["value"].ToString();
        _ = grade["letter"].ToString();
        return new Grade(parameter, Device, value);
    }

    private ValueDouble GetValueDouble(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Device, SymbolType, value);

    private ValueString GetValueString(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Device, value);

    private PassFail GetPassFail(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Device, value);

    public ValuePassFail GetValuePassFail(Parameters parameter, JObject valuePassFail)
    {
        if (valuePassFail is null)
            return null;

        string passFail = valuePassFail["result"].ToString();
        string val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, Device, SymbolType, val, passFail);
    }
}
