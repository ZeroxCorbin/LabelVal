using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.Sectors;

public partial class SectorParameters : ObservableObject, ISectorParameters
{
    public ISector Sector { get; set; }

    [ObservableProperty] private string units;

    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;

    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;

    [ObservableProperty] private bool isNotEmpty = false;

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

        IsNotEmpty = false;

        //Get thew symbology enum
        AvailableSymbologies theSymbology = Sector.Report.SymbolType;

        //Get the region type for the symbology
        AvailableRegionTypes theRegionType = theSymbology.GetSymbologyRegionType(Sector.Report.Device);

        //Get the parameters list based on the region type.
        List<AvailableParameters> theParamters = Params.ParameterGroups[theRegionType][Sector.Report.Device];

        //Interate through the parameters
        foreach (AvailableParameters parameter in theParamters)
        {
            string data = GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType), report.ReportData);

            if (string.IsNullOrWhiteSpace(data))
            {
                Parameters.Add(new Missing(parameter));
                continue;
            }

            Type type = parameter.GetParameterDataType(Sector.Report.Device, theSymbology);

            if (type == typeof(GradeValue))
            {
                GradeValue gradeValue = GetGradeValue(parameter, GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType), report.ReportData));

                if (gradeValue != null)
                    Parameters.Add(gradeValue);
            }
            else if (type == typeof(Grade))
            {
                Grade grade = GetGrade(parameter, GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType), report.ReportData));

                if (grade != null)
                    Parameters.Add(grade);
            }
            else if (type == typeof(ValueDouble))
            {
                ValueDouble valueDouble = GetValueDouble(parameter, GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType), report.ReportData));
                if (valueDouble != null)
                    Parameters.Add(valueDouble);
            }
            else if (type == typeof(ValueString))
            {
                ValueString valueString = GetValueString(parameter, GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType), report.ReportData));
                if (valueString != null)
                    Parameters.Add(valueString);
            }
            else if (type == typeof(PassFail))
            {
                PassFail passFail = GetPassFail(parameter, GetParameter(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType), report.ReportData));
                if (passFail != null)
                    Parameters.Add(passFail);
            }
        }

        //Check for alarms
        List<string> alarms = GetParameters("Warning", report.ReportData);
        if (alarms.Count > 0)
        {
            foreach (string alarm in alarms)
            {
                Alarms.Add(new Alarm(AvaailableAlarmCategories.Warning, alarm));
            }
        }
    }

    private string GetParameter(string key, List<ReportData> report) => report.Find((e) => e.ParameterName.Equals(key))?.ParameterValue;
    private List<string> GetParameters(string key, List<ReportData> report) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();

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
