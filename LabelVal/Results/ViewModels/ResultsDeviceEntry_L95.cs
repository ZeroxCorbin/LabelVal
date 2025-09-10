using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.L95.Sectors;
using LabelVal.Main.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
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
/// Represents a device-specific entry for an L95 device within the image results view.
/// This class manages the state, data, and operations related to image verification results from an L95 device.
/// </summary>
public partial class ResultsDeviceEntry_L95
    : ObservableRecipient, IResultsDeviceEntry, IRecipient<PropertyChangedMessage<FullReport>>, IDisposable
{
    /// <summary>
    /// Gets the parent <see cref="ResultsEntry"/> that this device entry belongs to.
    /// </summary>
    public ResultsEntry ResultsEntry { get; }
    /// <summary>
    /// Gets the manager for image results, providing access to shared data and services.
    /// </summary>
    public ResultssManager ResultssManager => ResultsEntry.ResultssManager;
    /// <summary>
    /// Gets the device type for this entry, which is L95.
    /// </summary>
    public ResultsEntryDevices Device { get; } = ResultsEntryDevices.L95;

    /// <summary>
    /// Gets or sets the database result record associated with this entry.
    /// </summary>
    [ObservableProperty] private Databases.Result resultRow;
    partial void OnResultRowChanged(Result value) { StoredImage = value?.Stored; HandlerUpdate(); }
    /// <summary>
    /// Gets or sets the result data for this entry. This is an alias for <see cref="ResultRow"/>.
    /// </summary>
    public Result Result { get => ResultRow; set { ResultRow = value; HandlerUpdate(); } }

    /// <summary>
    /// Gets or sets the stored image associated with the result.
    /// </summary>
    [ObservableProperty] private ImageEntry storedImage;
    /// <summary>
    /// Gets or sets the overlay for the stored image, which may contain sector outlines.
    /// </summary>
    [ObservableProperty] private DrawingImage storedImageOverlay;

    /// <summary>
    /// Gets or sets the current image being processed or displayed.
    /// </summary>
    [ObservableProperty] private ImageEntry currentImage;
    /// <summary>
    /// Gets or sets the overlay for the current image.
    /// </summary>
    [ObservableProperty] private DrawingImage currentImageOverlay;

    /// <summary>
    /// Gets or sets the JSON template for the current verification.
    /// </summary>
    public JObject CurrentTemplate { get; set; } = null;
    /// <summary>
    /// Gets the serialized JSON string of the current template.
    /// </summary>
    public string SerializeTemplate => JsonConvert.SerializeObject(CurrentTemplate);

    /// <summary>
    /// Gets the JSON report for the current verification.
    /// </summary>
    public JObject CurrentReport { get; private set; }
    /// <summary>
    /// Gets the serialized JSON string of the current report.
    /// </summary>
    public string SerializeReport => JsonConvert.SerializeObject(CurrentReport);

    /// <summary>
    /// Gets the collection of sectors from the current verification.
    /// </summary>
    public ObservableCollection<Sectors.Interfaces.ISector> CurrentSectors { get; } = [];
    /// <summary>
    /// Gets the collection of sectors from the stored result.
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
    /// Gets or sets a value indicating whether a long-running operation is in progress.
    /// </summary>
    [ObservableProperty] private bool isWorking = false;
    partial void OnIsWorkingChanged(bool value)
    {
        ResultssManager.WorkingUpdate(Device, value);
        OnPropertyChanged(nameof(IsNotWorking));
    }
    /// <summary>
    /// Gets a value indicating whether no long-running operation is in progress.
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
        ResultssManager.FaultedUpdate(Device, value);
        OnPropertyChanged(nameof(IsNotFaulted));
    }
    /// <summary>
    /// Gets a value indicating whether the last operation did not result in a fault.
    /// </summary>
    public bool IsNotFaulted => !IsFaulted;

    ////95xx Only
    //[ObservableProperty] private Sectors.Interfaces.ISector currentSectorSelected;
    /// <summary>
    /// Gets the appropriate label handler based on the current state of the L95 device and image roll settings.
    /// </summary>
    public LabelHandlers Handler => ResultssManager?.SelectedL95?.Controller != null && ResultssManager.SelectedL95.Controller.IsConnected && ResultssManager.SelectedL95.Controller.ProcessState == Watchers.lib.Process.Win32_ProcessWatcherProcessState.Running ? ResultssManager.SelectedL95.Controller.IsSimulator
            ? ResultssManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.SimulatorRestore
                    : LabelHandlers.SimulatorDetect
                : LabelHandlers.SimulatorTrigger
            : ResultssManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.CameraRestore
                    : LabelHandlers.CameraDetect
                : LabelHandlers.CameraTrigger
        : LabelHandlers.Offline;

    /// <summary>
    /// Notifies that the <see cref="Handler"/> property has changed.
    /// </summary>
    public void HandlerUpdate() => OnPropertyChanged(nameof(Handler));

    /// <summary>
    /// Gets or sets a value indicating whether this device entry is selected in the UI.
    /// </summary>
    [ObservableProperty] private bool isSelected = false;
    partial void OnIsSelectedChanging(bool value) { if (value) ResultsEntry.ResultssManager.ResetSelected(Device); }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsDeviceEntry_L95"/> class.
    /// </summary>
    /// <param name="imageResultsEntry">The parent image result entry.</param>
    public ResultsDeviceEntry_L95(ResultsEntry imageResultsEntry)
    {
        ResultsEntry = imageResultsEntry;

        _IsWorkingTimer.AutoReset = false;
        _IsWorkingTimer.Elapsed += _IsWorkingTimer_Elapsed;

        IsActive = true;
    }

    private void _IsWorkingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        Logger.Error($"Working timer elapsed for {Device}.");
        IsWorking = false;
        IsFaulted = true;
    }

    /// <summary>
    /// Receives property changed messages for <see cref="FullReport"/> to process new verification data.
    /// </summary>
    /// <param name="message">The message containing the new <see cref="FullReport"/>.</param>
    public void Receive(PropertyChangedMessage<FullReport> message)
    {
        if (IsSelected || IsWorking)
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message.NewValue, false));
    }

    /// <summary>
    /// Retrieves the stored verification result from the database for the current image.
    /// </summary>
    public void GetStored()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => GetStored());
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
            Result row = ResultsEntry.SelectedResultsDatabase.Select_Result(Device, ResultsEntry.ImageRollUID, ResultsEntry.SourceImageUID, ResultsEntry.ImageRollUID);

            if (row == null)
            {
                ResultRow = null;
                return;
            }

            List<Sectors.Interfaces.ISector> tempSectors = [];
            foreach (JToken rSec in row.Report.GetParameter<JArray>("AllReports"))
                tempSectors.Add(new Sector(((JObject)rSec).GetParameter<JObject>("Template"), ((JObject)rSec).GetParameter<JObject>("Report"), [ResultsEntry.ResultssManager.SelectedImageRoll.SelectedGradingStandard], ResultsEntry.ResultssManager.SelectedImageRoll.SelectedApplicationStandard, ResultsEntry.ResultssManager.SelectedImageRoll.SelectedGS1Table, ((JObject)rSec).GetParameter<string>("Template.Settings[SettingName:Version].SettingValue")));

            if (tempSectors.Count > 0)
            {
                tempSectors = ResultsEntry.SortList3(tempSectors);

                foreach (ISector sec in tempSectors)
                    StoredSectors.Add(sec);
            }

            ResultRow = row;
            RefreshStoredOverlay();

        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Logger.Error($"Error while loading stored results from: {ResultsEntry.SelectedResultsDatabase.File.Name}");
        }
    }

    /// <summary>
    /// Stores the currently selected sector into the database, overwriting if it already exists.
    /// </summary>
    [RelayCommand]
    public async Task StoreSingle()
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

        ISector old = StoredSectors.FirstOrDefault(x => x.Template.Name == CurrentSelectedSector.Template.Name);
        if (old != null)
        {
            if (await ResultsEntry.OkCancelDialog("Overwrite Stored Sector", $"The sector already exists.\r\nAre you sure you want to overwrite the stored sector?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;
        }

        //Save the list to the database.
        List<FullReport> temp = [];
        foreach (ISector sector in StoredSectors)
            temp.Add(new FullReport(((L95.Sectors.Sector)sector).Template.Original, ((L95.Sectors.Sector)sector).Report.Original));

        temp.Add(new FullReport(((L95.Sectors.Sector)CurrentSelectedSector).Template.Original, ((L95.Sectors.Sector)CurrentSelectedSector).Report.Original));

        JObject report = new()
        {
            ["AllReports"] = JArray.FromObject(temp)
        };

        _ = ResultsEntry.SelectedResultsDatabase.InsertOrReplace_Result(new Databases.Result
        {
            Device = Device,
            ImageRollUID = ResultsEntry.ImageRollUID,
            SourceImageUID = ResultsEntry.SourceImageUID,
            RunUID = ResultsEntry.ImageRollUID,

            Template = CurrentTemplate,
            Report = report,
            Stored = CurrentImage,
        });

        GetStored();
        ClearSingle();
    }
    /// <summary>
    /// Stores all current sectors into the database, overwriting any existing stored sectors for the image.
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

        Result res = GetCurrentReport();

        if (ResultsEntry.SelectedResultsDatabase.InsertOrReplace_Result(res) == null)
            Logger.Error($"Error while storing results to: {ResultsEntry.SelectedResultsDatabase.File.Name}");

        GetStored();
        ClearCurrent();

        //        else if (device == ResultsEntryDevices.L95)
        //{

        //    if (L95CurrentSectorSelected == null)
        //    {
        //        Logger.Error("No sector selected to store.");
        //        return;
        //    }
        //    //Does the selected sector exist in the Stored sectors list?
        //    //If so, prompt to overwrite or cancel.

        //    Sectors.Interfaces.ISector old = L95StoredSectors.FirstOrDefault(x => x.Template.Name == L95CurrentSectorSelected.Template.Name);
        //    if (old != null)
        //    {
        //        if (await OkCancelDialog("Overwrite Stored Sector", $"The sector already exists.\r\nAre you sure you want to overwrite the stored sector?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
        //            return;
        //    }

        //    //Save the list to the database.
        //    List<FullReport> temp = [];
        //    if (L95ResultRow != null)
        //        temp = L95ResultRow._AllSectors;

        //    temp.Add(new FullReport(((L95.Sectors.Sector)L95CurrentSectorSelected).Template.Original, ((L95.Sectors.Sector)L95CurrentSectorSelected).Report.Original));

        //    _ = SelectedResultsDatabase.InsertOrReplace_L95Result(new Databases.L95Result
        //    {
        //        ImageRollUID = ImageRollUID,
        //        RunUID = ImageRollUID,
        //        Source = SourceImage,
        //        Stored = L95CurrentImage,

        //        _AllSectors = temp,
        //    });

        //    ClearRead(device);

        //    L95GetStored();
        //}
        //else if (device == ResultsEntryDevices.L95All)
        //{

        //    if (L95CurrentSectors.Count == 0)
        //    {
        //        Logger.Debug($"There are no sectors to store for: {device}");
        //        return;
        //    }
        //    //Does the selected sector exist in the Stored sectors list?
        //    //If so, prompt to overwrite or cancel.

        //    if (L95StoredSectors.Count > 0)
        //        if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
        //            return;

        //    //Save the list to the database.
        //    List<FullReport> temp = [];
        //    foreach (Sectors.Interfaces.ISector sector in L95CurrentSectors)

        //        temp.Add(new FullReport(((L95.Sectors.Sector)sector).Template.Original, ((L95.Sectors.Sector)sector).Report.Original));

        //    _ = SelectedResultsDatabase.InsertOrReplace_L95Result(new Databases.L95Result
        //    {
        //        ImageRollUID = ImageRollUID,
        //        RunUID = ImageRollUID,
        //        Source = SourceImage,
        //        Stored = L95CurrentImage,

        //        _AllSectors = temp,
        //    });

        //    ClearRead(device);

        //    L95GetStored();
        //}
    }

    private Result GetCurrentReport()
    {
        //Save the list to the database.
        List<FullReport> temp = [];
        foreach (ISector sector in CurrentSectors)
            temp.Add(new FullReport(((L95.Sectors.Sector)sector).Template.Original, ((L95.Sectors.Sector)sector).Report.Original));

        JObject report = new()
        {
            ["AllReports"] = JArray.FromObject(temp)
        };

        var res = new Databases.Result
        {
            Device = Device,
            ImageRollUID = ResultsEntry.ImageRollUID,
            SourceImageUID = ResultsEntry.SourceImageUID,
            RunUID = ResultsEntry.ImageRollUID,

            Template = CurrentTemplate,
            Report = report,
            Stored = CurrentImage,
        };

        return res;
    }

    /// <summary>
    /// Initiates the verification process for the current image using the L95 device.
    /// </summary>
    [RelayCommand]
    public void Process()
    {
        IsSelected = true;

        Label lab = new()
        {
            Config = new Lvs95xx.lib.Core.Controllers.Config()
            {
                ApplicationStandard = ResultsEntry.ResultssManager.SelectedImageRoll.SelectedApplicationStandard.GetDescription(),
            },
            RepeatAvailable = ProcessFullReport,
        };

        if (ResultsEntry.ResultssManager.SelectedImageRoll.SelectedApplicationStandard == ApplicationStandards.GS1)
            lab.Config.Table = ResultsEntry.ResultssManager.SelectedImageRoll.SelectedGS1Table.GetTableName();

        if (ResultsEntry.ResultssManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Source)
            lab.Image = ResultsEntry.SourceImage.BitmapBytes;
        else if (ResultsEntry.ResultssManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
            lab.Image = ResultRow.Stored.BitmapBytes;

        IsWorking = true;
        IsFaulted = false;

        _ = Task.Run(() => ResultsEntry.ResultssManager.SelectedL95.Controller.ProcessLabelAsync(lab));
    }
    /// <summary>
    /// Processes the full report received from the L95 device.
    /// </summary>
    /// <param name="message">The full report data.</param>
    /// <param name="replaceSectors">A value indicating whether to replace existing current sectors.</param>
    public void ProcessFullReport(FullReport message, bool replaceSectors)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message, replaceSectors));
            return;
        }

        try
        {
            if (message == null || message.Report == null)// || message.Report.OverallGrade.StartsWith("Bar"))
            {
                IsFaulted = true;
                return;
            }

            if (message.Report.GetParameter<string>(Parameters.OverallGrade.GetPath(Devices.L95, Symbologies.DataMatrix)) != "Bar Code Not Detected")
            {

                System.Drawing.Point center = new(message.Template.GetParameter<int>("Report.X1") + (message.Template.GetParameter<int>("Report.SizeX") / 2), message.Template.GetParameter<int>("Report.Y1") + (message.Template.GetParameter<int>("Report.SizeY") / 2));

                string name = null;
                if ((name = ResultsEntry.GetName(center)) == null)
                    name ??= $"Verify_{CurrentSectors.Count + 1}";

                _ = message.Template.SetParameter<string>("Name", name);

                if (replaceSectors)
                    CurrentSectors.Clear();

                CurrentSectors.Add(new Sector(message.Template, message.Report, [ResultsEntry.ResultssManager.SelectedImageRoll.SelectedGradingStandard], ResultsEntry.ResultssManager.SelectedImageRoll.SelectedApplicationStandard, ResultsEntry.ResultssManager.SelectedImageRoll.SelectedGS1Table, message.Template.GetParameter<string>("Settings[SettingName:Version].SettingValue")));
            }
            else if (GlobalAppSettings.Instance.LvsIgnoreNoResults)
                return;

            List<FullReport> temp = [];
            foreach (ISector sector in CurrentSectors)
                temp.Add(new FullReport(((L95.Sectors.Sector)sector).Template.Original, ((L95.Sectors.Sector)sector).Report.Original));

            JObject report = new()
            {
                ["AllReports"] = JArray.FromObject(temp)
            };
            CurrentReport = report;
            CurrentTemplate = null;

            var tempSectors = CurrentSectors.ToList();

            if (tempSectors.Count > 0)
            {
                tempSectors = ResultsEntry.SortList3(tempSectors);
                SortObservableCollectionByList(tempSectors, CurrentSectors);
            }

            GetSectorDiff();


            CurrentImage = new ImageEntry(ResultsEntry.ImageRollUID, message.Template.GetParameter<byte[]>("Report.Thumbnail"), 0);
            RefreshCurrentOverlay();

            IsFaulted = false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            IsFaulted = true;
        }
        finally
        {
            IsWorking = false;
            Application.Current.Dispatcher.Invoke(ResultsEntry.BringIntoViewHandler);
        }
    }

    /// <summary>
    /// Clears the currently selected sector from the current results.
    /// </summary>
    [RelayCommand]
    public void ClearSingle()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => ClearSingle());
            return;
        }

        if (CurrentSelectedSector == null)
        {
            Logger.Error("No sector selected to clear.");
            return;
        }

        _ = CurrentSectors.Remove(CurrentSelectedSector);

        if (CurrentSectors.Count == 0)
        {
            DiffSectors.Clear();
            CurrentImage = null;
            CurrentImageOverlay = null;
        }
        else
        {
            GetSectorDiff();
            CurrentImageOverlay = IResultsDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);
        }
    }

    /// <summary>
    /// Clears all sectors and related data from the current verification result.
    /// </summary>
    [RelayCommand]
    public void ClearCurrent()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => ClearCurrent());
            return;
        }

        CurrentSectors.Clear();
        DiffSectors.Clear();

        CurrentImageOverlay = null;
        CurrentImage = null;
    }

    /// <summary>
    /// Clears all stored sectors for the current image from the database.
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
        foreach (Sector sec in StoredSectors)
        {
            foreach (Sector cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Report.Symbology == cSec.Report.Symbology)
                    {
                        SectorDifferences res = sec.SectorDetails.Compare(cSec.SectorDetails);
                        if (res == null)
                            continue;
                        diff.Add(res);
                        continue;
                    }
                    else
                    {
                        SectorDifferences dat = new()
                        {
                            Username = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Report.Symbology.GetDescription()} : Current Sector {cSec.Report.Symbology.GetDescription()}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (Sector sec in StoredSectors)
        {
            var found = false;
            foreach (Sector cSec in CurrentSectors)
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
            foreach (Sector sec in CurrentSectors)
            {
                var found = false;
                foreach (Sector cSec in StoredSectors)
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

        foreach (SectorDifferences d in diff)

            DiffSectors.Add(d);
    }

    /// <summary>
    /// This is exposed through the interface to allow for the ResultssManager to call this method.
    /// </summary>
    public void RefreshOverlays()
    {
        RefreshStoredOverlay();
        RefreshCurrentOverlay();
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
    /// Sorts an <see cref="ObservableCollection{ISector}"/> to match the order of a <see cref="List{ISector}"/>.
    /// </summary>
    /// <param name="list">The list with the desired order.</param>
    /// <param name="observableCollection">The observable collection to sort.</param>
    public static void SortObservableCollectionByList(List<ISector> list, ObservableCollection<ISector> observableCollection)
    {
        for (var i = 0; i < list.Count; i++)
        {
            ISector item = list[i];
            var currentIndex = observableCollection.IndexOf(item);
            if (currentIndex != i)
            {
                observableCollection.Move(currentIndex, i);
            }
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ResultsDeviceEntry_L95"/> and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        _IsWorkingTimer.Elapsed -= _IsWorkingTimer_Elapsed;
        _IsWorkingTimer.Dispose();
        GC.SuppressFinalize(this);
    }

    //public int LoadTask()
    //{
    //    return 1;
    //}
    //private DrawingImage CreateSectorsImageOverlay(bool useStored)
    //{
    //    var bmp = ImageUtilities.CreateBitmap(Image);

    //    //Draw the image outline the same size as the stored image
    //    var border = new GeometryDrawing
    //    {
    //        Geometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)),
    //        Pen = new Pen(Brushes.Transparent, 1)
    //    };

    //    var secAreas = new GeometryGroup();
    //    var bndAreas = new GeometryGroup();

    //    var drwGroup = new DrawingGroup();

    //    if (useStored)
    //    {
    //        foreach (var sec in StoredReport._event.data.cycleConfig.qualifiedResults)
    //        {
    //            if (sec.boundingBox == null)
    //                continue;

    //            secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec.boundingBox[0].x, sec.boundingBox[0].y), new Point(sec.boundingBox[2].x, sec.boundingBox[2].y))));
    //        }

    //        foreach (var sec in StoredReport._event.data.cycleConfig.job.toolList)
    //            foreach (var r in sec.SymbologyTool.regionList)
    //                bndAreas.Children.Add(new RectangleGeometry(new Rect(r.Region.shape.RectShape.x, r.Region.shape.RectShape.y, r.Region.shape.RectShape.width, r.Region.shape.RectShape.height)));

    //    }
    //    else
    //    {
    //        foreach (var sec in CurrentReport._event.data.cycleConfig.qualifiedResults)
    //        {
    //            if (sec.boundingBox == null)
    //                continue;

    //            secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec.boundingBox[0].x, sec.boundingBox[0].y), new Point(sec.boundingBox[2].x, sec.boundingBox[2].y))));
    //        }

    //        foreach (var sec in CurrentReport._event.data.cycleConfig.job.toolList)
    //            foreach (var r in sec.SymbologyTool.regionList)
    //                bndAreas.Children.Add(new RectangleGeometry(new Rect(r.Region.shape.RectShape.x, r.Region.shape.RectShape.y, r.Region.shape.RectShape.width, r.Region.shape.RectShape.height)));

    //    }

    //    var sectors = new GeometryDrawing
    //    {
    //        Geometry = secAreas,
    //        Pen = new Pen(Brushes.Red, 5)
    //    };

    //    var bounding = new GeometryDrawing
    //    {
    //        Geometry = bndAreas,
    //        Pen = new Pen(Brushes.Purple, 5)
    //    };

    //    drwGroup.Children.Add(bounding);
    //    drwGroup.Children.Add(sectors);
    //    drwGroup.Children.Add(border);

    //    var geometryImage = new DrawingImage(drwGroup);
    //    geometryImage.Freeze();
    //    return geometryImage;
    //}
}