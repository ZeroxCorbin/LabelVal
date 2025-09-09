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
        //_ = SetApeture(report);

        _ = SetOverallGrade(report);

        //foreach (AvailableParameters parameter in Params.CommonParameters)
        //{
        //    try
        //    {
        //        AddParameter(parameter, Symbology, Parameters, report, (JObject)secTemplate.Original);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Logger.Error(ex, $"Error processing parameter: {parameter}");
        //    }
        //}

        if (report["data"]["extendedData"] != null)
            ExtendedData = JsonConvert.DeserializeObject<ModuleData>(JsonConvert.SerializeObject(report["data"]["extendedData"]));
    }

    private bool SetSymbologyAndRegionType(JObject report)
    {
        var sym = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Symbology, Device, Symbologies.Unknown);
        if (sym == null)
        {
            Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Symbology.GetPath(Device, Symbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        Symbology = sym.GetSymbology(Device);

        if (Symbology == Symbologies.Unknown)
        {
            Logger.Error($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private bool SetOverallGrade(JObject report)
    {
        var overall = report.GetParameter<JObject>(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, Symbology);
        if (overall != null)
            OverallGrade = ParameterHandling.GetOverallGrade(overall);
        else
        {
            Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.OverallGrade.GetPath(Device, Symbology)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject template)
    {
        var isGs1 = template.GetParameter<bool>(BarcodeVerification.lib.Common.Parameters.GradingStandard, Device, Symbology);

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
            Logger.Info("GS1 is not enabled. Skipping GS1 Data.");
            return true;
        }

        var data = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology);
        var pass = report.GetParameter<bool>(BarcodeVerification.lib.Common.Parameters.Structure, Device, Symbology);

        List<string> list = [];
        if (!string.IsNullOrEmpty(data))
        {
            var spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (var str in spl)
                list.Add($"({str}");
        }
        GS1Results = new GS1Decode(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology, pass ? "PASS" : "FAIL", DecodeText, data, list , "");
        return true;
    }

    private bool SetXdimAndUnits(JObject report)
    {
        var xdim = report.GetParameter<JObject>(BarcodeVerification.lib.Common.Parameters.Xdim, Device, Symbology);
        XDimension = xdim != null
            ? xdim.GetParameter<double>("value")
            : report.GetParameter<double>(BarcodeVerification.lib.Common.Parameters.Xdim, Device, Symbology);

        var unit = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Units, Device, Symbology);
        if (unit == null)
        {
            Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Units.GetPath(Device, Symbology)}' in ReportData. {Device}");
            return false;
        }

        Units = unit.Equals("mil") ? AvailableUnits.Mils : AvailableUnits.MM;

        return true;
    }

    //private bool SetApeture(JObject report)
    //{
    //    Aperture = report.GetParameter<double>(BarcodeVerification.lib.Common.Parameters.Aperture, Device, Symbology);
    //    return true;
    //}

}
