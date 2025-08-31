using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
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
        var report = Sector.Report.Original;
        var template = Sector.Template.Original;

        if (Sector.Report.Symbology == Symbologies.Unknown)
        {
            if (!report.GetParameter<bool>("read"))
            {
                Alarms.Add(new Alarm(AvaailableAlarmCategories.Error, "Read failed"));
            }

            IsSectorMissing = true;
            SectorMissingText = "Sector is missing";
            return;
        }

        //Get the parameters list based on the region type.
        Parameters[] parameters = Sector.Report.Symbology.GetParameters(Sector.Report.Device, Sector.Report.GradingStandard, Sector.Report.ApplicationStandard);

        foreach (Parameters parameter in parameters)
        {
            try
            {
                ParamterHandling.AddParameter(parameter, Sector.Report.Symbology, Parameters, report, template);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing parameter: {parameter}");
            }
        }

        if (sec.Report.GS1Results is null)
            return;

        if (!sec.Report.GS1Results.Valid.Value)
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
