using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.V275.Sectors;

public partial class SectorDetails : ObservableObject, ISectorParameters
{
    public ISector Sector { get; set; }

    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;
    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;

    public ObservableCollection<IParameterValue> Parameters { get; } = [];
    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public SectorDetails() { }
    public SectorDetails(ISector sector) => Process(sector);
    public SectorDifferences Compare(ISectorParameters compare) => SectorDifferences.Compare(this, compare);

 public void Process(ISector sector)
    {
        if (sector is not V275.Sectors.Sector sec)
            return;

        Sector = sector;
        _ = sec.Report.Original;

        if (Sector.Report.SymbolType == AvailableSymbologies.Unknown)
        {
            IsSectorMissing = true;
            SectorMissingText = "Sector is missing";
            return;
        }
        //Get thew symbology enum
        AvailableSymbologies theSymbology = Sector.Report.SymbolType;

        //Get the region type for the symbology
        AvailableRegionTypes theRegionType = theSymbology.GetSymbologyRegionType(Sector.Report.Device);

        //Get the parameters list based on the region type.
        List<AvailableParameters> theParamters = Params.ParameterGroups[theRegionType][Sector.Report.Device];

        var report = (JObject)Sector.Report.Original;

        foreach (AvailableParameters parameter in theParamters)
        {
            var type = parameter.GetParameterDataType(Sector.Report.Device, theSymbology);

            if (type == typeof(GradeValue))
            {
                GradeValue gradeValue = GetGradeValue(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));

                if (gradeValue != null)
                {
                    Parameters.Add(gradeValue);
                    continue;
                }
            }
            else if (type == typeof(Grade))
            {
                Grade grade = GetGrade(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));

                if (grade != null)
                {
                    Parameters.Add(grade);
                    continue;
                }
            }
            else if (type == typeof(ValueDouble))
            {
                ValueDouble valueDouble = GetValueDouble(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
                if (valueDouble != null)
                {
                    Parameters.Add(valueDouble);
                    continue;
                }
            }
            else if (type == typeof(ValueString))
            {
                ValueString valueString = GetValueString(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
                if (valueString != null) { Parameters.Add(valueString); continue; }
            }
            else if (type == typeof(PassFail))
            {
                PassFail passFail = GetPassFail(parameter, report.GetParameter<string>  (parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
                if (passFail != null) { Parameters.Add(passFail); continue; }
            }
            else if (type == typeof(ValuePassFail))
            {
                ValuePassFail valuePassFail = GetValuePassFail(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
                if (valuePassFail != null) { Parameters.Add(valuePassFail); continue; }
            }

            Parameters.Add(new Missing(parameter));
            Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)}' missing or parse issue.");
        }

        //Check for alarms
        if (report["data"]?["alarms"] != null)
        {
            foreach (JObject alarm in report["data"]?["alarms"])
            {
                Alarms.Add(new Alarm(alarm["category"].Value<int>() == 1 ? AvaailableAlarmCategories.Warning : AvaailableAlarmCategories.Error, alarm["name"].ToString()));
            }
        }
    }

    private GradeValue GetGradeValue(AvailableParameters parameter, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        Grade grade = GetGrade(parameter, (JObject)gradeValue["grade"]);
        string value = gradeValue["value"].ToString();
        return new GradeValue(parameter, Sector.Report.Device, Sector.Report.SymbolType, grade, value);
    }

    private Grade GetGrade(AvailableParameters parameter, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        string value = gradeValue["value"].ToString();
        string letter = gradeValue["letter"].ToString();
        return new Grade(parameter, Sector.Report.Device, value);
    }

    private ValueDouble GetValueDouble(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Sector.Report.Device, Sector.Report.SymbolType, value);

    private ValueString GetValueString(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Sector.Report.Device, value);

    private PassFail GetPassFail(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Sector.Report.Device, value);

    public ValuePassFail GetValuePassFail(AvailableParameters parameter, JObject valuePassFail)
    {
        if (valuePassFail is null)
            return null;

        string passFail = valuePassFail["result"].ToString();
        string val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, Sector.Report.Device, Sector.Report.SymbolType, val, passFail);
    }
}
