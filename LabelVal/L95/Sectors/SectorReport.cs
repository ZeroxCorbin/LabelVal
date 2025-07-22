using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
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
            Logger.LogError($"Report or ReportData is null. {Device}");
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
        _ = SetApeture(report);
        _ = SetOverallGrade(report);

        //foreach (AvailableParameters parameter in Params.CommonParameters)
        //{
        //    try
        //    {
        //        AddParameter(parameter, Symbology, Parameters, report);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Logger.LogError(ex, $"Error processing parameter: {parameter}");
        //    }
        //}
    }
    private bool SetSymbologyAndRegionType(JObject report)
    {
        string sym = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Symbology, Device, Symbologies.Unknown);
        if (sym == null)
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Symbology.GetPath(Devices.L95, Symbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        string dataBarType = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.DataBarType, Device, Symbologies.Unknown);
        if (dataBarType != null)
            sym = $"DataBar {dataBarType}";

        Symbology = sym.Replace("GS1 ", "").GetSymbology(Devices.L95);

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
        if (overall != null)
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.OverallGrade.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject report)
    {
        string stdString = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GradingStandard, Device, Symbology);
        string tblString = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Table, Device, Symbology);

        if (stdString == null)
        {
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.GradingStandard.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
            return false;
        }
        GradingStandard = stdString.GetGradingStandard(Devices.L95, Specification);

        if (GradingStandard == GradingStandards.None)
        {
            Logger.LogError($"Could not determine standard from: '{stdString}' {Device}");
            return false;
        }

        if (tblString != null)
            GS1Table = tblString.GetGS1Table(Devices.L95);
        else
        {
            GS1Table = GS1Tables.Unknown;
            return true;
        }

        string data = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology);
        string pass = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.GS1DataStructure, Device, Symbology);

        List<string> list = [];
        if (!string.IsNullOrEmpty(data))
        {
            string[] spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in spl)
                list.Add($"({str}");
        }
        GS1Results = new GS1Decode(BarcodeVerification.lib.Common.Parameters.GS1Data, Device, Symbology, pass, DecodeText, data, list, "");
        return true;
    }

    private bool SetXdimAndUnits(JObject report)
    {
        string xdim = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.CellSize, Device, Symbology);
        xdim ??= report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Xdim, Device, Symbology);
        if (xdim == null)
        {
            Logger.LogWarning($"Could not find: '{BarcodeVerification.lib.Common.Parameters.CellSize.GetPath(Devices.L95, Symbology)}' or '{BarcodeVerification.lib.Common.Parameters.Xdim.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
            return true;
        }
        string[] split = xdim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 1)
        {
            if (xdim.EndsWith("mm"))
            {
                XDimension = xdim.ParseDouble();
                Units = AvailableUnits.MM;
            }
            else
            {
                Logger.LogError($"Could not determine units from: '{xdim}' {Device}");
                return false;
            }
               
        }
        else if(split.Length == 2)
        {
            XDimension = split[0].ParseDouble();
        if (split[1].Equals("mils"))
            Units = AvailableUnits.Mils;
        }
        else
        {
            Logger.LogError($"Could not determine units from: '{xdim}' {Device}");
            return false;
        }
        return true;
    }

    private bool SetApeture(JObject report)
    {
        string aperture = report.GetParameter<string>(BarcodeVerification.lib.Common.Parameters.Aperture, Device, Symbology);
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
            Logger.LogError($"Could not find: '{BarcodeVerification.lib.Common.Parameters.Aperture.GetPath(Devices.L95, Symbology)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private OverallGrade GetOverallGrade(string original)
    {
        string data = original.Replace("DPM", "");
        string[] spl = data.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(BarcodeVerification.lib.Common.Parameters.OverallGrade, Device, spl[0]);
        return new OverallGrade(Device, grade, original, spl[1], spl[2]);
    }

    private GradeValue GetGradeValue(Parameters parameter, string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        string[] spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Length != 2)
            return spl2.Length == 1 ? new GradeValue(parameter, Device, Symbology, spl2[0], string.Empty) : null;
        else
            return new GradeValue(parameter, Device, Symbology, spl2[0], spl2[1]);//  new GradeValue(name, ParseFloat(spl2[1]), new Grade(name, tmp, GetLetter(tmp)));
    }
    private Grade GetGrade(Parameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new Grade(parameter, Device, data);
    private ValueDouble GetValueDouble(Parameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueDouble(parameter, Device, Symbology, data);
    private ValueString GetValueString(Parameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueString(parameter, Device, data);
    private PassFail GetPassFail(Parameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new PassFail(parameter, Device, data);
}
