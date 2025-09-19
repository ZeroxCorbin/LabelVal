using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageUtilities.lib.Wpf;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Main.ViewModels;
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

namespace LabelVal.Results.ViewModels;

/// <summary>
/// ViewModel for handling image results from a V275 device.
/// This class manages the state, data, and operations for a single image result entry associated with the V275 device.
/// </summary>
public partial class ResultsDeviceEntryV275 : ObservableObject, IResultsDeviceEntry, IDisposable
{
    /// <summary>
    /// Gets the parent image result entry.
    /// </summary>
    public ResultsEntry ResultsEntry { get; }
    /// <summary>
    /// Gets the manager for all image results.
    /// </summary>
    public ResultsManagerViewModel ResultsManagerView => ResultsEntry.ResultsManagerView;

    /// <summary>
    /// Gets the device type for this entry.
    /// </summary>
    public ResultsEntryDevices Device { get; } = ResultsEntryDevices.V275;

    /// <summary>
    /// Gets or sets the database result row associated with this entry.
    /// </summary>
    [ObservableProperty] private Databases.Result resultRow;
    partial void OnResultRowChanged(Result value) { StoredImage = value?.Stored; HandlerUpdate(); }

    /// <summary>
    /// Gets or sets the database result record for the stored image.
    /// </summary>
    public Result Result { get => ResultRow; set { ResultRow = value; HandlerUpdate(); } }

    /// <summary>
    /// Gets or sets the stored image entry.
    /// </summary>
    [ObservableProperty] private ImageEntry storedImage;

    /// <summary>
    /// Gets or sets the overlay for the stored image, typically showing sector boundaries.
    /// </summary>
    [ObservableProperty] private DrawingImage storedImageOverlay;

    /// <summary>
    /// Gets or sets the currently processed image entry.
    /// </summary>
    [ObservableProperty] private ImageEntry currentImage;

    /// <summary>
    /// Gets or sets the overlay for the current image, typically showing sector boundaries.
    /// </summary>
    [ObservableProperty] private DrawingImage currentImageOverlay;

    /// <summary>
    /// Gets or sets the JSON template for the current image processing job.
    /// </summary>
    public JObject CurrentTemplate { get; set; }

    /// <summary>
    /// Gets the serialized JSON string of the current template.
    /// </summary>
    public string SerializeTemplate => JsonConvert.SerializeObject(CurrentTemplate);

    /// <summary>
    /// Gets the JSON report from the current image processing.
    /// </summary>
    public JObject CurrentReport { get; private set; }

    /// <summary>
    /// Gets the serialized JSON string of the current report.
    /// </summary>
    public string SerializeReport => JsonConvert.SerializeObject(CurrentReport);

    /// <summary>
    /// Gets the collection of sectors from the currently processed image.
    /// </summary>
    public ObservableCollection<Sectors.Interfaces.ISector> CurrentSectors { get; } = [];

    /// <summary>
    /// Gets the collection of sectors from the stored image.
    /// </summary>
    public ObservableCollection<Sectors.Interfaces.ISector> StoredSectors { get; } = [];

    /// <summary>
    /// Gets the collection of differences between stored and current sectors.
    /// </summary>
    public ObservableCollection<SectorDifferences> DiffSectors { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected sector in the UI.
    /// </summary>
    [ObservableProperty] private Sectors.Interfaces.ISector currentSelectedSector = null;

    /// <summary>
    /// Gets or sets the stored sector that has focus.
    /// </summary>
    [ObservableProperty] private Sectors.Interfaces.ISector focusedStoredSector = null;

    /// <summary>
    /// Gets or sets the current sector that has focus.
    /// </summary>
    [ObservableProperty] private Sectors.Interfaces.ISector focusedCurrentSector = null;

    /// <summary>
    /// Gets or sets a value indicating whether a process is currently running.
    /// A timer will set this to false if the operation takes too long.
    /// </summary>
    [ObservableProperty] private bool isWorking = false;
    partial void OnIsWorkingChanged(bool value)
    {
        if (value)
            _IsWorkingTimer.Start();
        else
            _IsWorkingTimer.Stop();

        ResultsManagerView.WorkingUpdate(Device, value);
        OnPropertyChanged(nameof(IsNotWorking));
    }

    /// <summary>
    /// Gets a value indicating whether the device is not currently processing.
    /// </summary>
    public bool IsNotWorking => !IsWorking;
    private const int _isWorkingTimerInterval = 30000;
    private readonly Timer _IsWorkingTimer = new(_isWorkingTimerInterval);

    /// <summary>
    /// Gets or sets a value indicating whether the last operation resulted in a fault.
    /// </summary>
    [ObservableProperty] private bool isFaulted = false;
    partial void OnIsFaultedChanged(bool value)
    {
        ResultsManagerView.FaultedUpdate(Device, value);
        OnPropertyChanged(nameof(IsNotFaulted));
    }

    /// <summary>
    /// Gets a value indicating whether the device is not in a faulted state.
    /// </summary>
    public bool IsNotFaulted => !IsFaulted;

    /// <summary>
    /// Gets or sets a value indicating whether this device entry is selected in the UI.
    /// </summary>
    [ObservableProperty] private bool isSelected = false;
    partial void OnIsSelectedChanging(bool value) { if (value) ResultsEntry.ResultsManagerView.ResetSelected(Device); }

    /// <summary>
    /// Gets the appropriate label handler based on the current state and settings.
    /// </summary>
    public LabelHandlers Handler => ResultsManagerView?.SelectedV275Node?.Controller != null && ResultsManagerView.SelectedV275Node.Controller.IsLoggedIn_Control ? ResultsManagerView.SelectedV275Node.Controller.IsSimulator
            ? ResultsManagerView.ActiveImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.SimulatorRestore
                    : LabelHandlers.SimulatorDetect
                : LabelHandlers.SimulatorTrigger
            : ResultsManagerView.ActiveImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.CameraRestore
                    : LabelHandlers.CameraDetect
                : LabelHandlers.CameraTrigger
        : LabelHandlers.Offline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsDeviceEntryV275"/> class.
    /// </summary>
    /// <param name="imageResultsEntry">The parent image result entry.</param>
    public ResultsDeviceEntryV275(ResultsEntry imageResultsEntry)
    {
        ResultsEntry = imageResultsEntry;

        _IsWorkingTimer.AutoReset = false;
        _IsWorkingTimer.Elapsed += _IsWorkingTimer_Elapsed;
    }

    private void _IsWorkingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        Logger.Error($"Working timer elapsed for {Device}.");
        IsWorking = false;
        IsFaulted = true;
    }

    /// <summary>
    /// Notifies that the <see cref="Handler"/> property has changed.
    /// </summary>
    public void HandlerUpdate() => OnPropertyChanged(nameof(Handler));

    /// <summary>
    /// Represents the method that will handle an image processing event.
    /// </summary>
    /// <param name="imageResults">The image result entry being processed.</param>
    /// <param name="type">The type of processing requested.</param>
    public delegate void ProcessImageDelegate(ResultsEntry imageResults, string type);

    /// <summary>
    /// Occurs when an image needs to be processed.
    /// </summary>
    public event ProcessImageDelegate ProcessImage;

    /// <summary>
    /// Retrieves the stored result and sectors from the database.
    /// </summary>
    public void GetStored()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(GetStored);
            return;
        }

        if (ResultsEntry.SelectedResultsDatabase == null)
        {
            Logger.Error("No image results database selected.");
            return;
        }

        StoredSectors.Clear();

        try
        {
            var row = ResultsEntry.SelectedResultsDatabase.Select_Result(Device, ResultsEntry.ImageRollUID, ResultsEntry.SourceImageUID, ResultsEntry.ImageRollUID);

            if (row == null)
            {
                ResultRow = null;
                return;
            }

            List<Sectors.Interfaces.ISector> tempSectors = [];

            if (!string.IsNullOrEmpty(row.ReportString) && !string.IsNullOrEmpty(row.TemplateString))
            {
                foreach (var jSec in row.Template["sectors"])
                {
                    try
                    {
                        foreach (JObject rSec in row.Report.GetParameter<JArray>("inspectLabel.inspectSector"))
                        {

                            if (jSec["name"].ToString() == rSec["name"].ToString())
                            {

                                StoredSectors.Add(new V275.Sectors.Sector((JObject)jSec, rSec, [ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGradingStandard], ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedApplicationStandard, ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGS1Table, row.Template["jobVersion"].ToString()));

                                break;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error(ex);
                        Logger.Error($"Error while loading stored results from: {ResultsEntry.SelectedResultsDatabase.File.Name}");
                    }
                }
            }

            //if (tempSectors.Count > 0)
            //{
            //    tempSectors = ResultsEntry.SortList3(tempSectors);

            //    foreach (var sec in tempSectors)
            //        StoredSectors.Add(sec);
            //}

            ResultRow = row;
            RefreshStoredOverlay();

        }
        catch (System.Exception ex)
        {
            Logger.Error(ex);
            Logger.Error($"Error while loading stored results from: {ResultsEntry.SelectedResultsDatabase.File.Name}");
        }
    }

    /// <summary>
    /// Stores the current sectors and image as a new stored result in the database.
    /// </summary>
    [RelayCommand]
    public async Task Store()
    {
        if (CurrentSectors.Count == 0)
        {
            Logger.Error("No sectors to store.");
            return;
        }
        if (ResultsEntry.SelectedResultsDatabase == null)
        {
            Logger.Error("No image results database selected.");
            return;
        }

        if (StoredSectors.Count > 0)
            if (await ResultsEntry.OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;

        var res = new Databases.Result
        {
            Device = Device,
            ImageRollUID = ResultsEntry.ImageRollUID,
            SourceImageUID = ResultsEntry.SourceImageUID,
            RunUID = ResultsEntry.ImageRollUID,
            Template = CurrentTemplate,
            Report = CurrentReport,
            Stored = CurrentImage
        };

        if (ResultsEntry.SelectedResultsDatabase.InsertOrReplace_Result(res) == null)
            Logger.Error($"Error while storing results to: {ResultsEntry.SelectedResultsDatabase.File.Name}");

        GetStored();
        ClearCurrent();
    }

    /// <summary>
    /// Initiates the image processing sequence based on the current handler.
    /// </summary>
    [RelayCommand]
    public void Process()
    {
        IsWorking = true;
        IsFaulted = false;

        V275_REST_Lib.Controllers.Label lab = new(ProcessRepeat, Handler is LabelHandlers.SimulatorRestore or LabelHandlers.CameraRestore ? [.. ResultRow.Template["sectors"]] : null, Handler, ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGS1Table);

        double srcDpiX, srcDpiY;
        int fallback = ResultsEntry.ResultsManagerView.SelectedV275Node?.Controller?.Dpi
                       ?? ResultsEntry.ResultsManagerView.ActiveImageRoll?.TargetDPI
                       ?? 600;

        if (ResultsEntry.ResultsManagerView.ActiveImageRoll.ImageType == ImageRollImageTypes.Source
            || Handler is LabelHandlers.CameraTrigger or LabelHandlers.CameraRestore or LabelHandlers.CameraDetect
            || (ResultRow?.Stored == null && ResultsEntry.ResultsManagerView.ActiveImageRoll.ImageType == ImageRollImageTypes.Stored))
        {
            lab.Image = GlobalAppSettings.Instance.PreseveImageFormat
                ? ImageFormatHelpers.EnsureDpi(ResultsEntry.SourceImage.OriginalImage, fallback, fallback, out srcDpiX, out srcDpiY)
                : ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(ResultsEntry.SourceImage.OriginalImage, fallback, out srcDpiX, out srcDpiY);
        }
        else if (ResultsEntry.ResultsManagerView.ActiveImageRoll.ImageType == ImageRollImageTypes.Stored)
        {
            lab.Image = GlobalAppSettings.Instance.PreseveImageFormat
                ? ImageFormatHelpers.EnsureDpi(ResultRow.Stored.ImageBytes, fallback, fallback, out srcDpiX, out srcDpiY)
                : ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(ResultRow.Stored.ImageBytes, fallback, out srcDpiX, out srcDpiY);
        }

        _ = ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.IsSimulator
            ? ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.ProcessLabel_Simulator(lab)
            : ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.ProcessLabel_Printer(lab, ResultsEntry.PrintCount, ResultsEntry.SelectedPrinter.PrinterName);

    }
    private void ProcessRepeat(V275_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat.FullReport);
    /// <summary>
    /// Processes the full report received from the device.
    /// </summary>
    /// <param name="report">The full report to process.</param>
    public void ProcessFullReport(V275_REST_Lib.Controllers.FullReport report)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(report));
            return;
        }

        try
        {
            if (report == null)
            {
                Logger.Error("No report data received.");
                IsFaulted = true;
                return;
            }

            CurrentTemplate = report.Job;
            CurrentReport = report.Report;

            if(report.Image == null || report.Image.Length == 0)
            {
                Logger.Error("No image data received.");
                IsFaulted = true;
                return;
            }

            var img = GlobalAppSettings.Instance.PreseveImageFormat
                ? ImageFormatHelpers.EnsureDpi(report.Image, ResultsEntry.ResultsManagerView.ActiveImageRoll?.TargetDPI ?? 600,
                    ResultsEntry.ResultsManagerView.ActiveImageRoll?.TargetDPI ?? 600, out var dpiX, out var dpiY)
                : ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(report.Image,
                    ResultsEntry.ResultsManagerView.ActiveImageRoll?.TargetDPI ?? 600, out var dpiX2, out var dpiY2);
            CurrentImage = new ImageEntry(ResultsEntry.ImageRollUID, img);
            CurrentImage.EnsureDpi(ResultsEntry.ResultsManagerView.ActiveImageRoll?.TargetDPI ?? 600);

            CurrentSectors.Clear();

            //List<Sectors.Interfaces.ISector> tempSectors = [];
            foreach (var templateSec in CurrentTemplate["sectors"])
            {
                foreach (var currentSect in CurrentReport["inspectLabel"]["inspectSector"])
                {
                    try
                    {
                        if (templateSec["name"].ToString() == currentSect["name"].ToString())
                        {
                            CurrentSectors.Add(new V275.Sectors.Sector(
                                (JObject)templateSec, 
                                (JObject)currentSect, 
                                [ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGradingStandard], 
                                ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedApplicationStandard, 
                                ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGS1Table, 
                                report.Job["jobVersion"].ToString()));
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error(ex);

                    }
                }
            }

            //if (tempSectors.Count > 0)
            //{
            //    tempSectors = ResultsEntry.SortList3(tempSectors);

            //    foreach (var sec in tempSectors)
            //        CurrentSectors.Add(sec);
            //}

            GetSectorDiff();

            RefreshCurrentOverlay();

            IsFaulted = false;
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex);

            CurrentImage = null;
            CurrentTemplate = null;
            CurrentReport = null;
            CurrentSectors.Clear();
            CurrentImageOverlay = null;

            IsFaulted = true;
        }
        finally
        {
            IsWorking = false;
            Application.Current.Dispatcher.Invoke(ResultsEntry.BringIntoViewHandler);
        }
    }

    /// <summary>
    /// Clears all data related to the current processing result.
    /// </summary>
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

    /// <summary>
    /// Clears the stored sectors and result from the database after user confirmation.
    /// </summary>
    [RelayCommand]
    public async Task ClearStored()
    {
        if (await ResultsEntry.OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            _ = ResultsEntry.SelectedResultsDatabase.Delete_Result(Device, ResultsEntry.ImageRollUID, ResultsEntry.SourceImageUID, ResultsEntry.ImageRollUID);
            GetStored();
            GetSectorDiff();
        }
    }

    private void GetSectorDiff()
    {
        DiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (var sec in StoredSectors)
        {
            foreach (var cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Report.Symbology == cSec.Report.Symbology)
                    {
                        var res = sec.SectorDetails.Compare(cSec.SectorDetails);
                        if (res != null)
                            diff.Add(res);
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

        //ToDo: Sort the diff list
        foreach (var d in diff)
            if (d.IsSectorMissing)
                DiffSectors.Add(d);

    }

    /// <summary>
    /// Reads the full report from the connected device.
    /// </summary>
    /// <returns>A task that represents the asynchronous read operation, returning true if successful.</returns>
    [RelayCommand]
    private Task<bool> Read() => ReadTask(0);
    /// <summary>
    /// Asynchronously reads the full report from the device.
    /// </summary>
    /// <param name="repeat">The repeat count for the read operation.</param>
    /// <returns>A task that represents the asynchronous read operation, returning true if successful.</returns>
    public async Task<bool> ReadTask(int repeat)
    {
        try
        {
            IsWorking = true;
            IsFaulted = false;

            V275_REST_Lib.Controllers.FullReport report;
            if ((report = await ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.GetFullReport(repeat, true)) == null)
            {
                Logger.Error("Unable to read the repeat report from the node.");
                ClearCurrent();
                return false;
            }

            ProcessFullReport(report);
        }
        finally
        {
            IsWorking = false;
            Application.Current.Dispatcher.Invoke(ResultsEntry.BringIntoViewHandler);
        }
        return true;
    }

    /// <summary>
    /// Loads the stored sectors to the connected device.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation, returning an integer status code.</returns>
    [RelayCommand]
    private Task<int> Load() => LoadTask();
    /// <summary>
    /// Asynchronously loads sectors to the device. If stored sectors exist, they are loaded; otherwise, sector detection is triggered.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an integer:
    /// 1 for successful load, 2 for successful detection, -1 for failure.
    /// </returns>
    public async Task<int> LoadTask()
    {
        if (!await ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.DeleteSectors())
            return -1;

        if (StoredSectors.Count == 0)
        {
            return !await ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.DetectSectors() ? -1 : 2;
        }

        foreach (var sec in StoredSectors)
        {
            if (!await ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.AddSector(sec.Template.Name, JsonConvert.SerializeObject(((V275.Sectors.SectorTemplate)sec.Template).Original)))
                return -1;

            if (sec.Template?.BlemishMask?.Layers != null)
            {

                foreach (var layer in sec.Template.BlemishMask.Layers)
                {
                    if (!await ResultsEntry.ResultsManagerView.SelectedV275Node.Controller.AddMask(sec.Template.Name, JsonConvert.SerializeObject(layer)))
                    {
                        if (layer.value != 0)
                            return -1;
                    }
                }
            }
        }

        return 1;
    }

    /// <summary>
    /// Refreshes both the current and stored image overlays.
    /// </summary>
    public void RefreshOverlays()
    {
        RefreshCurrentOverlay();
        RefreshStoredOverlay();
    }
    /// <summary>
    /// Refreshes the overlay for the current image to display sector boundaries.
    /// </summary>
    public void RefreshCurrentOverlay() => CurrentImageOverlay = IResultsDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);
    /// <summary>
    /// Refreshes the overlay for the stored image to display sector boundaries.
    /// </summary>
    public void RefreshStoredOverlay() => StoredImageOverlay = IResultsDeviceEntry.CreateSectorsImageOverlay(StoredImage, StoredSectors);

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ResultsDeviceEntryV275"/> and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ResultsDeviceEntryV275"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _IsWorkingTimer.Elapsed -= _IsWorkingTimer_Elapsed;
            _IsWorkingTimer.Dispose();
        }
    }
}