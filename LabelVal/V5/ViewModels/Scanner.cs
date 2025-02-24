using Cameras_Lib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using V5_REST_Lib.Cameras;
using V5_REST_Lib.Models;

namespace LabelVal.V5.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class Scanner : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    [JsonProperty] public long ID { get; set; } = DateTime.Now.Ticks;
    [JsonProperty] public V5_REST_Lib.Controller Controller { get; } = new();

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
    private int repeatedTriggerDelay = 50;

    [ObservableProperty][property: JsonProperty] private double quickSet_ImagePercent = 0.33d;
    partial void OnQuickSet_ImagePercentChanged(double value) => _ = App.Current.Dispatcher.BeginInvoke(() =>
    {
        if (Controller.IsSimulator)
            ImageFocusRegionOverlay = null;
        else
            ImageFocusRegionOverlay = CreateFocusRegionOverlay();
    });

    [ObservableProperty][property: JsonProperty] private bool fullResImages = false;

    [ObservableProperty] private string eventMessages;
    [ObservableProperty] private string explicitMessages;

    [ObservableProperty] private byte[] rawImage;
    partial void OnRawImageChanged(byte[] value) { Image = value == null ? null : GetImage(value); }

    [ObservableProperty] private BitmapImage image;
    [ObservableProperty] private DrawingImage imageOverlay;
    [ObservableProperty] private DrawingImage imageFocusRegionOverlay;

    public static List<CameraDetails> AvailableCameras => CameraModels.Available;

    [ObservableProperty] private CameraDetails selectedCamera;
    partial void OnSelectedCameraChanged(CameraDetails value) => _ = App.Current.Dispatcher.BeginInvoke(() =>
    {
        if (Controller.IsSimulator)
            ImageFocusRegionOverlay = null;
        else
            ImageFocusRegionOverlay = CreateFocusRegionOverlay();
    });

    private QuickSet_Photometry QuickSet_Photometry
    {
        get
        {
            if (SelectedCamera == null)
                return null;

            double width = SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent;
            double height = SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent;

            double x = (SelectedCamera.Sensor.PixelColumns - width) / 2;
            double y = (SelectedCamera.Sensor.PixelRows - height) / 2;

            return new QuickSet_Photometry((float)x, (float)y, (float)width, (float)height);
        }
    }
    private QuickSet_Focus QuickSet_Focus
    {
        get
        {
            if (SelectedCamera == null)
                return null;

            double width = SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent;
            double height = SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent;
            double x = (SelectedCamera.Sensor.PixelColumns - width) / 2;
            double y = (SelectedCamera.Sensor.PixelRows - height) / 2;

            return new QuickSet_Focus((float)x, (float)y, (float)width, (float)height);
        }
    }

    [ObservableProperty] private ImageRollEntry selectedImageRoll;

    [ObservableProperty] private bool repeatTrigger;

    [ObservableProperty] private JToken capture;
    [ObservableProperty] private string cycleID;
    public ObservableCollection<JToken> Results { get; } = [];
    private JObject ResultsJObject;

    private bool IsWaitingForFullImage;

    public ObservableCollection<JobSlots.Datum> JobSlots { get; } = [];
    [ObservableProperty] private JobSlots.Datum selectedJobSlot;
    partial void OnSelectedJobSlotChanged(JobSlots.Datum value)
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

        _ = App.Current.Dispatcher.BeginInvoke(() => ChangeJob(value));
    }

    private bool userChange;
    [ObservableProperty] private string jobName = "";
    partial void OnJobNameChanged(string value)
    {
        if (JobSlots == null)
        {
            SelectedJobSlot = null;
            return;
        }

        var jb = JobSlots.FirstOrDefault((e) => e.jobName == JobName);

        if (jb != null)
        {
            if (SelectedJobSlot != jb)
            {
                userChange = true;
                SelectedJobSlot = jb;
            }
        }
    }

    public ObservableCollection<string> Directories { get; set; } = [];
    [ObservableProperty] private string selectedDirectory;
    partial void OnSelectedDirectoryChanged(string value) => ChangeDirectory(value);

    public List<string> AcquisitionTypes => ["File", "Camera"];
    [ObservableProperty] private string selectedAcquisitionType;
    partial void OnSelectedAcquisitionTypeChanged(string value) => SwitchAquisitionType(value == "File");

    private async void SwitchAquisitionType(bool file)
    {
        if (!await Controller.SwitchAquisitionType(file, SelectedDirectory ?? Directories.First()))
            LogError("Could not switch acquisition type.");
    }
    private async void ChangeDirectory(string directory)
    {
        if (!Controller.IsConfigValid)
        {
            LogError("Could not get scanner configuration.");
            return;
        }

        Config.Source src = Controller.Config.response.data.job.channelMap.acquisition.AcquisitionChannel.source;

        if (src.FileAcquisitionSource.directory != directory)
        {
            src.FileAcquisitionSource.directory = directory;
            _ = await Controller.SetConfig(Controller.Config.response.data);
        }
    }

    public ScannerManager Manager { get; set; }
    public Scanner()
    {
        Controller.PropertyChanged += Controller_PropertyChanged;
        IsActive = true;
    }

    private void Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Config")
            ScannerController_ConfigUpdate();
        else if (e.PropertyName == "SysInfo")
            ScannerController_SysInfoUpdate();
        else if (e.PropertyName == "Image")
            ScannerController_ImageUpdate(Controller.Image);
        else if (e.PropertyName == "Report")
            ScannerController_ReportUpdate(Controller.Report);
        else if (e.PropertyName == "JobSlots")
            ScannerController_JobSlotsUpdate();

    }
    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => SelectedImageRoll = message.NewValue;

    private async void ScannerController_ConfigUpdate()
    {
        if (Controller.IsConfigValid)
        {
            V5_REST_Lib.Results meta = await Controller.Commands.GetMeta();
            if (meta.OK)
            {
                Meta metaConfig = (Meta)meta.Object;

                // Assuming metaConfig.response.data.FileAcquisitionSource.directory.sources is an array of strings
                string[] sources = metaConfig.response.data.FileAcquisitionSource.directory.sources;

                await App.Current.Dispatcher.BeginInvoke(() =>
                {
                    // Add new directories from sources to Directories if they're not already present
                    foreach (string source in sources)
                        if (!Directories.Contains(source))
                            Directories.Add(source);

                    // Remove directories from Directories if they're not present in sources
                    for (int i = Directories.Count - 1; i >= 0; i--)
                        if (!sources.Contains(Directories[i]))
                            Directories.RemoveAt(i);
                });
            }

            if (Controller.IsSimulator)
            {
                SelectedAcquisitionType = "File";
                SelectedDirectory = Controller.Config.response.data.job.channelMap.acquisition.AcquisitionChannel.source.FileAcquisitionSource.directory;
            }
            else
            {
                SelectedAcquisitionType = "Camera";
            }

            JobName = "";
            JobName = Controller.Config.response.data.job.name;
        }
        else
            LogError("V5 Config update but Config is invalid.");

        if (Controller.IsSysInfoValid)
        {
            SelectedCamera = AvailableCameras.FirstOrDefault((e) => Controller.SysInfo.response.data.hwal.lens.lensName.StartsWith(e.FocalLength.ToString()) && Controller.SysInfo.response.data.hwal.sensor.description.StartsWith(e.Sensor.PixelCount.ToString()));
            if (SelectedCamera == null)
                LogError("Could not find a camera matching the current lens and sensor.");
        }
        else
            LogError("V5 Config update but SysInfo is invalid.");
    }
    private void ScannerController_SysInfoUpdate()
    {
        if (Controller.IsSysInfoValid)
        {
            SelectedCamera = AvailableCameras.FirstOrDefault((e) => Controller.SysInfo.response.data.hwal.lens.lensName.StartsWith(e.FocalLength.ToString()) && Controller.SysInfo.response.data.hwal.sensor.description.StartsWith(e.Sensor.PixelCount.ToString()));
            if (SelectedCamera == null)
                LogError("Could not find a camera matching the current lens and sensor.");
        }
        else
            LogError("V5 Config update but SysInfo is invalid.");
    }
    private void ScannerController_ReportUpdate(JObject json)
    {
        if (json != null)
        {
            try
            {
                ResultsJObject = json;

                ExplicitMessages = JsonConvert.SerializeObject(json);

                if (json["event"]?["name"].ToString() == "cycle-report-alt")
                {
                    CycleID = json["event"]?["data"]?["cycleId"].ToString();

                    //var data1 = json["event"]?["data"]?["job"]?["captureList"];
                    //if (data1 != null)
                    //    if (data1.HasValues)
                    //        Capture = data1[0]?["Capture"];

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Results.Clear();

                        JToken data1 = json["event"]?["data"]?["decodeData"];
                        if (data1 != null)
                            if (data1.HasValues)
                                foreach (JToken b in data1)
                                    Results.Add(b);
                    });
                }
                else if (json["event"]?["name"].ToString() == "cycle-report")
                {
                    CycleID = json["event"]?["data"]?["cycleConfig"]?["cycleId"].ToString();

                    JToken data1 = json["event"]?["data"]?["cycleConfig"]?["job"]?["captureList"];
                    if (data1 != null)
                        if (data1.HasValues)
                            Capture = data1[0]?["Capture"];

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Results.Clear();

                        data1 = json["event"]?["data"]?["cycleConfig"]?["qualifiedResults"];
                        if (data1 != null)
                            if (data1.HasValues)
                                foreach (JToken b in data1)
                                    Results.Add(b);
                    });
                }
            }
            catch (Exception ex) { LogError(ex); }
        }
    }
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
                RawImage = LibImageUtilities.ImageTypes.Png.Utilities.GetPng(await Controller.GetImageFullRes(json));
            }
            catch { RawImage = null; }
        }
        else
        {
            try
            {
                RawImage = LibImageUtilities.ImageTypes.Png.Utilities.GetPng((byte[])json["msgData"]?["images"]?[0]?["imgData"]);
            }
            catch { RawImage = null; }
        }
    }

    private void ScannerController_JobSlotsUpdate()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(ScannerController_JobSlotsUpdate);
            return;
        }

        if (Controller.IsJobSlotsValid)
        {
            foreach (JobSlots.Datum job in Controller.JobSlots.response.data)
                if (!JobSlots.Any((e) => e.jobName == job.jobName && e.slotIndex == job.slotIndex))
                    JobSlots.Add(job);

            foreach (JobSlots.Datum job in JobSlots.ToArray())
                if (!Controller.JobSlots.response.data.Any((e) => e.jobName == job.jobName && e.slotIndex == job.slotIndex))
                    _ = JobSlots.Remove(job);
        }
        else
            JobSlots.Clear();
    }

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

    private void CheckOverlay()
    {
        ImageOverlay = Results.Count > 0 ? V5CreateSectorsImageOverlay(ResultsJObject) : null;
        ImageFocusRegionOverlay = Controller.IsSimulator ? null : CreateFocusRegionOverlay();
    }
    private DrawingImage CreateFocusRegionOverlay()
    {
        if (QuickSet_Photometry == null || Image == null)
            return null;

        DrawingGroup drwGroup = new();

        //Draw the image outline the same size as the stored image
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new System.Windows.Rect(0.5, 0.5, Image.PixelWidth - 1, Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        GeometryGroup secAreas = new();

        int div = FullResImages ? 1 : 2;

        secAreas.Children.Add(new RectangleGeometry(
            new Rect(
                new Point(QuickSet_Photometry.photometry.roi[0] / div, QuickSet_Photometry.photometry.roi[1] / div),
                new Point(QuickSet_Photometry.photometry.roi[0] + (QuickSet_Photometry.photometry.roi[2] / div), QuickSet_Photometry.photometry.roi[1] + (QuickSet_Photometry.photometry.roi[3] / div))
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

    private DrawingImage V5CreateSectorsImageOverlay(JObject results)
    {
        if (results == null || Image == null)
            return null;

        DrawingGroup drwGroup = new();

        int div = FullResImages ? 1 : 2;

        //Draw the image outline the same size as the stored image
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, Image.PixelWidth - 1, Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        GeometryGroup secCenter = new();
        GeometryGroup bndAreas = new();

        if (results["event"]?["name"].ToString() == "cycle-report-alt")
        {
            foreach (JToken sec in results["event"]?["data"]?["decodeData"])
            {
                if (sec["boundingBox"] == null)
                    continue;

                GeometryGroup secAreas = new();

                double brushWidth = 4.0 / div;
                double halfBrushWidth = brushWidth / 2.0 / div;

                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;

                    double dx = (sec["boundingBox"][nextIndex]["x"].Value<double>() - sec["boundingBox"][i]["x"].Value<double>()) / div;
                    double dy = (sec["boundingBox"][nextIndex]["y"].Value<double>() - sec["boundingBox"][i]["y"].Value<double>()) / div;

                    // Calculate the length of the line segment
                    double length = Math.Sqrt((dx * dx) + (dy * dy));

                    // Normalize the direction to get a unit vector
                    double ux = dx / length;
                    double uy = dy / length;

                    // Calculate the normal vector (perpendicular to the direction)
                    double nx = -uy;
                    double ny = ux;

                    // Calculate the adjustment vector
                    double ax = nx * halfBrushWidth;
                    double ay = ny * halfBrushWidth;

                    // Adjust the points
                    double startX = (sec["boundingBox"][i]["x"].Value<double>() - ax) / div;
                    double startY = (sec["boundingBox"][i]["y"].Value<double>() - ay) / div;
                    double endX = (sec["boundingBox"][nextIndex]["x"].Value<double>() - ax) / div;
                    double endY = (sec["boundingBox"][nextIndex]["y"].Value<double>() - ay) / div;

                    // Add the line to the geometry group
                    secAreas.Children.Add(new LineGeometry(new Point(startX, startY), new Point(endX, endY)));
                }

                drwGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Red, 4 / div), secAreas));

                drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0 / div, new Point((sec["boundingBox"][2]["x"].Value<double>() - 8) / div, (sec["boundingBox"][2]["y"].Value<double>() - 8) / div))));

                secCenter.Children.Add(new LineGeometry(new Point((sec["x"].Value<double>() + 10) / div, sec["y"].Value<double>() / div), new Point((sec["x"].Value<double>() + -10) / div, sec["y"].Value<double>() / div)));
                secCenter.Children.Add(new LineGeometry(new Point(sec["x"].Value<double>() / div, (sec["y"].Value<double>() + 10) / div), new Point(sec["x"].Value<double>() / div, (sec["y"].Value<double>() + -10) / div)));
            }
        }
        else if (results["event"]?["name"].ToString() == "cycle-report")
        {

            foreach (JToken sec in results["event"]["data"]["cycleConfig"]["qualifiedResults"])
            {
                if (sec["boundingBox"] == null)
                    continue;
                GeometryGroup secAreas = new();
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div), new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div), new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div), new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div), new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div)));

                drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0 / div, new Point((sec["boundingBox"][2]["x"].Value<double>() - 8) / div, (sec["boundingBox"][2]["y"].Value<double>() - 8) / div))));
            }

            foreach (JToken sec in results["event"]["data"]["cycleConfig"]["job"]["toolList"])
                foreach (JToken r in sec["SymbologyTool"]["regionList"])
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
    public static GlyphRun CreateGlyphRun(string text, Typeface typeface, double emSize, Point baselineOrigin)
    {

        if (!typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
        {
            throw new ArgumentException(string.Format(
                "{0}: no GlyphTypeface found", typeface.FontFamily));
        }

        ushort[] glyphIndices = new ushort[text.Length];
        double[] advanceWidths = new double[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[i]];
            glyphIndices[i] = glyphIndex;
            advanceWidths[i] = glyphTypeface.AdvanceWidths[glyphIndex] * emSize;
        }

        return new GlyphRun(
            glyphTypeface, 0, false, emSize, (float)MonitorUtilities.GetDpi().PixelsPerDip,
            glyphIndices, baselineOrigin, advanceWidths,
            null, null, null, null, null, null);
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (!Controller.IsConnected)
        {
            //if (!await PreLogin())
            //    return;

            if (await Task.Run(Controller.Connect))
                return;

            await Task.Run(Controller.Disconnect);
        }
        else
            await Task.Run(Controller.Disconnect);

        PostLogout();
    }

    //private async Task<bool> PreLogin()
    //{
    //    V5_REST_Lib.Results jobs = await Controller.Commands.GetJobSlots();
    //    if (jobs.OK)
    //    {
    //        JobSlots job = (JobSlots)jobs.Object;
    //        if (job != null)
    //            JobSlots = job.response.data;

    //        return true;
    //    }
    //    else
    //        return false;
    //}

    private void PostLogout()
    {
        JobSlots.Clear();
        SelectedJobSlot = null;
        JobName = "";
    }

    private CancellationTokenSource _tokenSrc;
    private bool running;
    private bool stop;
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
            CancellationToken cnlToken = _tokenSrc.Token;
            return App.Current.Dispatcher.Invoke(async () =>
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
        else
            _ = Controller.Commands.Trigger();

        return Task.CompletedTask;

    }

    [RelayCommand]
    private async Task QuickSetFocus()
    {
        Clear();
        _ = await Controller.QuickSet_Focus_Wait(QuickSet_Focus);
    }
    [RelayCommand]
    private async Task QuickSetPhotometry()
    {
        Clear();
        _ = await Controller.QuickSet_Photometry_Wait(QuickSet_Photometry);
    }
    [RelayCommand]
    private async Task SysInfo()
    {
        Clear();

        V5_REST_Lib.Results res = await Controller.Commands.GetSysInfo();
        if (res.OK)
        {
            ExplicitMessages = res.Json;
        }
    }
    [RelayCommand]
    private async Task Config()
    {
        Clear();
        V5_REST_Lib.Results res = await Controller.Commands.GetConfig();
        if (res.OK)
        {
            ExplicitMessages = res.Json;
        }
    }

    private async Task ChangeJob(JobSlots.Datum job) => await Controller.ChangeJobSlot(job);

    [RelayCommand]
    private async Task SwitchRun() => await Controller.Commands.ModeRun();
    [RelayCommand]
    public async Task SwitchEdit() => await Controller.SwitchToEdit();

    [RelayCommand]
    private void AddToImageRoll()
    {
        if (SelectedImageRoll == null)
        {
            LogWarning("No image roll selected.");
            return;
        }

        if (SelectedImageRoll.IsRooted)
        {
            LogWarning("Cannot add to a rooted image roll.");
            return;
        }

        if (SelectedImageRoll.IsLocked)
        {
            LogWarning("Cannot add to a locked image roll.");
            return;
        }

        if (RawImage == null)
        {
            LogWarning("No image to add.");
            return;
        }
        ImageEntry imagEntry = SelectedImageRoll.GetNewImageEntry(RawImage);

        SelectedImageRoll.AddImage(imagEntry);
    }

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

    #region Logging
    private void LogInfo(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
#if DEBUG
    private void LogDebug(string message) => Logging.lib.Logger.LogDebug(GetType(), message);
#else
    private void LogDebug(string message) { }
#endif
    private void LogWarning(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
    private void LogError(string message) => Logging.lib.Logger.LogError(GetType(), message);
    private void LogError(Exception ex) => Logging.lib.Logger.LogError(GetType(), ex);
    private void LogError(string message, Exception ex) => Logging.lib.Logger.LogError(GetType(), ex, message);

    #endregion
}
