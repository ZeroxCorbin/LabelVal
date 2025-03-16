using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using System.Drawing;

namespace LabelVal.LVS_95xx.Sectors;

public class SectorReport : ISectorReport
{
    public object Original { get; private set; }

    public AvailableStandards Standard { get; private set; }
    public AvailableTables GS1Table { get; private set; }

    public AvailableDevices Device => AvailableDevices.L95;
    public AvailableRegionTypes RegionType { get; private set; }
    public AvailableSymbologies SymbolType { get; private set; }

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

    public SectorReport(FullReport report)
    {
        Original = report;

        if (report.Report == null || report.ReportData == null)
        {
            Logger.LogError($"Report or ReportData is null. {Device}");
            return;
        }

        Top = report.Report.Y1;
        Left = report.Report.X1;
        Width = report.Report.SizeX;
        Height = report.Report.SizeY;
        AngleDeg = 0;

        CenterPoint = new Point((int)(Left + (Width / 2)), (int)(Top + (Height / 2)));

        _ = SetSymbologyAndRegionType(report.ReportData);

        DecodeText = report.ReportData.GetParameter(AvailableParameters.DecodeText, Device, SymbolType);

        _ = SetStandardAndTable(report.ReportData);
        _ = SetXdimAndUnits(report.ReportData);
        _ = SetApeture(report.ReportData);
        _ = SetOverallGrade(report.ReportData);

    }

    private bool SetSymbologyAndRegionType(List<ReportData> report)
    {
        string sym = report.GetParameter(AvailableParameters.Symbology, Device, SymbolType);
        if (sym == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Symbology.GetParameterPath(AvailableDevices.L95, AvailableSymbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        string dataBarType = report.GetParameter(AvailableParameters.DataBarType, Device, SymbolType);
        if (dataBarType != null)
            sym = $"DataBar {dataBarType}";

        SymbolType = sym.GetSymbology(AvailableDevices.L95);

        //Set RegionType
        RegionType = SymbolType.GetSymbologyRegionType(AvailableDevices.L95);

        if (SymbolType == AvailableSymbologies.Unknown)
        {
            Logger.LogError($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private bool SetOverallGrade(List<ReportData> report)
    {
        string overall = report.GetParameter(AvailableParameters.OverallGrade, Device, SymbolType);
        if (overall != null)
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogError($"Could not find: '{AvailableParameters.OverallGrade.GetParameterPath(AvailableDevices.L95, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(List<ReportData> report)
    {
        string stdString = report.GetParameter(AvailableParameters.Standard, Device, SymbolType);
        string tblString = report.GetParameter(AvailableParameters.GS1Table, Device, SymbolType);

        if (stdString == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Standard.GetParameterPath(AvailableDevices.L95, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        Standard = stdString.GetStandard(AvailableDevices.L95);

        if (Standard == AvailableStandards.Unknown)
        {
            Logger.LogError($"Could not determine standard from: '{stdString}' {Device}");
            return false;
        }

        if (tblString != null)
            GS1Table = tblString.GetTable(AvailableDevices.L95);
        else
        {
            GS1Table = AvailableTables.Unknown;
            return true;
        }

        string data = report.GetParameter(AvailableParameters.GS1Data, Device, SymbolType);
        string pass = report.GetParameter(AvailableParameters.GS1DataStructure, Device, SymbolType);

        List<string> list = [];
        if (!string.IsNullOrEmpty(data))
        {
            string[] spl = data.Split('(', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in spl)
                list.Add($"({str}");
        }
        GS1Results = new GS1Decode(AvailableParameters.GS1Data, Device, SymbolType, pass, DecodeText, data, pass.Equals("PASS") ? list : null, "");
        return true;
    }

    private bool SetXdimAndUnits(List<ReportData> report)
    {
        string xdim = report.GetParameter(AvailableParameters.CellSize, Device, SymbolType);
        xdim ??= report.GetParameter(AvailableParameters.Xdim, Device, SymbolType);
        if (xdim == null)
        {
            Logger.LogWarning($"Could not find: '{AvailableParameters.CellSize.GetParameterPath(AvailableDevices.L95, SymbolType)}' or '{AvailableParameters.Xdim.GetParameterPath(AvailableDevices.L95, SymbolType)}' in ReportData. {Device}");
            return true;
        }
        string[] split = xdim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length < 2)
        {
            Logger.LogError($"Could not parse: '{xdim}' to get XDimension. {Device}");
            return false;
        }
        XDimension = split[0].ParseDouble();
        if (split[1].Equals("mils"))
            Units = AvailableUnits.Mils;
        else if (split[1].Equals("mm"))
            Units = AvailableUnits.MM;
        else
        {
            Logger.LogError($"Could not determine units from: '{xdim}' {Device}");
            return false;
        }
        return true;
    }

    private bool SetApeture(List<ReportData> report)
    {
        string aperture = report.GetParameter(AvailableParameters.Aperture, Device, SymbolType);
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
            Logger.LogError($"Could not find: '{AvailableParameters.Aperture.GetParameterPath(AvailableDevices.L95, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private OverallGrade GetOverallGrade(string original)
    {
        string data = original.Replace("DPM", "");
        string[] spl = data.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(AvailableParameters.OverallGrade, Device, spl[0]);
        return new OverallGrade(Device, grade, original, spl[1], spl[2]);
    }
}
