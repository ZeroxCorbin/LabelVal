using BarcodeVerification.lib.Common;
using LabelVal.ImageRolls.Databases;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

/// <summary>
/// Specifies the device associated with an image result entry.
/// </summary>
[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageResultEntryDevices
{
    /// <summary>
    /// V275 device.
    /// </summary>
    V275,
    /// <summary>
    /// V5 device.
    /// </summary>
    V5,
    /// <summary>
    /// L95 device.
    /// </summary>
    L95,
    /// <summary>
    /// All devices.
    /// </summary>
    All
}

/// <summary>
/// Specifies the type of image in an image result entry.
/// </summary>
[SQLite.StoreAsText]
[JsonConverter(typeof(StringEnumConverter))]
public enum ImageResultEntryImageTypes
{
    /// <summary>
    /// The original source image.
    /// </summary>
    Source,
    /// <summary>
    /// A stored image from the V275 device.
    /// </summary>
    V275Stored,
    /// <summary>
    /// The current image from the V275 device.
    /// </summary>
    V275Current,
    /// <summary>
    /// A print-related image from the V275 device.
    /// </summary>
    V275Print,
    /// <summary>
    /// A stored image from the V5 device.
    /// </summary>
    V5Stored,
    /// <summary>
    /// The current image from the V5 device.
    /// </summary>
    V5Current,
    /// <summary>
    /// An image from the V5 device's sensor.
    /// </summary>
    V5Sensor,
    /// <summary>
    /// A stored image from the L95 device.
    /// </summary>
    L95Stored,
    /// <summary>
    /// The current image from the L95 device.
    /// </summary>
    L95Current
}

/// <summary>
/// Defines the contract for a device-specific entry in the image results.
/// This interface manages the state and operations for images and sectors from a particular device.
/// </summary>
public interface IImageResultDeviceEntry
{
    /// <summary>
    /// Gets the device type for this entry.
    /// </summary>
    ImageResultEntryDevices Device { get; }

    /// <summary>
    /// Gets the manager for all image results.
    /// </summary>
    ImageResultsManager ImageResultsManager { get; }
    /// <summary>
    /// Gets the parent image result entry.
    /// </summary>
    ImageResultEntry ImageResultEntry { get; }

    /// <summary>
    /// Gets the result data associated with this entry.
    /// </summary>
    Result Result { get; }

    /// <summary>
    /// Gets the stored image entry.
    /// </summary>
    ImageEntry StoredImage { get; }
    /// <summary>
    /// Gets the overlay for the stored image, typically displaying sectors.
    /// </summary>
    DrawingImage StoredImageOverlay { get; }
    /// <summary>
    /// Gets the collection of sectors for the stored image.
    /// </summary>
    ObservableCollection<ISector> StoredSectors { get; }
    /// <summary>
    /// Gets or sets the currently focused sector in the stored image.
    /// </summary>
    ISector FocusedStoredSector { get; set; }

    /// <summary>
    /// Gets the current image entry.
    /// </summary>
    ImageEntry CurrentImage { get; }
    /// <summary>
    /// Gets the overlay for the current image.
    /// </summary>
    DrawingImage CurrentImageOverlay { get; }

    /// <summary>
    /// Gets the JSON template for the current device configuration.
    /// </summary>
    JObject CurrentTemplate { get; }
    /// <summary>
    /// Gets the JSON report for the current analysis.
    /// </summary>
    JObject CurrentReport { get; }

    /// <summary>
    /// Gets the collection of sectors for the current image.
    /// </summary>
    ObservableCollection<ISector> CurrentSectors { get; }
    /// <summary>
    /// Gets or sets the currently focused sector in the current image.
    /// </summary>
    ISector FocusedCurrentSector { get; set; }

    /// <summary>
    /// Gets the label handler associated with this device.
    /// </summary>
    LabelHandlers Handler { get; }
    /// <summary>
    /// Updates the handler, typically refreshing its state or data.
    /// </summary>
    void HandlerUpdate();

    /// <summary>
    /// Gets or sets a value indicating whether this entry is selected.
    /// </summary>
    bool IsSelected { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this entry is performing a background operation.
    /// </summary>
    bool IsWorking { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether an error has occurred.
    /// </summary>
    bool IsFaulted { get; set; }

    /// <summary>
    /// Clears the data related to the current image and analysis.
    /// </summary>
    void ClearCurrent();

    /// <summary>
    /// Retrieves the stored data for this entry.
    /// </summary>
    void GetStored();
    /// <summary>
    /// Asynchronously stores the current data.
    /// </summary>
    Task Store();

    /// <summary>
    /// Processes the current image, performing analysis and generating results.
    /// </summary>
    void Process();

    /// <summary>
    /// Refreshes all overlays for both stored and current images.
    /// </summary>
    void RefreshOverlays();
    /// <summary>
    /// Refreshes the overlay for the current image.
    /// </summary>
    void RefreshCurrentOverlay();
    /// <summary>
    /// Refreshes the overlay for the stored image.
    /// </summary>
    void RefreshStoredOverlay();

    /// <summary>
    /// Gets a collection of differences between stored and current sectors.
    /// </summary>
    ObservableCollection<SectorDifferences> DiffSectors { get; }

    /// <summary>
    /// Creates a drawing overlay for an image to visualize sectors.
    /// </summary>
    /// <param name="image">The image on which the overlay is based.</param>
    /// <param name="sectors">The collection of sectors to draw.</param>
    /// <param name="showExtended">A flag to indicate whether to show extended details like module grids.</param>
    /// <returns>A <see cref="DrawingImage"/> containing the sector visualizations, or <c>null</c> if no overlay can be created.</returns>
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
        foreach (var newSec in sectors)
        {
            //if (newSec.Report.RegionType is AvailableRegionTypes.OCR or AvailableRegionTypes.OCV or AvailableRegionTypes.Blemish)
            //    continue;

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

    /// <summary>
    /// Creates a <see cref="GlyphRun"/> for rendering text.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="typeface">The typeface to use.</param>
    /// <param name="emSize">The font size in DIPs.</param>
    /// <param name="baselineOrigin">The baseline origin point for the text.</param>
    /// <returns>A <see cref="GlyphRun"/> object for the specified text.</returns>
    private static GlyphRun CreateGlyphRun(string text, Typeface typeface, double emSize, System.Windows.Point baselineOrigin)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
        {
            // Fallback to a system font if the specified typeface is not available.
            if (!new Typeface(System.Windows.SystemFonts.MessageFontFamily, System.Windows.SystemFonts.MessageFontStyle, System.Windows.SystemFonts.MessageFontWeight, new System.Windows.FontStretch()).TryGetGlyphTypeface(out glyphTypeface))
            {
                throw new ArgumentException($"No GlyphTypeface found for {typeface.FontFamily} or fallback font.");
            }
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

    /// <summary>
    /// Creates a drawing group that visualizes the module grid for 2D matrix codes.
    /// </summary>
    /// <param name="sectors">The collection of sectors to process.</param>
    /// <returns>A <see cref="DrawingGroup"/> containing the module grid visualization.</returns>
    private static DrawingGroup GetModuleGrid(ObservableCollection<Sectors.Interfaces.ISector> sectors)
    {
        DrawingGroup drwGroup = new();
        Typeface typeface = new("Arial");

        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
        {
            // If Arial is not available, we cannot proceed with text rendering.
            // The grid will be drawn without text.
            glyphTypeface = null;
        }


        foreach (var sect in sectors)
        {

            if (sect?.Report?.ExtendedData?.ModuleReflectance == null)
                continue;

            if (sect.Report.Symbology is Symbologies.QRCode or Symbologies.DataMatrix)
            {
                var res = sect.Report;

                GeometryGroup moduleGrid = new();
                DrawingGroup textGrp = new();

                double qzX = (sect.Report.Symbology == Symbologies.DataMatrix) ? 0 : res.ExtendedData.QuietZone;
                double qzY = res.ExtendedData.QuietZone;

                var dX = (sect.Report.Symbology == Symbologies.DataMatrix) ? 0 : (res.ExtendedData.DeltaX / 2);
                var dY = (sect.Report.Symbology == Symbologies.DataMatrix) ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

                var startX = -0.5;// sec.left + res.ExtendedData.Xnw - dX + 1 - (qzX * res.ExtendedData.DeltaX);
                var startY = -0.5;// sec.top + res.ExtendedData.Ynw - dY + 1 - (qzY * res.ExtendedData.DeltaY);

                var cnt = 0;

                for (var row = -qzX; row < res.ExtendedData.NumRows + qzX; row++)
                    for (var col = -qzY; col < res.ExtendedData.NumColumns + qzY; col++)
                    {
                        RectangleGeometry area1 = new(new System.Windows.Rect(startX + (res.ExtendedData.DeltaX * (col + qzX)), startY + (res.ExtendedData.DeltaY * (row + qzY)), res.ExtendedData.DeltaX, res.ExtendedData.DeltaY));
                        moduleGrid.Children.Add(area1);

                        if (glyphTypeface != null)
                        {
                            var text = res.ExtendedData.ModuleModulation[cnt].ToString();
                            var _glyphIndexes = new ushort[text.Length];
                            var _advanceWidths = new double[text.Length];

                            for (var ix = 0; ix < text.Length; ix++)
                            {
                                var glyphIndex = glyphTypeface.CharacterToGlyphMap[text[ix]];
                                _glyphIndexes[ix] = glyphIndex;
                                _advanceWidths[ix] = glyphTypeface.AdvanceWidths[glyphIndex] * 2;
                            }

                            GlyphRun gr = new(
                                glyphTypeface, 0, false, 2, 1.0f, _glyphIndexes,
                                new System.Windows.Point(
                                    startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                    startY + (res.ExtendedData.DeltaY * (row + qzY)) + (glyphTypeface.Height * (res.ExtendedData.DeltaY / 4))),
                                _advanceWidths, null, null, null, null, null, null);

                            textGrp.Children.Add(new GlyphRunDrawing(Brushes.Blue, gr));

                            text = res.ExtendedData.ModuleReflectance[cnt++].ToString();
                            var _glyphIndexes1 = new ushort[text.Length];
                            var _advanceWidths1 = new double[text.Length];

                            for (var ix = 0; ix < text.Length; ix++)
                            {
                                var glyphIndex = glyphTypeface.CharacterToGlyphMap[text[ix]];
                                _glyphIndexes1[ix] = glyphIndex;
                                _advanceWidths1[ix] = glyphTypeface.AdvanceWidths[glyphIndex] * 2;
                            }

                            GlyphRun gr1 = new(
                                glyphTypeface, 0, false, 2, 1.0f, _glyphIndexes1,
                                new System.Windows.Point(
                                    startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                    startY + (res.ExtendedData.DeltaY * (row + qzY)) + (glyphTypeface.Height * (res.ExtendedData.DeltaY / 2))),
                                _advanceWidths1, null, null, null, null, null, null);

                            textGrp.Children.Add(new GlyphRunDrawing(Brushes.Blue, gr1));
                        }
                        else
                        {
                            cnt++;
                        }
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
                    var x = sect.Report.Symbology == Symbologies.DataMatrix
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

    /// <summary>
    /// Gets a brush for a given grade with a specified transparency.
    /// </summary>
    /// <param name="grade">The grade letter (e.g., "A", "B", "F").</param>
    /// <param name="trans">The alpha channel value (0-255).</param>
    /// <returns>A <see cref="SolidColorBrush"/> with the appropriate color and transparency.</returns>
    private static SolidColorBrush GetGradeBrush(string grade, byte trans) => grade switch
    {
        "A" => ChangeTransparency((SolidColorBrush)Application.Current.Resources["CB_Green"], trans),
        "B" => ChangeTransparency((SolidColorBrush)Application.Current.Resources["ISO_GradeB_Brush"], trans),
        "C" => ChangeTransparency((SolidColorBrush)Application.Current.Resources["ISO_GradeC_Brush"], trans),
        "D" => ChangeTransparency((SolidColorBrush)Application.Current.Resources["ISO_GradeD_Brush"], trans),
        "F" => ChangeTransparency((SolidColorBrush)Application.Current.Resources["ISO_GradeF_Brush"], trans),
        _ => ChangeTransparency((SolidColorBrush)Application.Current.Resources["CB_Green"], trans),
    };

    /// <summary>
    /// Creates a new brush from an existing one with a new transparency value.
    /// </summary>
    /// <param name="original">The original brush.</param>
    /// <param name="trans">The new alpha channel value (0-255).</param>
    /// <returns>A new <see cref="SolidColorBrush"/> with the modified transparency.</returns>
    private static SolidColorBrush ChangeTransparency(SolidColorBrush original, byte trans) => new(Color.FromArgb(trans, original.Color.R, original.Color.G, original.Color.B));
}