using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using V5_REST_Lib.Cameras;
using V5_REST_Lib.Models;

namespace LabelVal.V5.ViewModels
{
    public partial class ScannerViewModel : ObservableObject
    {
        private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static V5_REST_Lib.Controller ScannerController { get; } = new();


        [ObservableProperty] private static string host = ScannerController.Host = App.Settings.GetValue("ScannerHostName", "192.168.188.2");
        partial void OnHostChanged(string value) { App.Settings.SetValue("ScannerHostName", value); ScannerController.Host = value; }


        [ObservableProperty] private static int port = ScannerController.Port = App.Settings.GetValue("ScannerPort", 80);
        partial void OnPortChanged(int value) { App.Settings.SetValue("ScannerPort", value); ScannerController.Port = value; }


        [ObservableProperty] private static bool fullResImages = App.Settings.GetValue(nameof(FullResImages), true);
        partial void OnFullResImagesChanged(bool value) => App.Settings.SetValue(nameof(FullResImages), value);

        public int RepeatedTriggerDelay
        {
            get => App.Settings.GetValue(nameof(RepeatedTriggerDelay), 50, true);
            set
            {
                if (value < 1)
                    App.Settings.SetValue(nameof(RepeatedTriggerDelay), 1);
                else if (value > 1000)
                    App.Settings.SetValue(nameof(RepeatedTriggerDelay), 1000);
                else
                    App.Settings.SetValue(nameof(RepeatedTriggerDelay), value);

                OnPropertyChanged();
            }
        }

       // private TestViewModel TestViewModel { get; }

        [ObservableProperty] private bool isEventWSConnected;
        partial void OnIsEventWSConnectedChanged(bool value) => OnPropertyChanged(nameof(IsWSConnected));

        [ObservableProperty] private bool isImageWSConnected;
        partial void OnIsImageWSConnectedChanged(bool value) => OnPropertyChanged(nameof(IsWSConnected));

        [ObservableProperty] private bool isResultWSConnected;
        partial void OnIsResultWSConnectedChanged(bool value) => OnPropertyChanged(nameof(IsWSConnected));

        public bool IsWSConnected { get => IsEventWSConnected || IsImageWSConnected || IsResultWSConnected; }

        [ObservableProperty] private string eventMessages;
        [ObservableProperty] private string explicitMessages;

        [ObservableProperty] private byte[] rawImage;
        partial void OnRawImageChanged(byte[] value) { Image = value == null ? null : GetImage(value); App.Current.Dispatcher.InvokeAsync(CheckOverlay); }

        [ObservableProperty] private BitmapImage image;
        [ObservableProperty] private DrawingImage imageOverlay;
        [ObservableProperty] private DrawingImage imageFocusRegionOverlay;

        public static ObservableCollection<CameraDetails> AvailableCameras => V5_REST_Lib.Cameras.Cameras.Available;

        [ObservableProperty] private CameraDetails? selectedCamera;
        partial void OnSelectedCameraChanged(CameraDetails? value) => _ = App.Current.Dispatcher.BeginInvoke(() => { ImageFocusRegionOverlay = CreateFocusRegionOverlay(); });

        public double QuickSet_ImagePercent
        {
            get => App.Settings.GetValue(nameof(QuickSet_ImagePercent), 0.33d, true);
            set
            {
                App.Settings.SetValue(nameof(QuickSet_ImagePercent), value);

                OnPropertyChanged();

                _ = App.Current.Dispatcher.BeginInvoke(() =>
                {
                    ImageFocusRegionOverlay = CreateFocusRegionOverlay();
                });
            }
        }

        private QuickSet_Photometry QuickSet_Photometry => new            (
                (float)((SelectedCamera.Sensor.PixelColumns - (SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent)) / 2),
                (float)((SelectedCamera.Sensor.PixelRows - (SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent)) / 2),
                (float)((SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent)),
                (float)((SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent))
            );
        private QuickSet_Focus QuickSet_Focus => new            (
                (float)((SelectedCamera.Sensor.PixelColumns - (SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent)) / 2),
                (float)((SelectedCamera.Sensor.PixelRows - (SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent)) / 2),
                (float)((SelectedCamera.Sensor.PixelColumns * QuickSet_ImagePercent)),
                (float)((SelectedCamera.Sensor.PixelRows * QuickSet_ImagePercent))
            );

        [ObservableProperty] private V5_REST_Lib.Controller.ScannerModes scannerMode;

        public bool RepeatTrigger
        {
            get => App.Settings.GetValue(nameof(RepeatTrigger), false);
            set
            {
                App.Settings.SetValue(nameof(RepeatTrigger), value);
                OnPropertyChanged();
            }
        }

        [ObservableProperty] private JToken capture;
        [ObservableProperty] private string cycleID;
        public ObservableCollection<JToken> Results { get; } = [];

        public ScannerViewModel()
        {
            ScannerController.StateChanged += ScannerController_StateChanged;
            ScannerController.ScannerModeChanged += ScannerController_ScannerModeChanged;
            ScannerController.ImageUpdate += ScannerController_ImageUpdate;
            ScannerController.ResultUpdate += ScannerController_ResultUpdate;

            SelectedCamera = Cameras.Available.FirstOrDefault();
            //App.RunController.StateChanged += RunController_StateChanged;
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
                    ExplicitMessages = JsonConvert.SerializeObject(json);

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
                catch (Exception ex) { Logger.Error(ex); }
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
                    RawImage = ImageUtilities.ConvertToPng(await ScannerController.GetImageFullRes(json));
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
        }

        private void CheckOverlay()
        {
            if (Results.Count > 0)
                ImageOverlay = CreateResultOverlay(Results[0]);
            else
                ImageOverlay = null;

            ImageFocusRegionOverlay = CreateFocusRegionOverlay();
        }

        private DrawingImage CreateResultOverlay(JToken json)
        {
            var data1 = json["boundingBox"];

            if (data1 == null || Image == null)
                return null;

            DrawingGroup drwGroup = new();

            //Draw the image outline the same size as the repeat image
            GeometryDrawing border = new()
            {
                Geometry = new RectangleGeometry(new System.Windows.Rect(0, 0, Image.PixelWidth, Image.PixelHeight)),
                Pen = new Pen(Brushes.Transparent, 1)
            };

            double x1, x2, y1, y2;
            LineGeometry ln;

            GeometryGroup bndImageCenterGg = new();

            x1 = (Image.PixelWidth / 2) + 20;
            y1 = (Image.PixelHeight / 2);
            x2 = (Image.PixelWidth / 2) - 20;
            y2 = (Image.PixelHeight / 2);
            ln = new LineGeometry(new System.Windows.Point(x1, y1), new System.Windows.Point(x2, y2));
            bndImageCenterGg.Children.Add(ln);

            x1 = (Image.PixelWidth / 2);
            y1 = (Image.PixelHeight / 2) + 20;
            x2 = (Image.PixelWidth / 2);
            y2 = (Image.PixelHeight / 2) - 20;
            ln = new LineGeometry(new System.Windows.Point(x1, y1), new System.Windows.Point(x2, y2));
            bndImageCenterGg.Children.Add(ln);

            GeometryDrawing bndImageCenter = new()
            {
                Geometry = bndImageCenterGg,
                Pen = new Pen(Brushes.Pink, 1)
            };

            int div = FullResImages ? 1 : 2;

            GeometryGroup bndBoxGg = new();
            for (int i = 0; i < 4; i++)
            {
                int ii = i + 1;
                if (ii >= 4)
                    ii = 0;
                x1 = double.Parse(data1[i]?["x"].ToString());
                y1 = double.Parse(data1[i]?["y"].ToString());
                x2 = double.Parse(data1[ii]?["x"].ToString());
                y2 = double.Parse(data1[ii]?["y"].ToString());
                ln = new LineGeometry(new System.Windows.Point(x1 / div, y1 / div), new System.Windows.Point(x2 / div, y2 / div));
                //var area = new RectangleGeometry(new System.Windows.Rect(sec.left, sec.top, sec.width, sec.height));
                bndBoxGg.Children.Add(ln);
            }
            GeometryDrawing bndBox = new()
            {
                Geometry = bndBoxGg,
                Pen = new Pen(Brushes.Red, 1)
            };

            var rot = Rotate(double.Parse(json?["angleDeg"].ToString()), 20);

            GeometryGroup bndBoxCenterGg = new();

            x1 = (double.Parse(json?["x"].ToString()) / div) + 14;
            y1 = (double.Parse(json?["y"].ToString()) / div) - rot;
            x2 = (double.Parse(json?["x"].ToString()) / div) - 14;
            y2 = (double.Parse(json?["y"].ToString()) / div) + rot;
            ln = new LineGeometry(new System.Windows.Point(x1, y1), new System.Windows.Point(x2, y2));
            bndBoxCenterGg.Children.Add(ln);

            x1 = (double.Parse(json?["x"].ToString()) / div) + rot;
            y1 = (double.Parse(json?["y"].ToString()) / div) + 14;
            x2 = (double.Parse(json?["x"].ToString()) / div) - rot;
            y2 = (double.Parse(json?["y"].ToString()) / div) - 14;
            ln = new LineGeometry(new System.Windows.Point(x1, y1), new System.Windows.Point(x2, y2));
            bndBoxCenterGg.Children.Add(ln);

            GeometryDrawing bndBoxCenter = new()
            {
                Geometry = bndBoxCenterGg,
                Pen = new Pen(Brushes.Red, 1)
            };

            //DrawingGroup drwGroup = new DrawingGroup();

            drwGroup.Children.Add(bndImageCenter);

            drwGroup.Children.Add(bndBox);
            drwGroup.Children.Add(bndBoxCenter);

            //drwGroup.Children.Add(mGrid);
            drwGroup.Children.Add(border);

            DrawingImage geometryImage = new(drwGroup);
            geometryImage.Freeze();
            return geometryImage;


            //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(bmp.PixelWidth, bmp.PixelHeight);
            //using (var g = System.Drawing.Graphics.FromImage(bitmap))
            //{
            //    using (System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Brushes.Red, 5))
            //    {
            //        if (!isRepeat)
            //        {
            //            DrawModuleGrid(g, LabelTemplate.sectors, LabelSectors);
            //        }
            //        else
            //        {
            //            DrawModuleGrid(g, RepeatTemplate.sectors, RepeatSectors);
            //        }
            //    }
            //}

            //using (MemoryStream memory = new MemoryStream())
            //{
            //    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            //    memory.Position = 0;
            //    BitmapImage bitmapImage = new BitmapImage();
            //    bitmapImage.BeginInit();
            //    bitmapImage.StreamSource = memory;
            //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //    bitmapImage.EndInit();
            //    return bitmapImage;
            //}

            //string text = "Verify1D";
            //Typeface typeface = new Typeface("Arial");
            //if (typeface.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface))
            //{

            //    GlyphRun gr = new GlyphRun
            //    {
            //        PixelsPerDip = 4,
            //        IsSideways = false,
            //        FontRenderingEmSize = 1.0,
            //        BidiLevel = 0,
            //        GlyphTypeface = _glyphTypeface
            //    };

            //    double textWidth = 0;
            //    for (int ix = 0; ix < text.Length; ix++)
            //    {
            //        ushort glyphIndex = _glyphTypeface.CharacterToGlyphMap[text[ix]];
            //        gr.GlyphIndices.Add(glyphIndex);

            //        double width = _glyphTypeface.AdvanceWidths[glyphIndex] * 8;
            //        gr.AdvanceWidths.Add(width);

            //        textWidth += width;
            //        double textHeight = _glyphTypeface.Height * 8;


            //    }
            //    gr.BaselineOrigin = new System.Windows.Point(0, 0);
            //    GlyphRunDrawing grd = new GlyphRunDrawing(Brushes.Black, gr);
            //    drwGroup.Children.Add(grd);
            //}

        }

        private static double Rotate(double angle, double radius = 40)
        {
            while (angle >= 360)
                angle -= 360;
            while (angle <= -360)
                angle += 360;

            //var res = new double[2];

            var rad = angle * Math.PI / 180;
            var sin = Math.Sin(rad);
            //var cos = Math.Cos(rad);
            //var tan = Math.Tan(rad);
            return sin * radius;
            //res[0] = sin * radius;
            //res[1] = cos * radius;

            //return res;
        }
        private DrawingImage CreateFocusRegionOverlay()
        {
            if (QuickSet_Photometry == null || Image == null)
                return null;

            //Draw the image outline the same size as the repeat image
            GeometryDrawing border = new()
            {
                Geometry = new RectangleGeometry(new System.Windows.Rect(0, 0, Image.PixelWidth, Image.PixelHeight)),
                Pen = new Pen(Brushes.Transparent, 1)
            };

            GeometryGroup secAreas = new();
            DrawingGroup drwGroup = new();

            int div = FullResImages ? 1 : 2;

            var x1 = QuickSet_Photometry.photometry.roi[0];
            var y1 = QuickSet_Photometry.photometry.roi[1];
            var x2 = QuickSet_Photometry.photometry.roi[0] + QuickSet_Photometry.photometry.roi[2];
            var y2 = QuickSet_Photometry.photometry.roi[1];
            var ln = new LineGeometry(new System.Windows.Point(x1 / div, y1 / div), new System.Windows.Point(x2 / div, y2 / div));

            secAreas.Children.Add(ln);

            x1 = QuickSet_Photometry.photometry.roi[0] + QuickSet_Photometry.photometry.roi[2];
            y1 = QuickSet_Photometry.photometry.roi[1];
            x2 = QuickSet_Photometry.photometry.roi[0] + QuickSet_Photometry.photometry.roi[2];
            y2 = QuickSet_Photometry.photometry.roi[1] + QuickSet_Photometry.photometry.roi[3];
            var ln1 = new LineGeometry(new System.Windows.Point(x1 / div, y1 / div), new System.Windows.Point(x2 / div, y2 / div));
            secAreas.Children.Add(ln1);

            x1 = QuickSet_Photometry.photometry.roi[0] + QuickSet_Photometry.photometry.roi[2];
            y1 = QuickSet_Photometry.photometry.roi[1] + QuickSet_Photometry.photometry.roi[3];
            x2 = QuickSet_Photometry.photometry.roi[0];
            y2 = QuickSet_Photometry.photometry.roi[1] + QuickSet_Photometry.photometry.roi[3];
            var ln2 = new LineGeometry(new System.Windows.Point(x1 / div, y1 / div), new System.Windows.Point(x2 / div, y2 / div));
            secAreas.Children.Add(ln2);

            x1 = QuickSet_Photometry.photometry.roi[0];
            y1 = QuickSet_Photometry.photometry.roi[1] + QuickSet_Photometry.photometry.roi[3];
            x2 = QuickSet_Photometry.photometry.roi[0];
            y2 = QuickSet_Photometry.photometry.roi[1];
            var ln3 = new LineGeometry(new System.Windows.Point(x1 / div, y1 / div), new System.Windows.Point(x2 / div, y2 / div));
            secAreas.Children.Add(ln3);

            //}

            GeometryDrawing sectors = new()
            {
                Geometry = secAreas,
                Pen = new Pen(Brushes.LightBlue, 2)
            };



            //DrawingGroup drwGroup = new DrawingGroup();
            drwGroup.Children.Add(sectors);
            //drwGroup.Children.Add(mGrid);
            drwGroup.Children.Add(border);

            DrawingImage geometryImage = new(drwGroup);
            geometryImage.Freeze();
            return geometryImage;
        }


        //private void UpdateQuickSetRegions()
        //{
        //    var photo = new V5_REST_Lib.Models.QuickSet_Photometry();
        //    var focus = new V5_REST_Lib.Models.QuickSet_Focus();


        //    float x = (float)((CameraDetails.Sensor.PixelColumns - (CameraDetails.Sensor.PixelColumns * QuickSet_ImagePercent)) / 2);
        //    float y = (float)((CameraDetails.Sensor.PixelRows - (CameraDetails.Sensor.PixelRows * QuickSet_ImagePercent)) / 2);

        //    float width = (float)((CameraDetails.Sensor.PixelColumns * QuickSet_ImagePercent));
        //    float height = (float)((CameraDetails.Sensor.PixelRows * QuickSet_ImagePercent));

        //    photo.photometry.SetROI(x, y, width, height);
        //    focus.focus.SetROI(x, y, width, height);

        //    QuickSet_Photometry = photo;
        //    QuickSet_Focus = focus;

        //    App.Current.Dispatcher.Invoke(() =>
        //    {
        //        ImageFocusRegionOverlay = CreateFocusRegionOverlay();
        //    });
        //}

        [RelayCommand]
        private void WebsocketConnect()
        {
            if (!ScannerController.IsWSConnected)
                ScannerController.Connect();
            else
                ScannerController.Disconnect();

            ScannerMode = V5_REST_Lib.Controller.ScannerModes.Offline;
        }

        private bool running;
        private bool stop;
        [RelayCommand]
        private void Trigger()
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
                        stop = false;
                        running = false;
                    }
                });
            else
                _ = ScannerController.Trigger_Wait();
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


        [RelayCommand]
        private void Reboot()
        {
            _ = ScannerController.Reboot();
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

    }
}
