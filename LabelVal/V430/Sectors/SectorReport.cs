using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using Logging.lib;

namespace LabelVal.V430.Sectors;

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

    public SectorReport(JObject report, string reportId, string decodeId, ISectorTemplate template, GS1Tables desiredTable)
    {
        Original = report;
        GS1Table = desiredTable;

        var ipReport = report.GetParameter<JObject>($"ipReports[uId:{reportId}]");
        var decode = ipReport.GetParameter<JObject>($"decodes[dId:{decodeId}]");
        if(decode == null)
        {
            _ = SetOverallGrade(decode);
            Logger.Debug($"Could not find decode: '{decodeId}' in ReportData. {Device}");
            return;
        }

        if (!SetBoudingBox(decode))
        {
            Top = template.Top;
            Left = template.Left;
            Width = template.Width;
            Height = template.Height;
            AngleDeg = template.AngleDeg;

            CenterPoint = new System.Drawing.Point((int)(Left + (Width / 2)), (int)(Top + (Height / 2)));
        }

        //Set Symbology
        _ = SetSymbologyAndRegionType(decode);

        DecodeText = decode.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.DecodeText, Device, Symbology);

        _ = SetStandardAndTable(report, template.Original);
        //Set GS1 Data
        _ = SetGS1Data(report);
        //Set XDimension
        _ = SetXdimAndUnits(report, template.Original);
        _ = SetOverallGrade(decode);
        //Set Aperture
        _ = SetApeture();

        //foreach (BarcodeVerification.lib.Common.Parameters parameter in Params.CommonBarcodeVerification.lib.Common.Parameters)
        //{
        //    try
        //    {
        //        AddParameter(parameter, SymbolType, BarcodeVerification.lib.Common.Parameters, report, template);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Logger.Error(ex, $"Error processing parameter: {parameter}");
        //    }
        //}

    }

    private bool SetBoudingBox(JObject decode)
    {
        (double Top, double Left, double Width, double Height) bb = ConvertBoundingBox(decode.GetParameter<JArray>("corners"));
        if (bb == (0, 0, 0, 0))
        {
            Logger.Error("Could not find the bounding box.");
            return false;
        }

        Top = bb.Top;
        Left = bb.Left;
        Width = bb.Width;
        Height = bb.Height;
        AngleDeg = 0;

        CenterPoint = new System.Drawing.Point((int)(Left + (Width / 2)), (int)(Top + (Height / 2)));

        return true;
    }
    public static (double Top, double Left, double Width, double Height) ConvertBoundingBox(JArray corners)
    {
        if (corners == null)
        {
            Logger.Error("Could not find the bounding box.");
            return (0, 0, 0, 0);
        }

        if (corners.Count != 4)
        {
            throw new ArgumentException("Bounding box must have exactly 4 corners.");
        }
        // This is the array values as strings: {"760, 567"} {"560, 566"} {"561, 366"} {"761, 367"}
        //Please extract the values and convert them to minX, minY, width, height
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        foreach (var corner in corners)
        {
            var point = corner.Value<string>().Trim('{', '}', '\"').Split(',');
            if (point.Length != 2)
            {
                throw new ArgumentException("Each corner must have exactly 2 coordinates.");
            }
            var x = double.Parse(point[0]);
            var y = double.Parse(point[1]);
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }
        var width = maxX - minX;
        var height = maxY - minY;

        // Return the bounding box as a tuple
        return (minY, minX, width, height);
    }

    private bool SetSymbologyAndRegionType(JObject decode)
    {
        var sym = decode.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Symbology, Device, Symbology);
        if (sym == null)
        {
            Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Symbology.GetPath(Device, Symbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        Symbology = sym.GetSymbology(Device);

        //Set RegionType
       // RegionType = SymbolType.GetSymbologyRegionType(Device);

        if (Symbology == Symbologies.Unknown)
        {
            Logger.Error($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private bool SetOverallGrade(JObject decode)
    {
        if(decode == null)
        {
            OverallGrade = new OverallGrade(Device, new Grade(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, 0), "0.0/0/0", "0", "0");
            return true;
        }
        OverallGrade = new OverallGrade(Device, new Grade(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, 4.0), "4.0/0/0", "0", "0");

        return true;
    }

    private bool SetStandardAndTable(JObject report, JObject template)
    {
        ApplicationStandard = ApplicationStandards.None;
        AperturePercentage = double.NaN;
        ApertureMils = double.NaN;

        return true;
    }

    private bool SetGS1Data(JObject report)
    {
        if (ApplicationStandard != ApplicationStandards.GS1)
        {
            Logger.Info("GS1 is not enabled. Skipping GS1 Data.");
            return true;
        }

        return true;
    }

    private bool SetXdimAndUnits(JObject report, JObject template)
    {
        Units = AvailableUnits.Pixels;
        XDimension = double.NaN;

        return true;
    }

    private bool SetApeture()
    {
        if (OverallGrade == null)
        {
            Logger.Error("OverallGrade is null. Cannot calculate Aperture.");
            return false;
        }

        if (string.IsNullOrEmpty(OverallGrade.Value))
        {
            //Would have to calculate the aperture based on the ppe and the target aperture percentage.
            Aperture = double.NaN;
            return true;
        }

        var spl = OverallGrade.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (spl.Length < 2)
        {
            Logger.Error($"Could not parse: '{OverallGrade.Value}' to get Aperture. {Device}");
            return false;
        }

        Aperture = spl[1].TrimStart('0').ParseDouble();

        return true;
    }

    private OverallGrade GetOverallGrade(string original)
    {
        var spl = original.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, spl[0].ParseDouble());
        return new OverallGrade(Device, grade, original, spl[1], spl[2]);
    }

    private void AddParameter(BarcodeVerification.lib.Common.Parameters parameter, Symbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report, JObject template)
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

            //if (parameter is BarcodeVerification.lib.Common.Parameters.UnusedEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Device, SymbolType, report.GetParameter<double>("Datamatrix.uec"));
            //    if (valueDouble != null)
            //    {
            //        BarcodeVerification.lib.Common.Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}

            //if (parameter is BarcodeVerification.lib.Common.Parameters.MinimumEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Device, SymbolType, report.GetParameter<double>("Datamatrix.ecc"));
            //    if (valueDouble != null)
            //    {
            //        BarcodeVerification.lib.Common.Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}
        }

        target.Add(new Missing(parameter));
        Logger.Debug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(Device, Symbology)}' missing or parse issue.");
    }

    private GradeValue? GetGradeValue(BarcodeVerification.lib.Common.Parameters parameter, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        Grade grade = new(parameter, Device, gradeValue["grade"].ToString());
        var value = gradeValue["value"].ToString();
        return new GradeValue(parameter, Device, Symbology, grade, value);
    }

    private Grade? GetGrade(BarcodeVerification.lib.Common.Parameters parameter, JObject grade)
    {
        if (grade is null)
            return null;
        var value = grade["value"].ToString();
        _ = grade["letter"].ToString();
        return new Grade(parameter, Device, value);
    }

    private ValueDouble? GetValueDouble(BarcodeVerification.lib.Common.Parameters parameter, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Device, Symbology, value);

    private ValueString? GetValueString(BarcodeVerification.lib.Common.Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Device, value);

    private PassFail? GetPassFail(BarcodeVerification.lib.Common.Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Device, value);

    public ValuePassFail? GetValuePassFail(BarcodeVerification.lib.Common.Parameters parameter, JObject valuePassFail)
    {
        if (valuePassFail is null)
            return null;

        var passFail = valuePassFail["result"].ToString();
        var val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, Device, Symbology, val, passFail);
    }
}
