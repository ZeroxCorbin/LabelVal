using BarcodeVerification.lib.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Utilities;
using LibImageUtilities.ImageTypes;
using Lvs95xx.lib.Core.Controllers;
using Lvs95xx.lib.Core.Models;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

public enum ImageResultEntryDevices
{
    V275,
    V5,
    L95xx,
    L95xxAll
}

public enum ImageResultEntryImageTypes
{
    Source,
    V275Stored,
    V275Current,
    V275Print,
    V5Stored,
    V5Current,
    V5Sensor,
    L95xxStored,
    L95xxCurrent
}

public partial class ImageResultEntry : ObservableRecipient, IImageResultEntry, IRecipient<PropertyChangedMessage<Databases.ImageResultsDatabase>>, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    public delegate void BringIntoViewDelegate();
    public event BringIntoViewDelegate BringIntoView;

    public delegate void DeleteImageDelegate(ImageResultEntry imageResults);
    public event DeleteImageDelegate DeleteImage;

    public ImageEntry SourceImage { get; }
    public string SourceImageUID => SourceImage.UID;
    public bool IsPlaceholder => SourceImage.IsPlaceholder;

    public ImageResults ImageResults { get; }
    public string ImageRollUID => ImageResults.SelectedImageRoll.UID;

    /// <see cref="ImagesMaxHeight"/>>
    [ObservableProperty] private int imagesMaxHeight = App.Settings.GetValue<int>(nameof(ImagesMaxHeight));
    /// <see cref="DualSectorColumns"/>>
    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue<bool>(nameof(DualSectorColumns));
    /// <see cref="ShowExtendedData"/>>
    [ObservableProperty] private bool showExtendedData = App.Settings.GetValue<bool>(nameof(ShowExtendedData));
    partial void OnShowExtendedDataChanged(bool value)
    {
        if (V275StoredImage != null)
            V275StoredImageOverlay = CreateSectorsImageOverlay(V275StoredImage, V275StoredSectors);

        if (V275CurrentImage != null)
            V275CurrentImageOverlay = CreateSectorsImageOverlay(V275CurrentImage, V275CurrentSectors);

        if (V5StoredImage != null)
            V5StoredImageOverlay = CreateSectorsImageOverlay(V5StoredImage, V5StoredSectors);

        if (V5CurrentImage != null)
            V5CurrentImageOverlay = CreateSectorsImageOverlay(V5CurrentImage, V5CurrentSectors);
    }

    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));

    /// <see cref="ShowPrinterAreaOverSource"/>>
    [ObservableProperty] private bool showPrinterAreaOverSource;
    /// <see cref="PrinterAreaOverlay"/>>
    [ObservableProperty] private DrawingImage printerAreaOverlay;
    partial void OnShowPrinterAreaOverSourceChanged(bool value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;

    private V275_REST_Lib.Printer.Controller PrinterController { get; } = new();
    [ObservableProperty] private PrinterSettings selectedPrinter;
    [ObservableProperty] private ImageResultsDatabase selectedDatabase;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;
    partial void OnSelectedDatabaseChanged(Databases.ImageResultsDatabase value) => GetStored();

    [ObservableProperty] private bool showDetails;
    partial void OnShowDetailsChanged(bool value)
    {
        //if(value)
        //{
        //    SourceImage?.InitPrinterVariables(SelectedPrinter);

        //    V275CurrentImage?.InitPrinterVariables(SelectedPrinter);
        //    V275StoredImage?.InitPrinterVariables(SelectedPrinter);

        //    V5CurrentImage?.InitPrinterVariables(SelectedPrinter);
        //    V5StoredImage?.InitPrinterVariables(SelectedPrinter);
        //}
    }

    public ImageResultEntry(ImageEntry sourceImage, ImageResults imageResults)
    {
        ImageResults = imageResults;
        SourceImage = sourceImage;

        IsActive = true;
        RecieveAll();

        App.Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImagesMaxHeight))
                ImagesMaxHeight = App.Settings.GetValue<int>(nameof(ImagesMaxHeight));
            else if (e.PropertyName == nameof(DualSectorColumns))
                DualSectorColumns = App.Settings.GetValue<bool>(nameof(DualSectorColumns));
            else if (e.PropertyName == nameof(ShowExtendedData))
                ShowExtendedData = App.Settings.GetValue<bool>(nameof(ShowExtendedData));
        };
    }

    private void RecieveAll()
    {
        RequestMessage<PrinterSettings> mes2 = new();
        _ = WeakReferenceMessenger.Default.Send(mes2);
        SelectedPrinter = mes2.Response;

        RequestMessage<ImageResultsDatabase> mes4 = new();
        _ = WeakReferenceMessenger.Default.Send(mes4);
        SelectedDatabase = mes4.Response;
    }

    //public StoredImageResultGroup GetStoredImageResultGroup(string runUID) => new()
    //{
    //    RunUID = runUID,
    //    ImageRollUID = ImageRollUID,
    //    SourceImageUID = SourceImageUID,
    //    V275Result = V275ResultRow,
    //    V5Result = V5ResultRow,
    //    L95xxResult = L95xxResultRow,
    //};

    //public CurrentImageResultGroup GetCurrentImageResultGroup(string runUID) => new()
    //{
    //    RunUID = runUID,
    //    ImageRollUID = ImageRollUID,
    //    SourceImageUID = SourceImageUID,
    //    V275Result = new Databases.V275Result
    //    {
    //        RunUID = runUID,
    //        SourceImageUID = SourceImageUID,
    //        ImageRollUID = ImageRollUID,

    //        SourceImage = SourceImage?.Serialize,
    //        StoredImage = V275CurrentImage?.Serialize,

    //        Template = V275SerializeTemplate,
    //        Report = V275SerializeReport,
    //    },
    //    V5Result = new Databases.V5Result
    //    {
    //        RunUID = runUID,
    //        SourceImageUID = SourceImageUID,
    //        ImageRollUID = ImageRollUID,

    //        SourceImage = SourceImage?.Serialize,
    //        StoredImage = V5CurrentImage?.Serialize,

    //        Template = V5SerializeTemplate,
    //        Report = V5SerializeReport,
    //    },
    //    L95xxResult = new Databases.L95xxResult
    //    {
    //        RunUID = runUID,
    //        ImageRollUID = ImageRollUID,
    //        SourceImageUID = SourceImageUID,

    //        SourceImage = SourceImage?.Serialize,
    //        Report = L95xxSerializeReport
    //        //Report = JsonConvert.SerializeObject(L95xxStoredSectors.Select(x => new L95xxReport() { Report = ((LVS_95xx.Sectors.Sector)x).L95xxPacket, Template = (LVS_95xx.Sectors.Template)x.Template }).ToList()),
    //    },
    //};

    private void GetStored()
    {
        V275GetStored();
        V5GetStored();
        L95xxGetStored();
    }

    [RelayCommand]
    private void Save(ImageResultEntryImageTypes type)
    {
        string path = GetSaveFilePath();

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            byte[] bmp = type == ImageResultEntryImageTypes.V275Stored
                    ? V275StoredImage.ImageBytes
                    : type == ImageResultEntryImageTypes.V275Current
                    ? V275CurrentImage.ImageBytes
                    : type == ImageResultEntryImageTypes.V5Stored
                    ? V5StoredImage.ImageBytes
                    : type == ImageResultEntryImageTypes.V5Current
                    ? V5CurrentImage.ImageBytes
                    : type == ImageResultEntryImageTypes.L95xxStored
                    ? L95xxStoredImage.ImageBytes
                    : type == ImageResultEntryImageTypes.Source
                    ? SourceImage.ImageBytes : null;

            if (bmp != null)
            {
                if (Path.GetExtension(path).Contains("png", StringComparison.InvariantCultureIgnoreCase))
                    File.WriteAllBytes(path, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(bmp));
                else
                {
                    ImageUtilities.DPI dpi = LibImageUtilities.ImageTypes.ImageUtilities.GetImageDPI(bmp);
                    LibImageUtilities.ImageTypes.Bmp.Bmp format = new(LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(bmp));
                    //Lvs95xx.lib.Core.Controllers.Controller.ApplyWatermark(format.ImageData);

                    byte[] img = format.RawData;

                    _ = LibImageUtilities.ImageTypes.ImageUtilities.SetImageDPI(img, dpi);
                    //LibImageUtilities.ImageTypes.Bmp.Utilities.SetDPI(format.RawData, newDPI);

                    File.WriteAllBytes(path, img);
                }

                Clipboard.SetText(path);
            }
        }
        catch { }
    }
    [RelayCommand]
    private async Task Store(ImageResultEntryDevices device)
    {
        if (device == ImageResultEntryDevices.V275)
        {
            if (V275CurrentSectors.Count == 0)
            {
                Logger.LogDebug($"There are no sectors to store for: {device}");
                return;
            }

            if (V275StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V275Result(new Databases.V275Result
            {
                ImageRollUID = ImageRollUID,
                RunUID = ImageRollUID,
                Source = SourceImage,
                Stored = V275CurrentImage,

                _Job = V275CurrentTemplate,
                _Report = V275CurrentReport,
            });

            ClearRead(device);

            V275GetStored();
        }
        else if (device == ImageResultEntryDevices.V5)
        {
            if (V5CurrentSectors.Count == 0)
            {
                Logger.LogDebug($"There are no sectors to store for: {device}");
                return;
            }

            if (V5StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V5Result(new Databases.V5Result
            {
                ImageRollUID = ImageRollUID,
                RunUID = ImageRollUID,
                Source = SourceImage,
                Stored= V5CurrentImage,

                _Config = V5CurrentTemplate,
                _Report = V5CurrentReport,
            });

            ClearRead(device);

            V5GetStored();
        }
        else if (device == ImageResultEntryDevices.L95xx)
        {

            if (L95xxCurrentSectorSelected == null)
            {
                Logger.LogError("No sector selected to store.");
                return;
            }
            //Does the selected sector exist in the Stored sectors list?
            //If so, prompt to overwrite or cancel.

            Sectors.Interfaces.ISector old = L95xxStoredSectors.FirstOrDefault(x => x.Template.Name == L95xxCurrentSectorSelected.Template.Name);
            if (old != null)
            {
                if (await OkCancelDialog("Overwrite Stored Sector", $"The sector already exists.\r\nAre you sure you want to overwrite the stored sector?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;
            }

            //Save the list to the database.
            List<FullReport> temp = [];
            if (L95xxResultRow != null)
                temp = L95xxResultRow._Report;

            temp.Add(((LVS_95xx.Sectors.Sector)L95xxCurrentSectorSelected).L95xxFullReport);

            _ = SelectedDatabase.InsertOrReplace_L95xxResult(new Databases.L95xxResult
            {
                ImageRollUID = ImageRollUID,
                RunUID = ImageRollUID,
                Source = SourceImage,
                Stored = L95xxCurrentImage,

                _Settings = ((LVS_95xx.Sectors.Sector)L95xxCurrentSectorSelected).L95xxFullReport.Settings,
                _Report = temp,
            });

            ClearRead(device);

            L95xxGetStored();
        }
        else if (device == ImageResultEntryDevices.L95xxAll)
        {

            if (L95xxCurrentSectors.Count == 0)
            {
                Logger.LogDebug($"There are no sectors to store for: {device}");
                return;
            }
            //Does the selected sector exist in the Stored sectors list?
            //If so, prompt to overwrite or cancel.

            if (L95xxStoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            //Save the list to the database.
            List<FullReport> temp = [];
            List<Setting> tempSettings = [];
            foreach (Sectors.Interfaces.ISector sector in L95xxCurrentSectors)
            {
                temp.Add(((LVS_95xx.Sectors.Sector)sector).L95xxFullReport);
                tempSettings = ((LVS_95xx.Sectors.Sector)sector).L95xxFullReport.Settings;
            }

            _ = SelectedDatabase.InsertOrReplace_L95xxResult(new Databases.L95xxResult
            {
                ImageRollUID = ImageRollUID,
                RunUID = ImageRollUID,
                Source = SourceImage,
                Stored = L95xxCurrentImage,

                _Settings = tempSettings,
                _Report = temp,
            });

            ClearRead(device);

            L95xxGetStored();
        }
    }
    [RelayCommand]
    private async Task ClearStored(ImageResultEntryDevices device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            if (device is ImageResultEntryDevices.V275)
            {
                _ = SelectedDatabase.Delete_V275Result(ImageRollUID, SourceImageUID, ImageRollUID);
                V275GetStored();
            }
            else if (device is ImageResultEntryDevices.V5)
            {
                _ = SelectedDatabase.Delete_V5Result(ImageRollUID, SourceImageUID, ImageRollUID);
                V5GetStored();
            }
            else if (device is ImageResultEntryDevices.L95xx or ImageResultEntryDevices.L95xxAll)
            {
                _ = SelectedDatabase.Delete_L95xxResult(ImageRollUID, SourceImageUID, ImageRollUID);
                L95xxGetStored();
            }
        }
    }
    [RelayCommand]
    private void ClearRead(ImageResultEntryDevices device)
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => ClearRead(device));
            return;
        }

        if (device == ImageResultEntryDevices.V275)
        {

            V275CurrentReport = null;
            V275CurrentTemplate = null;

            V275CurrentSectors.Clear();
            V275DiffSectors.Clear();
            V275CurrentImage = null;
            V275CurrentImageOverlay = null;
        }
        else if (device == ImageResultEntryDevices.V5)
        {
            V5CurrentReport = null;
            V5CurrentTemplate = null;

            V5CurrentSectors.Clear();
            V5DiffSectors.Clear();
            V5CurrentImage = null;
            V5CurrentImageOverlay = null;
        }
        else if (device == ImageResultEntryDevices.L95xx)
        {
            //No CurrentReport for L95xx
            //No template used for L95xx

            _ = L95xxCurrentSectors.Remove(L95xxCurrentSectorSelected);
            L95xxDiffSectors.Clear();

            if (L95xxCurrentSectors.Count == 0)
            {
                L95xxCurrentImage = null;
                L95xxCurrentImageOverlay = null;
            }
        }
        else if (device == ImageResultEntryDevices.L95xxAll)
        {
            L95xxCurrentReport = null;

            L95xxCurrentSectors.Clear();
            L95xxDiffSectors.Clear();
            L95xxCurrentImage = null;
            L95xxCurrentImageOverlay = null;
        }
    }

    [RelayCommand] private void RedoFiducial() => ImageUtilities.RedrawFiducial(SourceImage.Path, false);

    [RelayCommand] private void Delete() => DeleteImage?.Invoke(this);
    //const UInt32 WM_KEYDOWN = 0x0100;
    //const int VK_F5 = 0x74;

    //[DllImport("user32.dll")]
    //static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

    private void SendTo95xxApplication() => _ = Process.GetProcessesByName("LVS-95XX");//foreach (Process proc in processes)//    PostMessage(proc.MainWindowHandle, WM_KEYDOWN, VK_F5, 0);
    private string GetSaveFilePath()
    {
        SaveFileDialog saveFileDialog1 = new()
        {
            Filter = "PNG Image|*.png|Bitmap Image|*.bmp",
            Title = "Save Image File"
        };
        _ = saveFileDialog1.ShowDialog();

        return saveFileDialog1.FileName;
    }
    private string SaveImageBytesToFile(string path, byte[] img)
    {
        File.WriteAllBytes(path, img);

        return "";
    }

    private void PrintImage(byte[] image, int count, string printerName) => Task.Run(() =>
    {
        PrinterController.Print(image, count, printerName, "");
    });

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
            Geometry = new System.Windows.Media.RectangleGeometry(new Rect(
                lineWidth / 2,
                lineWidth / 2,
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

    private DrawingImage CreateSectorsImageOverlay(ImageEntry image, ObservableCollection<Sectors.Interfaces.ISector> sectors)
    {
        if (sectors == null || sectors.Count == 0)
            return null;

        if (image == null)
            return null;

        DrawingGroup drwGroup = new();
        // Define the clipping rectangle based on the image bounds
        Rect imageBounds = new(0.5, 0.5, image.Image.PixelWidth - 1, image.Image.PixelHeight - 1);
        drwGroup.ClipGeometry = new RectangleGeometry(imageBounds);

        //Draw the image outline the same size as the stored image
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new Rect(0, 0, image.Image.PixelWidth, image.Image.PixelHeight)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        // Define a scaling factor (e.g., text height should be 5% of the image height)
        double scalingFactor = 0.04;

        // Calculate the renderingEmSize based on the image height and scaling factor
        double renderingEmSize = image.Image.PixelHeight * scalingFactor;
        double renderingEmSizeHalf = renderingEmSize / 2;

        double warnSecThickness = renderingEmSize / 5;
        double warnSecThicknessHalf = warnSecThickness / 2;

        GeometryGroup secCenter = new();
        foreach (Sectors.Interfaces.ISector newSec in sectors)
        {
            if (newSec.Report.RegionType is AvailableRegionTypes.OCR or AvailableRegionTypes.OCV or AvailableRegionTypes.Blemish)
                continue;

            bool hasReportSec = newSec.Report.Width > 0;

            GeometryDrawing sectorT = new()
            {
                Geometry = new RectangleGeometry(new Rect(
                    newSec.Template.Left + renderingEmSizeHalf,
                    newSec.Template.Top + renderingEmSizeHalf,
                    Math.Clamp(newSec.Template.Width - renderingEmSize, 0, double.MaxValue),
                    Math.Clamp(newSec.Template.Height - renderingEmSize, 0, double.MaxValue))),
                Pen = new Pen(GetGradeBrush(newSec.Report.OverallGrade != null ? newSec.Report.OverallGrade.Grade.Letter : "F", (byte)(newSec.IsFocused || newSec.IsMouseOver ? 0xFF : 0x28)), renderingEmSize),
            };
            drwGroup.Children.Add(sectorT);

            GeometryDrawing warnSector = new()
            {
                Geometry = new RectangleGeometry(new Rect(
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

            drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun(newSec.Template.Username, new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, new FontStretch()), renderingEmSize, new Point(newSec.Template.Left - 8, newSec.Template.Top - 8))));

            if (hasReportSec)
            {
                GeometryDrawing sector = new()
                {
                    Geometry = new RectangleGeometry(new Rect(newSec.Report.Left + 0.5, newSec.Report.Top + 0.5, newSec.Report.Width, newSec.Report.Height)),
                    Pen = new Pen(Brushes.Black, 1)
                };
                //sector.Geometry.Transform = new RotateTransform(newSec.Report.AngleDeg, newSec.Report.Left + (newSec.Report.Width / 2), newSec.Report.Top + (newSec.Report.Height / 2));
                drwGroup.Children.Add(sector);


                double x = newSec.Report.Left + (newSec.Report.Width / 2);
                double y = newSec.Report.Top + (newSec.Report.Height / 2);
                secCenter.Children.Add(new LineGeometry(new Point(x + 10, y), new Point(x + -10, y)));
                secCenter.Children.Add(new LineGeometry(new Point(x, y + 10), new Point(x, y + -10)));
            }
        }

        GeometryDrawing sectorCenters = new()
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4)
        };
        drwGroup.Children.Add(sectorCenters);

        if (ShowExtendedData)
            drwGroup.Children.Add(GetModuleGrid(sectors));

        // drwGroup.Transform = new RotateTransform(ImageResults.SelectedScanner.RotateImage ? 180 : 0);

        DrawingImage geometryImage = new(drwGroup);
        geometryImage.Freeze();

        return geometryImage;
    }

    public static GlyphRun CreateGlyphRun(string text, Typeface typeface, double emSize, Point baselineOrigin)
    {
        if (text == null)
            return null;

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

                double dX = (sect.Report.SymbolType == AvailableSymbologies.DataMatrix) ? 0 : (res.ExtendedData.DeltaX / 2);
                double dY = (sect.Report.SymbolType == AvailableSymbologies.DataMatrix) ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

                double startX = -0.5;// sec.left + res.ExtendedData.Xnw - dX + 1 - (qzX * res.ExtendedData.DeltaX);
                double startY = -0.5;// sec.top + res.ExtendedData.Ynw - dY + 1 - (qzY * res.ExtendedData.DeltaY);

                int cnt = 0;

                for (double row = -qzX; row < res.ExtendedData.NumRows + qzX; row++)
                    for (double col = -qzY; col < res.ExtendedData.NumColumns + qzY; col++)
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

                            GlyphRun gr = new(
                                _glyphTypeface,
                                0,
                                false,
                                2,
                                1.0f,
                                _glyphIndexes,
                                new Point(
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

                            GlyphRun gr = new(
                                _glyphTypeface1,
                                0,
                                false,
                                2,
                                1.0f,
                                _glyphIndexes,
                                new Point(
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
                    double x = sect.Report.SymbolType == AvailableSymbologies.DataMatrix
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

    public static void SortList(List<Sectors.Interfaces.ISector> list) => list.Sort((item1, item2) =>
    {
        double distance1 = Math.Sqrt(Math.Pow(item1.Report.CenterPoint.X, 2) + Math.Pow(item1.Report.CenterPoint.Y, 2));
        double distance2 = Math.Sqrt(Math.Pow(item2.Report.CenterPoint.X, 2) + Math.Pow(item2.Report.CenterPoint.Y, 2));
        int distanceComparison = distance1.CompareTo(distance2);

        if (distanceComparison == 0)
        {
            // If distances are equal, sort by X coordinate, then by Y if necessary
            int xComparison = item1.Report.CenterPoint.X.CompareTo(item2.Report.CenterPoint.X);
            if (xComparison == 0)
            {
                // If X coordinates are equal, sort by Y coordinate
                return item1.Report.CenterPoint.Y.CompareTo(item2.Report.CenterPoint.Y);
            }
            return xComparison;
        }
        return distanceComparison;
    });

    //Sort the list by row and column, given x,y coordinates
    public static void SortList2(List<Sectors.Interfaces.ISector> list) => list.Sort((item1, item2) =>
    {
        int row1 = (int)Math.Floor(item1.Report.CenterPoint.Y / item1.Report.Height);
        int row2 = (int)Math.Floor(item2.Report.CenterPoint.Y / item2.Report.Height);
        int rowComparison = row1.CompareTo(row2);
        if (rowComparison == 0)
        {
            // If distances are equal, sort by X coordinate, then by Y if necessary
            int col1 = (int)Math.Floor(item1.Report.CenterPoint.X / item1.Report.Width);
            int col2 = (int)Math.Floor(item2.Report.CenterPoint.X / item2.Report.Width);
            int colComparison = col1.CompareTo(col2);
            if (colComparison == 0)
            {
                // If X coordinates are equal, sort by Y coordinate
                return item1.Report.CenterPoint.Y.CompareTo(item2.Report.CenterPoint.Y);
            }
            return colComparison;
        }
        return rowComparison;
    });

    public void SortList3(List<Sectors.Interfaces.ISector> list)
    {
        //Sort the list from top to bottom, left to right given x,y coordinates
        list = list.OrderBy(x => x.Report.Top).ThenBy(x => x.Report.Left).ToList();
    }
    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Databases.ImageResultsDatabase> message) => SelectedDatabase = message.NewValue;
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

}
