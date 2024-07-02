using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Converters;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Messages;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
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
using V275_REST_Lib.Models;
using V5_REST_Lib.Cameras;
using V5_REST_Lib.Models;

namespace LabelVal.V5.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class Scanner : ObservableRecipient, IRecipient<PropertyChangedMessage<ImageRollEntry>>
{
    public V5_REST_Lib.Controller ScannerController { get; } = new();
    public V5_REST_Lib.FTP.FTPClient FTPClient { get; } = new();

    [ObservableProperty][property: JsonProperty] private static string host;
    partial void OnHostChanged(string value) { ScannerController.Host = value; }

    [ObservableProperty][property: JsonProperty] private static int port;
    partial void OnPortChanged(int value) { ScannerController.Port = value; }

    [ObservableProperty][property: JsonProperty] private static bool fullResImages = true;

    [ObservableProperty][property: JsonProperty] private static string fTPUsername;
    partial void OnFTPUsernameChanged(string value) { FTPClient.Username = value; }

    [ObservableProperty][property: JsonProperty] private static string fTPPassword;
    partial void OnFTPPasswordChanged(string value) { FTPClient.Password = value; }

    [ObservableProperty][property: JsonProperty] private static string fTPHost;
    partial void OnFTPHostChanged(string value) { FTPClient.Host = value; }

    [ObservableProperty][property: JsonProperty] private static int fTPPort;
    partial void OnFTPPortChanged(int value) { FTPClient.Port = value; }

    [ObservableProperty][property: JsonProperty] private static string fTPRemotePath;
    partial void OnFTPRemotePathChanged(string value) { FTPClient.RemotePath = value; }

    [ObservableProperty] private bool isSimulator;

    [JsonProperty]
    public int RepeatedTriggerDelay
    {
        get => repeatedTriggerDelay;
        set
        {
            if (value < 1)
                repeatedTriggerDelay = 1;
            else repeatedTriggerDelay = value > 1000 ? 1000 : value;

            OnPropertyChanged();
        }
    }
    private int repeatedTriggerDelay = 50;

    // private TestViewModel TestViewModel { get; }

    [ObservableProperty] private bool isEventWSConnected;
    partial void OnIsEventWSConnectedChanged(bool value) => OnPropertyChanged(nameof(IsConnected));

    [ObservableProperty] private bool isImageWSConnected;
    partial void OnIsImageWSConnectedChanged(bool value) => OnPropertyChanged(nameof(IsConnected));

    [ObservableProperty] private bool isResultWSConnected;
    partial void OnIsResultWSConnectedChanged(bool value) => OnPropertyChanged(nameof(IsConnected));

    public bool IsConnected => IsEventWSConnected || IsImageWSConnected || IsResultWSConnected;

    [ObservableProperty] private string eventMessages;
    [ObservableProperty] private string explicitMessages;

    [ObservableProperty] private byte[] rawImage;
    partial void OnRawImageChanged(byte[] value) { Image = value == null ? null : GetImage(value); }

    [ObservableProperty] private BitmapImage image;
    [ObservableProperty] private DrawingImage imageOverlay;
    [ObservableProperty] private DrawingImage imageFocusRegionOverlay;

    public static List<CameraDetails> AvailableCameras => V5_REST_Lib.Cameras.Cameras.Available;

    [ObservableProperty][property: JsonProperty] private CameraDetails selectedCamera;
    partial void OnSelectedCameraChanged(CameraDetails value) => _ = App.Current.Dispatcher.BeginInvoke(() =>
    {
        if (IsSimulator)
            ImageFocusRegionOverlay = null;
        else
            ImageFocusRegionOverlay = CreateFocusRegionOverlay();
    });

    [ObservableProperty][property: JsonProperty] private double quickSet_ImagePercent = 0.33d;
    partial void OnQuickSet_ImagePercentChanged(double value) => _ = App.Current.Dispatcher.BeginInvoke(() =>
    {
        if (IsSimulator)
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

            var width = SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent;
            var height = SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent;

            var x = (SelectedCamera.Sensor.PixelColumns - width) / 2;
            var y = (SelectedCamera.Sensor.PixelRows - height) / 2;

            return new QuickSet_Photometry((float)x, (float)y, (float)width, (float)height);
        }
    }
    private QuickSet_Focus QuickSet_Focus
    {
        get
        {
            if (SelectedCamera == null)
                return null;

            var width = SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent;
            var height = SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent;
            var x = (SelectedCamera.Sensor.PixelColumns - width) / 2;
            var y = (SelectedCamera.Sensor.PixelRows - height) / 2;

            return new QuickSet_Focus((float)x, (float)y, (float)width, (float)height);
        }
    }

    [ObservableProperty] private V5_REST_Lib.Controller.ScannerModes scannerMode;

    [ObservableProperty] private ImageRollEntry selectedImageRoll;

    [ObservableProperty] private bool repeatTrigger;

    [ObservableProperty] private JToken capture;
    [ObservableProperty] private string cycleID;
    public ObservableCollection<JToken> Results { get; } = [];
    private JObject ResultsJObject;

    private bool IsWaitingForFullImage;

    [ObservableProperty] private List<JobSlots.Datum> jobs;
    [ObservableProperty] private JobSlots.Datum selectedJob;
    partial void OnSelectedJobChanged(JobSlots.Datum value)
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

        App.Current.Dispatcher.BeginInvoke(() => ChangeJob(value));
    }

    private bool userChange;
    [ObservableProperty] private string jobName = "";
    partial void OnJobNameChanged(string value)
    {
        if (Jobs == null)
        {
            SelectedJob = null;
            return;
        }

        var jb = Jobs.FirstOrDefault((e) => e.jobName == jobName);

        if (jb != null)
        {
            if (SelectedJob != jb)
            {
                userChange = true;
                SelectedJob = jb;
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
        var res = await ScannerController.GetConfig();

        if (!res.OK)
        {
            UpdateStatus("Could not get scanner configuration.", SystemMessages.StatusMessageType.Error);
            return;
        }

        var config = (V5_REST_Lib.Models.NewConfig)res.Object;
        var src = config.response.data.job.channelMap.acquisition.AcquisitionChannel.source;
        if (!file)
        {
            if (src.SensorAcquisitionSource != null)
                return;

            src.FileAcquisitionSource = null;
            src.SensorAcquisitionSource = new V5_REST_Lib.Models.NewConfig.Sensoracquisitionsource()
            {
                baseClass = "ChannelSource",
                extlight = "inactive",
                targeting = true,
                text = "Sensor",
                trigdebounce = 5000,
                trigpolarity = "normally-open",
                uiScale = 1,
            };
            src.type = "SensorAcquisitionSource";
           // src.uid = DateTime.Now.Ticks.ToString();
        }
        else
        {
            if (src.FileAcquisitionSource != null)
                return;

            src.SensorAcquisitionSource = null;
            src.FileAcquisitionSource = new NewConfig.Fileacquisitionsource()
            {
                baseClass = "ChannelSource",
                directory = SelectedDirectory == null ? Directories.First() : SelectedDirectory,
                uiScale = 1,
            };
            src.type = "FileAcquisitionSource";
            //src.uid = DateTime.Now.Ticks.ToString();
        }

        _ = await ScannerController.SendJob(config.response.data);
    }
    private async void ChangeDirectory(string directory)
    {
        var res = await ScannerController.GetConfig();

        if (!res.OK)
        {
            UpdateStatus("Could not get scanner configuration.", SystemMessages.StatusMessageType.Error);
            return;
        }

        var config = (V5_REST_Lib.Models.NewConfig)res.Object;
        var src = config.response.data.job.channelMap.acquisition.AcquisitionChannel.source;

        if(src.FileAcquisitionSource.directory != directory)
        {
            src.FileAcquisitionSource.directory = directory;
            //src.uid = DateTime.Now.Ticks.ToString();
            _ = await ScannerController.SendJob(config.response.data);
        }
    }

    public ScannerManager Manager { get; set; }
    public Scanner()
    {
        ScannerController.StateChanged += ScannerController_StateChanged;
        ScannerController.ScannerModeChanged += ScannerController_ScannerModeChanged;
        ScannerController.ImageUpdate += ScannerController_ImageUpdate;
        ScannerController.ResultUpdate += ScannerController_ResultUpdate;
        ScannerController.ConfigUpdate += ScannerController_ConfigUpdate;

        // SelectedCamera = Cameras.Available.FirstOrDefault();

        Host = ScannerController.Host;
        Port = ScannerController.Port;

        FTPUsername = FTPClient.Username;
        FTPPassword = FTPClient.Password;
        FTPHost = FTPClient.Host;
        FTPPort = FTPClient.Port;
        FTPRemotePath = FTPClient.RemotePath;
        
        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => SelectedImageRoll = message.NewValue;

    private async void ScannerController_ConfigUpdate(JObject json)
    {
        var res = await ScannerController.GetConfig();
        if (res.OK)
        {
            var config = (NewConfig)res.Object;
            IsSimulator = config.response.data.job.channelMap.acquisition.AcquisitionChannel.source.FileAcquisitionSource != null;

            var meta = await ScannerController.Commands.GetMeta();
            if (meta.OK)
            {
                var metaConfig = (Meta)meta.Object;

                // Assuming metaConfig.response.data.FileAcquisitionSource.directory.sources is an array of strings
                var sources = metaConfig.response.data.FileAcquisitionSource.directory.sources;

                await App.Current.Dispatcher.BeginInvoke(() =>
                {
                    // Add new directories from sources to Directories if they're not already present
                    foreach (var source in sources)
                        if (!Directories.Contains(source))
                            Directories.Add(source);

                    // Remove directories from Directories if they're not present in sources
                    for (int i = Directories.Count - 1; i >= 0; i--)
                        if (!sources.Contains(Directories[i]))
                            Directories.RemoveAt(i);
                });
            }

            if (IsSimulator)
            {
                SelectedAcquisitionType = "File";
                SelectedDirectory = config.response.data.job.channelMap.acquisition.AcquisitionChannel.source.FileAcquisitionSource.directory;
            }
            else
            {
                SelectedAcquisitionType = "Camera";
            }

            JobName = null;
            JobName = config.response.data.job.name;
        }
    }

    //private void RunController_StateChanged(RunController.States state, string msg)
    //{
    //    if (state == RunController.States.Running)
    //        stop = true;
    //}

    private void ScannerController_ScannerModeChanged(V5_REST_Lib.Controller.ScannerModes mode) => ScannerMode = mode;
    private void ScannerController_ResultUpdate(JObject json)
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

                        var data1 = json["event"]?["data"]?["decodeData"];
                        if (data1 != null)
                            if (data1.HasValues)
                                foreach (var b in data1)
                                    Results.Add(b);
                    });
                }
                else if (json["event"]?["name"].ToString() == "cycle-report")
                {
                    CycleID = json["event"]?["data"]?["cycleConfig"]?["cycleId"].ToString();

                    var data1 = json["event"]?["data"]?["cycleConfig"]?["job"]?["captureList"];
                    if (data1 != null)
                        if (data1.HasValues)
                            Capture = data1[0]?["Capture"];

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Results.Clear();

                        data1 = json["event"]?["data"]?["cycleConfig"]?["qualifiedResults"];
                        if (data1 != null)
                            if (data1.HasValues)
                                foreach (var b in data1)
                                    Results.Add(b);
                    });
                }

            }
            catch (Exception ex) { UpdateStatus(ex); }
        }
    }
    private async void ScannerController_ImageUpdate(JObject json)
    {
        if (json == null)
        {
            RawImage = null;
            IsWaitingForFullImage = false;
            return;
        }

        if (FullResImages)
        {
            try
            {
                RawImage = ImageUtilities.ConvertToPng(await ScannerController.GetImageFullRes(json));
                IsWaitingForFullImage = false;
                return;
            }
            catch { }
        }
        else
        {
            try
            {
                RawImage = ImageUtilities.ConvertToPng((byte[])json["msgData"]?["images"]?[0]?["imgData"]);
                return;
            }
            catch { }
        }

        RawImage = null;
    }
    private BitmapImage GetImage(byte[] raw)
    {
        var img = new BitmapImage();
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
    private void ScannerController_StateChanged(bool eventWS, bool resultsWS, bool imageWS)
    {
        IsEventWSConnected = eventWS;
        IsResultWSConnected = resultsWS;
        IsImageWSConnected = imageWS;

        if (IsEventWSConnected && IsResultWSConnected && IsImageWSConnected)
            PostLogin();
    }

    private void CheckOverlay()
    {
        ImageOverlay = Results.Count > 0 ? V5CreateSectorsImageOverlay(ResultsJObject) : null;

        ImageFocusRegionOverlay = IsSimulator ? null : CreateFocusRegionOverlay();
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
                new Point(QuickSet_Photometry.photometry.roi[0] + QuickSet_Photometry.photometry.roi[2] / div, QuickSet_Photometry.photometry.roi[1] + QuickSet_Photometry.photometry.roi[3] / div)
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

        var drwGroup = new DrawingGroup();

        int div = FullResImages ? 1 : 2;

        //Draw the image outline the same size as the stored image
        var border = new GeometryDrawing
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, Image.PixelWidth - 1, Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        var secCenter = new GeometryGroup();
        var bndAreas = new GeometryGroup();

        if (results["event"]?["name"].ToString() == "cycle-report-alt")
        {
            foreach (var sec in results["event"]?["data"]?["decodeData"])
            {
                if (sec["boundingBox"] == null)
                    continue;

                var secAreas = new GeometryGroup();

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

            foreach (var sec in results["event"]["data"]["cycleConfig"]["qualifiedResults"])
            {
                if (sec["boundingBox"] == null)
                    continue;
                var secAreas = new GeometryGroup();
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div), new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div), new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div), new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div)));
                secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div), new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div)));

                drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0 / div, new Point((sec["boundingBox"][2]["x"].Value<double>() - 8) / div, (sec["boundingBox"][2]["y"].Value<double>() - 8) / div))));
            }

            foreach (var sec in results["event"]["data"]["cycleConfig"]["job"]["toolList"])
                foreach (var r in sec["SymbologyTool"]["regionList"])
                    bndAreas.Children.Add(new RectangleGeometry(
                        new Rect(
                            r["Region"]["shape"]["RectShape"]["x"].Value<double>() / div,
                            r["Region"]["shape"]["RectShape"]["y"].Value<double>() / div,
                            r["Region"]["shape"]["RectShape"]["width"].Value<double>() / div,
                            r["Region"]["shape"]["RectShape"]["height"].Value<double>() / div)
                        ));
        }

        var sectorCenters = new GeometryDrawing
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4 / div)
        };
        var bounding = new GeometryDrawing
        {
            Geometry = bndAreas,
            Pen = new Pen(Brushes.Purple, 4 / div)
        };

        drwGroup.Children.Add(bounding);
        drwGroup.Children.Add(sectorCenters);

        var geometryImage = new DrawingImage(drwGroup);
        geometryImage.Freeze();
        return geometryImage;
    }
    public static GlyphRun CreateGlyphRun(string text, Typeface typeface, double emSize, Point baselineOrigin)
    {
        GlyphTypeface glyphTypeface;

        if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
        {
            throw new ArgumentException(string.Format(
                "{0}: no GlyphTypeface found", typeface.FontFamily));
        }

        var glyphIndices = new ushort[text.Length];
        var advanceWidths = new double[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            var glyphIndex = glyphTypeface.CharacterToGlyphMap[text[i]];
            glyphIndices[i] = glyphIndex;
            advanceWidths[i] = glyphTypeface.AdvanceWidths[glyphIndex] * emSize;
        }

        return new GlyphRun(
            glyphTypeface, 0, false, emSize,
            glyphIndices, baselineOrigin, advanceWidths,
            null, null, null, null, null, null);
    }


    [RelayCommand]
    private async Task Connect()
    {
        ScannerMode = V5_REST_Lib.Controller.ScannerModes.Offline;

        if (!ScannerController.IsConnected)
        {
            if (!await PreLogin())
                return;

            if (await Task.Run(ScannerController.Connect))
                return;

            await Task.Run(ScannerController.Disconnect);
        }
        else
            await Task.Run(ScannerController.Disconnect);

        PostLogout();
    }

    private async Task<bool> PreLogin()
    {
        var jobs = await ScannerController.Commands.GetJobSlots();
        if (jobs.OK)
        {
            var job = (JobSlots)jobs.Object;
            if (job != null)
                Jobs = job.response.data;

            return true;
        }
        else
            return false;
    }

    private void PostLogin()
    {
        ScannerController_ConfigUpdate(null);
    }

    private void PostLogout()
    {
        Jobs = null;
        IsSimulator = false;
    }

    private bool running;
    private bool stop;
    [RelayCommand]
    private async void Trigger()
    {
        if (running == true)
        {
            stop = true;
            return;
        }

        Clear();

        if (RepeatTrigger)
            _ = Task.Run(async () =>
            {
                try
                {
                    while (RepeatTrigger)
                    {
                        running = true;

                        if (await ScannerController.Trigger_Wait() != true)
                            return;

                        if (stop)
                            return;

                    }
                }
                finally
                {
                    CheckOverlay();
                    stop = false;
                    running = false;
                }
            });
        else
        {
            IsWaitingForFullImage = FullResImages;

            if (await ScannerController.Trigger_Wait())
            {
                if (FullResImages)
                    await WaitForImage();

                CheckOverlay();
            }
            else
                IsWaitingForFullImage = false;

        }
    }

    private async Task<bool> WaitForImage()
    {
        await Task.Run(() =>
        {
            DateTime start = DateTime.Now;
            while (IsWaitingForFullImage)
            {
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(2000))
                    break;
                Thread.Sleep(1);
            }
        });

        return !IsWaitingForFullImage;
    }

    [RelayCommand]
    private async Task QuickSetFocus()
    {
        Clear();

        var res = await ScannerController.QuickSet_Focus_Wait(QuickSet_Focus);
    }
    [RelayCommand]
    private async Task QuickSetPhotometry()
    {
        Clear();

        var res = await ScannerController.QuickSet_Photometry_Wait(QuickSet_Photometry);
    }
    [RelayCommand]
    private async Task SysInfo()
    {
        Clear();

        var res = await ScannerController.GetSysInfo();
        if (res.OK)
        {
            ExplicitMessages = res.Data;
        }
    }
    [RelayCommand]
    private async Task Config()
    {
        Clear();
        var res = await ScannerController.GetConfig();
        if (res.OK)
        {
            ExplicitMessages = res.Data;
        }
    }

    private async Task ChangeJob(JobSlots.Datum job)
    {
        _ = await ScannerController.Commands.PutJobSlots(job);
        ScannerController_ConfigUpdate(null);
    }
    [RelayCommand]
    private async Task SwitchRun()
    {
        _ = await ScannerController.Commands.ModeRun();
        ScannerController_ConfigUpdate(null);
    }
    [RelayCommand]
    private async Task SwitchEdit()
    {
        if (await ScannerController.Commands.ModeSetup())
            _ = ScannerController.Commands.TriggerEnable();
        ScannerController_ConfigUpdate(null);
    }

    [RelayCommand]
    private void Reboot()
    {
        _ = ScannerController.Reboot();
    }
    [RelayCommand]
    private void AddToImageRoll()
    {
        if(SelectedImageRoll == null)
        {
            UpdateStatus("No image roll selected.", SystemMessages.StatusMessageType.Warning);
            return;
        }

        if(SelectedImageRoll.IsRooted)
        {
            UpdateStatus("Cannot add to a rooted image roll.",SystemMessages.StatusMessageType.Warning);
            return;
        }

        if(SelectedImageRoll.IsLocked)
        {
            UpdateStatus("Cannot add to a locked image roll.",SystemMessages.StatusMessageType.Warning);
            return;
        }

        if (RawImage == null)
        {
            UpdateStatus("No image to add.", SystemMessages.StatusMessageType.Warning);
            return;
        }
        var imagEntry = SelectedImageRoll.GetNewImageEntry(RawImage);

        SelectedImageRoll.AddImage(imagEntry);
    }

    private void Clear()
    {
        Image = null;
        ImageOverlay = null;
        ImageFocusRegionOverlay = null;

        Results.Clear();
        ExplicitMessages = null;
        Capture = null;
    }


    #region Logging & Status Messages

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private void UpdateStatus(string message)
    {
        UpdateStatus(message);
        _ = Messenger.Send(new SystemMessages.StatusMessage(message, SystemMessages.StatusMessageType.Info));
    }
    private void UpdateStatus(string message, SystemMessages.StatusMessageType type)
    {
        switch (type)
        {
            case SystemMessages.StatusMessageType.Info:
                UpdateStatus(message);
                break;
            case SystemMessages.StatusMessageType.Debug:
                Logger.Debug(message);
                break;
            case SystemMessages.StatusMessageType.Warning:
                Logger.Warn(message);
                break;
            case SystemMessages.StatusMessageType.Error:
                Logger.Error(message);
                break;
            default:
                UpdateStatus(message);
                break;
        }
        _ = Messenger.Send(new SystemMessages.StatusMessage(message, type));
    }
    private void UpdateStatus(Exception ex)
    {
        Logger.Error(ex);
        _ = Messenger.Send(new SystemMessages.StatusMessage(ex));
    }
    private void UpdateStatus(string message, Exception ex)
    {
        Logger.Error(ex);
        _ = Messenger.Send(new SystemMessages.StatusMessage(ex));
    }

    private void SendControlMessage(string message)
    {
        _ = Messenger.Send(new SystemMessages.ControlMessage(this, message));
    }

    #endregion
}
