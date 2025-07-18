using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
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
        Symbologies theSymbology = Sector.Report.Symbology;

        //Get the parameters list based on the region type.
        List<Parameters> theParamters = [.. BarcodeVerification.lib.Common.Parameters.DeviceParameters[(theRegionType, Sector.Report.Device)]];

        if (sector.Report.Standard == AvailableStandards.DPM)
        {
            List<Parameters> dpmParameters = [.. BarcodeVerification.lib.Common.Parameters.DeviceParameters[(AvailableRegionTypes.DPM, Sector.Report.Device)]];
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
        foreach (Parameters parameter in theParamters)
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

    private void AddParameter(Parameters parameter, Symbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report)
    {
        Type type = parameter.GetDataType(Sector.Report.Device, theSymbology);

        if (type == typeof(GradeValue) || type == typeof(Grade))
        {
            IParameterValue gradeValue = GetGradeValueOrGrade(parameter, report.GetParameter<string>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, report.GetParameter<string>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = GetValueString(parameter, report.GetParameter<string>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));
            if (valueString != null)
            {
                target.Add(valueString); return;
            }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(Custom))
        {

        }

        target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)}' missing or parse issue.");
    }

    private IParameterValue GetGradeValueOrGrade(Parameters parameter, string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return null;

        string[] spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        return spl2.Length == 2
            ? new GradeValue(parameter, Sector.Report.Device, Sector.Report.Symbology, spl2[0], spl2[1])
            : spl2.Length == 1 ? new Grade(parameter, Sector.Report.Device, data) : (IParameterValue)null;
    }

    private ValueDouble GetValueDouble(Parameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueDouble(parameter, Sector.Report.Device, Sector.Report.Symbology, data);
    private ValueString GetValueString(Parameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueString(parameter, Sector.Report.Device, data);
    private PassFail GetPassFail(Parameters parameter, string data) => string.IsNullOrWhiteSpace(data) ? null : new PassFail(parameter, Sector.Report.Device, data);

}
