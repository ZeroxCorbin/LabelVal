using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.V275.Sectors;

public partial class SectorDetails : ObservableObject, ISectorDetails
{
    public ISector Sector { get; set; }

    [ObservableProperty] private string symbolType;
    [ObservableProperty] private string units;
    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;
    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;
    [ObservableProperty] private bool isNotEmpty = false;

    public ObservableCollection<IParameterValue> Parameters { get; } = [];
    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public SectorDetails() { }
    public SectorDetails(ISector sector) => Process(sector);
    public SectorDifferences Compare(ISectorDetails compare) => SectorDifferences.Compare(this, compare);

    private string GetParameter(string path, JObject report)
    {
        string[] parts = path.Split('.');
        JObject current = report;
        for (int i = 0; i < parts.Length; i++)
        {
            if (current[parts[i]] is null)
                return null;
            if (i == parts.Length - 1)
                return current[parts[i]].ToString();
            current = (JObject)current[parts[i]];
        }
        return null;
    }

    public void Process(ISector sector)
    {
        if (sector is not V275.Sectors.Sector sec)
            return;

        Sector = sector;
        _ = sec.Report.Original;

        IsNotEmpty = false;

        if (Sector.Report.SymbolType == AvailableSymbologies.Unknown)
        {
            IsSectorMissing = true;
            SectorMissingText = "Sector is missing";
            return;
        }
        //Get thew symbology enum
        AvailableSymbologies theSymbology = Sector.Report.SymbolType;

        //Get the region type for the symbology
        AvailableRegionTypes theRegionType = theSymbology.GetSymbologyRegionType(AvailableDevices.V275);

        //Get the parameters list based on the region type.
        List<AvailableParameters> theParamters = Params.ParameterGroups[theRegionType][AvailableDevices.V275];

        var report = (JObject)Sector.Report.Original;

        foreach (AvailableParameters parameter in theParamters)
        {
            string data = GetParameter(parameter.GetParameterPath(AvailableDevices.V275), report);

            if (string.IsNullOrWhiteSpace(data))
            {
                Parameters.Add(new Missing(parameter));
                continue;
            }

            bool found = false;

            var type = parameter.GetParameterDataType(AvailableDevices.V275, theSymbology);

            if (type == typeof( BarcodeVerification.lib.ISO.GradeValue))
            {
                GradeValue gradeValue = GetGradeValue(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), report));

                if (gradeValue != null)
                {
                    Parameters.Add(gradeValue);
                    found = true;
                }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.Grade))
            {
                Grade grade = GetGrade(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), report));

                if (grade != null)
                {
                    Parameters.Add(grade);
                    found = true;
                }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValueDouble))
            {
                ValueDouble valueDouble = GetValueDouble(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), report));
                if (valueDouble != null)
                {
                    Parameters.Add(valueDouble);
                    found = true;
                }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValueString))
            {
                ValueString valueString = GetValueString(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), report));
                if (valueString != null) { Parameters.Add(valueString); found = true; }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.PassFail))
            {
                PassFail passFail = GetPassFail(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), report));
                if (passFail != null) { Parameters.Add(passFail); found = true; }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValuePassFail))
            {
                ValuePassFail valuePassFail = GetValuePassFail(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), report));
                if (valuePassFail != null) { Parameters.Add(valuePassFail); found = true; }
            }

            if (!found)
                Logger.LogWarning($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(AvailableDevices.V275)}' parse issue.");
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

    private GradeValue GetGradeValue(AvailableParameters parameter, string gradestring)
    {
        if (string.IsNullOrWhiteSpace(gradestring))
            return null;

        JObject gradeValue = JObject.Parse(gradestring);

        if (gradeValue is null)
            return null;

        Grade grade = GetGrade(parameter, gradeValue["grade"].ToString());
        string value = gradeValue["value"].ToString();
        return new GradeValue(parameter, AvailableDevices.V275, grade, value);
    }

    private Grade GetGrade(AvailableParameters parameter, string gradeString)
    {
        if (string.IsNullOrWhiteSpace(gradeString))
            return null;

        JObject gradeValue = JObject.Parse(gradeString);

        if (gradeValue is null)
            return null;
        string value = gradeValue["value"].ToString();
        string letter = gradeValue["letter"].ToString();
        return new Grade(parameter, AvailableDevices.V275, value);
    }

    private ValueDouble GetValueDouble(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, AvailableDevices.V275, value);

    private ValueString GetValueString(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, AvailableDevices.V275, value);

    private PassFail GetPassFail(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, AvailableDevices.V275, value);

    public ValuePassFail GetValuePassFail(AvailableParameters parameter, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        JObject valuePassFail = JObject.Parse(value);

        if (valuePassFail is null)
            return null;

        string passFail = valuePassFail["result"].ToString();
        string val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, AvailableDevices.V275, val, passFail);
    }
}
