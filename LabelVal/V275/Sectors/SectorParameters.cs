using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
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
        if (sector is not V275.Sectors.Sector)
            return;

        Sector = sector;

        if (Sector.Report.Symbology == Symbologies.Unknown)
        {
            IsSectorMissing = true;
            SectorMissingText = "Sector is missing";
            return;
        }

        //Get the parameters list based on the region type.
        var parameters = Sector.Report.Symbology.GetParameters(Sector.Report.Device, Sector.Report.GradingStandard, Sector.Report.ApplicationStandard);

        JObject report = (JObject)Sector.Report.Original;
        JObject template = (JObject)Sector.Template.Original;

        foreach (Parameters parameter in parameters)
        {
            try
            {
                AddParameter(parameter, Sector.Report.Symbology, Parameters, report, template);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing parameter: {parameter}");
            }
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

    private void AddParameter(Parameters parameter, Symbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report, JObject template)
    {
        Type type = parameter.GetDataType(Sector.Report.Device, theSymbology);

        if (type == typeof(GradeValue) || type == typeof(Grade))
        {
            IParameterValue gradeValue = GetGradeValueOrGrade(parameter, report.GetParameter<JObject>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));

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
            ValueString valueString = parameter is BarcodeVerification.lib.Common.Parameters.GS1Table
                ? GetValueString(parameter, template.GetParameter<string>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)))
                : GetValueString(parameter, report.GetParameter<string>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));
            if (valueString != null) { target.Add(valueString); return; }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(ValuePassFail))
        {
            ValuePassFail valuePassFail = GetValuePassFail(parameter, report.GetParameter<JObject>(parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)));
            if (valuePassFail != null) { target.Add(valuePassFail); return; }
        }
        else if (type == typeof(Custom))
        {

            //if (parameter is AvailableParameters.UnusedEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Sector.Report.Device, Sector.Report.Symbology, report.GetParameter<double>("Datamatrix.uec"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}

            //if (parameter is AvailableParameters.MinimumEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Sector.Report.Device, Sector.Report.Symbology, report.GetParameter<double>("Datamatrix.ecc"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}
        }

        target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(Sector.Report.Device, Sector.Report.Symbology)}' missing or parse issue.");
    }

    private IParameterValue GetGradeValueOrGrade(Parameters parameter, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;
        string value = gradeValue["value"].ToString();
        Grade grade = GetGrade(parameter, (JObject)gradeValue["grade"]);
        return grade == null
            ? new Grade(parameter, Sector.Report.Device, value)
            : new GradeValue(parameter, Sector.Report.Device, Sector.Report.Symbology, grade, value);
    }
    private Grade GetGrade(Parameters parameter, JObject gradeValue) => gradeValue is null
            ? null
            : new Grade(parameter, Sector.Report.Device, gradeValue["value"].ToString());

    private ValueDouble GetValueDouble(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Sector.Report.Device, Sector.Report.Symbology, value);

    private ValueString GetValueString(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Sector.Report.Device, value);

    private PassFail GetPassFail(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Sector.Report.Device, value);

    public ValuePassFail GetValuePassFail(Parameters parameter, JObject valuePassFail) => valuePassFail is null
            ? null
            : new ValuePassFail(parameter, Sector.Report.Device, Sector.Report.Symbology, valuePassFail["value"].ToString(), valuePassFail["result"].ToString());
}
