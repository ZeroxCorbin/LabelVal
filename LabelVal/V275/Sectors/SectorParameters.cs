using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.V275.Sectors;

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

    public SectorParameters() { }
    public SectorParameters(ISector sector) => Process(sector);
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

        List<Parameters> parameters = Sector.Report.Symbology.GetParameters(Sector.Report.Device, Sector.Report.GradingStandard, Sector.Report.ApplicationStandard).ToList();

        Parameters[] symPars = Sector.Report.Symbology.GetParameters(Sector.Report.Device);
        Parameters[] gradingPars = Sector.Report.GradingStandard.GetParameters(Sector.Report.Specification);
        Parameters[] applicationPars = Sector.Report.ApplicationStandard.GetParameters();

        //Add the symbology parameters
        var tempSymPars = new List<IParameterValue>();
        foreach (Parameters parameter in symPars)
        {
            try
            {
                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, tempSymPars, Sector.Report.Original, Sector.Template.Original);
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
                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, tempGradingPars, Sector.Report.Original, Sector.Template.Original);
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
                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, tempApplicationPars, Sector.Report.Original, Sector.Template.Original);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing application parameter: {parameter}");
            }
        }
        tempApplicationPars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));
        foreach (IParameterValue p in tempApplicationPars)
            ApplicationParameters.Add(p);

        JObject report = Sector.Report.Original;
        JObject template = Sector.Template.Original;
        var pars = new List<IParameterValue>();
        //Interate through the parameters
        foreach (Parameters parameter in parameters)
        {
            try
            {

                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, pars, report, template);
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
        if (report["data"]?["alarms"] != null)
        {
            foreach (JObject alarm in report["data"]?["alarms"])
            {
                Alarms.Add(new Alarm(alarm["category"].Value<int>() == 1 ? AvaailableAlarmCategories.Warning : AvaailableAlarmCategories.Error, alarm["name"].ToString()));
            }
        }
    }
}
