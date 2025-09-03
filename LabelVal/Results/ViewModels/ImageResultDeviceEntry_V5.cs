using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Classes;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using LabelVal.Sectors.Interfaces;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultDeviceEntry_V5 : ObservableObject, IImageResultDeviceEntry
{
    public ImageResultEntry ImageResultEntry { get; }
    public ImageResultsManager ImageResultsManager => ImageResultEntry.ImageResultsManager;

    public ImageResultEntryDevices Device { get; } = ImageResultEntryDevices.V5;
    public string Version => throw new NotImplementedException();

    [ObservableProperty] private Databases.Result resultRow;
    partial void OnResultRowChanged(Result value) { StoredImage = value?.Stored; HandlerUpdate(); }
    public Result Result { get => ResultRow; set { ResultRow = value; HandlerUpdate(); } }

    [ObservableProperty] private ImageEntry storedImage;
    [ObservableProperty] private DrawingImage storedImageOverlay;

    [ObservableProperty] private ImageEntry currentImage;
    [ObservableProperty] private DrawingImage currentImageOverlay;

    public JObject CurrentTemplate { get; set; }
    public string SerializeTemplate => JsonConvert.SerializeObject(CurrentTemplate);

    public JObject CurrentReport { get; private set; }
    public string SerializeReport => JsonConvert.SerializeObject(CurrentReport);

    public ObservableCollection<Sectors.Interfaces.ISector> CurrentSectors { get; } = [];
    public ObservableCollection<Sectors.Interfaces.ISector> StoredSectors { get; } = [];
    public ObservableCollection<SectorDifferences> DiffSectors { get; } = [];

    [ObservableProperty] private Sectors.Interfaces.ISector currentSelectedSector = null;

    [ObservableProperty] private Sectors.Interfaces.ISector focusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector focusedCurrentSector = null;

    [ObservableProperty] private bool isWorking = false;
    partial void OnIsWorkingChanged(bool value)
    {
        ImageResultsManager.WorkingUpdate(Device, value);
        OnPropertyChanged(nameof(IsNotWorking));
    }
    public bool IsNotWorking => !IsWorking;
    private const int _isWorkingTimerInterval = 30000;
    private Timer _IsWorkingTimer = new(_isWorkingTimerInterval);

    [ObservableProperty] private bool isFaulted = false;
    partial void OnIsFaultedChanged(bool value)
    {
        ImageResultsManager.FaultedUpdate(Device, value);
        OnPropertyChanged(nameof(IsNotFaulted));
    }
    public bool IsNotFaulted => !IsFaulted;

    [ObservableProperty] private bool isSelected = false;

    public ImageResultDeviceEntry_V5(ImageResultEntry imageResultsEntry)
    {
        ImageResultEntry = imageResultsEntry;
        _IsWorkingTimer.AutoReset = false;
        _IsWorkingTimer.Elapsed += _IsWorkingTimer_Elapsed;
    }

    private void _IsWorkingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        Logger.Error($"Working timer elapsed for {Device}.");
        IsWorking = false;
        IsFaulted = true;
    }
    partial void OnIsSelectedChanging(bool value) { if (value) ImageResultEntry.ImageResultsManager.ResetSelected(Device); }

    public LabelHandlers Handler => ImageResultsManager?.SelectedV5?.Controller != null && ImageResultsManager.SelectedV5.Controller.IsConnected ? ImageResultsManager.SelectedV5.Controller.IsSimulator
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

    public void HandlerUpdate() => OnPropertyChanged(nameof(Handler));

    public void GetStored()
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

        List<Sectors.Interfaces.ISector> tempSectors = [];
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

    [RelayCommand]
    public async Task Store()
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

    [RelayCommand]
    public void Process()
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

            List<Sectors.Interfaces.ISector> tempSectors = [];
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

    [RelayCommand]
    public void ClearCurrent()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(ClearCurrent);
            return;
        }

        CurrentSectors.Clear();
        DiffSectors.Clear();

        CurrentTemplate = null;
        CurrentReport = null;
        CurrentImage = null;
        CurrentImageOverlay = null;
    }

    [RelayCommand]
    public async Task ClearStored()
    {
        if (await ImageResultEntry.OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            _ = ImageResultEntry.SelectedDatabase.Delete_Result(Device, ImageResultEntry.ImageRollUID, ImageResultEntry.SourceImageUID, ImageResultEntry.ImageRollUID);
            GetStored();
            GetSectorDiff();
        }
    }

    private void GetSectorDiff()
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
                        var dat = SectorDifferences.Compare(sec.SectorDetails, cSec.SectorDetails);
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
        //V275V5_REST_Lib.FullReport report;
        //if ((report = await ImageResults.SelectedV275Node.Controller.GetFullReport(repeat, true)) == null)
        //{
        //    Logger.Error("Unable to read the repeat report from the node.");
        //    ClearRead(ImageResultEntryDevices.V275);
        //    return false;
        //}

        //V275ProcessRepeat(new V275V5_REST_Lib.Repeat(0, null) { FullReport = report });
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
            //return await ImageResults.Selected.Controller.Learn();
        }

        if (await ImageResultEntry.ImageResultsManager.SelectedV5.Controller.CopySectorsSetConfig(null, ResultRow.Template) == V5_REST_Lib.Controllers.RestoreSectorsResults.Failure)
            return -1;

        //if (!await ImageResults.SelectedV275Node.Controller.DeleteSectors())
        //    return -1;

        //if (V275StoredSectors.Count == 0)
        //{
        //    return !await ImageResults.SelectedV275Node.Controller.DetectSectors() ? -1 : 2;
        //}

        //foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        //{
        //    if (!await ImageResults.SelectedV275Node.Controller.AddSector(sec.Template.Name, JsonConvert.SerializeObject(((V275.Sectors.SectorTemplate)sec.Template).Original)))
        //        return -1;

        //    if (sec.Template.BlemishMask.Layers != null)
        //    {

        //        foreach (V275V5_REST_Lib.Models.Job.Layer layer in sec.Template.BlemishMask.Layers)
        //        {
        //            if (!await ImageResults.SelectedV275Node.Controller.AddMask(sec.Template.Name, JsonConvert.SerializeObject(layer)))
        //            {
        //                if (layer.value != 0)
        //                    return -1;
        //            }
        //        }
        //    }
        //}

        return 1;
    }

    public void RefreshOverlays()
    {
        RefreshCurrentOverlay();
        RefreshStoredOverlay();
    }
    public void RefreshStoredOverlay() => StoredImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(StoredImage, StoredSectors);
    public void RefreshCurrentOverlay() => CurrentImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);

}
