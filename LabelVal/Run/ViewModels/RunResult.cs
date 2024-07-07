using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Results.ViewModels;
using LabelVal.Run.Databases;
using LabelVal.Utilities;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using V275_REST_lib.Models;

namespace LabelVal.Run.ViewModels;
public partial class RunResult : ObservableRecipient, IImageResultEntry, IRecipient<PropertyChangedMessage<PrinterSettings>>
{

    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v275CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v275StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.SectorDifferences> v275DiffSectors = [];
    [ObservableProperty] private Sectors.ViewModels.Sector v275FocusedStoredSector = null;
    [ObservableProperty] private Sectors.ViewModels.Sector v275FocusedCurrentSector = null;

    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry sourceImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v275CurrentImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v275StoredImage;

    [ObservableProperty] private System.Windows.Media.DrawingImage v275CurrentImageOverlay;
    [ObservableProperty] private System.Windows.Media.DrawingImage v275StoredImageOverlay;

    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v5CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v5StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.SectorDifferences> v5DiffSectors = [];
    [ObservableProperty] private Sectors.ViewModels.Sector v5FocusedStoredSector = null;
    [ObservableProperty] private Sectors.ViewModels.Sector v5FocusedCurrentSector = null;

    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v5SourceImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v5CurrentImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v5StoredImage;

    [ObservableProperty] private System.Windows.Media.DrawingImage v5CurrentImageOverlay;
    [ObservableProperty] private System.Windows.Media.DrawingImage v5StoredImageOverlay;

    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> l95xxCurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> l95xxStoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.SectorDifferences> l95xxDiffSectors = [];
    [ObservableProperty] private Sectors.ViewModels.Sector l95xxFocusedStoredSector = null;
    [ObservableProperty] private Sectors.ViewModels.Sector l95xxFocusedCurrentSector = null;

    public RunEntry RunEntry { get; }

    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));
    private int LoopCount => App.Settings.GetValue(nameof(LoopCount), 1);

    public CurrentImageResultGroup CurrentImageResultGroup { get; }
    public StoredImageResultGroup StoredImageResultGroup { get; }

    [ObservableProperty] private PrinterSettings selectedPrinter;

    public RunResult() => IsActive = true;

    public RunResult(CurrentImageResultGroup current, StoredImageResultGroup stored, RunEntry runEntry)
    {
        CurrentImageResultGroup = current;
        StoredImageResultGroup = stored;
        RunEntry = runEntry;

        V275LoadStored();
        V275LoadCurrent();

        IsActive = true;
    }
    private void V275LoadStored()
    {
        V275StoredSectors.Clear();

        if (StoredImageResultGroup == null)
            return;

        SourceImage = JsonConvert.DeserializeObject<ImageEntry>(StoredImageResultGroup.V275Result.SourceImage);
        V275StoredImage = JsonConvert.DeserializeObject<ImageEntry>(StoredImageResultGroup.V275Result.StoredImage);

        List<Sectors.ViewModels.Sector> tempSectors = [];
        if (!string.IsNullOrEmpty(StoredImageResultGroup.V275Result.Report) && !string.IsNullOrEmpty(StoredImageResultGroup.V275Result.Template))
        {
            foreach (V275_REST_lib.Models.Job.Sector jSec in StoredImageResultGroup.V275Result._Job.sectors)
            {
                foreach (JObject rSec in StoredImageResultGroup.V275Result._Report.inspectLabel.inspectSector)
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        object fSec = V275DeserializeSector(rSec, false);

                        if (fSec == null)
                            break;

                        tempSectors.Add(new Sectors.ViewModels.Sector(jSec, fSec, RunEntry.GradingStandard, RunEntry.Gs1TableName));

                        break;
                    }
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.ViewModels.Sector sec in tempSectors)
                V275StoredSectors.Add(sec);

            V275StoredImageOverlay = V275CreateSectorsImageOverlay(StoredImageResultGroup.V275Result._Job, false, StoredImageResultGroup.V275Result._Report, V275StoredImage, V275StoredSectors);
        }
    }

    private void V275LoadCurrent()
    {
        V275CurrentSectors.Clear();

        if (CurrentImageResultGroup == null)
            return;

        V275CurrentImage = JsonConvert.DeserializeObject<ImageEntry>(CurrentImageResultGroup.V275Result.StoredImage);

        List<Sectors.ViewModels.Sector> tempSectors = [];
        if (!string.IsNullOrEmpty(CurrentImageResultGroup.V275Result.Report) && !string.IsNullOrEmpty(CurrentImageResultGroup.V275Result.Template))
        {
            foreach (V275_REST_lib.Models.Job.Sector jSec in CurrentImageResultGroup.V275Result._Job.sectors)
            {
                foreach (JObject rSec in CurrentImageResultGroup.V275Result._Report.inspectLabel.inspectSector)
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        object fSec = V275DeserializeSector(rSec, false);

                        if (fSec == null)
                            break;

                        tempSectors.Add(new Sectors.ViewModels.Sector(jSec, fSec, RunEntry.GradingStandard, RunEntry.Gs1TableName));

                        break;
                    }
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.ViewModels.Sector sec in tempSectors)
                V275CurrentSectors.Add(sec);

            V275CurrentImageOverlay = V275CreateSectorsImageOverlay(CurrentImageResultGroup.V275Result._Job, false, CurrentImageResultGroup.V275Result._Report, V275CurrentImage, V275CurrentSectors);
        }
    }

    private DrawingImage V275CreateSectorsImageOverlay(V275_REST_lib.Models.Job template, bool isDetailed, V275_REST_lib.Models.Report report, ImageRolls.ViewModels.ImageEntry image, ObservableCollection<Sectors.ViewModels.Sector> sectors)
    {
        DrawingGroup drwGroup = new();

        //Draw the image outline the same size as the stored image
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, image.Image.PixelWidth - 1, image.Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        GeometryGroup secCenter = new();

        foreach (V275_REST_lib.Models.Job.Sector jSec in template.sectors)
        {
            foreach (JObject rSec in report.inspectLabel.inspectSector.Cast<JObject>())
            {
                if (jSec.name == rSec["name"].ToString())
                {
                    if (rSec["type"].ToString() is "blemish" or "ocr" or "ocv")
                        continue;

                    JObject fSec = JsonConvert.DeserializeObject<JObject>(rSec["data"].ToString());
                    JObject result = JsonConvert.DeserializeObject<JObject>(fSec["overallGrade"].ToString());

                    GeometryDrawing sector = new()
                    {
                        Geometry = new RectangleGeometry(new Rect(rSec["left"].Value<double>(), rSec["top"].Value<double>(), rSec["width"].Value<double>(), rSec["height"].Value<double>())),
                        Pen = new Pen(GetGradeBrush(result["grade"]?["letter"].ToString()), 5)
                    };
                    drwGroup.Children.Add(sector);

                    drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun(jSec.username, new Typeface("Arial"), 30.0, new Point(jSec.left - 8, jSec.top - 8))));

                    double y = rSec["top"].Value<double>() + (rSec["height"].Value<double>() / 2);
                    double x = rSec["left"].Value<double>() + (rSec["width"].Value<double>() / 2);
                    secCenter.Children.Add(new LineGeometry(new Point(x + 10, y), new Point(x + -10, y)));
                    secCenter.Children.Add(new LineGeometry(new Point(x, y + 10), new Point(x, y + -10)));

                    break;
                }
            }
        }

        GeometryDrawing sectorCenters = new()
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4)
        };
        drwGroup.Children.Add(sectorCenters);

        if (isDetailed)
            drwGroup.Children.Add(V275GetModuleGrid(template.sectors, sectors));

        DrawingImage geometryImage = new(drwGroup);
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
    private static DrawingGroup V275GetModuleGrid(Job.Sector[] sectors, ObservableCollection<Sectors.ViewModels.Sector> parsedSectors)
    {
        DrawingGroup drwGroup = new();
        //GeometryGroup moduleGrid = new GeometryGroup();

        foreach (Job.Sector sec in sectors)
        {
            Sectors.ViewModels.Sector sect = parsedSectors.FirstOrDefault((e) => e.Template.Name.Equals(sec.name));

            if (sect != null)
            {
                GeometryGroup secArea = new();

                secArea.Children.Add(new RectangleGeometry(new Rect(sec.left, sec.top, sec.width, sec.height)));

                if (sec.symbology is "qr" or "dataMatrix")
                {

                    Sectors.ViewModels.Report res = sect.Report;

                    if (res.ExtendedData != null)
                    {
                        if (res.ExtendedData.ModuleReflectance != null)
                        {
                            GeometryGroup moduleGrid = new();
                            DrawingGroup textGrp = new();

                            int qzX = (sec.symbology == "dataMatrix") ? 1 : res.ExtendedData.QuietZone;
                            int qzY = res.ExtendedData.QuietZone;

                            double dX = (sec.symbology == "dataMatrix") ? 0 : (res.ExtendedData.DeltaX / 2);
                            double dY = (sec.symbology == "dataMatrix") ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

                            int startX = 0;// sec.left + res.ExtendedData.Xnw - dX + 1 - (qzX * res.ExtendedData.DeltaX);
                            int startY = 0;// sec.top + res.ExtendedData.Ynw - dY + 1 - (qzY * res.ExtendedData.DeltaY);

                            int cnt = 0;

                            for (int row = -qzX; row < res.ExtendedData.NumRows + qzX; row++)
                            {
                                for (int col = -qzY; col < res.ExtendedData.NumColumns + qzY; col++)
                                {
                                    RectangleGeometry area1 = new(new Rect(startX + (res.ExtendedData.DeltaX * (col + qzX)), startY + (res.ExtendedData.DeltaY * (row + qzY)), res.ExtendedData.DeltaX, res.ExtendedData.DeltaY));
                                    moduleGrid.Children.Add(area1);

                                    string text = res.ExtendedData.ModuleModulation[cnt].ToString();
                                    Typeface typeface = new("Arial");
                                    if (typeface.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface))
                                    {
                                        ushort[] _glyphIndexes = new ushort[text.Length];
                                        double[] _advanceWidths = new double[text.Length];

                                        double textWidth = 0;
                                        for (int ix = 0; ix < text.Length; ix++)
                                        {
                                            ushort glyphIndex = _glyphTypeface.CharacterToGlyphMap[text[ix]];
                                            _glyphIndexes[ix] = glyphIndex;

                                            double width = _glyphTypeface.AdvanceWidths[glyphIndex] * 2;
                                            _advanceWidths[ix] = width;

                                            textWidth += width;
                                        }

                                        GlyphRun gr = new(_glyphTypeface, 0, false, 2, 1.0f, _glyphIndexes,
                                            new Point(startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface.Height * (res.ExtendedData.DeltaY / 4))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        GlyphRunDrawing grd = new(Brushes.Blue, gr);

                                        textGrp.Children.Add(grd);
                                    }

                                    text = res.ExtendedData.ModuleReflectance[cnt++].ToString();
                                    Typeface typeface1 = new("Arial");
                                    if (typeface1.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface1))
                                    {
                                        ushort[] _glyphIndexes = new ushort[text.Length];
                                        double[] _advanceWidths = new double[text.Length];

                                        double textWidth = 0;
                                        for (int ix = 0; ix < text.Length; ix++)
                                        {
                                            ushort glyphIndex = _glyphTypeface1.CharacterToGlyphMap[text[ix]];
                                            _glyphIndexes[ix] = glyphIndex;

                                            double width = _glyphTypeface1.AdvanceWidths[glyphIndex] * 2;
                                            _advanceWidths[ix] = width;

                                            textWidth += width;
                                        }

                                        GlyphRun gr = new(_glyphTypeface1, 0, false, 2, 1.0f, _glyphIndexes,
                                            new Point(startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface1.Height * (res.ExtendedData.DeltaY / 2))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        GlyphRunDrawing grd = new(Brushes.Blue, gr);
                                        textGrp.Children.Add(grd);
                                    }

                                    //FormattedText formattedText = new FormattedText(
                                    //    res.ExtendedData.ModuleReflectance[row + col].ToString(),
                                    //    CultureInfo.GetCultureInfo("en-us"),
                                    //    FlowDirection.LeftToRight,
                                    //    new Typeface("Arial"),
                                    //    4,
                                    //    System.Windows.Media.Brushes.Black // This brush does not matter since we use the geometry of the text.
                                    //);

                                    //// Build the geometry object that represents the text.
                                    //Geometry textGeometry = formattedText.BuildGeometry(new System.Windows.Point(startX + (res.ExtendedData.DeltaX * row), startY + (res.ExtendedData.DeltaY * col)));
                                    //moduleGrid.Children.Add(textGeometry);
                                }
                            }

                            TransformGroup transGroup = new();

                            transGroup.Children.Add(new RotateTransform(
                                sec.orientation,
                                res.ExtendedData.DeltaX * (res.ExtendedData.NumColumns + (qzX * 2)) / 2,
                                res.ExtendedData.DeltaY * (res.ExtendedData.NumRows + (qzY * 2)) / 2));

                            transGroup.Children.Add(new TranslateTransform(sec.left, sec.top));

                            //transGroup.Children.Add(new TranslateTransform (res.ExtendedData.Xnw - dX + 1 - (qzX * res.ExtendedData.DeltaX), res.ExtendedData.Ynw - dY + 1 - (qzY * res.ExtendedData.DeltaY)));
                            if (sec.orientation == 0)
                                transGroup.Children.Add(new TranslateTransform(
                                    res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1,
                                    res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - dY + 1));

                            //works for dataMatrix
                            //if (sec.orientation == 90)
                            //    transGroup.Children.Add(new TranslateTransform(
                            //         sec.width - res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - 1, 
                            //         res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1));

                            if (sec.orientation == 90)
                            {
                                double x = sec.symbology == "dataMatrix"
                                    ? sec.width - res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - 1
                                    : sec.width - res.ExtendedData.Ynw - dY - ((res.ExtendedData.NumColumns + qzY) * res.ExtendedData.DeltaY);
                                transGroup.Children.Add(new TranslateTransform(
                                     x,
                                     res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1));
                            }

                            if (sec.orientation == 180)
                            {
                                transGroup.Children.Add(new TranslateTransform(
                                    res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1,
                                    res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - dY + 1));
                            }

                            moduleGrid.Transform = transGroup;
                            textGrp.Transform = transGroup;

                            GeometryDrawing mGrid = new()
                            {
                                Geometry = moduleGrid,
                                Pen = new Pen(Brushes.Yellow, 0.25)
                            };

                            drwGroup.Children.Add(mGrid);
                            drwGroup.Children.Add(textGrp);
                        }
                    }
                }
            }
        }

        //GeometryDrawing mGrid = new GeometryDrawing
        //{
        //    Geometry = moduleGrid,
        //    Pen = new Pen(Brushes.Yellow, 0.25)
        //};

        //drwGroup.Children.Add(mGrid);

        return drwGroup;
    }
    private static object V275DeserializeSector(JObject reportSec, bool removeGS1Data)
    {
        if (reportSec["type"].ToString() == "verify1D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<Report_InspectSector_Verify1D>(reportSec.ToString());
        }
        else if (reportSec["type"].ToString() == "verify2D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<Report_InspectSector_Verify2D>(reportSec.ToString());
        }
        else
        {
            return reportSec["type"].ToString() == "ocr"
                ? JsonConvert.DeserializeObject<Report_InspectSector_OCR>(reportSec.ToString())
                : reportSec["type"].ToString() == "ocv"
                            ? JsonConvert.DeserializeObject<Report_InspectSector_OCV>(reportSec.ToString())
                            : reportSec["type"].ToString() == "blemish"
                                        ? JsonConvert.DeserializeObject<Report_InspectSector_Blemish>(reportSec.ToString())
                                        : (object)null;
        }
    }

    private static SolidColorBrush GetGradeBrush(string grade) => grade switch
    {
        "A" => (SolidColorBrush)App.Current.Resources["CB_Green"],
        "B" => (SolidColorBrush)App.Current.Resources["ISO_GradeB_Brush"],
        "C" => (SolidColorBrush)App.Current.Resources["ISO_GradeC_Brush"],
        "D" => (SolidColorBrush)App.Current.Resources["ISO_GradeD_Brush"],
        "F" => (SolidColorBrush)App.Current.Resources["ISO_GradeF_Brush"],
        _ => Brushes.Black,
    };

    public static void SortList(List<Sectors.ViewModels.Sector> list) => list.Sort((item1, item2) =>
    {
        double distance1 = Math.Sqrt(Math.Pow(item1.Template.CenterPoint.X, 2) + Math.Pow(item1.Template.CenterPoint.Y, 2));
        double distance2 = Math.Sqrt(Math.Pow(item2.Template.CenterPoint.X, 2) + Math.Pow(item2.Template.CenterPoint.Y, 2));
        int distanceComparison = distance1.CompareTo(distance2);

        if (distanceComparison == 0)
        {
            // If distances are equal, sort by X coordinate, then by Y if necessary
            int xComparison = item1.Template.CenterPoint.X.CompareTo(item2.Template.CenterPoint.X);
            if (xComparison == 0)
            {
                // If X coordinates are equal, sort by Y coordinate
                return item1.Template.CenterPoint.Y.CompareTo(item2.Template.CenterPoint.Y);
            }
            return xComparison;
        }
        return distanceComparison;
    });

    #region Recieve Messages    
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion
}
