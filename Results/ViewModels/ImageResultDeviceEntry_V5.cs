using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultDeviceEntry_V5 : ImageResultDeviceEntryBase
{
    public override ImageResultEntryDevices Device { get; } = ImageResultEntryDevices.V5;

    public override LabelHandlers Handler => ImageResultsManager?.SelectedV5?.Controller != null && ImageResultsManager.SelectedV5.Controller.IsConnected ? ImageResultsManager.SelectedV5.Controller.IsSimulator
            ? ImageResultsManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.SimulatorRestore
                    : LabelHandlers.SimulatorDetect
                : LabelHandlers.SimulatorTrigger
            : ImageResultsManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.CameraRestore
                    : LabelHandlers.CameraDetect
                : LabelHandlers.CameraTrigger
        : LabelHandlers.Offline;

    public ImageResultDeviceEntry_V5(ImageResultEntry imageResultsEntry) : base(imageResultsEntry)
    {
    }

    public override void GetStored()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(GetStored);
            return;
        }

        StoredSectors.Clear();

        var row = ImageResultEntry.SelectedDatabase.Select_Result(Device, ImageResultEntry.ImageRollUID, ImageResultEntry.SourceImageUID, ImageResultEntry.ImageRollUID);

        if (row == null)
        {
            ResultRow = null;
            return;
        }

        if (row.Report == null || row.Template == null)
        {
            Logger.Debug(" result is missing data.");
            return;
        }

        List<ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(row.ReportString))
        {
            foreach (var toolResult in row.Report.GetParameter<JArray>("event.data.toolResults"))
            {

                try
                {
                    tempSectors.AddRange(((JObject)toolResult).GetParameter<JArray>("results").Select(result => new V5.Sectors.Sector((JObject)result, row.Template, [ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGradingStandard], ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, row.Template.GetParameter<string>("response.message"))).Cast<ISector>());
                }
                catch (System.Exception ex)
                {
                    Logger.Error(ex, ex.StackTrace);
                    Logger.Warning($"Error while loading stored results from: {ImageResultEntry.SelectedDatabase.File.Name}");
                    continue;
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            _ = ImageResultEntry.SortList3(tempSectors);

            foreach (var sec in tempSectors)
                StoredSectors.Add(sec);
        }

        ResultRow = row;
        RefreshStoredOverlay();

    }

    public override async Task Store()
    {
        if (CurrentSectors.Count == 0)
        {
            Logger.Error("No sectors to store.");
            return;
        }
        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.Error("No image results database selected.");
            return;
        }

        if (StoredSectors.Count > 0)
            if (await ImageResultEntry.OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;

        var res = new Databases.Result
        {
            Device = Device,
            ImageRollUID = ImageResultEntry.ImageRollUID,
            SourceImageUID = ImageResultEntry.SourceImageUID,
            RunUID = ImageResultEntry.ImageRollUID,
            Template = CurrentTemplate,
            Report = CurrentReport,
            Stored = CurrentImage
        };

        if (ImageResultEntry.SelectedDatabase.InsertOrReplace_Result(res) == null)
            Logger.Error($"Error while storing results to: {ImageResultEntry.SelectedDatabase.File.Name}");

        GetStored();
        ClearCurrent();
    }

    public override void Process()
    {

        V5_REST_Lib.Controllers.Label lab = new(ProcessRepeat, Handler is LabelHandlers.SimulatorRestore or LabelHandlers.CameraRestore ? ResultRow.Template : null, Handler, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table);

        if (Handler is LabelHandlers.SimulatorRestore or LabelHandlers.SimulatorDetect or LabelHandlers.SimulatorTrigger)
        {
            if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Source || (ResultRow?.SourceImage == null && ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored))
                lab.Image = ImageResultEntry.SourceImage.BitmapBytes;
            else if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
                lab.Image = ResultRow.Stored.ImageBytes;
        }

        _ = ImageResultEntry.ImageResultsManager.SelectedV5.Controller.ProcessLabel(lab);

        IsWorking = true;
        IsFaulted = false;
    }

    private void ProcessRepeat(V5_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat?.FullReport);
    public void ProcessFullReport(V5_REST_Lib.Controllers.FullReport report)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(report));
            return;
        }

        try
        {

            if (report == null || report.Image == null)
            {
                Logger.Error("Can not proces null results.");
                IsFaulted = true;
                return;
            }

            if (!ImageResultEntry.ImageResultsManager.SelectedV5.Controller.IsSimulator)
            {
                CurrentImage = new ImageEntry(ImageResultEntry.ImageRollUID, report.Image, 600);
            }
            else
            {
                using var img = new ImageMagick.MagickImage(report.Image);
                CurrentImage = new ImageEntry(ImageResultEntry.ImageRollUID, report.Image, (int)Math.Round(ImageResultEntry.SourceImage.Image.DpiX));
            }

            CurrentTemplate = ImageResultEntry.ImageResultsManager.SelectedV5.Controller.Config;
            CurrentReport = report.Report;

            CurrentSectors.Clear();

            List<ISector> tempSectors = [];
            //Tray and match a toolResult to a toolList
            foreach (var toolResult in CurrentReport.GetParameter<JArray>("event.data.toolResults"))
            {

                foreach (var result in ((JObject)toolResult).GetParameter<JArray>("results"))
                {
                    try
                    {
                        tempSectors.Add(new V5.Sectors.Sector((JObject)result, CurrentTemplate, [ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGradingStandard], ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, CurrentTemplate.GetParameter<string>("response.message")));
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error(ex, ex.StackTrace);
                        Logger.Warning("Error while processing results.");
                        continue;
                    }
                }
            }

            if (tempSectors.Count > 0)
            {
                tempSectors = ImageResultEntry.SortList3(tempSectors);

                foreach (var sec in tempSectors)
                    CurrentSectors.Add(sec);
            }

            GetSectorDiff();

            RefreshCurrentOverlay();

            IsFaulted = false;
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex, ex.StackTrace);
            Logger.Warning("Error while processing results.");
            IsFaulted = true;
        }
        finally
        {
            IsWorking = false;
            Application.Current.Dispatcher.Invoke(ImageResultEntry.BringIntoViewHandler);
        }
    }

    protected override void GetSectorDiff()
    {
        DiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing here. To keep found at top of list.
        foreach (var sec in StoredSectors)
        {
            foreach (var cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Report.Symbology == cSec.Report.Symbology)
                    {
                        var dat = sec.SectorDetails.Compare(cSec.SectorDetails);
                        if (dat != null)
                            diff.Add(dat);
                    }
                    else
                    {
                        SectorDifferences dat = new()
                        {
                            Username = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Report.Symbology.GetDescription()}  : Current Sector  {cSec.Report.Symbology.GetDescription()}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (var sec in StoredSectors)
        {
            var found = false;
            foreach (var cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    found = true;
                    continue;
                }

            if (!found)
            {
                SectorDifferences dat = new()
                {
                    Username = $"{sec.Template.Username} (MISSING)",
                    IsSectorMissing = true,
                    SectorMissingText = "Not found in current Sectors"
                };
                diff.Add(dat);
            }
        }

        //check for missing
        if (StoredSectors.Count > 0)
            foreach (var sec in CurrentSectors)
            {
                var found = false;
                foreach (var cSec in StoredSectors)
                    if (sec.Template.Name == cSec.Template.Name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    SectorDifferences dat = new()
                    {
                        Username = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (var d in diff)
            if (d.IsSectorMissing)
                DiffSectors.Add(d);
    }

    [RelayCommand]
    private Task<bool> Read() => ReadTask();
    public async Task<bool> ReadTask()
    {
        var result = await ImageResultEntry.ImageResultsManager.SelectedV5.Controller.Trigger_Wait_Return(true);
        ProcessFullReport(result);
        return true;
    }

    [RelayCommand]
    private Task<int> Load() => LoadTask();
    public async Task<int> LoadTask()
    {
        if (ResultRow == null)
        {
            Logger.Error("No  result row selected.");
            return -1;
        }

        if (StoredSectors.Count == 0)
        {
            return 0;
        }

        if (await ImageResultEntry.ImageResultsManager.SelectedV5.Controller.CopySectorsSetConfig(null, ResultRow.Template) == V5_REST_Lib.Controllers.RestoreSectorsResults.Failure)
            return -1;

        return 1;
    }
}
