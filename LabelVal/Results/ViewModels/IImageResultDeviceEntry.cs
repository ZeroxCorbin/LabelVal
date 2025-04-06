using BarcodeVerification.lib.Common;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;
[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageResultEntryDevices
{
    V275,
    V5,
    L95,
    All
}

[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageResultEntryImageTypes
{
    Source,
    V275Stored,
    V275Current,
    V275Print,
    V5Stored,
    V5Current,
    V5Sensor,
    L95Stored,
    L95Current
}

public interface IImageResultDeviceEntry
{
    ImageResultEntryDevices Device { get; }

    string Version { get; }
    ImageResultsManager ImageResultsManager { get; }
    ImageResultEntry ImageResultEntry { get; }

    Result Result { get; }

    ImageEntry StoredImage { get; }
    DrawingImage StoredImageOverlay { get; }
    ObservableCollection<ISector> StoredSectors { get; }
    ISector FocusedStoredSector { get; set; }

    ImageEntry CurrentImage { get; }
    DrawingImage CurrentImageOverlay { get; }

    public JObject CurrentTemplate { get; }
    public JObject CurrentReport { get; }

    ObservableCollection<ISector> CurrentSectors { get; }
    ISector FocusedCurrentSector { get; set; }

    LabelHandlers Handler { get; }
    void HandlerUpdate();

    bool IsSelected { get; set; }
    bool IsWorking { get; set; }
    bool IsFaulted { get; set; }

    void ClearCurrent();

    void GetStored();
    Task Store();

    void Process();

    void RefreshOverlays();
    void RefreshCurrentOverlay();
    void RefreshStoredOverlay();

    ObservableCollection<SectorDifferences> DiffSectors { get; }

    internal static DrawingImage CreateSectorsImageOverlay(ImageEntry image, ObservableCollection<Sectors.Interfaces.ISector> sectors, bool showExtended = false)
    {
        if (sectors == null || sectors.Count == 0)
            return null;

        if (image == null)
            return null;

        DrawingGroup drwGroup = new();
        // Define the clipping rectangle based on the image bounds
        System.Windows.Rect imageBounds = new(0.5, 0.5, image.Image.PixelWidth - 1, image.Image.PixelHeight - 1);
        drwGroup.ClipGeometry = new RectangleGeometry(imageBounds);

        //Draw the image outline the same size as the stored image
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new System.Windows.Rect(0, 0, image.Image.PixelWidth, image.Image.PixelHeight)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        // Define a scaling factor (e.g., text height should be 5% of the image height)
        var scalingFactor = 0.04;

        // Calculate the renderingEmSize based on the image height and scaling factor
        var renderingEmSize = image.Image.PixelHeight * scalingFactor;
        var renderingEmSizeHalf = renderingEmSize / 2;

        var warnSecThickness = renderingEmSize / 5;
        var warnSecThicknessHalf = warnSecThickness / 2;

        GeometryGroup secCenter = new();
        foreach (Sectors.Interfaces.ISector newSec in sectors)
        {
            if (newSec.Report.RegionType is AvailableRegionTypes.OCR or AvailableRegionTypes.OCV or AvailableRegionTypes.Blemish)
                continue;

            var hasReportSec = newSec.Report.Width > 0;

            GeometryDrawing sectorT = new()
            {
                Geometry = new RectangleGeometry(new System.Windows.Rect(
                    newSec.Template.Left + renderingEmSizeHalf,
                    newSec.Template.Top + renderingEmSizeHalf,
                    Math.Clamp(newSec.Template.Width - renderingEmSize, 0, double.MaxValue),
                    Math.Clamp(newSec.Template.Height - renderingEmSize, 0, double.MaxValue))),
                Pen = new Pen(GetGradeBrush(newSec.Report.OverallGrade != null ? newSec.Report.OverallGrade.Grade.Letter : "F", (byte)(newSec.IsFocused || newSec.IsMouseOver ? 0xFF : 0x28)), renderingEmSize),
            };
            drwGroup.Children.Add(sectorT);

            GeometryDrawing warnSector = new()
            {
                Geometry = new RectangleGeometry(new System.Windows.Rect(
                    newSec.Template.Left - warnSecThicknessHalf,
                    newSec.Template.Top - warnSecThicknessHalf,
                       newSec.Template.Width + warnSecThickness,
                       newSec.Template.Height + warnSecThickness)),

                Pen = new Pen(
                    newSec.IsWarning ? new SolidColorBrush(Colors.Yellow) :
                        newSec.IsError ? new SolidColorBrush(Colors.Red) : Brushes.Transparent,
                    warnSecThickness)
            };
            drwGroup.Children.Add(warnSector);

            drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun(newSec.Template.Username, new Typeface(System.Windows.SystemFonts.MessageFontFamily, System.Windows.SystemFonts.MessageFontStyle, System.Windows.SystemFonts.MessageFontWeight, new System.Windows.FontStretch()), renderingEmSize, new System.Windows.Point(newSec.Template.Left - 8, newSec.Template.Top - 8))));

            if (hasReportSec)
            {
                GeometryDrawing sector = new()
                {
                    Geometry = new RectangleGeometry(new System.Windows.Rect(newSec.Report.Left + 0.5, newSec.Report.Top + 0.5, newSec.Report.Width, newSec.Report.Height)),
                    Pen = new Pen(Brushes.Black, 1)
                };
                //sector.Geometry.Transform = new RotateTransform(newSec.Report.AngleDeg, newSec.Report.Left + (newSec.Report.Width / 2), newSec.Report.Top + (newSec.Report.Height / 2));
                drwGroup.Children.Add(sector);

                var x = newSec.Report.Left + (newSec.Report.Width / 2);
                var y = newSec.Report.Top + (newSec.Report.Height / 2);
                secCenter.Children.Add(new LineGeometry(new System.Windows.Point(x + 10, y), new System.Windows.Point(x + -10, y)));
                secCenter.Children.Add(new LineGeometry(new System.Windows.Point(x, y + 10), new System.Windows.Point(x, y + -10)));
            }
        }

        GeometryDrawing sectorCenters = new()
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4)
        };
        drwGroup.Children.Add(sectorCenters);

        if (showExtended)
            drwGroup.Children.Add(GetModuleGrid(sectors));

        // drwGroup.Transform = new RotateTransform(ImageResults.SelectedV5.RotateImage ? 180 : 0);

        DrawingImage geometryImage = new(drwGroup);
        geometryImage.Freeze();

        return geometryImage;
    }
    private static GlyphRun CreateGlyphRun(string text, Typeface typeface, double emSize, System.Windows.Point baselineOrigin)
    {
        if (text == null)
            return null;

        if (!typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
        {
            throw new ArgumentException(string.Format(
                "{0}: no GlyphTypeface found", typeface.FontFamily));
        }

        var glyphIndices = new ushort[text.Length];
        var advanceWidths = new double[text.Length];

        for (var i = 0; i < text.Length; i++)
        {
            var glyphIndex = glyphTypeface.CharacterToGlyphMap[text[i]];
            glyphIndices[i] = glyphIndex;
            advanceWidths[i] = glyphTypeface.AdvanceWidths[glyphIndex] * emSize;
        }

        return new GlyphRun(
            glyphTypeface,
            0,
            false,
            emSize,
            (float)MonitorUtilities.GetDpi().PixelsPerDip,
            glyphIndices,
            baselineOrigin,
            advanceWidths,
            null,
            null,
            null,
            null,
            null,
            null);
    }
    private static DrawingGroup GetModuleGrid(ObservableCollection<Sectors.Interfaces.ISector> sectors)
    {
        DrawingGroup drwGroup = new();

        foreach (Sectors.Interfaces.ISector sect in sectors)
        {

            if (sect == null)
                continue;

            if (sect.Report.SymbolType is AvailableSymbologies.QRCode or AvailableSymbologies.DataMatrix)
            {
                Sectors.Interfaces.ISectorReport res = sect.Report;

                if (res.ExtendedData == null)
                    continue;

                if (res.ExtendedData.ModuleReflectance == null)
                    continue;

                GeometryGroup moduleGrid = new();
                DrawingGroup textGrp = new();

                double qzX = (sect.Report.SymbolType == AvailableSymbologies.DataMatrix) ? 0 : res.ExtendedData.QuietZone;
                double qzY = res.ExtendedData.QuietZone;

                var dX = (sect.Report.SymbolType == AvailableSymbologies.DataMatrix) ? 0 : (res.ExtendedData.DeltaX / 2);
                var dY = (sect.Report.SymbolType == AvailableSymbologies.DataMatrix) ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

                var startX = -0.5;// sec.left + res.ExtendedData.Xnw - dX + 1 - (qzX * res.ExtendedData.DeltaX);
                var startY = -0.5;// sec.top + res.ExtendedData.Ynw - dY + 1 - (qzY * res.ExtendedData.DeltaY);

                var cnt = 0;

                for (var row = -qzX; row < res.ExtendedData.NumRows + qzX; row++)
                    for (var col = -qzY; col < res.ExtendedData.NumColumns + qzY; col++)
                    {
                        RectangleGeometry area1 = new(new System.Windows.Rect(startX + (res.ExtendedData.DeltaX * (col + qzX)), startY + (res.ExtendedData.DeltaY * (row + qzY)), res.ExtendedData.DeltaX, res.ExtendedData.DeltaY));
                        moduleGrid.Children.Add(area1);

                        var text = res.ExtendedData.ModuleModulation[cnt].ToString();
                        Typeface typeface = new("Arial");
                        if (typeface.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface))
                        {
                            var _glyphIndexes = new ushort[text.Length];
                            var _advanceWidths = new double[text.Length];

                            double textWidth = 0;
                            for (var ix = 0; ix < text.Length; ix++)
                            {
                                var glyphIndex = _glyphTypeface.CharacterToGlyphMap[text[ix]];
                                _glyphIndexes[ix] = glyphIndex;

                                var width = _glyphTypeface.AdvanceWidths[glyphIndex] * 2;
                                _advanceWidths[ix] = width;

                                textWidth += width;
                            }

                            GlyphRun gr = new(
                                _glyphTypeface,
                                0,
                                false,
                                2,
                                1.0f,
                                _glyphIndexes,
                                new System.Windows.Point(
                                    startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                    startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface.Height * (res.ExtendedData.DeltaY / 4))),
                                _advanceWidths,
                                null,
                                null,
                                null,
                                null,
                                null,
                                null);

                            GlyphRunDrawing grd = new(Brushes.Blue, gr);

                            textGrp.Children.Add(grd);
                        }

                        text = res.ExtendedData.ModuleReflectance[cnt++].ToString();
                        Typeface typeface1 = new("Arial");
                        if (typeface1.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface1))
                        {
                            var _glyphIndexes = new ushort[text.Length];
                            var _advanceWidths = new double[text.Length];

                            double textWidth = 0;
                            for (var ix = 0; ix < text.Length; ix++)
                            {
                                var glyphIndex = _glyphTypeface1.CharacterToGlyphMap[text[ix]];
                                _glyphIndexes[ix] = glyphIndex;

                                var width = _glyphTypeface1.AdvanceWidths[glyphIndex] * 2;
                                _advanceWidths[ix] = width;

                                textWidth += width;
                            }

                            GlyphRun gr = new(
                                _glyphTypeface1,
                                0,
                                false,
                                2,
                                1.0f,
                                _glyphIndexes,
                                new System.Windows.Point(
                                    startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                    startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface1.Height * (res.ExtendedData.DeltaY / 2))),
                                _advanceWidths,
                                null,
                                null,
                                null,
                                null,
                                null,
                                null);

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

                TransformGroup transGroup = new();

                transGroup.Children.Add(new RotateTransform(
                    sect.Template.Orientation,
                    res.ExtendedData.DeltaX * (res.ExtendedData.NumColumns + (qzX * 2)) / 2,
                    res.ExtendedData.DeltaY * (res.ExtendedData.NumRows + (qzY * 2)) / 2));

                transGroup.Children.Add(new TranslateTransform(sect.Report.Left, sect.Report.Top));

                if (sect.Template.Orientation == 0)
                    transGroup.Children.Add(new TranslateTransform(
                        res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1,
                        res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - dY + 1));

                if (sect.Template.Orientation == 90)
                {
                    var x = sect.Report.SymbolType == AvailableSymbologies.DataMatrix
                        ? sect.Report.Width - res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - 1
                        : sect.Report.Width - res.ExtendedData.Ynw - dY - ((res.ExtendedData.NumColumns + qzY) * res.ExtendedData.DeltaY);
                    transGroup.Children.Add(new TranslateTransform(
                        x,
                        res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1));
                }

                if (sect.Template.Orientation == 180)
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

        return drwGroup;
    }

    private static SolidColorBrush GetGradeBrush(string grade, byte trans) => grade switch
    {
        "A" => ChangeTransparency((SolidColorBrush)App.Current.Resources["CB_Green"], trans),
        "B" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeB_Brush"], trans),
        "C" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeC_Brush"], trans),
        "D" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeD_Brush"], trans),
        "F" => ChangeTransparency((SolidColorBrush)App.Current.Resources["ISO_GradeF_Brush"], trans),
        _ => ChangeTransparency((SolidColorBrush)App.Current.Resources["CB_Green"], trans),
    };
    private static SolidColorBrush ChangeTransparency(SolidColorBrush original, byte trans) => new(Color.FromArgb(trans, original.Color.R, original.Color.G, original.Color.B));
}