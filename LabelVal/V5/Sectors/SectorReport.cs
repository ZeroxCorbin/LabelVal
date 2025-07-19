using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.V5.Sectors;

public class SectorReport : ISectorReport
{
    public Devices Device => Devices.V5;

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
    public System.Drawing.Point CenterPoint { get; private set; }

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

    public SectorReport(JObject report, ISectorTemplate template, GS1Tables desiredTable)
    {
        Original = report;
        GS1Table = desiredTable;

        if (!SetBoudingBox(report))
        {
            Top = template.Top;
            Left = template.Left;
            Width = template.Width;
            Height = template.Height;
            AngleDeg = template.AngleDeg;

            CenterPoint = new System.Drawing.Point((int)(Left + (Width / 2)), (int)(Top + (Height / 2)));
        }

        //Set Symbology
        _ = SetSymbologyAndRegionType(report);

        DecodeText = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.DecodeText, Device, Symbology);

        _ = SetStandardAndTable(report,(JObject)template.Original);
        //Set GS1 Data
        _ = SetGS1Data(report);
        //Set XDimension
        _ = SetXdimAndUnits(report, (JObject)template.Original);
        _ = SetOverallGrade(report);
        //Set Aperture
        _ = SetApeture();

        //foreach (AvailableParameters parameter in Params.CommonParameters)
        //{
        //    try
        //    {
        //        AddParameter(parameter, Symbology, Parameters, report, template);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Logger.LogError(ex, $"Error processing parameter: {parameter}");
        //    }
        //}

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

        CenterPoint = new System.Drawing.Point((int)(Left + (Width / 2)), (int)(Top + (Height/2)));

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
        string sym = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Symbology, Device, Symbology);
        if (sym == null)
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Symbology.GetPath(Device, Symbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        string version = report.GetParameter<string>($"{sym}.version");
        if (version != null)
            sym = version;

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
        string overall = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, Symbology);
        if (!string.IsNullOrEmpty(overall))
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogWarning($"Could not find: '{BarcodeVerification.lib.Common.Parameters.OverallGrade.GetPath(Device, Symbology)}' in ReportData. {Device}");

            //bool qualityEnabled = report.GetParameter<bool>(AvailableParameters.QuailityEnabled, Device, Symbology);
            //bool goodquality = report.GetParameter<bool>(AvailableParameters.GoodQuality, Device, Symbology);
            string apertureS = "00";
            double aperture = double.NaN;
            if (AperturePercentage != double.NaN)
            {
                double ppe = report.GetParameter<double>(BarcodeVerification.lib.Common.Parameters.PPE, Device, Symbology);
                aperture = ppe * AperturePercentage / 100;
                apertureS = ppe >= 10 ? $"{aperture:N0}" : $"0{aperture:N0}";
            }

            OverallGrade = new OverallGrade(Device, new Grade(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, double.NaN, report.GetParameter<bool>("passed") ? "" : "F"), $"NaN/{apertureS}/623", $"{aperture:N0}", "600");
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
            GradingStandard = GradingStandards.ISO_IEC_15415;
            AperturePercentage = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15415.aperture");
            ApertureMils = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15415.aperture_mil");
        }
        else
        if (template.GetParameter<bool>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15416.enabled"))
        {
            GradingStandard = GradingStandards.ISO_IEC_15416;
            AperturePercentage = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15416.aperture");
            ApertureMils = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso15416.aperture_mil");
        }
        else
        if (template.GetParameter<bool>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso29158.enabled"))
        {
            GradingStandard = GradingStandards.ISO_IEC_TR_29158;
            AperturePercentage = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso29158.aperture");
            ApertureMils = template.GetParameter<double>($"response.data.job.toolList[{toolSlot}].SymbologyTool.settings.SymbologySettings.iso29158.aperture_mil");
        }

        //Set GS1 last
        if (report.GetParameter<bool>($"gs1Enabled"))
        {
            ApplicationStandard = ApplicationStandards.GS1;
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
        GS1Results = new GS1Decode(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology, pass ? "PASS" : "FAIL", DecodeText, data, pass ? list : null, "");

        return true;
    }

    private bool SetXdimAndUnits(JObject report, JObject template)
    {
        Units = AvailableUnits.Pixels;

        string ppi = template.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.PPI, Device, Symbology);

        if (ppi == null)
        {
            Ppi = double.NaN;
            Logger.LogInfo($"Could not find: '{BarcodeVerification.lib.Common.Parameters.PPI.GetPath(Device, Symbology)}' in the Job. {Device}");
        }
        else
            Ppi = ppi.ParseDouble();

        string ppe = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.PPE, Device, Symbology);
        if (ppe == null)
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.PPE.GetPath(Device, Symbology)}' in ReportData. {Device}");
            return false;
        }

        XDimension = double.IsNaN(Ppi) ? ppe.ParseDouble() : ppe.ParseDouble() * 1000 / Ppi;

        return true;
    }

    private bool SetApeture()
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

        Grade grade = new(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, spl[0].ParseDouble());
        return new OverallGrade(Device, grade, original, spl[1], spl[2]);
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
            ValueString valueString = GetValueString(parameter, report.GetParameter<string>(parameter.GetPath(Device, Symbology)));
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
            return;

        }
        else if (type == typeof(Custom))
        {

            //if (parameter is AvailableParameters.UnusedEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Device, Symbology, report.GetParameter<double>("Datamatrix.uec"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}

            //if (parameter is AvailableParameters.MinimumEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Device, Symbology, report.GetParameter<double>("Datamatrix.ecc"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}
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
