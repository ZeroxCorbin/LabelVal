using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.Sectors;

public partial class SectorParameters : ObservableObject, ISectorParameters
{
    public ISector Sector { get; set; }

    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;

    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;


    public ObservableCollection<IParameterValue> Parameters { get; } = [];

    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public LabelVal.Sectors.Classes.SectorDifferences? Compare(ISectorParameters compare) => LabelVal.Sectors.Classes.SectorDifferences.Compare(this, compare);

    public SectorParameters() { }
    public SectorParameters(ISector sector) => ProcessNew(sector);

    public void ProcessNew(ISector sector)
    {
        if (sector is not LVS_95xx.Sectors.Sector sec)
            return;

        Sector = sector;
        FullReport report = sec.L95xxFullReport;

        //Get thew symbology enum
        AvailableSymbologies theSymbology = Sector.Report.SymbolType;

        //Get the region type for the symbology
        AvailableRegionTypes theRegionType = theSymbology.GetSymbologyRegionType(Sector.Report.Device);

        //Get the parameters list based on the region type.
        List<AvailableParameters> theParamters = Params.ParameterGroups[theRegionType][Sector.Report.Device];

        //Interate through the parameters
        foreach (AvailableParameters parameter in theParamters)
        {
            try
            {
                AddParameter(parameter, theSymbology, Parameters, report);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing parameter: {parameter}");
            }
        }

        //Check for alarms
        List<string> alarms = report.ReportData.GetParameters("Warning");
        if (alarms.Count > 0)
        {
            foreach (string alarm in alarms)
            {
                Alarms.Add(new Alarm(AvaailableAlarmCategories.Error, alarm));
            }
        }
    }

    private void AddParameter(AvailableParameters parameter, AvailableSymbologies theSymbology, ObservableCollection<IParameterValue> target, FullReport report)
    {
        Type type = parameter.GetParameterDataType(Sector.Report.Device, theSymbology);

        if (type == typeof(GradeValue))
        {
            GradeValue gradeValue = GetGradeValue(parameter, report.ReportData.GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(Grade))
        {
            Grade grade = GetGrade(parameter, report.ReportData.GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));

            if (grade != null)
            {
                target.Add(grade);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, report.ReportData.GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = GetValueString(parameter, report.ReportData.GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (valueString != null)
            {
                target.Add(valueString); return;
            }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.ReportData.GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if(type == typeof(Custom))
        {

        }

            target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)}' missing or parse issue.");
    }

    private GradeValue GetGradeValue(AvailableParameters parameter, string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        string[] spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Length != 2)
            return spl2.Length == 1 ? new GradeValue(parameter, Sector.Report.Device, Sector.Report.SymbolType, spl2[0], string.Empty) : null;
        else
            return new GradeValue(parameter, Sector.Report.Device, Sector.Report.SymbolType, spl2[0], spl2[1]);//  new GradeValue(name, ParseFloat(spl2[1]), new Grade(name, tmp, GetLetter(tmp)));
    }
    private Grade GetGrade(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new Grade(parameter, Sector.Report.Device, data);
    private ValueDouble GetValueDouble(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueDouble(parameter, Sector.Report.Device, Sector.Report.SymbolType, data);
    private ValueString GetValueString(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueString(parameter, Sector.Report.Device, data);
    private PassFail GetPassFail(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new PassFail(parameter, Sector.Report.Device, data);

}
