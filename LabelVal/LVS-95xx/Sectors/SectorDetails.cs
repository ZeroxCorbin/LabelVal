using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.Sectors;

public partial class SectorDetails : ObservableObject, ISectorDetails
{
    public ISector Sector { get; set; }

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

    public SectorDifferences? Compare(ISectorDetails compare) => SectorDifferences.Compare(this, compare);

    public SectorDetails() { }
    public SectorDetails(ISector sector) => ProcessNew(sector);

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
        AvailableRegionTypes theRegionType = theSymbology.GetSymbologyRegionType(AvailableDevices.L95);

        //Get the parameters list based on the region type.
        List<AvailableParameters> theParamters = Params.ParameterGroups[theRegionType][AvailableDevices.L95];

        //Interate through the parameters
        foreach (AvailableParameters parameter in theParamters)
        {
            string data = GetParameter(parameter.GetParameterPath(AvailableDevices.L95), report.ReportData, true);

            if (string.IsNullOrWhiteSpace(data))
            {
                MissingParameters.Add(parameter);
                continue;
            }

            var type = parameter.GetParameterDataType(AvailableDevices.L95, theSymbology);

            if (type == typeof(BarcodeVerification.lib.ISO.GradeValue))
            {
                GradeValue gradeValue = GetGradeValue(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.L95), report.ReportData, true));

                if (gradeValue != null)
                    Grades.Add(gradeValue);
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.Grade))
            {
                Grade grade = GetGrade(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.L95), report.ReportData, true));

                if (grade != null)
                    Grades.Add(grade);
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValueDouble))
            {
                ValueDouble valueDouble = GetValueDouble(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.L95), report.ReportData, true));
                if (valueDouble != null)
                    ValueDoubles.Add(valueDouble);
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.ValueString))
            {
                ValueString valueString = GetValueString(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.L95), report.ReportData, true));
                if (valueString != null)
                    ValueStrings.Add(valueString);
            }
            else if (type == typeof(BarcodeVerification.lib.ISO.PassFail))
            {
                PassFail passFail = GetPassFail(parameter, GetParameter(parameter.GetParameterPath(AvailableDevices.L95), report.ReportData, true));
                if (passFail != null)
                    PassFails.Add(passFail);
            }
        }
    }

    private string GetParameter(string key, List<ReportData> report, bool equal = false) => report.Find((e) => equal ? e.ParameterName.Equals(key) : e.ParameterName.StartsWith(key))?.ParameterValue;
    private List<string> GetParameters(string key, List<ReportData> report) => report.FindAll((e) => e.ParameterName.StartsWith(key)).Select((e) => e.ParameterValue).ToList();

    private string[] GetKeyValuePair(string key, List<string> report)
    {
        string item = report.Find((e) => e.StartsWith(key));

        //if it was not found or the item does not contain a comma.
        return item?.Contains(',') != true ? null : [item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]];
    }
    private List<string[]> GetMultipleKeyValuePairs(string key, List<string> report)
    {
        List<string> items = report.FindAll((e) => e.StartsWith(key));

        if (items == null || items.Count == 0)
            return null;

        List<string[]> res = [];
        foreach (string item in items)
        {
            if (!item.Contains(','))
                continue;

            res.Add([item[..item.IndexOf(',')], item[(item.IndexOf(',') + 1)..]]);
        }
        return res;
    }

    private string[] GetValues(string name, List<string> splitPacket)
    {
        List<string> warn = splitPacket.FindAll((e) => e.StartsWith(name));

        List<string> ret = [];
        foreach (string line in warn)
        {
            //string[] spl1 = new string[2];
            //spl1[0] = line.Substring(0, line.IndexOf(','));
            ret.Add(line[(line.IndexOf(',') + 1)..]);
        }
        return ret.ToArray();
    }



    private BarcodeVerification.lib.ISO.GradeValue GetGradeValue(AvailableParameters parameter, string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        string[] spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Length != 2)
            return spl2.Length == 1 ? new BarcodeVerification.lib.ISO.GradeValue(parameter, spl2[0], string.Empty, AvailableDevices.L95) : null;
        else
            return new BarcodeVerification.lib.ISO.GradeValue(parameter, spl2[0], spl2[1], AvailableDevices.L95);//  new GradeValue(name, ParseFloat(spl2[1]), new Grade(name, tmp, GetLetter(tmp)));
    }

    private BarcodeVerification.lib.ISO.Grade GetGrade(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new BarcodeVerification.lib.ISO.Grade(parameter, data);

    private BarcodeVerification.lib.ISO.ValueDouble GetValueDouble(AvailableParameters parameter, string data ) => string.IsNullOrWhiteSpace(data) ? null : new BarcodeVerification.lib.ISO.ValueDouble(parameter, data, AvailableDevices.L95);

    private BarcodeVerification.lib.ISO.ValueString GetValueString(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new BarcodeVerification.lib.ISO.ValueString(parameter, data);

    private BarcodeVerification.lib.ISO.PassFail GetPassFail(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new BarcodeVerification.lib.ISO.PassFail(parameter, data);

}
