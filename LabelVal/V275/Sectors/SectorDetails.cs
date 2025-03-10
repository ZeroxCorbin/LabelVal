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

    public ObservableCollection<IParameterValue> Grades { get; } = [];
    public ObservableCollection<IParameterValue> PassFails { get; } = [];

    public ObservableCollection<ValueDouble> ValueDoubles { get; } = [];
    public ObservableCollection<ValueString> ValueStrings { get; } = [];

    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public ObservableCollection<AvailableParameters> MissingParameters { get; } = [];

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

        foreach (AvailableParameters parameter in theParamters)
        {
            string data = GetParameter(parameter.GetParameterPath(AvailableDevices.V275), (JObject)Sector.Report.Original);

            if (string.IsNullOrWhiteSpace(data))
            {
                MissingParameters.Add(parameter);
                continue;
            }

            bool found = false;

            var type = parameter.GetParameterDataType(AvailableDevices.V275, theSymbology);

            if (type == typeof( BarcodeVerification.lib.ISO.GradeValue))
            {
                GradeValue gradeValue = GetGradeValue(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), (JObject)Sector.Report.Original));

                if (gradeValue != null)
                {
                    Grades.Add(gradeValue);
                    found = true;
                }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.Grade))
            {
                Grade grade = GetGrade(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), (JObject)Sector.Report.Original));

                if (grade != null)
                {
                    Grades.Add(grade);
                    found = true;
                }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValueDouble))
            {
                ValueDouble valueDouble = GetValueDouble(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), (JObject)Sector.Report.Original));
                if (valueDouble != null)
                {
                    ValueDoubles.Add(valueDouble);
                    found = true;
                }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValueString))
            {
                ValueString valueString = GetValueString(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), (JObject)Sector.Report.Original));
                if (valueString != null) { ValueStrings.Add(valueString); found = true; }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.PassFail))
            {
                PassFail passFail = GetPassFail(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), (JObject)Sector.Report.Original));
                if (passFail != null) { PassFails.Add(passFail); found = true; }
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValuePassFail))
            {
                ValuePassFail valuePassFail = GetValuePassFail(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.V275), (JObject)Sector.Report.Original));
                if (valuePassFail != null) { PassFails.Add(valuePassFail); found = true; }
            }

            if (!found)
                Logger.LogWarning($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(AvailableDevices.V275)}' parse issue.");
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
        return new GradeValue(parameter, grade, value, AvailableDevices.V275);
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
        return new Grade(parameter, value, letter);
    }

    private ValueDouble GetValueDouble(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, value, AvailableDevices.V275);

    private ValueString GetValueString(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, value);

    private PassFail GetPassFail(AvailableParameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, value);

    public ValuePassFail GetValuePassFail(AvailableParameters parameter, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        JObject valuePassFail = JObject.Parse(value);

        if (valuePassFail is null)
            return null;

        string passFail = valuePassFail["result"].ToString();
        string val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, val, passFail, AvailableDevices.V275);
    }
}
