using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using Newtonsoft.Json.Linq;
using SharpDX.Direct2D1;
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
    public ObservableCollection<IParameterValue> GradingParameters { get; } = [];
    public ObservableCollection<IParameterValue> ApplicationParameters { get; } = [];
    public ObservableCollection<IParameterValue> SymbologyParameters { get; } = [];


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

        //Get the parameters list based on the region type.
        var parameters = Sector.Report.Symbology.GetParameters(Sector.Report.Device, Sector.Report.GradingStandard, Sector.Report.ApplicationStandard).ToList();


        var symPars = Sector.Report.Symbology.GetParameters(Sector.Report.Device);
        var gradingPars = Sector.Report.GradingStandard.GetParameters();
        var applicationPars = Sector.Report.ApplicationStandard.GetParameters();

        //Add the symbology parameters
        var tempSymPars = new List<IParameterValue>();
        foreach (Parameters parameter in symPars)
        {
            try
            {
                AddParameter(parameter, Sector.Report.Symbology, tempSymPars, Sector.Report.Original);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing symbology parameter: {parameter}");
            }
        }
        tempSymPars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));
        foreach (IParameterValue p in tempSymPars)
            SymbologyParameters.Add(p);

        //Add the grading parameters
        var tempGradingPars = new List<IParameterValue>();
        foreach (Parameters parameter in gradingPars)
        {
            try
            {
                AddParameter(parameter, Sector.Report.Symbology, tempGradingPars, Sector.Report.Original);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing grading parameter: {parameter}");
            }
        }
        tempGradingPars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));
        foreach (IParameterValue p in tempGradingPars)
            GradingParameters.Add(p);

        //Add the application parameters
        var tempApplicationPars = new List<IParameterValue>();
        foreach (Parameters parameter in applicationPars)
        {
            try
            {
                AddParameter(parameter, Sector.Report.Symbology, tempApplicationPars, Sector.Report.Original);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing application parameter: {parameter}");
            }
        }
        tempApplicationPars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));
        foreach (IParameterValue p in tempApplicationPars)
            ApplicationParameters.Add(p);

        JObject report = (JObject)Sector.Report.Original;
        JObject template = (JObject)Sector.Template.Original;
                var pars = new List<IParameterValue>();
        //Interate through the parameters
        foreach (Parameters parameter in parameters)
        {
            try
            {

                AddParameter(parameter, Sector.Report.Symbology, pars, report);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing parameter: {parameter}");
            }
        }
                pars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));

                foreach (IParameterValue p in pars)
                    Parameters.Add(p);
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

    private void AddParameter(Parameters parameter, Symbologies theSymbology, List<IParameterValue> target, JObject report)
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

    private OverallGrade GetOverallGrade(string original)
    {
        string data = original.Replace("DPM", "");
        string[] spl = data.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(BarcodeVerification.lib.Common.Parameters.OverallGrade, Sector.Device, spl[0]);
        return new OverallGrade(Sector.Device, grade, original, spl[1], spl[2]);
    }
}
