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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using LabelVal.Sectors.Interfaces;
using LabelVal.Main.ViewModels;
using LabelVal.Results.Helpers;
using LabelVal.Utilities;
using ImageUtilities.lib.Wpf;

namespace LabelVal.Results.ViewModels;

/// <summary>
/// Represents a device-specific entry for V5 devices in the image results view.
/// This class manages the state, data, and operations for images and sectors from a V5 device.
/// </summary>
public partial class ResultsDeviceEntryV5 : ObservableObject, IResultsDeviceEntry, IDisposable
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
    public ResultsEntryDevices Device { get; } = ResultsEntryDevices.V5;

    /// <summary>
    /// Gets or sets the database result row associated with this entry.
    /// </summary>
    [ObservableProperty] private Databases.Result resultRow;
    partial void OnResultRowChanged(Result value) { StoredImage = value?.Stored; HandlerUpdate(); }

    /// <summary>
    /// Gets or sets the result data, which is an alias for ResultRow.
    /// </summary>
    public Result Result { get => ResultRow; set { ResultRow = value; HandlerUpdate(); } }

    /// <summary>
    /// Gets or sets the stored image entry.
    /// </summary>
    [ObservableProperty] private ImageEntry storedImage;

    /// <summary>
    /// Gets or sets the overlay for the stored image, displaying sector boundaries.
    /// </summary>
    [ObservableProperty] private DrawingImage storedImageOverlay;

    /// <summary>
    /// Gets or sets the current (newly processed) image entry.
    /// </summary>
    [ObservableProperty] private ImageEntry currentImage;

    /// <summary>
    /// Gets or sets the overlay for the current image, displaying sector boundaries.
    /// </summary>
    [ObservableProperty] private DrawingImage currentImageOverlay;

    /// <summary>
    /// Gets or sets the JSON template from the current processing result.
    /// </summary>
    public JObject CurrentTemplate { get; set; }

    /// <summary>
    /// Gets the serialized JSON string of the current template.
    /// </summary>
    public string SerializeTemplate => JsonConvert.SerializeObject(CurrentTemplate);

    /// <summary>
    /// Gets the JSON report from the current processing result.
    /// </summary>
    public JObject CurrentReport { get; private set; }

    /// <summary>
    /// Gets the serialized JSON string of the current report.
    /// </summary>
    public string SerializeReport => JsonConvert.SerializeObject(CurrentReport);

    /// <summary>
    /// Gets the collection of sectors from the current processing result.
    /// </summary>
    public ObservableCollection<ISector> CurrentSectors { get; } = [];

    /// <summary>
    /// Gets the collection of sectors from the stored result.
    /// </summary>
    public ObservableCollection<ISector> StoredSectors { get; } = [];

    /// <summary>
    /// Gets the collection of differences between stored and current sectors.
    /// </summary>
    public ObservableCollection<SectorDifferences> DiffSectors { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected sector in the UI.
    /// </summary>
    [ObservableProperty] private ISector currentSelectedSector = null;

    /// <summary>
    /// Gets or sets the stored sector that has focus.
    /// </summary>
    [ObservableProperty] private ISector focusedStoredSector = null;

    /// <summary>
    /// Gets or sets the current sector that has focus.
    /// </summary>
    [ObservableProperty] private ISector focusedCurrentSector = null;

    /// <summary>
    /// Gets or sets a value indicating whether a process is currently running.
    /// A timer will set this to false if the operation takes too long.
    /// </summary>
    [ObservableProperty] private bool isWorking = false;
    partial void OnIsWorkingChanged(bool value)
    {
        if (value) _IsWorkingTimer.Start();
        else _IsWorkingTimer.Stop();
        ResultsManagerView.WorkingUpdate(Device, value);
        OnPropertyChanged(nameof(IsNotWorking));
    }

    /// <summary>
    /// Gets a value indicating whether a process is not currently running.
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
    /// Gets a value indicating whether the last operation did not result in a fault.
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
    public LabelHandlers Handler => ResultsManagerView?.SelectedV5?.Controller != null && ResultsManagerView.SelectedV5.Controller.IsConnected
        ? ResultsManagerView.SelectedV5.Controller.IsSimulator
            ? ResultsManagerView.ActiveImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString) ? LabelHandlers.SimulatorRestore : LabelHandlers.SimulatorDetect
                : LabelHandlers.SimulatorTrigger
            : ResultsManagerView.ActiveImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString) ? LabelHandlers.CameraRestore : LabelHandlers.CameraDetect
                : LabelHandlers.CameraTrigger
        : LabelHandlers.Offline;

    public ResultsDeviceEntryV5(ResultsEntry resultsEntry)
    {
        ResultsEntry = resultsEntry ?? throw new ArgumentNullException(nameof(resultsEntry));
        _IsWorkingTimer.Elapsed += _IsWorkingTimer_Elapsed;
        _IsWorkingTimer.AutoReset = false;
    }
    private void _IsWorkingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        Logger.Error($"Working timer elapsed for {Device}.");
        IsWorking = false;
        IsFaulted = true;
    }

    /// <summary>
    /// Notifies the UI that the <see cref="Handler"/> property has changed.
    /// </summary>
    public void HandlerUpdate() => OnPropertyChanged(nameof(Handler));

    /// <summary>
    /// Retrieves and loads the stored result and sectors from the database.
    /// </summary>
    public void GetStored()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(GetStored);
            return;
        }

        StoredSectors.Clear();

        var row = ResultsEntry.SelectedResultsDatabase.Select_Result(Device, ResultsEntry.ImageRollUID, ResultsEntry.SourceImageUID, ResultsEntry.ImageRollUID);
        if (row == null)
        {
            ResultRow = null;
            return;
        }

        if (row.Report == null || row.Template == null)
        {
            Logger.Debug("V5 stored result is missing template or report.");
            return;
        }

        List<ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(row.ReportString))
        {
            foreach (var toolResult in row.Report.GetParameter<JArray>("event.data.toolResults"))
            {
                try
                {
                    tempSectors.AddRange(((JObject)toolResult)
                        .GetParameter<JArray>("results")
                        .Select(result => (ISector)new V5.Sectors.Sector(
                            (JObject)result,
                            row.Template,
                            [ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGradingStandard],
                            ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedApplicationStandard,
                            ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGS1Table,
                            row.Template.GetParameter<string>("response.message"))));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    Logger.Warning($"Error while loading stored V5 results from: {ResultsEntry.SelectedResultsDatabase.File.Name}");
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            _ = ResultsEntry.SortList3(tempSectors);
            foreach (var sec in tempSectors)
                StoredSectors.Add(sec);
        }

        ResultRow = row;
        RefreshStoredOverlay();
    }

    /// <summary>
    /// Stores the current sectors, template, and report to the database.
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
            if (await ResultsEntry.OkCancelDialog("Overwrite Stored Sectors",
                    "Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
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
            Logger.Error($"Error while storing V5 results to: {ResultsEntry.SelectedResultsDatabase.File.Name}");

        GetStored();
        ClearCurrent();
    }

    /// <summary>
    /// Initiates the processing of an image by the V5 device or simulator.
    /// </summary>
    [RelayCommand]
    public void Process()
    {
        int fallback =  ResultsEntry.ResultsManagerView.ActiveImageRoll?.TargetDPI
                       ?? 600;

        var lab = new V5_REST_Lib.Controllers.Label(
            ProcessRepeat,
            Handler is LabelHandlers.SimulatorRestore or LabelHandlers.CameraRestore ? ResultRow?.Template : null,
            Handler,
            ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGS1Table);

        // Simulator path requires we supply the image bytes
        if (Handler is LabelHandlers.SimulatorRestore or LabelHandlers.SimulatorDetect or LabelHandlers.SimulatorTrigger)
        {
            if (ResultsEntry.ResultsManagerView.ActiveImageRoll.ImageType == ImageRollImageTypes.Source ||
                (ResultRow?.Stored == null && ResultsEntry.ResultsManagerView.ActiveImageRoll.ImageType == ImageRollImageTypes.Stored))
            {
                // Source image
                lab.Image = GlobalAppSettings.Instance.PreseveImageFormat
                    ? ImageFormatHelpers.EnsureDpi(ResultsEntry.SourceImage.OriginalImage, fallback, fallback, out _, out _)
                    : ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(ResultsEntry.SourceImage.OriginalImage, fallback, out _, out _);
            }
            else if (ResultsEntry.ResultsManagerView.ActiveImageRoll.ImageType == ImageRollImageTypes.Stored)
            {
                lab.Image = GlobalAppSettings.Instance.PreseveImageFormat
                    ? ImageFormatHelpers.EnsureDpi(ResultRow.Stored.ImageBytes, fallback, fallback, out _, out _)
                    : ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(ResultRow.Stored.ImageBytes, fallback, out _, out _);
            }
        }

        _ = ResultsEntry.ResultsManagerView.SelectedV5.Controller.ProcessLabel(lab);

        IsWorking = true;
        IsFaulted = false;
    }

    private void ProcessRepeat(V5_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat?.FullReport);

    /// <summary>
    /// Processes the full report received from the V5 device, updating the current image, sectors, and overlays.
    /// </summary>
    /// <param name="report">The full report from the device.</param>
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
                Logger.Error("Cannot process null V5 report/image.");
                IsFaulted = true;
                return;
            }

            int fallback =  ResultsEntry.ResultsManagerView.ActiveImageRoll?.TargetDPI
                           ?? 600;

            byte[] processedBytes;
            if (GlobalAppSettings.Instance.PreseveImageFormat)
            {
                processedBytes = ImageFormatHelpers.EnsureDpi(report.Image, fallback, fallback, out _, out _);
                try { report.Image = processedBytes; } catch { }
            }
            else
            {
                processedBytes = ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(report.Image, fallback, out _, out _);
                try { report.Image = processedBytes; } catch { }
            }

            if (!ResultsEntry.ResultsManagerView.SelectedV5.Controller.IsSimulator)
            {
                CurrentImage = new ImageEntry(ResultsEntry.ImageRollUID, processedBytes);
            }
            else
            {
                // Kept pattern (Magick still optional if later you manipulate)
                CurrentImage = new ImageEntry(ResultsEntry.ImageRollUID, processedBytes);
            }
            CurrentImage.EnsureDpi(fallback);

            CurrentTemplate = ResultsEntry.ResultsManagerView.SelectedV5.Controller.Config;
            CurrentReport = report.Report;

            CurrentSectors.Clear();

            List<ISector> tempSectors = [];
            foreach (var toolResult in CurrentReport.GetParameter<JArray>("event.data.toolResults"))
            {
                foreach (var result in ((JObject)toolResult).GetParameter<JArray>("results"))
                {
                    try
                    {
                        tempSectors.Add(new V5.Sectors.Sector(
                            (JObject)result,
                            CurrentTemplate,
                            [ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGradingStandard],
                            ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedApplicationStandard,
                            ResultsEntry.ResultsManagerView.ActiveImageRoll.SelectedGS1Table,
                            CurrentTemplate.GetParameter<string>("response.message")));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        Logger.Warning("Error while processing V5 sector result.");
                    }
                }
            }

            if (tempSectors.Count > 0)
            {
                tempSectors = ResultsEntry.SortList3(tempSectors);
                foreach (var sec in tempSectors)
                    CurrentSectors.Add(sec);
            }

            GetSectorDiff();
            RefreshCurrentOverlay();
            IsFaulted = false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Logger.Warning("V5 processing failed.");
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
    /// Clears the stored result from the database after user confirmation.
    /// </summary>
    [RelayCommand]
    public async Task ClearStored()
    {
        if (await ResultsEntry.OkCancelDialog("Clear Stored Sectors",
                "Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
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

        foreach (var sec in StoredSectors)
        {
            foreach (var cSec in CurrentSectors)
            {
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
                        diff.Add(new SectorDifferences
                        {
                            Username = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Report.Symbology.GetDescription()}  : Current Sector  {cSec.Report.Symbology.GetDescription()}"
                        });
                    }
                }
            }
        }

        foreach (var sec in StoredSectors)
        {
            bool found = CurrentSectors.Any(c => c.Template.Name == sec.Template.Name);
            if (!found)
            {
                diff.Add(new SectorDifferences
                {
                    Username = $"{sec.Template.Username} (MISSING)",
                    IsSectorMissing = true,
                    SectorMissingText = "Not found in current Sectors"
                });
            }
        }

        if (StoredSectors.Count > 0)
        {
            foreach (var sec in CurrentSectors)
            {
                bool found = StoredSectors.Any(c => c.Template.Name == sec.Template.Name);
                if (!found)
                {
                    diff.Add(new SectorDifferences
                    {
                        Username = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    });
                }
            }
        }

        foreach (var d in diff)
            if (d.IsSectorMissing)
                DiffSectors.Add(d);
    }

    /// <summary>
    /// Triggers the device to read/scan and waits for the result.
    /// </summary>
    [RelayCommand]
    private Task<bool> Read() => ReadTask();

    /// <summary>
    /// Asynchronously triggers the device to read/scan and processes the resulting report.
    /// </summary>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> ReadTask()
    {
        var result = await ResultsEntry.ResultsManagerView.SelectedV5.Controller.Trigger_Wait_Return(true);
        ProcessFullReport(result);
        return result != null;
    }

    /// <summary>
    /// Loads the stored configuration to the device.
    /// </summary>
    [RelayCommand]
    private Task<int> Load() => LoadTask();

    /// <summary>
    /// Asynchronously loads the stored configuration (template) to the connected device.
    /// </summary>
    /// <returns>An integer indicating the result: 1 for success, -1 for failure, 0 if no action was taken.</returns>
    public async Task<int> LoadTask()
    {
        if (ResultRow == null)
        {
            Logger.Error("No V5 result row selected.");
            return -1;
        }
        if (StoredSectors.Count == 0)
            return 0;

        if (await ResultsEntry.ResultsManagerView.SelectedV5.Controller.CopySectorsSetConfig(null, ResultRow.Template) == V5_REST_Lib.Controllers.RestoreSectorsResults.Failure)
            return -1;

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
    /// Refreshes the overlay for the stored image.
    /// </summary>
    public void RefreshStoredOverlay() => StoredImageOverlay = IResultsDeviceEntry.CreateSectorsImageOverlay(StoredImage, StoredSectors);
    /// <summary>
    /// Refreshes the overlay for the current image.
    /// </summary>
    public void RefreshCurrentOverlay() => CurrentImageOverlay = IResultsDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);

    /// <summary>
    /// Releases the resources used by the <see cref="ResultsDeviceEntryV5"/> object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ResultsDeviceEntryV5"/> and optionally releases the managed resources.
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