using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.L95.Sectors;
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
public partial class ImageResultDeviceEntry_L95
    : ObservableRecipient, IImageResultDeviceEntry, IRecipient<PropertyChangedMessage<FullReport>>
{
    public ImageResultEntry ImageResultEntry { get; }
    public ImageResultsManager ImageResultsManager => ImageResultEntry.ImageResultsManager;
    public ImageResultEntryDevices Device { get; } = ImageResultEntryDevices.L95;
    public string Version => throw new NotImplementedException();

    [ObservableProperty] private Databases.Result resultRow;
    partial void OnResultRowChanged(Result value) { StoredImage = value?.Stored; HandlerUpdate(); }
    public Result Result { get => ResultRow; set { ResultRow = value; HandlerUpdate(); } }

    [ObservableProperty] private ImageEntry storedImage;
    [ObservableProperty] private DrawingImage storedImageOverlay;

    [ObservableProperty] private ImageEntry currentImage;
    [ObservableProperty] private DrawingImage currentImageOverlay;

    public JObject CurrentTemplate { get; set; } = null;
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

    ////95xx Only
    //[ObservableProperty] private Sectors.Interfaces.ISector currentSectorSelected;
    public LabelHandlers Handler => ImageResultsManager?.SelectedL95?.Controller != null && ImageResultsManager.SelectedL95.Controller.IsConnected && ImageResultsManager.SelectedL95.Controller.ProcessState == Watchers.lib.Process.Win32_ProcessWatcherProcessState.Running ? ImageResultsManager.SelectedL95.Controller.IsSimulator
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

    [ObservableProperty] private bool isSelected = false;
    partial void OnIsSelectedChanging(bool value) { if (value) ImageResultEntry.ImageResultsManager.ResetSelected(Device); }

    public ImageResultDeviceEntry_L95(ImageResultEntry imageResultsEntry)
    {
        ImageResultEntry = imageResultsEntry;

        _IsWorkingTimer.AutoReset = false;
        _IsWorkingTimer.Elapsed += _IsWorkingTimer_Elapsed;

        IsActive = true;
    }

    private void _IsWorkingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        Logger.LogError($"Working timer elapsed for {Device}.");
        IsWorking = false;
        IsFaulted = true;
    }

    public void Receive(PropertyChangedMessage<FullReport> message)
    {
        if (IsSelected || IsWorking)
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message.NewValue, false));
    }

    public void GetStored()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => GetStored());
            return;
        }

        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.LogError("No image results database selected.");
            return;
        }

        StoredSectors.Clear();

        try
        {
            var row = ImageResultEntry.SelectedDatabase.Select_Result(Device, ImageResultEntry.ImageRollUID, ImageResultEntry.SourceImageUID, ImageResultEntry.ImageRollUID);

            if (row == null)
            {
                ResultRow = null;
                return;
            }

            List<Sectors.Interfaces.ISector> tempSectors = [];
            foreach (var rSec in row.Report.GetParameter<JArray>("AllReports"))
                tempSectors.Add(new Sector(((JObject)rSec).GetParameter<JObject>("Template"), ((JObject)rSec).GetParameter<JObject>("Report"), [ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGradingStandard], ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, ((JObject)rSec).GetParameter<string>("Template.Settings[SettingName:Version].SettingValue")));

            if (tempSectors.Count > 0)
            {
                tempSectors = ImageResultEntry.SortList3(tempSectors);

                foreach (var sec in tempSectors)
                    StoredSectors.Add(sec);
            }

            ResultRow = row;
            RefreshStoredOverlay();

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            Logger.LogError($"Error while loading stored results from: {ImageResultEntry.SelectedDatabase.File.Name}");
        }
    }

    [RelayCommand]
    public async Task StoreSingle()
    {
        if (CurrentSectors.Count == 0)
        {
            Logger.LogError("No sectors to store.");
            return;
        }

        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.LogError("No image results database selected.");
            return;
        }

        var old = StoredSectors.FirstOrDefault(x => x.Template.Name == CurrentSelectedSector.Template.Name);
        if (old != null)
        {
            if (await ImageResultEntry.OkCancelDialog("Overwrite Stored Sector", $"The sector already exists.\r\nAre you sure you want to overwrite the stored sector?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;
        }

        //Save the list to the database.
        List<FullReport> temp = [];
        foreach (var sector in StoredSectors)
            temp.Add(new FullReport(((L95.Sectors.Sector)sector).Template.Original, ((L95.Sectors.Sector)sector).Report.Original));

        temp.Add(new FullReport(((L95.Sectors.Sector)CurrentSelectedSector).Template.Original, ((L95.Sectors.Sector)CurrentSelectedSector).Report.Original));

        JObject report = new()
        {
            ["AllReports"] = JArray.FromObject(temp)
        };

        _ = ImageResultEntry.SelectedDatabase.InsertOrReplace_Result(new Databases.Result
        {
            Device = Device,
            ImageRollUID = ImageResultEntry.ImageRollUID,
            SourceImageUID = ImageResultEntry.SourceImageUID,
            RunUID = ImageResultEntry.ImageRollUID,

            Template = CurrentTemplate,
            Report = report,
            Stored = CurrentImage,
        });

        GetStored();
        ClearSingle();
    }
    [RelayCommand]
    public async Task Store()
    {
        if (CurrentSectors.Count == 0)
        {
            Logger.LogError("No sectors to store.");
            return;
        }

        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.LogError("No image results database selected.");
            return;
        }

        if (StoredSectors.Count > 0)
            if (await ImageResultEntry.OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;

        var res = GetCurrentReport();

        if (ImageResultEntry.SelectedDatabase.InsertOrReplace_Result(res) == null)
            Logger.LogError($"Error while storing results to: {ImageResultEntry.SelectedDatabase.File.Name}");

        GetStored();
        ClearCurrent();

        //        else if (device == ImageResultEntryDevices.L95)
        //{

        //    if (L95CurrentSectorSelected == null)
        //    {
        //        Logger.LogError("No sector selected to store.");
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

        //    _ = SelectedDatabase.InsertOrReplace_L95Result(new Databases.L95Result
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
        //else if (device == ImageResultEntryDevices.L95All)
        //{

        //    if (L95CurrentSectors.Count == 0)
        //    {
        //        Logger.LogDebug($"There are no sectors to store for: {device}");
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

        //    _ = SelectedDatabase.InsertOrReplace_L95Result(new Databases.L95Result
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
        foreach (var sector in CurrentSectors)
            temp.Add(new FullReport(((L95.Sectors.Sector)sector).Template.Original, ((L95.Sectors.Sector)sector).Report.Original));

        JObject report = new()
        {
            ["AllReports"] = JArray.FromObject(temp)
        };

        var res = new Databases.Result
        {
            Device = Device,
            ImageRollUID = ImageResultEntry.ImageRollUID,
            SourceImageUID = ImageResultEntry.SourceImageUID,
            RunUID = ImageResultEntry.ImageRollUID,

            Template = CurrentTemplate,
            Report = report,
            Stored = CurrentImage,
        };

        return res;
    }

    [RelayCommand]
    public void Process()
    {
        Label lab = new()
        {
            Config = new Lvs95xx.lib.Core.Controllers.Config()
            {
                ApplicationStandard = ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard.GetDescription(),
            },
            RepeatAvailable = ProcessFullReport,
        };

        if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard == ApplicationStandards.GS1)
            lab.Config.Table = ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table.GetTableName();

        if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Source)
            lab.Image = ImageResultEntry.SourceImage.BitmapBytes;
        else if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
            lab.Image = ResultRow.Stored.BitmapBytes;

        IsWorking = true;
        IsFaulted = false;

        _ = Task.Run(() => ImageResultEntry.ImageResultsManager.SelectedL95.Controller.ProcessLabelAsync(lab));
    }
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

            System.Drawing.Point center = new(message.Template.GetParameter<int>("Report.X1") + (message.Template.GetParameter<int>("Report.SizeX") / 2), message.Template.GetParameter<int>("Report.Y1") + (message.Template.GetParameter<int>("Report.SizeY") / 2));

            string name = null;
            if ((name = ImageResultEntry.GetName(center)) == null)
                name ??= $"Verify_{CurrentSectors.Count + 1}";

            _ = message.Template.SetParameter<string>("Name", name);

            if (replaceSectors)
                CurrentSectors.Clear();

            System.IO.File.WriteAllText(System.IO.Path.Combine(App.UserDataDirectory, "L95Report.json"), message.Report.ToString());

            CurrentSectors.Add(new Sector(message.Template, message.Report, [ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGradingStandard], ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, message.Template.GetParameter<string>("Settings[SettingName:Version].SettingValue")));

            var tempSectors = CurrentSectors.ToList();

            if (tempSectors.Count > 0)
            {
                tempSectors = ImageResultEntry.SortList3(tempSectors);
                SortObservableCollectionByList(tempSectors, CurrentSectors);
            }

            GetSectorDiff();

            CurrentImage = new ImageEntry(ImageResultEntry.ImageRollUID, message.Template.GetParameter<byte[]>("Report.Thumbnail"), 0);
            RefreshCurrentOverlay();

            IsFaulted = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            IsFaulted = true;
        }
        finally
        {
            IsWorking = false;
            Application.Current.Dispatcher.Invoke(ImageResultEntry.BringIntoViewHandler);
        }
    }

    [RelayCommand]
    public void ClearSingle()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => ClearCurrent());
            return;
        }

        if (CurrentSelectedSector == null)
        {
            Logger.LogError("No sector selected to clear.");
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
            CurrentImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);
        }
    }

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

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (Sector sec in StoredSectors)
        {
            foreach (Sector cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Report.Symbology == cSec.Report.Symbology)
                    {
                        var res = sec.SectorDetails.Compare(cSec.SectorDetails);
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

        foreach (var d in diff)

            DiffSectors.Add(d);
    }

    /// <summary>
    /// This is exposed through the interface to allow for the ImageResultsManager to call this method.
    /// </summary>
    public void RefreshOverlays()
    {
        RefreshStoredOverlay();
        RefreshCurrentOverlay();
    }

    public void RefreshStoredOverlay() => StoredImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(StoredImage, StoredSectors);
    public void RefreshCurrentOverlay() => CurrentImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);

    public static void SortObservableCollectionByList(List<ISector> list, ObservableCollection<ISector> observableCollection)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var currentIndex = observableCollection.IndexOf(item);
            if (currentIndex != i)
            {
                observableCollection.Move(currentIndex, i);
            }
        }
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
