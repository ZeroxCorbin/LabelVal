using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.L95.Sectors;

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
        if (sector is not L95.Sectors.Sector sec)
            return;

        Sector = sector;

        //Get thew symbology enum
        AvailableSymbologies theSymbology = Sector.Report.SymbolType;

        //Get the region type for the symbology
        AvailableRegionTypes theRegionType = theSymbology.GetSymbologyRegionType(Sector.Report.Device);

        //Get the parameters list based on the region type.
        List<AvailableParameters> theParamters = Params.ParameterGroups[theRegionType][Sector.Report.Device].ToList();

        if (sector.Report.Standard == AvailableStandards.DPM)
        {
            var dpmParameters = Params.ParameterGroups[AvailableRegionTypes.DPM][Sector.Report.Device];
            //Add but do not duplicate DPM parameters
            foreach (var dpmParameter in dpmParameters)
            {
                if (!theParamters.Contains(dpmParameter))
                {
                    theParamters.Add(dpmParameter);
                }
            }
        }

        //Sort the parameters by their name
        theParamters.Sort((x, y) => x.ToString().CompareTo(y.ToString()));

        JObject report = (JObject)Sector.Report.Original;
        JObject template = (JObject)Sector.Template.Original;

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
        var alarms = report.GetParameters<string>("Data[ParameterName:Warning].ParameterValue");
        if (alarms != null && alarms.Count > 0)
        {
            foreach (var alarm in alarms)
            {
                Alarms.Add(new Alarm(AvaailableAlarmCategories.Error, alarm.ToString()));
            }
        }
    }

    private void AddParameter(AvailableParameters parameter, AvailableSymbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report)
    {
        Type type = parameter.GetParameterDataType(Sector.Report.Device, theSymbology);

        if (type == typeof(GradeValue) || type == typeof(Grade))
        {
            IParameterValue gradeValue = GetGradeValueOrGrade(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = GetValueString(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (valueString != null)
            {
                target.Add(valueString); return;
            }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(Custom))
        {

        }

        target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)}' missing or parse issue.");
    }

    private IParameterValue GetGradeValueOrGrade(AvailableParameters parameter, string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        string[] spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        return spl2.Length == 2
            ? new GradeValue(parameter, Sector.Report.Device, Sector.Report.SymbolType, spl2[0], spl2[1])
            : spl2.Length == 1 ? new Grade(parameter, Sector.Report.Device, data) : (IParameterValue)null;
    }

    private ValueDouble GetValueDouble(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueDouble(parameter, Sector.Report.Device, Sector.Report.SymbolType, data);
    private ValueString GetValueString(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueString(parameter, Sector.Report.Device, data);
    private PassFail GetPassFail(AvailableParameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new PassFail(parameter, Sector.Report.Device, data);

}
