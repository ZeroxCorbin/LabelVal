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
    public Devices Device => Devices.V275;

    public Symbologies Symbology { get; private set; }
    public SymbologySpecifications Specification => Symbology.GetSpecification(Device);
    public SymbologySpecificationTypes Type => Specification.GetCodeType();

    public ApplicationStandards ApplicationStandard { get; private set; }
    public GradingStandards GradingStandard { get; private set; }
    public GS1Tables GS1Table { get; private set; }

    public ObservableCollection<IParameterValue> Parameters { get; } = [];

    public JObject Original { get; private set; }

    public OverallGrade OverallGrade { get; private set; }

    public double XDimension { get; private set; }
    public double Aperture { get; private set; } = 0.0;
    public AvailableUnits Units { get; private set; }

    public string DecodeText { get; private set; }

    public double Top { get; private set; }
    public double Left { get; private set; }
    public double Width { get; private set; }
    public double Height { get; private set; }
    public double AngleDeg { get; private set; }
    public Point CenterPoint { get; private set; }

    //GS1
    public GS1Decode GS1Results { get; private set; }

    //OCR
    public string Text { get; private set; }
    public double Score { get; private set; }

    //Blemish
    public int BlemishCount { get; private set; }

    //V275 2D module data
    public ModuleData ExtendedData { get; private set; }

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

        DecodeText = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.DecodeText, Device, Symbology);

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
        //        AddParameter(parameter, Symbology, Parameters, report, (JObject)secTemplate.Original);
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
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Symbology.GetPath(Device, Symbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        Symbology = sym.GetSymbology(Device);

        if (Symbology == Symbologies.Unknown)
        {
            Logger.LogError($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private bool SetOverallGrade(JObject report)
    {
        JObject overall = report.GetParameter<JObject>(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, Symbology);
        if (overall != null)
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.OverallGrade.GetPath(Device, Symbology)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject template)
    {
        bool isGs1 = template.GetParameter<bool>(BarcodeVerification.lib.Common.Parameters.GradingStandard, Device, Symbology);

        if (!isGs1)
        {
            GradingStandard = "ISO".GetGradingStandard(Device, Specification);
            ApplicationStandard = ApplicationStandards.None;
            GS1Table = GS1Tables.Unknown;
        }
        else
        {
            GradingStandard = "ISO".GetGradingStandard(Device, Specification);
            ApplicationStandard = ApplicationStandards.GS1;
            GS1Table = template.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Table, Device, Symbology).GetGS1Table(Device);
        }

        return true;
    }

    private bool SetGS1Data(JObject report)
    {
        if (ApplicationStandard != ApplicationStandards.GS1)
        {
            Logger.LogInfo("GS1 is not enabled. Skipping GS1 Data.");
            return true;
        }

        string data = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology);
        bool pass = report.GetParameter<bool>(BarcodeVerification.lib.Common.Parameters.GS1DataStructure, Device, Symbology);

        List<string> list = [];
        if (!string.IsNullOrEmpty(data))
        {
            string[] spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in spl)
                list.Add($"({str}");
        }
        GS1Results = new GS1Decode(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology, pass ? "PASS" : "FAIL", DecodeText, data, list , "");
        return true;
    }

    private bool SetXdimAndUnits(JObject report)
    {
        JObject xdim = report.GetParameter<JObject>(BarcodeVerification.lib.Common.Parameters.Xdim, Device, Symbology);
        XDimension = xdim != null
            ? xdim.GetParameter<double>("value")
            : report.GetParameter<double>(BarcodeVerification.lib.Common.Parameters.XDimension, Device, Symbology);

        string unit = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Units, Device, Symbology);
        if (unit == null)
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Units.GetPath(Device, Symbology)}' in ReportData. {Device}");
            return false;
        }

        Units = unit.Equals("mil") ? AvailableUnits.Mils : AvailableUnits.MM;

        return true;
    }

    private bool SetApeture(JObject report)
    {
        Aperture = report.GetParameter<double>(BarcodeVerification.lib.Common.Parameters.Aperture, Device, Symbology);

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
        Type type = parameter.GetDataType(Device, theSymbology);

        if (type == typeof(GradeValue))
        {
            GradeValue gradeValue = GetGradeValue(parameter, report.GetParameter<JObject>(parameter.GetPath(Device, Symbology)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(Grade))
        {
            Grade grade = GetGrade(parameter, report.GetParameter<JObject>(parameter.GetPath(Device, Symbology)));

            if (grade != null)
            {
                target.Add(grade);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, report.GetParameter<string>(parameter.GetPath(Device, Symbology)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = parameter is BarcodeVerification.lib.Common.Parameters.GradingStandard or BarcodeVerification.lib.Common.Parameters.GS1Table
                ? GetValueString(parameter, template.GetParameter<string>(parameter.GetPath(Device, Symbology)))
                : GetValueString(parameter, report.GetParameter<string>(parameter.GetPath(Device, Symbology)));
            if (valueString != null) { target.Add(valueString); return; }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetPath(Device, Symbology)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(ValuePassFail))
        {
            ValuePassFail valuePassFail = GetValuePassFail(parameter, report.GetParameter<JObject>(parameter.GetPath(Device, Symbology)));
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
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(Device, Symbology)}' missing or parse issue.");
    }


    private GradeValue GetGradeValue(Parameters parameter, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        Grade grade = new(parameter, Device, gradeValue["grade"].ToString());
        string value = gradeValue["value"].ToString();
        return new GradeValue(parameter, Device, Symbology, grade, value);
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
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Device, Symbology, value);

    private ValueString GetValueString(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Device, value);

    private PassFail GetPassFail(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Device, value);

    public ValuePassFail GetValuePassFail(Parameters parameter, JObject valuePassFail)
    {
        if (valuePassFail is null)
            return null;

        string passFail = valuePassFail["result"].ToString();
        string val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, Device, Symbology, val, passFail);
    }
}
