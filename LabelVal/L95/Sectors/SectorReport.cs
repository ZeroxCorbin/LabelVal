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
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Drawing;

namespace LabelVal.L95.Sectors;

public class SectorReport : ISectorReport
{
    public JObject Original { get; private set; }

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

    public ObservableCollection<IParameterValue> Parameters { get; } = [];

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

        DecodeText = report.GetParameter<string>(AvailableParameters.DecodeText, Device, SymbolType);

        _ = SetStandardAndTable(report);
        _ = SetXdimAndUnits(report);
        _ = SetApeture(report);
        _ = SetOverallGrade(report);

        //foreach (AvailableParameters parameter in Params.CommonParameters)
        //{
        //    try
        //    {
        //        AddParameter(parameter, SymbolType, Parameters, report);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Logger.LogError(ex, $"Error processing parameter: {parameter}");
        //    }
        //}
    }
    private bool SetSymbologyAndRegionType(JObject report)
    {
        string sym = report.GetParameter<string>(AvailableParameters.Symbology, Device, AvailableSymbologies.Unknown);
        if (sym == null)
        {
            Logger.LogError($"Could not find: '{AvailableParameters.Symbology.GetParameterPath(AvailableDevices.L95, AvailableSymbologies.Unknown)}' in ReportData. {Device}");
            return false;
        }

        string dataBarType = report.GetParameter<string>(AvailableParameters.DataBarType, Device, AvailableSymbologies.Unknown);
        if (dataBarType != null)
            sym = $"DataBar {dataBarType}";

        SymbolType = sym.Replace("GS1 ", "").GetSymbology(AvailableDevices.L95);

        //Set RegionType
        RegionType = SymbolType.GetSymbologyRegionType(AvailableDevices.L95);

        if (SymbolType == AvailableSymbologies.Unknown)
        {
            Logger.LogError($"Could not determine symbology from: '{sym}' {Device}");
            return false;
        }

        return true;
    }

    private void AddParameter(AvailableParameters parameter, AvailableSymbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report)
    {
        Type type = parameter.GetParameterDataType(Device, theSymbology);

        if (type == typeof(GradeValue))
        {
            GradeValue gradeValue = GetGradeValue(parameter, report.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(Grade))
        {
            Grade grade = GetGrade(parameter, report.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)));

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
            ValueString valueString = GetValueString(parameter, report.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)));
            if (valueString != null)
            {
                target.Add(valueString); 
                return;
            }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetParameterPath(Device, SymbolType)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(Custom))
        {

        }

        target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(Device, SymbolType)}' missing or parse issue.");
    }




    private bool SetOverallGrade(JObject report)
    {
        string overall = report.GetParameter<string>(AvailableParameters.OverallGrade, Device, SymbolType);
        if (overall != null)
            OverallGrade = GetOverallGrade(overall);
        else
        {
            Logger.LogError($"Could not find: '{AvailableParameters.OverallGrade.GetParameterPath(AvailableDevices.L95, SymbolType)}' in ReportData. {Device}");
            return false;
        }
        return true;
    }

    private bool SetStandardAndTable(JObject report)
    {
        string stdString = report.GetParameter<string>(AvailableParameters.Standard, Device, SymbolType);
        string tblString = report.GetParameter<string>(AvailableParameters.GS1Table, Device, SymbolType);

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

        string data = report.GetParameter<string>(AvailableParameters.GS1Data, Device, SymbolType);
        string pass = report.GetParameter<string>(AvailableParameters.GS1DataStructure, Device, SymbolType);

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

    private bool SetXdimAndUnits(JObject report)
    {
        string xdim = report.GetParameter<string>(AvailableParameters.CellSize, Device, SymbolType);
        xdim ??= report.GetParameter<string>(AvailableParameters.Xdim, Device, SymbolType);
        if (xdim == null)
        {
            Logger.LogWarning($"Could not find: '{AvailableParameters.CellSize.GetParameterPath(AvailableDevices.L95, SymbolType)}' or '{AvailableParameters.Xdim.GetParameterPath(AvailableDevices.L95, SymbolType)}' in ReportData. {Device}");
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
        string aperture = report.GetParameter<string>(AvailableParameters.Aperture, Device, SymbolType);
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

    private GradeValue GetGradeValue(AvailableParameters parameter, string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        string[] spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Length != 2)
            return spl2.Length == 1 ? new GradeValue(parameter, Device, SymbolType, spl2[0], string.Empty) : null;
        else
            return new GradeValue(parameter, Device, SymbolType, spl2[0], spl2[1]);//  new GradeValue(name, ParseFloat(spl2[1]), new Grade(name, tmp, GetLetter(tmp)));
    }
    private Grade GetGrade(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new Grade(parameter, Device, data);
    private ValueDouble GetValueDouble(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueDouble(parameter, Device, SymbolType, data);
    private ValueString GetValueString(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueString(parameter, Device, data);
    private PassFail GetPassFail(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new PassFail(parameter, Device, data);
}
