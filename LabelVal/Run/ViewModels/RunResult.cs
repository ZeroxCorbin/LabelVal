using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Results.Databases;
using LabelVal.Results.ViewModels;
using LabelVal.Run.Databases;
using LabelVal.Utilities;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Run.ViewModels;
public partial class RunResult : ObservableRecipient, IImageResultEntry, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v275CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v275StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISectorDifferences> v275DiffSectors = [];
    [ObservableProperty] private Sectors.Interfaces.ISector v275FocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector v275FocusedCurrentSector = null;

    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry sourceImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v275CurrentImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v275StoredImage;

    [ObservableProperty] private System.Windows.Media.DrawingImage v275CurrentImageOverlay;
    [ObservableProperty] private System.Windows.Media.DrawingImage v275StoredImageOverlay;

    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v5CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v5StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISectorDifferences> v5DiffSectors = [];
    [ObservableProperty] private Sectors.Interfaces.ISector v5FocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector v5FocusedCurrentSector = null;

    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v5SourceImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v5CurrentImage;
    [ObservableProperty] private ImageRolls.ViewModels.ImageEntry v5StoredImage;

    [ObservableProperty] private System.Windows.Media.DrawingImage v5CurrentImageOverlay;
    [ObservableProperty] private System.Windows.Media.DrawingImage v5StoredImageOverlay;

    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> l95xxCurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> l95xxStoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISectorDifferences> l95xxDiffSectors = [];
    [ObservableProperty] private Sectors.Interfaces.ISector l95xxFocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector l95xxFocusedCurrentSector = null;

    public RunEntry RunEntry { get; }

    public int Order => CurrentImageResultGroup.Order;
    public int Loop => CurrentImageResultGroup.TotalLoops;
    public bool HasDiff => V275DiffSectors.Count > 0 || V5DiffSectors.Count > 0 || L95xxDiffSectors.Count > 0;

    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));

    public ResultEntry CurrentImageResultGroup { get; }
    public ResultEntry StoredImageResultGroup { get; }

    [ObservableProperty] private PrinterSettings selectedPrinter;

    public bool IsPlaceholder => SourceImage.IsPlaceholder;

    [ObservableProperty] private bool showDetails;
    partial void OnShowDetailsChanged(bool value)
    {
        if (value)
        {
            SourceImage?.InitPrinterVariables(SelectedPrinter);

            V275CurrentImage?.InitPrinterVariables(SelectedPrinter);
            V275StoredImage?.InitPrinterVariables(SelectedPrinter);

            V5CurrentImage?.InitPrinterVariables(SelectedPrinter);
            V5StoredImage?.InitPrinterVariables(SelectedPrinter);
        }
    }

    [ObservableProperty] private bool showPrinterAreaOverSource;
    [ObservableProperty] private DrawingImage printerAreaOverlay;
    partial void OnShowPrinterAreaOverSourceChanged(bool value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;

    public RunResult() { IsActive = true; RecieveAll(); }

    public RunResult(ResultEntry current, ResultEntry stored, RunEntry runEntry)
    {
        CurrentImageResultGroup = current;
        StoredImageResultGroup = stored;
        RunEntry = runEntry;

        V275LoadStored();
        V275LoadCurrent();
        V275GetSectorDiff();

        IsActive = true;
        RecieveAll();
    }

    private void RecieveAll()
    {
        RequestMessage<PrinterSettings> mes2 = new();
        WeakReferenceMessenger.Default.Send(mes2);
        SelectedPrinter = mes2.Response;
    }

    [RelayCommand]
    private void Save(string type)
    {
        //SendTo95xxApplication();

        string path = GetSaveFilePath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            byte[] bmp = type == "v275Stored"
                    ? V275StoredImage.ImageBytes
                    : type == "v275Current"
                    ? V275CurrentImage.ImageBytes
                    : type == "v5Stored"
                    ? V5StoredImage.ImageBytes
                    : type == "v5Current"
                    ? V5CurrentImage.ImageBytes
                    //: type == "l95xxStored"
                    //? L95xxStoredImage.OriginalImage
                    //: type == "l95xxCurrent"
                    //? L95xxCurrentImage.OriginalImage
                    : SourceImage.ImageBytes;
            if (bmp != null)
            {
                File.WriteAllBytes(path, bmp);
                Clipboard.SetText(path);
            }
        }
        catch { }
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
    private static SolidColorBrush GetGradeBrush(string grade) => grade switch
    {
        "A" => (SolidColorBrush)App.Current.Resources["CB_Green"],
        "B" => (SolidColorBrush)App.Current.Resources["ISO_GradeB_Brush"],
        "C" => (SolidColorBrush)App.Current.Resources["ISO_GradeC_Brush"],
        "D" => (SolidColorBrush)App.Current.Resources["ISO_GradeD_Brush"],
        "F" => (SolidColorBrush)App.Current.Resources["ISO_GradeF_Brush"],
        _ => Brushes.Black,
    };
    public static void SortList(List<Sectors.Interfaces.ISector> list) => list.Sort((item1, item2) =>
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

    private string GetSaveFilePath()
    {
        SaveFileDialog saveFileDialog1 = new()
        {
            Filter = "Bitmap Image|*.bmp",//|Gif Image|*.gif|JPeg Image|*.jpg";
            Title = "Save an Image File"
        };
        _ = saveFileDialog1.ShowDialog();

        return saveFileDialog1.FileName;
    }


    public DrawingImage CreatePrinterAreaOverlay(bool useRatio)
    {
        if (SelectedPrinter == null) return null;

        double xRatio, yRatio;
        if (useRatio)
        {
            xRatio = (double)SourceImage.ImageLow.PixelWidth / SourceImage.Image.PixelWidth;
            yRatio = (double)SourceImage.ImageLow.PixelHeight / SourceImage.Image.PixelHeight;
        }
        else
        {
            xRatio = 1;
            yRatio = 1;
        }

        double lineWidth = 10 * xRatio;

        GeometryDrawing printer = new()
        {
            Geometry = new System.Windows.Media.RectangleGeometry(new Rect(lineWidth / 2, lineWidth / 2,
            (SelectedPrinter.DefaultPageSettings.PaperSize.Width / 100 * SelectedPrinter.DefaultPageSettings.PrinterResolution.X * xRatio) - lineWidth,
            (SelectedPrinter.DefaultPageSettings.PaperSize.Height / 100 * SelectedPrinter.DefaultPageSettings.PrinterResolution.Y * yRatio) - lineWidth)),
            Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, lineWidth)
        };

        DrawingGroup drwGroup = new();
        drwGroup.Children.Add(printer);

        DrawingImage geometryImage = new(drwGroup);
        geometryImage.Freeze();
        return geometryImage;
    }

    #region Recieve Messages    
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

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
