using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace LabelVal.L95.Sectors;

public class SectorReport : ISectorReport
{
    public Devices Device => Devices.L95;

    public Symbologies Symbology { get; private set; }
    public SymbologySpecifications Specification => Symbology.GetSpecification(Device);
    public SymbologySpecificationTypes Type => Specification.GetCodeType();

    public ApplicationStandards ApplicationStandard { get; private set; }
    public GradingStandards GradingStandard { get; private set; }
    public GS1Tables GS1Table { get; private set; }

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

    public SectorReport(JObject report, ISectorTemplate template)
    {
        Original = report;

        if (report == null)
        {
            Logger.Error($"Report or ReportData is null. {Device}");
            return;
        }

        Top = template.Top;
        Left = template.Left;
        Width = template.Width;
        Height = template.Height;
        AngleDeg = template.AngleDeg;

        CenterPoint = new Point((int)(Left + (Width / 2)), (int)(Top + (Height / 2)));

        _ = SetSymbologyAndRegionType(report);

        DecodeText = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.DecodeText, Device, Symbology);

        _ = SetStandardAndTable(report);
        _ = SetXdimAndUnits(report);
        //_ = SetApeture(report);
        _ = SetOverallGrade(report);

        //foreach (AvailableParameters parameter in Params.CommonParameters)
        //{
        //    try
        //    {
        //        AddParameter(parameter, Symbology, Parameters, report);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Logger.Error(ex, $"Error processing parameter: {parameter}");
        //    }
        //}
    }
    private bool SetSymbologyAndRegionType(JObject report)
    {
        var sym = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Symbology, Device, Symbologies.Unknown);
        if (sym == null)
        {
            Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Symbology.GetPath(Devices.L95, Symbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        var dataBarType = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.DataBarType, Device, Symbologies.Unknown);
        if (dataBarType != null)
            sym = $"DataBar {dataBarType}";

        Symbology = sym.Replace("GS1 ", "").GetSymbology(Devices.L95);

        if (Symbology == Symbologies.Unknown)
        {
            Logger.Error($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private bool SetOverallGrade(JObject report)
    {
        var overall = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, Symbology);
        if (overall != null)
            OverallGrade = ParameterHandling.GetOverallGrade(overall);
        else
        {
            Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.OverallGrade.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject report)
    {
        var stdString = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GradingStandard, Device, Symbology);
        var tblString = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Table, Device, Symbology);

        if (stdString == null)
        {
            Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.GradingStandard.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
            return false;
        }
        GradingStandard = "ISO".GetGradingStandard(Device, Specification);

        if (GradingStandard == GradingStandards.None)
        {
            Logger.Error($"Could not determine standard from: '{stdString}' {Device}");
            return false;
        }

        ApplicationStandard = stdString.GetApplicationStandards(Device);

        if (ApplicationStandard == ApplicationStandards.GS1)
        {
            if (tblString != null)
                GS1Table = tblString.GetGS1Table(Devices.L95);
            else
            {
                GS1Table = GS1Tables.Unknown;
                return true;
            }

            var data = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology);
            var pass = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Structure, Device, Symbology);

            List<string> list = [];
            if (!string.IsNullOrEmpty(data))
            {
                var spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
                foreach (var str in spl)
                    list.Add($"({str}");
            }
            GS1Results = new GS1Decode(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology, pass, DecodeText, data, list, "");
        }
        return true;
    }

    private bool SetXdimAndUnits(JObject report)
    {
        var xdim = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.CellSize, Device, Symbology);
        xdim ??= report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Xdim, Device, Symbology);
        if (xdim == null)
        {
            Logger.Warning($"Could not find: '{BarcodeVerification.lib.Common.Parameters.CellSize.GetPath(Devices.L95, Symbology)}' or '{BarcodeVerification.lib.Common.Parameters.Xdim.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
            return true;
        }
        var split = xdim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 1)
        {
            if (xdim.EndsWith("mm"))
            {
                XDimension = xdim.ParseDouble();
                Units = AvailableUnits.MM;
            }
            else
            {
                Logger.Error($"Could not determine units from: '{xdim}' {Device}");
                return false;
            }
        }
        else if (split.Length == 2)
        {
            XDimension = split[0].ParseDouble();
            if (split[1].Equals("mils"))
                Units = AvailableUnits.Mils;
        }
        else
        {
            Logger.Error($"Could not determine units from: '{xdim}' {Device}");
            return false;
        }
        return true;
    }

    //private bool SetApeture(JObject report)
    //{
    //    var aperture = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Aperture, Device, Symbology);
    //    if (aperture != null)
    //    {
    //        //GetParameter returns: Reference number 12 (12 mil)
    //        var split = aperture.Split('(', StringSplitOptions.RemoveEmptyEntries);
    //        if (split.Length != 2)
    //        {
    //            Logger.Error($"Could not parse: '{aperture}' to get Aperture. {Device}");
    //            return false;
    //        }
    //        Aperture = split[1].ParseDouble();
    //    }
    //    else
    //    {
    //        Logger.Error($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Aperture.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
    //        return false;
    //    }
    //    return true;
    //}
}
