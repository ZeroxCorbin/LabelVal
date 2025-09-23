using BarcodeVerification.lib.Extensions;
using Cameras_Lib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using V5_REST_Lib.Cameras;
using V5_REST_Lib.Controllers;
using V5_REST_Lib.Models;

namespace LabelVal.V5.ViewModels;

/// <summary>
/// ViewModel (MVVM) encapsulating interaction with a V5 scanner device.
/// Handles connection management, job slot selection, triggering, quick-set
/// (focus & photometry) operations, image acquisition, overlays, and
/// synchronization with other components (e.g., <see cref="ImageRoll"/>).
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class Scanner : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRoll>>
{
    #region Private Fields

    private int repeatedTriggerDelay = 50;
    private JObject ResultsJObject;
    private bool IsWaitingForFullImage;
    private bool userChange;
    private CancellationTokenSource _tokenSrc;

    // Currently unused (left in place intentionally; may be used by future logic)
    private bool running;
    private bool stop;

    #endregion

    #region Construction / Initialization

    /// <summary>
    /// Initializes a new instance and wires controller property change events.
    /// </summary>
    public Scanner()
    {
        Controller.PropertyChanged += Controller_PropertyChanged;
        IsActive = true;
    }

    #endregion

    #region Core Device / Identification

    /// <summary>
    /// Unique ID for this instance (serialized).
    /// </summary>
    [JsonProperty]
    public long ID { get; set; } = DateTime.Now.Ticks;

    /// <summary>
    /// Underlying REST controller used for all device interactions.
    /// </summary>
    [JsonProperty]
    public V5_REST_Lib.Controllers.Controller Controller { get; } = new();

    #endregion

    #region Camera Trigger

    /// <summary>
    /// When true, continuous trigger mode is active until cancelled.
    /// </summary>
    [ObservableProperty]
    private bool repeatTrigger;

    /// <summary>
    /// Delay (ms) between repeated trigger operations. Enforced range [1,1000].
    /// </summary>
    [JsonProperty]
    public int RepeatedTriggerDelay
    {
        get => repeatedTriggerDelay;
        set
        {
            repeatedTriggerDelay = value < 1 ? 1 : value > 1000 ? 1000 : value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Image / Overlay Related Properties

    /// <summary>
    /// Percentage of the sensor dimension used for QuickSet (centered ROI).
    /// </summary>
    [ObservableProperty]
    [property: JsonProperty]
    private double quickSet_ImagePercent = 0.33d;

    partial void OnQuickSet_ImagePercentChanged(double value) =>
        _ = Application.Current.Dispatcher.BeginInvoke(UpdateFocusOverlay);

    /// <summary>
    /// If true, requests full-resolution images from the controller.
    /// Affects overlay scaling.
    /// </summary>
    [ObservableProperty]
    [property: JsonProperty]
    private bool fullResImages = false;

    partial void OnFullResImagesChanged(bool value) => CheckOverlay();

    /// <summary>
    /// Raw JSON capture element (first capture entry if provided).
    /// </summary>
    [ObservableProperty]
    private JToken capture;

    /// <summary>
    /// The current cycle ID extracted from report events.
    /// </summary>
    [ObservableProperty]
    private string cycleID;

    /// <summary>
    /// Raw binary image data.
    /// </summary>
    [ObservableProperty]
    private byte[] rawImage;

    partial void OnRawImageChanged(byte[] value) =>
        Image = value == null ? null : GetImage(value);

    /// <summary>
    /// Decoded <see cref="BitmapImage"/> generated from <see cref="RawImage"/>.
    /// </summary>
    [ObservableProperty]
    private BitmapImage image;

    /// <summary>
    /// Overlay for decode results sectors / bounding shapes.
    /// </summary>
    [ObservableProperty]
    private DrawingImage imageOverlay;

    /// <summary>
    /// Overlay indicating QuickSet focus/photometry ROI.
    /// </summary>
    [ObservableProperty]
    private DrawingImage imageFocusRegionOverlay;

    #endregion

    #region Job / Directory / Acquisition

    /// <summary>
    /// All job slots reported by the device.
    /// </summary>
    public ObservableCollection<JObject> JobSlots { get; } = [];

    /// <summary>
    /// Currently selected job slot object.
    /// </summary>
    [ObservableProperty]
    private JObject selectedJobSlot;

    partial void OnSelectedJobSlotChanged(JObject value)
    {
        if (value == null)
        {
            userChange = false;
            return;
        }

        if (userChange)
        {
            userChange = false;
            return;
        }

        _ = Application.Current.Dispatcher.BeginInvoke(() => ChangeJob(value));
    }

    /// <summary>
    /// Current job name (synced to the active job slot selection).
    /// </summary>
    [ObservableProperty]
    private string jobName = "";

    partial void OnJobNameChanged(string value)
    {
        if (JobSlots == null)
        {
            SelectedJobSlot = null;
            return;
        }

        var jb = JobSlots.FirstOrDefault(e => e["jobName"].ToString() == JobName);
        if (jb != null && SelectedJobSlot != jb)
        {
            userChange = true;
            SelectedJobSlot = jb;
        }
    }

    /// <summary>
    /// Available acquisition source directories (when using File acquisition).
    /// </summary>
    public ObservableCollection<string> Directories { get; set; } = [];

    /// <summary>
    /// Currently selected acquisition directory.
    /// </summary>
    [ObservableProperty]
    private string selectedDirectory;

    partial void OnSelectedDirectoryChanged(string value) => ChangeDirectory(value);

    /// <summary>
    /// The logical acquisition modes supported.
    /// </summary>
    public List<string> AcquisitionTypes => ["File", "Camera"];

    /// <summary>
    /// Selected acquisition type string.
    /// </summary>
    [ObservableProperty]
    private string selectedAcquisitionType;

    partial void OnSelectedAcquisitionTypeChanged(string value) =>
        SwitchAquisitionType(value == "File");

    #endregion

    #region Camera / ROI Helpers

    /// <summary>
    /// List of all supported camera model descriptors.
    /// </summary>
    public static List<CameraDetails> AvailableCameras => CameraModels.Available;

    /// <summary>
    /// Currently detected (matched) camera meta from SysInfo.
    /// </summary>
    [ObservableProperty]
    private CameraDetails selectedCamera;

    partial void OnSelectedCameraChanged(CameraDetails value) =>
        _ = Application.Current.Dispatcher.BeginInvoke(UpdateFocusOverlay);

    /// <summary>
    /// Computed QuickSet Photometry ROI as centered rectangle in sensor coordinates.
    /// </summary>
    private JObject QuickSet_Photometry
    {
        get
        {
            if (SelectedCamera == null)
                return null;

            var width = SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent;
            var height = SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent;
            var x = (SelectedCamera.Sensor.PixelColumns - width) / 2;
            var y = (SelectedCamera.Sensor.PixelRows - height) / 2;

            return JObject.FromObject(new QuickSet_Photometry((float)x, (float)y, (float)width, (float)height));
        }
    }

    /// <summary>
    /// Computed QuickSet Focus ROI as centered rectangle in sensor coordinates.
    /// </summary>
    private JObject QuickSet_Focus
    {
        get
        {
            if (SelectedCamera == null)
                return null;

            var width = SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent;
            var height = SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent;
            var x = (SelectedCamera.Sensor.PixelColumns - width) / 2;
            var y = (SelectedCamera.Sensor.PixelRows - height) / 2;

            return JObject.FromObject(new QuickSet_Focus((float)x, (float)y, (float)width, (float)height));
        }
    }

    #endregion

    #region Results / Messages

    /// <summary>
    /// Collection of parsed decode or qualification results from the last cycle.
    /// </summary>
    public ObservableCollection<JToken> Results { get; } = [];

    /// <summary>
    /// Accumulated textual event log (if implemented externally).
    /// </summary>
    [ObservableProperty]
    private string eventMessages;

    /// <summary>
    /// Raw JSON strings or diagnostics returned by explicit calls (Config, SysInfo, Reports).
    /// </summary>
    [ObservableProperty]
    private string explicitMessages;

    #endregion

    #region Image Rolls

    /// <summary>
    /// The currently active user-selected image roll to which images can be added.
    /// </summary>
    [ObservableProperty]
    private ImageRoll activeImageRoll;



    /// <summary>
    /// Optional manager reference set externally (lifecycle/controller aggregator).
    /// </summary>
    public ScannerManager Manager { get; set; }

    #endregion

    #region Public Commands

    /// <summary>
    /// Connect or disconnect from the device. On disconnect resets job context.
    /// </summary>
    [RelayCommand]
    private async Task Connect()
    {
        if (!Controller.IsConnected)
        {
            if (await Task.Run(Controller.Connect))
                return;

            await Task.Run(Controller.Disconnect);
        }
        else
        {
            await Task.Run(Controller.Disconnect);
        }

        PostLogout();
    }

    /// <summary>
    /// Trigger a capture once or repeatedly (if RepeatTrigger set).
    /// Repeated triggers honor <see cref="RepeatedTriggerDelay"/> indirectly via controller logic.
    /// </summary>
    [RelayCommand]
    private Task Trigger()
    {
        if (_tokenSrc != null)
        {
            _tokenSrc.Cancel();
            return Task.CompletedTask;
        }

        Clear();

        if (RepeatTrigger)
        {
            _tokenSrc = new CancellationTokenSource();
            return Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    while (RepeatTrigger && !_tokenSrc.IsCancellationRequested)
                    {
                        if (await Controller.Trigger_Wait() != true)
                            _tokenSrc.Token.ThrowIfCancellationRequested();
                    }
                    _tokenSrc.Token.ThrowIfCancellationRequested();
                }
                finally
                {
                    CheckOverlay();
                    _tokenSrc = null;
                }
            });
        }

        _ = Controller.Commands.Trigger();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes QuickSet focus operation using the centered ROI.
    /// </summary>
    [RelayCommand]
    private async Task QuickSetFocus()
    {
        Clear();
        _ = await Controller.QuickSet_Focus_Wait(QuickSet_Focus);
    }

    /// <summary>
    /// Executes QuickSet photometry operation using the centered ROI.
    /// </summary>
    [RelayCommand]
    private async Task QuickSetPhotometry()
    {
        Clear();
        _ = await Controller.QuickSet_Photometry_Wait(QuickSet_Photometry);
    }

    /// <summary>
    /// Retrieves SysInfo and stores raw JSON in <see cref="ExplicitMessages"/>.
    /// </summary>
    [RelayCommand]
    private async Task SysInfo()
    {
        Clear();
        var res = await Controller.Commands.GetSysInfo();
        if (res.OK)
            ExplicitMessages = res.Json;
    }

    /// <summary>
    /// Retrieves Config and stores raw JSON in <see cref="ExplicitMessages"/>.
    /// </summary>
    [RelayCommand]
    private async Task Config()
    {
        Clear();
        var res = await Controller.Commands.GetConfig();
        if (res.OK)
            ExplicitMessages = res.Json;
    }

    /// <summary>
    /// Switches controller to RUN mode.
    /// </summary>
    [RelayCommand]
    private async Task SwitchRun() => await Controller.Commands.ModeRun();

    /// <summary>
    /// Switches controller to EDIT (configuration) mode.
    /// </summary>
    [RelayCommand]
    public async Task SwitchEdit() => await Controller.SwitchToEdit();

    /// <summary>
    /// Adds the current image to the active image roll (if writable).
    /// </summary>
    [RelayCommand]
    private void AddToImageRoll()
    {
        if (ActiveImageRoll == null)
        {
            Logger.Warning("No image roll selected.");
            return;
        }

        if (ActiveImageRoll.RollType == ImageRollTypes.Directory)
        {
            Logger.Warning("Cannot add to a directory based image roll.");
            return;
        }

        if (ActiveImageRoll.IsLocked)
        {
            Logger.Warning("Cannot add to a locked image roll.");
            return;
        }

        if (RawImage == null)
        {
            Logger.Warning("No image to add.");
            return;
        }

        var imagEntry = ActiveImageRoll.GetImageEntry(RawImage);
        ActiveImageRoll.AddImage(ImageAddPositions.Top, imagEntry.entry);
    }

    /// <summary>
    /// Opens the device web interface in the default browser.
    /// Shift key chooses alternate port.
    /// </summary>
    [RelayCommand]
    private void OpenInBrowser()
    {
        var addr = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
            ? $"http://{Controller.Host}:9898"
            : $"http://{Controller.Host}:{Controller.Port}";

        ProcessStartInfo ps = new(addr)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        _ = Process.Start(ps);
    }

    #endregion

    #region Messenger Integration

    /// <summary>
    /// Messenger callback for active <see cref="ImageRoll"/> changes.
    /// </summary>
    public void Receive(PropertyChangedMessage<ImageRoll> message) =>
        ActiveImageRoll = message.NewValue;

    #endregion

    #region Controller Event Handlers

    private void Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "Config":
                ScannerController_ConfigUpdate();
                break;
            case "SysInfo":
                ScannerController_SysInfoUpdate();
                break;
            case "Image":
                ScannerController_ImageUpdate(Controller.Image);
                break;
            case "Report":
                ScannerController_ReportUpdate(Controller.Report);
                break;
            case "JobSlots":
                ScannerController_JobSlotsUpdate();
                break;
        }
    }

    /// <summary>
    /// Handles controller Config updates: directories, acquisition type, job name, camera selection.
    /// </summary>
    private async void ScannerController_ConfigUpdate()
    {
        if (Controller.IsConfigValid)
        {
            var meta = await Controller.Commands.GetMeta();
            if (meta.OK)
            {
                var metaConfig = (JObject)meta.Object;
                var sources = metaConfig.GetParameter<JArray>("response.data.FileAcquisitionSource.directory.sources");

                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (sources != null)
                    {
                        // Add any missing
                        foreach (var source in sources)
                            if (!Directories.Contains(source.ToString()))
                                Directories.Add(source.ToString());

                        // Remove stale
                        for (var i = Directories.Count - 1; i >= 0; i--)
                            if (!sources.Values().Contains(Directories[i]))
                                Directories.RemoveAt(i);
                    }
                });
            }

            if (Controller.IsSimulator)
            {
                SelectedAcquisitionType = "File";
                SelectedDirectory = Controller.Config.GetParameter<string>("response.data.job.channelMap.acquisition.AcquisitionChannel.source.FileAcquisitionSource.directory");
            }
            else
            {
                SelectedAcquisitionType = "Camera";
            }

            // Force re-selection update (two-step ensures OnJobNameChanged logic engages)
            JobName = "";
            JobName = Controller.Config.GetParameter<string>("response.data.job.name");
        }
        else
        {
            Logger.Error("V5 Config update but Config is invalid.");
        }

        if (Controller.IsSysInfoValid)
            UpdateSelectedCameraFromSysInfo();
        else
            Logger.Error("V5 Config update but SysInfo is invalid.");
    }

    /// <summary>
    /// Handles controller SysInfo updates (camera selection).
    /// </summary>
    private void ScannerController_SysInfoUpdate()
    {
        if (Controller.IsSysInfoValid)
            UpdateSelectedCameraFromSysInfo();
        else
            Logger.Error("V5 Config update but SysInfo is invalid.");
    }

    /// <summary>
    /// Handles Report JSON updates; extracts cycle IDs, capture / results collections.
    /// Supports both "cycle-report" and "cycle-report-alt".
    /// </summary>
    private void ScannerController_ReportUpdate(JObject json)
    {
        if (json == null) return;

        try
        {
            ResultsJObject = json;
            ExplicitMessages = JsonConvert.SerializeObject(json);

            var evtName = json["event"]?["name"]?.ToString();
            if (evtName == "cycle-report-alt")
            {
                CycleID = json["event"]?["data"]?["cycleId"]?.ToString();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Results.Clear();
                    var data1 = json["event"]?["data"]?["decodeData"];
                    if (data1 != null && data1.HasValues)
                        foreach (var b in data1)
                            Results.Add(b);
                });
            }
            else if (evtName == "cycle-report")
            {
                CycleID = json["event"]?["data"]?["cycleConfig"]?["cycleId"]?.ToString();

                var data1 = json["event"]?["data"]?["cycleConfig"]?["job"]?["captureList"];
                if (data1 != null && data1.HasValues)
                    Capture = data1[0]?["Capture"];

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Results.Clear();
                    var qualified = json["event"]?["data"]?["cycleConfig"]?["qualifiedResults"];
                    if (qualified != null && qualified.HasValues)
                        foreach (var b in qualified)
                            Results.Add(b);
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    /// <summary>
    /// Handles raw image JSON (partial/full resolution).
    /// </summary>
    private async void ScannerController_ImageUpdate(JObject json)
    {
        if (json == null)
        {
            RawImage = null;
            return;
        }

        if (FullResImages)
        {
            try
            {
                RawImage = await Controller.GetImageFullRes(json);
            }
            catch
            {
                RawImage = null;
            }
        }
        else
        {
            try
            {
                RawImage = (byte[])json["msgData"]?["images"]?[0]?["imgData"];
            }
            catch
            {
                RawImage = null;
            }
        }
    }

    /// <summary>
    /// Handles job slots updates (add/remove diff).
    /// </summary>
    private void ScannerController_JobSlotsUpdate()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(ScannerController_JobSlotsUpdate);
            return;
        }

        if (Controller.IsJobSlotsValid)
        {
            var srcArray = Controller.JobSlots.GetParameter<JArray>("response.data");
            foreach (JObject job in srcArray)
            {
                var jName = job.GetParameter<string>("jobName");
                var jSlot = job.GetParameter<string>("slotIndex");
                if (!JobSlots.Any(e =>
                        e.GetParameter<string>("jobName") == jName &&
                        e.GetParameter<string>("slotIndex") == jSlot))
                {
                    JobSlots.Add(job);
                }
            }

            // Remove stale
            foreach (var job in JobSlots.ToArray())
                if (!srcArray.Any(e =>
                        e["jobName"]?.ToString() == job["jobName"]?.ToString() &&
                        e["slotIndex"]?.ToString() == job["slotIndex"]?.ToString()))
                    _ = JobSlots.Remove(job);
        }
        else
        {
            JobSlots.Clear();
        }
    }

    #endregion

    #region Acquisition / Directory Switching

    /// <summary>
    /// Requests a change in acquisition type on the controller (File vs Camera).
    /// </summary>
    private async void SwitchAquisitionType(bool file)
    {
        if (!await Controller.SwitchAquisitionType(file, SelectedDirectory ?? Directories.First()))
            Logger.Error("Could not switch acquisition type.");
    }

    /// <summary>
    /// Applies new directory selection to the FileAcquisitionSource if config valid.
    /// </summary>
    private async void ChangeDirectory(string directory)
    {
        if (!Controller.IsConfigValid)
        {
            Logger.Error("Could not get scanner configuration.");
            return;
        }

        var source = Controller.Config.GetParameter<JObject>("response.data.job.channelMap.acquisition.AcquisitionChannel.source");
        if (source == null)
        {
            Logger.Error("Could not get the source object from the configuration.");
            return;
        }

        if (source.GetParameter<string>("FileAcquisitionSource.directory") != directory)
        {
            _ = source.SetParameter("FileAcquisitionSource.directory", directory);
            var send = Controller.Config.GetParameter<JObject>("response.data");
            if (send == null)
            {
                Logger.Error("Could not get the data object from the configuration.");
                return;
            }

            _ = await Controller.SetConfig(send);
        }
    }

    #endregion

    #region Helper Methods (Overlay, Image, Camera Matching)

    /// <summary>
    /// Builds a <see cref="BitmapImage"/> from a raw byte array.
    /// </summary>
    private BitmapImage GetImage(byte[] raw)
    {
        BitmapImage img = new();
        using (MemoryStream memStream = new(raw))
        {
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = memStream;
            img.EndInit();
            img.Freeze();
        }
        return img;
    }

    /// <summary>
    /// Updates both overlay layers (decode + focus ROI) respecting current states.
    /// </summary>
    private void CheckOverlay()
    {
        ImageOverlay = Results.Count > 0 ? V5CreateSectorsImageOverlay(ResultsJObject) : null;
        ImageFocusRegionOverlay = Controller.IsSimulator ? null : CreateFocusRegionOverlay();
    }

    /// <summary>
    /// Regenerates focus region overlay (invoked on camera / ROI changes).
    /// </summary>
    private void UpdateFocusOverlay()
    {
        if (Controller.IsSimulator)
            ImageFocusRegionOverlay = null;
        else
            ImageFocusRegionOverlay = CreateFocusRegionOverlay();
    }

    /// <summary>
    /// Creates overlay to display centered ROI used by QuickSet operations.
    /// </summary>
    private DrawingImage CreateFocusRegionOverlay()
    {
        if (QuickSet_Photometry == null || Image == null)
            return null;

        DrawingGroup drwGroup = new();

        // Transparent border (ensures coordinates align with image edges).
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, Image.PixelWidth - 1, Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        GeometryGroup secAreas = new();
        var div = FullResImages ? 1 : 2;

        secAreas.Children.Add(new RectangleGeometry(
            new Rect(
                new Point(QuickSet_Photometry.GetParameter<double>("photometry.roi[0]") / div, QuickSet_Photometry.GetParameter<double>("photometry.roi[1]") / div),
                new Point(
                    QuickSet_Photometry.GetParameter<double>("photometry.roi[0]") + (QuickSet_Photometry.GetParameter<double>("photometry.roi[2]") / div),
                    QuickSet_Photometry.GetParameter<double>("photometry.roi[1]") + (QuickSet_Photometry.GetParameter<double>("photometry.roi[3]") / div))
            )));

        GeometryDrawing sectors = new()
        {
            Geometry = secAreas,
            Pen = new Pen(Brushes.DarkRed, 5)
        };

        drwGroup.Children.Add(sectors);

        DrawingImage geometryImage = new(drwGroup);
        geometryImage.Freeze();
        return geometryImage;
    }

    /// <summary>
    /// Creates overlay for decode bounding boxes / tool regions.
    /// </summary>
    private DrawingImage V5CreateSectorsImageOverlay(JObject results)
    {
        if (results == null || Image == null)
            return null;

        DrawingGroup drwGroup = new();
        var div = FullResImages ? 1 : 2;

        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, Image.PixelWidth - 1, Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        GeometryGroup secCenter = new();
        GeometryGroup bndAreas = new();

        var evtName = results["event"]?["name"]?.ToString();

        if (evtName == "cycle-report-alt")
        {
            foreach (var sec in results["event"]?["data"]?["decodeData"])
            {
                if (sec["boundingBox"] == null)
                    continue;

                GeometryGroup secAreas = new();
                var brushWidth = 4.0 / div;
                var halfBrushWidth = brushWidth / 2.0 / div;

                for (var i = 0; i < 4; i++)
                {
                    var nextIndex = (i + 1) % 4;

                    var dx = (sec["boundingBox"][nextIndex]["x"].Value<double>() - sec["boundingBox"][i]["x"].Value<double>()) / div;
                    var dy = (sec["boundingBox"][nextIndex]["y"].Value<double>() - sec["boundingBox"][i]["y"].Value<double>()) / div;
                    var length = Math.Sqrt((dx * dx) + (dy * dy));
                    var ux = dx / length;
                    var uy = dy / length;

                    // Normal vector
                    var nx = -uy;
                    var ny = ux;

                    var ax = nx * halfBrushWidth;
                    var ay = ny * halfBrushWidth;

                    var startX = (sec["boundingBox"][i]["x"].Value<double>() - ax) / div;
                    var startY = (sec["boundingBox"][i]["y"].Value<double>() - ay) / div;
                    var endX = (sec["boundingBox"][nextIndex]["x"].Value<double>() - ax) / div;
                    var endY = (sec["boundingBox"][nextIndex]["y"].Value<double>() - ay) / div;

                    secAreas.Children.Add(new LineGeometry(new Point(startX, startY), new Point(endX, endY)));
                }

                drwGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Red, 4 / div), secAreas));

                drwGroup.Children.Add(new GlyphRunDrawing(
                    Brushes.Black,
                    CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0 / div,
                        new Point((sec["boundingBox"][2]["x"].Value<double>() - 8) / div,
                                  (sec["boundingBox"][2]["y"].Value<double>() - 8) / div))));

                secCenter.Children.Add(new LineGeometry(
                    new Point((sec["x"].Value<double>() + 10) / div, sec["y"].Value<double>() / div),
                    new Point((sec["x"].Value<double>() - 10) / div, sec["y"].Value<double>() / div)));
                secCenter.Children.Add(new LineGeometry(
                    new Point(sec["x"].Value<double>() / div, (sec["y"].Value<double>() + 10) / div),
                    new Point(sec["x"].Value<double>() / div, (sec["y"].Value<double>() - 10) / div)));
            }
        }
        else if (evtName == "cycle-report")
        {
            foreach (var sec in results["event"]?["data"]?["cycleConfig"]?["qualifiedResults"])
            {
                if (sec["boundingBox"] == null)
                    continue;

                GeometryGroup secAreas = new();
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div),
                    new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div),
                    new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div),
                    new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div),
                    new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div)));

                drwGroup.Children.Add(new GlyphRunDrawing(
                    Brushes.Black,
                    CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0 / div,
                        new Point((sec["boundingBox"][2]["x"].Value<double>() - 8) / div,
                                  (sec["boundingBox"][2]["y"].Value<double>() - 8) / div))));
            }

            foreach (var sec in results["event"]?["data"]?["cycleConfig"]?["job"]?["toolList"])
                foreach (var r in sec["SymbologyTool"]["regionList"])
                    bndAreas.Children.Add(new RectangleGeometry(
                        new Rect(
                            r["Region"]["shape"]["RectShape"]["x"].Value<double>() / div,
                            r["Region"]["shape"]["RectShape"]["y"].Value<double>() / div,
                            r["Region"]["shape"]["RectShape"]["width"].Value<double>() / div,
                            r["Region"]["shape"]["RectShape"]["height"].Value<double>() / div)
                    ));
        }

        GeometryDrawing sectorCenters = new()
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4 / div)
        };
        GeometryDrawing bounding = new()
        {
            Geometry = bndAreas,
            Pen = new Pen(Brushes.Purple, 4 / div)
        };

        drwGroup.Children.Add(bounding);
        drwGroup.Children.Add(sectorCenters);

        DrawingImage geometryImage = new(drwGroup);
        geometryImage.Freeze();
        return geometryImage;
    }

    /// <summary>
    /// Creates a glyph run used for labeling overlays.
    /// </summary>
    public static GlyphRun CreateGlyphRun(string text, Typeface typeface, double emSize, Point baselineOrigin)
    {
        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
            throw new ArgumentException($"{typeface.FontFamily}: no GlyphTypeface found");

        var glyphIndices = new ushort[text.Length];
        var advanceWidths = new double[text.Length];

        for (var i = 0; i < text.Length; i++)
        {
            var glyphIndex = glyphTypeface.CharacterToGlyphMap[text[i]];
            glyphIndices[i] = glyphIndex;
            advanceWidths[i] = glyphTypeface.AdvanceWidths[glyphIndex] * emSize;
        }

        return new GlyphRun(
            glyphTypeface, 0, false, emSize, (float)MonitorUtilities.GetDpi().PixelsPerDip,
            glyphIndices, baselineOrigin, advanceWidths,
            null, null, null, null, null, null);
    }

    /// <summary>
    /// Attempts to match and set <see cref="SelectedCamera"/> based on current SysInfo.
    /// </summary>
    private void UpdateSelectedCameraFromSysInfo()
    {
        SelectedCamera = AvailableCameras.FirstOrDefault(e =>
            Controller.SysInfo.GetParameter<string>("response.data.hwal.lens.lensName")
                .StartsWith(e.FocalLength.ToString()) &&
            Controller.SysInfo.GetParameter<string>("response.data.hwal.sensor.description")
                .StartsWith(e.Sensor.PixelCount.ToString()));

        if (SelectedCamera == null)
            Logger.Error("Could not find a camera matching the current lens and sensor.");
    }

    #endregion

    #region Job / Mode Helpers

    /// <summary>
    /// Called post-log-out to reset job state UI.
    /// </summary>
    private void PostLogout()
    {
        JobSlots.Clear();
        SelectedJobSlot = null;
        JobName = "";
    }

    /// <summary>
    /// Sends the controller a request to change job slot.
    /// </summary>
    private async Task ChangeJob(JObject job) => await Controller.ChangeJobSlot(job);

    #endregion

    #region Utility

    /// <summary>
    /// Clears all transient state related to current capture / results.
    /// </summary>
    private void Clear()
    {
        RawImage = null;
        ImageOverlay = null;
        ImageFocusRegionOverlay = null;

        ResultsJObject = null;
        Results.Clear();
        ExplicitMessages = null;
        Capture = null;
    }

    #endregion
}