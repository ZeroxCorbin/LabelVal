using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Python;
using LabelVal.Sectors.Interfaces;
using LabelVal.Utilities;
using LabelVal.V430.Extensions;
using Newtonsoft.Json.Linq;
using OMRON.Reader.SDK.KCommands.Models;
using OMRON.Reader.SDK.Reader.V430.Images;
using OMRON.Reader.SDK.Reader.V430.Reports;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Rect = System.Windows.Rect;

namespace LabelVal.V430.ViewModels
{

    public partial class V430ViewModel : ObservableRecipient
    {
        private LibSimpleDatabase.SimpleDatabase _settings;
        public V430_REST_Lib.MicroHawkController Controller { get; }

        [ObservableProperty] string postLoginCommands = App.GetService<LibSimpleDatabase.SimpleDatabase>()!.GetValue("V430PostLoginCommands", "")!;
        partial void OnPostLoginCommandsChanged(string value) { _settings.SetValue("V430PostLoginCommands", value); }
        [ObservableProperty] string preLogoutCommands = App.GetService<LibSimpleDatabase.SimpleDatabase>()!.GetValue("V430PreLogoutCommands", "")!;
        partial void OnPreLogoutCommandsChanged(string value) { _settings.SetValue("V430PreLogoutCommands", value); }

        [ObservableProperty] private int woiHeightPercentage = App.GetService<LibSimpleDatabase.SimpleDatabase>()!.GetValue("V430WoiHeightPercentage", 100);
        partial void OnWoiHeightPercentageChanged(int value) { _settings.SetValue("V430WoiHeightPercentage", value); _=Controller.SetWoi(WoiHeightPercentage, WoiWidthPercentage); }
        [ObservableProperty] private int woiWidthPercentage = App.GetService<LibSimpleDatabase.SimpleDatabase>()!.GetValue("V430WoiWidthPercentage", 100);
        partial void OnWoiWidthPercentageChanged(int value) { _settings.SetValue("V430WoiWidthPercentage", value); _ = Controller.SetWoi(WoiHeightPercentage, WoiWidthPercentage); }

        [ObservableProperty] private static string host = string.Empty;
        partial void OnHostChanged(string value) { _settings.SetValue("V430Host", value); Controller.Host = value; }

        [ObservableProperty] private bool getImage = App.GetService<LibSimpleDatabase.SimpleDatabase>()!.GetValue("V430GetImage", true)!;
        partial void OnGetImageChanged(bool value) { _settings.SetValue("V430GetImage", value); }

        //[ObservableProperty] private static uint port = _settings.GetValue("V430Port", 80u);
        //partial void OnPortChanged(uint value) { _settings.SetValue("V430Port", value); Controller.Port = value; }
        /// <see cref="RepeatTrigger"/>
        [ObservableProperty] private bool repeatTrigger;

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

        /// <see cref="FullResImages"/>
        [ObservableProperty] private bool fullResImages = false;

        /// <see cref="EventMessages"/>
        [ObservableProperty] private string eventMessages;
        //[ObservableProperty] private string explicitMessages;

        [NotifyPropertyChangedRecipients][ObservableProperty] private byte[] rawImage;
        partial void OnRawImageChanged(byte[] value) { Image = UpdateBitmapImage(); }

        /// <see cref="Image"/>
        [ObservableProperty] private BitmapImage? image;
        /// <see cref="ImageOverlay"/>
        [ObservableProperty] private DrawingImage? imageOverlay;
        /// <see cref="ImageFocusRegionOverlay"/>
        [ObservableProperty] private DrawingImage? imageFocusRegionOverlay;

        [ObservableProperty] private BitmapImage? contaminationDirtImage;
        [ObservableProperty] private BitmapImage? contaminationBadPixelImage;

        public ObservableCollection<ISector> Results { get; } = [];

        [ObservableProperty] string newCommand = string.Empty;

        /// <see cref="Capture"/>
        [ObservableProperty] private JToken capture;

        /// <see cref="ResultsJObject"/>
        [ObservableProperty] private JObject? resultsJObject;
        partial void OnResultsJObjectChanged(JObject? value)
        {
            if (value != null)
            {
                ImageResults = value.GetParameter<JObject>("imageRecords[0]");
            }

        }

        /// <see cref="ImageResults"/>
        [ObservableProperty] private JObject? imageResults;

        /// <see cref="Decode"/>
        [ObservableProperty] OMRON.Reader.SDK.Reader.V430.Reports.Decode decode;

        private double ExposureTarget => App.GetService<LibSimpleDatabase.SimpleDatabase>()!.GetValue("ExposureTarget", 16000);

        private bool IsWaitingForFullImage;

        public V430ViewModel(LibSimpleDatabase.SimpleDatabase settings, V430_REST_Lib.MicroHawkController controller)
        {
            _settings = settings;
            Controller = controller;
            Host = _settings.GetValue("V430Host", "")!;
            Controller.Host = Host;
            IsActive = true;
        }

        private void Controller_CommandUpdate(V430_REST_Lib.ConnectionStates state, string message)
        {
            if (state == V430_REST_Lib.ConnectionStates.Closed)
                return;

            if (message != null)
            {
                try
                {

                    //ExplicitMessages = JsonConvert.SerializeObject(json);

                    // CycleID = json["cycleReport"]["@cycleId"].Value<string>();
                    //Focus = json["cycleReport"]["focusInfo"]["@focus"].Value<int>();
                    //var data1 = json["event"]?["data"]?["job"]?["captureList"];
                    //if (data1 != null)
                    //    if (data1.HasValues)
                    //        Capture = data1[0]?["Capture"];

                    //App.Current.Dispatcher.Invoke(() =>
                    //{
                    //    Results.Clear();

                    //    JToken data1 = json.cycleReport.;
                    //    if (data1 != null)
                    //        if (data1.HasValues)
                    //            foreach (JToken b in data1)
                    //                Results.Add(b);
                    //});

                }
                catch (Exception ex) { Logger.Error(ex); }
            }
        }
        private void Controller_ReportUpdate(V430_REST_Lib.ConnectionStates state, object obj)
        {
            if (!App.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.BeginInvoke(() => Controller_ReportUpdate(state, obj));
                return;
            }

            Results.Clear();

            if (obj != null)
            {
                try
                {
                    if (state == V430_REST_Lib.ConnectionStates.Image)
                    {
                        if (obj is ImageReport report)
                        {
                            ResultsJObject = JObject.FromObject(report.CycleReport);
                            ImageUpdate((byte[])report.Image);


                            foreach (var ipReport in report.CycleReport.ipReports)
                            {
                                if (ipReport.decodes.Count == 0)
                                {
                                    Results.Add(new Sectors.Sector(ResultsJObject, ipReport.uId, "", ResultsJObject, ApplicationStandards.None, BarcodeVerification.lib.GS1.GS1Tables.Unknown, "V430"));
                                }
                                else
                                {
                                    foreach (var decode in ipReport.decodes)
                                    {
                                        Decode = decode;
                                        Results.Add(new Sectors.Sector(ResultsJObject, ipReport.uId, decode.dId, ResultsJObject, ApplicationStandards.None, BarcodeVerification.lib.GS1.GS1Tables.Unknown, "V430"));
                                    }
                                }

                            }
                        }

                    }
                    else if (state == V430_REST_Lib.ConnectionStates.Report)
                    {
                        if (obj is CycleReport report)
                        {
                            ResultsJObject = JObject.FromObject(report);

                            foreach (var ipReport in report.ipReports)
                            {
                                if (ipReport.decodes.Count == 0)
                                {
                                    Results.Add(new Sectors.Sector(ResultsJObject, ipReport.uId, "", ResultsJObject, ApplicationStandards.None, BarcodeVerification.lib.GS1.GS1Tables.Unknown, "V430"));
                                }
                                else
                                {
                                    foreach (var decode in ipReport.decodes)
                                    {
                                        Results.Add(new Sectors.Sector(ResultsJObject, ipReport.uId, decode.dId, ResultsJObject, ApplicationStandards.None, BarcodeVerification.lib.GS1.GS1Tables.Unknown, "V430"));
                                    }
                                }
                            }

                        }

                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            ImageOverlay = CreateSectorsImageOverlay(Results);
        }

        private void ImageUpdate(byte[] image)
        {

            if (image == null)
            {
                RawImage = null;
                return;
            }

            try
            {

                RawImage = image;
            }
            catch { RawImage = null; }

            _ = App.Current.Dispatcher.InvokeAsync(CheckOverlay);

        }

        private BitmapImage? UpdateBitmapImage()
        {
            // Ensure RawImage, SensorSize, and ImageWoi are valid
            if (RawImage == null || Controller.SensorSize == null || Controller.Woi == null)
                return null;

            // Create a DrawingGroup to hold the image and other drawings
            DrawingGroup drawingGroup = new();

            // Draw the border matching the SensorSize
            //GeometryDrawing border = new()
            //{
            //    Geometry = new RectangleGeometry(new Rect(0, 0, Controller.SensorSize.Value.Width, Controller.SensorSize.Value.Height)),
            //    Pen = new Pen(Brushes.Transparent, 1)
            //};
            //drawingGroup.Children.Add(border);

            // Convert RawImage to a BitmapImage
            BitmapImage rawBitmap = new();
            using (MemoryStream memStream = new(RawImage))
            {
                rawBitmap.BeginInit();
                rawBitmap.CacheOption = BitmapCacheOption.OnLoad;
                rawBitmap.StreamSource = memStream;
                rawBitmap.EndInit();
                rawBitmap.Freeze();
            }

            // Add the RawImage to the DrawingGroup at the location of ImageWoi
            //drawingGroup.Children.Add(new ImageDrawing(rawBitmap, new Rect(Controller.Woi.Value.X, Controller.Woi.Value.Y, Controller.Woi.Value.Width, Controller.Woi.Value.Height)));
            //drawingGroup.Freeze(); // Freeze the DrawingGroup for performance optimization
            //// Convert the DrawingGroup to a BitmapImage
            return rawBitmap;
        }

        private BitmapImage ConvertDrawingGroupToBitmapImage(DrawingGroup drawingGroup, int width, int height)
        {
            // Create a RenderTargetBitmap to render the DrawingGroup
            RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                width,
                height,
                96, // DPI X
                96, // DPI Y
                PixelFormats.Pbgra32);

            // Use a DrawingVisual to render the DrawingGroup
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawDrawing(drawingGroup);
            }

            // Render the DrawingVisual onto the RenderTargetBitmap
            renderTarget.Render(drawingVisual);

            // Convert RenderTargetBitmap to BitmapImage
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Encode the RenderTargetBitmap to a PNG format
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                encoder.Save(memoryStream);

                // Load the BitmapImage from the memory stream
                memoryStream.Seek(0, SeekOrigin.Begin);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        private void CheckOverlay()
        {
            //ImageOverlay = Results.Count > 0 ? CreateSectorsImageOverlay(Results) : null;
            ImageFocusRegionOverlay = CreateFocusRegionOverlay();
        }
        private DrawingImage? CreateFocusRegionOverlay()
        {
            if (Image == null || Controller.SensorSize == null || Controller.Woi == null)
                return null;

            DrawingGroup drawingGroup = new();

            // Draw the border matching the SensorSize
            //GeometryDrawing border = new()
            //{
            //    Geometry = new RectangleGeometry(new Rect(0,0, Controller.SensorSize.Value.Width, Controller.SensorSize.Value.Height)),
            //    Pen = new Pen(Brushes.Transparent, 1)
            //};
            //drawingGroup.Children.Add(border);

            //GeometryDrawing woi = new()
            //{
            //    Geometry = new RectangleGeometry(new Rect(Controller.Woi.Value.X, Controller.Woi.Value.Y, Controller.Woi.Value.Width, Controller.Woi.Value.Height)),
            //    Pen = new Pen(Brushes.DarkBlue, 1)
            //};
            //drawingGroup.Children.Add(woi);

            //var multiplier = (Image.PixelWidth * Image.PixelHeight) / (SelectedCamera.Sensor.PixelColumns * SelectedCamera.Sensor.PixelRows);

            double x, y, width, height;
            x = 0;
            y = 0;
            width = Controller.Woi.Value.Width;
            height = Controller.Woi.Value.Height;

            //Draw crosshairs at the center of the focus region, from edge to edge
            GeometryGroup secCenter = new();
            secCenter.Children.Add(new LineGeometry(new Point(x + (width / 2), y), new Point(x + (width / 2), y + height)));
            secCenter.Children.Add(new LineGeometry(new Point(x, y + (height / 2)), new Point(x + width, y + (height / 2))));

            GeometryDrawing sectorCenters = new()
            {
                Geometry = secCenter,
                Pen = new Pen(Brushes.DarkBlue, 4)
            };

            drawingGroup.Children.Add(sectorCenters);

            DrawingImage geometryImage = new(drawingGroup);
            geometryImage.Freeze();
            return geometryImage;
        }

        //private DrawingImage V5CreateSectorsImageOverlay(JObject results)
        //{
        //    if (results == null || Image == null)
        //        return null;

        //    DrawingGroup drwGroup = new();

        //    int div = FullResImages ? 1 : 2;

        //    //Draw the image outline the same size as the stored image
        //    GeometryDrawing border = new()
        //    {
        //        Geometry = new RectangleGeometry(new Rect(0.5, 0.5, Image.PixelWidth - 1, Image.PixelHeight - 1)),
        //        Pen = new Pen(Brushes.Transparent, 1)
        //    };
        //    drwGroup.Children.Add(border);

        //    GeometryGroup secCenter = new();
        //    GeometryGroup bndAreas = new();

        //    if (results["event"]?["name"].ToString() == "cycle-report-alt")
        //    {
        //        foreach (JToken sec in results["event"]?["data"]?["decodeData"])
        //        {
        //            if (sec["boundingBox"] == null)
        //                continue;

        //            GeometryGroup secAreas = new();

        //            double brushWidth = 4.0 / div;
        //            double halfBrushWidth = brushWidth / 2.0 / div;

        //            for (int i = 0; i < 4; i++)
        //            {
        //                int nextIndex = (i + 1) % 4;

        //                double dx = (sec["boundingBox"][nextIndex]["x"].Value<double>() - sec["boundingBox"][i]["x"].Value<double>()) / div;
        //                double dy = (sec["boundingBox"][nextIndex]["y"].Value<double>() - sec["boundingBox"][i]["y"].Value<double>()) / div;

        //                // Calculate the length of the line segment
        //                double length = Math.Sqrt((dx * dx) + (dy * dy));

        //                // Normalize the direction to get a unit vector
        //                double ux = dx / length;
        //                double uy = dy / length;

        //                // Calculate the normal vector (perpendicular to the direction)
        //                double nx = -uy;
        //                double ny = ux;

        //                // Calculate the adjustment vector
        //                double ax = nx * halfBrushWidth;
        //                double ay = ny * halfBrushWidth;

        //                // Adjust the points
        //                double startX = (sec["boundingBox"][i]["x"].Value<double>() - ax) / div;
        //                double startY = (sec["boundingBox"][i]["y"].Value<double>() - ay) / div;
        //                double endX = (sec["boundingBox"][nextIndex]["x"].Value<double>() - ax) / div;
        //                double endY = (sec["boundingBox"][nextIndex]["y"].Value<double>() - ay) / div;

        //                // Add the line to the geometry group
        //                secAreas.Children.Add(new LineGeometry(new Point(startX, startY), new Point(endX, endY)));
        //            }

        //            drwGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Red, 4 / div), secAreas));

        //            drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0 / div, new Point((sec["boundingBox"][2]["x"].Value<double>() - 8) / div, (sec["boundingBox"][2]["y"].Value<double>() - 8) / div))));

        //            secCenter.Children.Add(new LineGeometry(new Point((sec["x"].Value<double>() + 10) / div, sec["y"].Value<double>() / div), new Point((sec["x"].Value<double>() + -10) / div, sec["y"].Value<double>() / div)));
        //            secCenter.Children.Add(new LineGeometry(new Point(sec["x"].Value<double>() / div, (sec["y"].Value<double>() + 10) / div), new Point(sec["x"].Value<double>() / div, (sec["y"].Value<double>() + -10) / div)));
        //        }
        //    }
        //    else if (results["event"]?["name"].ToString() == "cycle-report")
        //    {

        //        foreach (JToken sec in results["event"]["data"]["cycleConfig"]["qualifiedResults"])
        //        {
        //            if (sec["boundingBox"] == null)
        //                continue;
        //            GeometryGroup secAreas = new();
        //            secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div), new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div)));
        //            secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][1]["x"].Value<double>() / div, sec["boundingBox"][1]["y"].Value<double>() / div), new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div)));
        //            secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][2]["x"].Value<double>() / div, sec["boundingBox"][2]["y"].Value<double>() / div), new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div)));
        //            secAreas.Children.Add(new LineGeometry(new Point(sec["boundingBox"][3]["x"].Value<double>() / div, sec["boundingBox"][3]["y"].Value<double>() / div), new Point(sec["boundingBox"][0]["x"].Value<double>() / div, sec["boundingBox"][0]["y"].Value<double>() / div)));

        //            drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0 / div, new Point((sec["boundingBox"][2]["x"].Value<double>() - 8) / div, (sec["boundingBox"][2]["y"].Value<double>() - 8) / div))));
        //        }

        //        foreach (JToken sec in results["event"]["data"]["cycleConfig"]["job"]["toolList"])
        //            foreach (JToken r in sec["SymbologyTool"]["regionList"])
        //                bndAreas.Children.Add(new RectangleGeometry(
        //                    new Rect(
        //                        r["Region"]["shape"]["RectShape"]["x"].Value<double>() / div,
        //                        r["Region"]["shape"]["RectShape"]["y"].Value<double>() / div,
        //                        r["Region"]["shape"]["RectShape"]["width"].Value<double>() / div,
        //                        r["Region"]["shape"]["RectShape"]["height"].Value<double>() / div)
        //                    ));
        //    }

        //    GeometryDrawing sectorCenters = new()
        //    {
        //        Geometry = secCenter,
        //        Pen = new Pen(Brushes.Red, 4 / div)
        //    };
        //    GeometryDrawing bounding = new()
        //    {
        //        Geometry = bndAreas,
        //        Pen = new Pen(Brushes.Purple, 4 / div)
        //    };

        //    drwGroup.Children.Add(bounding);
        //    drwGroup.Children.Add(sectorCenters);

        //    DrawingImage geometryImage = new(drwGroup);
        //    geometryImage.Freeze();
        //    return geometryImage;
        //}
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

        private DrawingImage? CreateSectorsImageOverlay(ObservableCollection<ISector> sectors)
        {
            if (sectors == null || sectors.Count == 0)
                return null;

            if (Image == null)
                return null;

            DrawingGroup drawingGroup = new();

            // Draw the border matching the SensorSize
            //GeometryDrawing border = new()
            //{
            //    Geometry = new RectangleGeometry(new Rect(0, 0, Controller.SensorSize.Value.Width, Controller.SensorSize.Value.Height)),
            //    Pen = new Pen(Brushes.Transparent, 1)
            //};
            //drawingGroup.Children.Add(border);

            // Define a scaling factor (e.g., text height should be 5% of the image height)
            var scalingFactor = 0.04;

            // Calculate the renderingEmSize based on the image height and scaling factor
            var renderingEmSize = Image.PixelHeight * scalingFactor;
            var renderingEmSizeHalf = renderingEmSize / 2;

            var warnSecThickness = renderingEmSize / 5;
            var warnSecThicknessHalf = warnSecThickness / 2;

            var offsetX = 0;
            var offsetY = 0;

            GeometryGroup secCenter = new();
            foreach (ISector newSec in sectors)
            {
                var hasReportSec = newSec.Report.Width > 0;

                GeometryDrawing sectorT = new()
                {
                    Geometry = new RectangleGeometry(new Rect(
                        newSec.Template.Left + 10,
                        newSec.Template.Top + 10,
                        newSec.Template.Width + 20,
                        newSec.Template.Height + 20)),
                    Pen = new Pen(GetGradeBrush(newSec.Report.OverallGrade != null ? newSec.Report.OverallGrade.Grade.Letter : "F", (byte)(newSec.IsFocused || newSec.IsMouseOver ? 0xFF : 0xFF)), 20),
                };
                drawingGroup.Children.Add(sectorT);

                //GeometryDrawing warnSector = new()
                //{
                //    Geometry = new RectangleGeometry(new Rect(
                //        newSec.Template.Left,
                //        newSec.Template.Top,
                //           newSec.Template.Width,
                //           newSec.Template.Height)),

                //    Pen = new Pen(
                //        newSec.IsWarning ? new SolidColorBrush(Colors.Yellow) :
                //            newSec.IsError ? new SolidColorBrush(Colors.Red) : Brushes.Transparent,
                //        2)
                //};
                //drawingGroup.Children.Add(warnSector);

                // drawingGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun(newSec.Template.Username, new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, new FontStretch()), renderingEmSize, new Point(newSec.Template.Left - 8, newSec.Template.Top - 8))));

                if (hasReportSec)
                {
                    var x = newSec.Report.Left + (newSec.Report.Width / 2);
                    var y = newSec.Report.Top + (newSec.Report.Height / 2);
                    secCenter.Children.Add(new LineGeometry(new Point(x + 50, y), new Point(x + -50, y)));
                    secCenter.Children.Add(new LineGeometry(new Point(x, y + 50), new Point(x, y + -50)));

                    GeometryDrawing sectorCenters = new()
                    {
                        Geometry = secCenter,
                        Pen = new Pen(Brushes.DarkRed, 6)
                    };
                    drawingGroup.Children.Add(sectorCenters);
                }
                else
                {
                    // Draw the sector center crosshairs
                    var x = newSec.Template.Left + (newSec.Template.Width / 2) + offsetX;
                    var y = newSec.Template.Top + (newSec.Template.Height / 2) + offsetY;
                    secCenter.Children.Add(new LineGeometry(new Point(x + renderingEmSizeHalf, y), new Point(x - renderingEmSizeHalf, y)));
                    secCenter.Children.Add(new LineGeometry(new Point(x, y + renderingEmSizeHalf), new Point(x, y - renderingEmSizeHalf)));
                    GeometryDrawing sectorCenters = new()
                    {
                        Geometry = secCenter,
                        Pen = new Pen(Brushes.DarkBlue, renderingEmSize)
                    };
                    drawingGroup.Children.Add(sectorCenters);
                }
            }

            DrawingImage geometryImage = new(drawingGroup);
            geometryImage.Freeze();

            return geometryImage;
        }

        private static SolidColorBrush GetGradeBrush(string grade, byte trans) => grade switch
        {
            "A" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeA_Brush"], trans),
            "B" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeB_Brush"], trans),
            "C" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeC_Brush"], trans),
            "D" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeD_Brush"], trans),
            "F" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeF_Brush"], trans),
            _ => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeF_Brush"], trans),
        };
        private static SolidColorBrush ChangeTransparency(SolidColorBrush original, byte trans) => new(Color.FromArgb(trans, original.Color.R, original.Color.G, original.Color.B));

        [RelayCommand]
        private async Task Connect()
        {
            if (!Controller.IsConnected)
            {
                if (Controller.Connect(Controller_CommandUpdate, Controller_ReportUpdate))
                {
                    PostLogin();
                    await Task.Delay(1000);
                    //await SysInfo();
                }

                Controller.SetAutoExposureGain(false);
            }
            else
            {
                PreLogout();
                Controller.Disconnect();
            }
        }

        private void PostLogin()
        {
            Controller.SendCommand("<Zrd>");
            Thread.Sleep(1000);

            var spl = PostLoginCommands.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var cmd in spl)
            {
                if (string.IsNullOrWhiteSpace(cmd))
                    continue;

                if (!cmd.StartsWith("<"))
                    continue;

                //extract the value from 0 to the first > symbol
                var match = Regex.Match(cmd, @"<[^>]+>");
                if (match.Success)
                {
                    Controller.SendCommand(match.Value);
                }

            }

            Controller.SendCommand($"<K541,{ExposureTarget},0>");
            Controller.SetWoi(WoiHeightPercentage, WoiWidthPercentage);
        }


        private void PreLogout()
        {
            var spl = PreLogoutCommands.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var cmd in spl)
            {
                if (string.IsNullOrWhiteSpace(cmd))
                    continue;
                if (!cmd.StartsWith("<"))
                    continue;
                //extract the value from 0 to the first > symbol
                var match = Regex.Match(cmd, @"<[^>]+>");
                if (match.Success)
                {
                    Controller.SendCommand(match.Value);
                }
            }
        }

        CancellationTokenSource _tokenSrc;
        private bool running;
        private bool stop;
        [RelayCommand]
        private async Task Trigger()
        {
            if (_tokenSrc != null)
            {
                _tokenSrc.Cancel();
                return;
            }

            Clear();

            if (RepeatTrigger)
            {
                _tokenSrc = new CancellationTokenSource();
                var cnlToken = _tokenSrc.Token;
                _ = App.Current.Dispatcher.Invoke(async () =>
                {
                    try
                    {
                        while (RepeatTrigger && !_tokenSrc.IsCancellationRequested)
                        {
                            var start = DateTime.Now;
                            if (GetImage)
                            {
                                if (await Controller.Trigger_Wait_Image() != true)
                                    _tokenSrc.Token.ThrowIfCancellationRequested();
                            }
                            else
                            {
                                if (await Controller.Trigger_Wait_Report() != true)
                                    _tokenSrc.Token.ThrowIfCancellationRequested();
                            }
                            var end = DateTime.Now;
                            TriggerTime = (end - start).TotalMilliseconds;
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
            {
                var start = DateTime.Now;
                if (GetImage)
                    await Controller.Trigger_Wait_Image();
                else
                    await Controller.Trigger_Wait_Report();

                var end = DateTime.Now;
                TriggerTime = (end - start).TotalMilliseconds;
            }

        }

        [ObservableProperty] private double triggerTime;


        [RelayCommand]
        private async Task ContaminationCheck()
        {
            if (_tokenSrc != null)
            {
                _tokenSrc.Cancel();
                return;
            }

            Clear();

            if (RepeatTrigger)
            {
                _tokenSrc = new CancellationTokenSource();
                var cnlToken = _tokenSrc.Token;
                _ = App.Current.Dispatcher.Invoke(async () =>
                {
                    try
                    {
                        while (RepeatTrigger && !_tokenSrc.IsCancellationRequested)
                        {
                            var start = DateTime.Now;
                            await contaminationCheck();
                            var end = DateTime.Now;
                            TriggerTime = (end - start).TotalMilliseconds;
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
            {
                var start = DateTime.Now;
                await contaminationCheck();
                var end = DateTime.Now;
                TriggerTime = (end - start).TotalMilliseconds;
            }

        }

        private async Task contaminationCheck()
        {
            if (await Controller.Trigger_Wait_Image())
            {
                Logger.Info("Contamination Check: Starting");
                PythonTests.RunPythonTests(RawImage);

                var dirtPath = $"{App.PythonWorkingDirectory}\\output_dirt\\imageUnderTest_result.png";
                if (File.Exists(dirtPath))
                {
                    using var img = new FileStream(dirtPath, FileMode.Open, FileAccess.Read);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = img;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ContaminationDirtImage = bitmap;
                    Logger.Warning("Contamination Check: Dirt Found");
                }
                else
                    ContaminationDirtImage = null;

                var pixPath = $"{App.PythonWorkingDirectory}\\output_pix\\imageUnderTest_badpix_output.png";
                if (File.Exists(pixPath))
                {
                    using var img = new FileStream(pixPath, FileMode.Open, FileAccess.Read);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = img;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ContaminationBadPixelImage = bitmap;
                    Logger.Warning("Contamination Check: Bad Pixels");
                }
                else
                    ContaminationBadPixelImage = null;
            }
        }

        [RelayCommand]
        private async Task Focus()
        {
            Clear();
            Controller.SetAutoFocus(true);
            //bool res = await Controller.QuickSet_Focus_Wait(QuickSet_Focus);
        }
        [RelayCommand]
        private async Task Photometry()
        {
            Clear();
            await Controller.Photometry_Wait(true);
            //bool res = await Controller.QuickSet_Photometry_Wait(QuickSet_Photometry);
        }
        [RelayCommand]
        private async Task SysInfo()
        {
            Clear();
            ResultsJObject = await GetSysInfo();
        }
        private async Task<JObject?> GetSysInfo() => Controller.GetJObject(await Controller.SendCommand("<op,10>", "<op,010"));

        [RelayCommand]
        private async Task Config()
        {
            Clear();
        }

        [RelayCommand]
        private async Task ReadK(object fields)
        {
            if (fields is ReadOnlyObservableCollection<object> srcFields)
            {
                if (srcFields.Count <= 0)
                    return;

                var f = (Field)srcFields[0];

                var retString = await Controller.SendCommand(Controller.CommandBuilder.GetCommand(f.Cmd), Controller.CommandBuilder.GetResponse(f.Cmd));

                if (retString == null)
                    return;

                var reg = Regex.Matches(retString, @"<.*?>").Select(m => m.Value).ToArray();

                if (reg.Length != 1)
                    return;
                var resFields = Controller.CommandBuilder.ParseResponse(reg[0], false);

                if (resFields.Count != srcFields.Count)
                    return;

                foreach (var resF in resFields)
                {
                    if (srcFields.FirstOrDefault(f => ((Field)f).FieldNumber == resF.FieldNumber) is Field srcF)
                    {
                        if (resF.Editor is "hexarray" or "chararray")
                        {
                            int size = 0;
                            if (resF.Range != null)
                                size = resF.Range.Count;

                            srcF.Value = ((string)resF.Value).FromHEX(size: size);
                            continue;
                        }

                        foreach (var src in resF.Source)
                        {
                            if (resF.Value is int ii)
                            {
                                if (src.Value == ii)

                                    srcF.Value = src;

                            }
                            else if (resF.Value is string s)
                            {
                                if (src.Value.ToString() == s)

                                    srcF.Value = src;

                            }
                            else
                            {

                            }

                        }
                    }
                }
            }
        }

        [RelayCommand]
        private void SendCommand()
        {
            if (NewCommand == null || string.IsNullOrWhiteSpace(NewCommand.Trim()))
                return;

            if (NewCommand.StartsWith("<") && NewCommand.EndsWith(">"))
            {

                Controller.SendCommand(NewCommand);
            }
        }

        private JObject GetJObject(string source, string seperator = ",", string keyValueSeperator = "=")
        {
            var jsonObject = new JObject();

            var spl = source.Split(seperator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var s in spl)
            {
                if (!s.Contains(keyValueSeperator))
                    continue;

                var keyVal = s.Split(keyValueSeperator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (keyVal.Length == 2)
                    jsonObject.Add(keyVal[0], keyVal[1]);
            }

            return jsonObject;

        }

        //private async Task ChangeJob(JobSlots.Datum job) => await Controller.ChangeJobSlot(job);

        //[RelayCommand]
        //private async Task SwitchRun() => await Controller.Commands.ModeRun();
        //[RelayCommand]
        //private async Task SwitchEdit() => await Controller.SwitchToEdit();

        private void Clear()
        {
            RawImage = null;
            ImageOverlay = null;
            ImageFocusRegionOverlay = null;

            ResultsJObject = null;
            //Results.Clear();
            //ExplicitMessages = null;
            Capture = null;
        }

        // private void SendControlMessage(string message) => _ = Messenger.Send(new LabelVal.Logging.Messages.SystemMessages.ControlMessage(this, message));


    }

}
