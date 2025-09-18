using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.V5.Sectors;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using System.Collections.ObjectModel;

namespace LabelVal.V5.Sectors;

public partial class SectorDetails : ObservableObject, ISectorParameters
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

    public SectorDifferences Compare(ISectorParameters compare) => SectorDifferences.Compare(this, compare);

    public SectorDetails() { }

    public SectorDetails(ISector sector) => Process(sector);
    public void Process(ISector sector)
    {
        if (sector is not V5.Sectors.Sector sec)
            return;

        Sector = sector;

        if (Sector.Report.Symbology == Symbologies.Unknown)
        {
            IsSectorMissing = true;
            SectorMissingText = "Sector is missing";
            return;
        }

        var parameters = Sector.Report.Symbology.GetParameters(Sector.Report.Device, Sector.Report.GradingStandard, Sector.Report.ApplicationStandard).ToList();

        var symPars = Sector.Report.Symbology.GetParameters(Sector.Report.Device);
        var gradingPars = Sector.Report.GradingStandard.GetParameters(Sector.Report.Specification);
        var applicationPars = Sector.Report.ApplicationStandard.GetParameters();

        //Add the symbology parameters
        var tempSymPars = new List<IParameterValue>();
        foreach (var parameter in symPars)
        {
            try
            {
                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, tempSymPars, Sector.Report.Original, Sector.Template.Original);
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex, $"Error processing symbology parameter: {parameter}");
            }
        }
        tempSymPars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));
        foreach (var p in tempSymPars)
            SymbologyParameters.Add(p);

        //Add the grading parameters
        var tempGradingPars = new List<IParameterValue>();
        foreach (var parameter in gradingPars)
        {
            try
            {
                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, tempGradingPars, Sector.Report.Original, Sector.Template.Original);
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex, $"Error processing grading parameter: {parameter}");
            }
        }
        tempGradingPars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));
        foreach (var p in tempGradingPars)
            GradingParameters.Add(p);

        //Add the application parameters
        var tempApplicationPars = new List<IParameterValue>();
        foreach (var parameter in applicationPars)
        {
            try
            {
                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, tempApplicationPars, Sector.Report.Original, Sector.Template.Original);
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex, $"Error processing application parameter: {parameter}");
            }
        }
        tempApplicationPars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));
        foreach (var p in tempApplicationPars)
            ApplicationParameters.Add(p);

        var report = Sector.Report.Original;
        var template = Sector.Template.Original;
        var pars = new List<IParameterValue>();
        //Interate through the parameters
        foreach (var parameter in parameters)
        {
            try
            {

                ParameterHandling.AddParameter(parameter, Sector.Report.Symbology, pars, report, template);
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex, $"Error processing parameter: {parameter}");
            }
        }
        pars.Sort((x, y) => x.Parameter.ToString().CompareTo(y.Parameter.ToString()));

        foreach (var p in pars)
            Parameters.Add(p);

        if (sec.Report.GS1Results != null && !sec.Report.GS1Results.PassFail.Value)
        {
            Alarms.Add(new Alarm(AvaailableAlarmCategories.Error, sec.Report.GS1Results.FormattedOut));
        }

        //if (SymbolType == "verify2D")
        //{
        //    if (results.Datamatrix != null)
        //    {
        //        Values.Add(new Value_("rows", results.Datamatrix.rows));
        //        Values.Add(new Value_("columns", results.Datamatrix.columns));
        //        Values.Add(new Value_("uec", results.Datamatrix.uec));
        //        Values.Add(new Value_("ecc", results.Datamatrix.ecc));
        //        Values.Add(new Value_("mirror", results.Datamatrix.mirror ? 1 : 0));
        //        Values.Add(new Value_("readerConfig", results.Datamatrix.readerConfig ? 1 : 0));
        //    }
        //    else if (results.QR != null)
        //    {
        //        Values.Add(new Value_("rows", results.QR.rows));
        //        Values.Add(new Value_("columns", results.QR.columns));
        //        Values.Add(new Value_("uec", results.QR.uec));
        //        Values.Add(new Value_("mirror", results.QR.mirror ? 1 : 0));
        //        Values.Add(new Value_("model", results.QR.model));
        //        Values.Add(new Value_("locatorCount", results.QR.locator.Count()));
        //    }
        //}
        //else if (SymbolType == "verify1D")
        //{
        //    if (results.Code128 != null)
        //        Values.Add(new Value_("barCount", results.Code128.barCount));
        //    else if (results.PDF417 != null)
        //    {
        //        Values.Add(new Value_("rows", results.PDF417.rows));
        //        Values.Add(new Value_("columns", results.PDF417.columns));
        //        Values.Add(new Value_("ecc", results.PDF417.ecc));
        //    }
        //    else if (results.UPC != null)
        //    {
        //        Values.Add(new Value_("barCount", results.UPC.barCount));
        //        Values.Add(new Value_("supplemental", results.UPC.supplemental));
        //    }
        //}
    }
}
