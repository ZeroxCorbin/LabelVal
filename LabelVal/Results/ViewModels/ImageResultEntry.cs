using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.LVS_95xx.Models;
using LabelVal.Results.Databases;
using LabelVal.Utilities;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

public partial class ImageResultEntry : ObservableRecipient, IImageResultEntry, IRecipient<PropertyChangedMessage<Databases.ImageResultsDatabase>>, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    public delegate void BringIntoViewDelegate();
    public event BringIntoViewDelegate BringIntoView;

    public delegate void DeleteImageDelegate(ImageResultEntry imageResults);
    public event DeleteImageDelegate DeleteImage;

    public ImageEntry SourceImage { get; }
    public string SourceImageUID => SourceImage.UID;

    public ImageResults ImageResults { get; }
    public string ImageRollUID => ImageResults.SelectedImageRoll.UID;

    public bool IsPlaceholder => SourceImage.IsPlaceholder;

    [ObservableProperty] private int imagesMaxHeight = App.Settings.GetValue<int>(nameof(ImagesMaxHeight));
    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue<bool>(nameof(DualSectorColumns));
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

    [ObservableProperty] private bool showPrinterAreaOverSource;
    [ObservableProperty] private DrawingImage printerAreaOverlay;
    partial void OnShowPrinterAreaOverSourceChanged(bool value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;

    [ObservableProperty] private PrinterSettings selectedPrinter;
    [ObservableProperty] private ImageResultsDatabase selectedDatabase;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;
    partial void OnSelectedDatabaseChanged(Databases.ImageResultsDatabase value) => GetStored();

    [ObservableProperty] private Sectors.Interfaces.ISector selectedSector;

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
        WeakReferenceMessenger.Default.Send(mes2);
        SelectedPrinter = mes2.Response;

        RequestMessage<ImageResultsDatabase> mes4 = new();
        WeakReferenceMessenger.Default.Send(mes4);
        SelectedDatabase = mes4.Response;
    }

    public StoredImageResultGroup GetStoredImageResultGroup(string runUID) => new()
    {
        RunUID = runUID,
        ImageRollUID = ImageRollUID,
        SourceImageUID = SourceImageUID,
        V275Result = V275ResultRow,
        V5Result = V5ResultRow,
        L95xxResult = L95xxResultRow,
    };

    public CurrentImageResultGroup GetCurrentImageResultGroup(string runUID) => new()
    {
        RunUID = runUID,
        ImageRollUID = ImageRollUID,
        SourceImageUID = SourceImageUID,
        V275Result = new Databases.V275Result
        {
            RunUID = runUID,
            SourceImageUID = SourceImageUID,
            ImageRollUID = ImageRollUID,

            SourceImage = JsonConvert.SerializeObject(SourceImage),
            StoredImage = JsonConvert.SerializeObject(V275CurrentImage),

            Template = JsonConvert.SerializeObject(V275CurrentTemplate),
            Report = JsonConvert.SerializeObject(V275CurrentReport),
        },
        V5Result = new Databases.V5Result
        {
            RunUID = runUID,
            SourceImageUID = SourceImageUID,
            ImageRollUID = ImageRollUID,

            SourceImage = JsonConvert.SerializeObject(SourceImage),
            StoredImage = JsonConvert.SerializeObject(V5CurrentImage),

            Template = JsonConvert.SerializeObject(V5CurrentTemplate),
            Report = JsonConvert.SerializeObject(V5CurrentReport),
        },
        L95xxResult = new Databases.L95xxResult
        {
            RunUID = runUID,
            ImageRollUID = ImageRollUID,
            SourceImageUID = SourceImageUID,

            SourceImage = JsonConvert.SerializeObject(SourceImage),
            //Report = JsonConvert.SerializeObject(L95xxStoredSectors.Select(x => new L95xxReport() { Report = ((LVS_95xx.Sectors.Sector)x).L95xxPacket, Template = (LVS_95xx.Sectors.Template)x.Template }).ToList()),
        },
    };

    private void GetStored()
    {
        V275GetStored();
        V5GetStored();
        L95xxGetStored();
    }

    [RelayCommand]
    private void Save(string type)
    {
        SendTo95xxApplication();

        string path = GetSaveFilePath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            byte[] bmp = type == "v275Stored"
                    ? V275StoredImage.GetBitmapBytes()
                    : type == "v275Current"
                    ? V275CurrentImage.GetBitmapBytes()
                    : type == "v5Stored"
                    ? V5StoredImage.GetBitmapBytes()
                    : type == "v5Current"
                    ? V5CurrentImage.GetBitmapBytes()
                    : type == "l95xxStored"
                    ? L95xxStoredImage.GetBitmapBytes()
                    : type == "l95xxCurrent"
                    ? L95xxCurrentImage.GetBitmapBytes()
                    : SourceImage.GetBitmapBytes();
            if (bmp != null)
            {
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
        }
        catch { }
    }
    [RelayCommand]
    private async Task Store(string device)
    {
        if (device == "V275")
        {
            if (V275StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V275Result(new Databases.V275Result
            {
                SourceImageUID = SourceImageUID,
                ImageRollUID = ImageRollUID,

                SourceImage = JsonConvert.SerializeObject(SourceImage),
                StoredImage = JsonConvert.SerializeObject(V275CurrentImage),

                Template = JsonConvert.SerializeObject(V275CurrentTemplate),
                Report = JsonConvert.SerializeObject(V275CurrentReport),
            });

            ClearRead(device);

            V275GetStored();
        }
        else if (device == "V5")
        {
            if (V5StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V5Result(new Databases.V5Result
            {
                SourceImageUID = SourceImageUID,
                ImageRollUID = ImageRollUID,

                SourceImage = JsonConvert.SerializeObject(SourceImage),
                StoredImage = JsonConvert.SerializeObject(V5CurrentImage),

                Template = JsonConvert.SerializeObject(V5CurrentTemplate),
                Report = JsonConvert.SerializeObject(V5CurrentReport),
            });

            ClearRead(device);

            V5GetStored();
        }
        else if (device == "L95xx")
        {

            if (L95xxCurrentSectorSelected == null)
            {
                LogError("No sector selected to store.");
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
            if(L95xxResultRow != null)
                temp = L95xxResultRow._Report;
            
            temp.Add(((LVS_95xx.Sectors.Sector)L95xxCurrentSectorSelected).L95xxFullReport);

            _ = SelectedDatabase.InsertOrReplace_L95xxResult(new Databases.L95xxResult
            {
                ImageRollUID = ImageRollUID,
                SourceImageUID = SourceImageUID,

                SourceImage = JsonConvert.SerializeObject(SourceImage),
                StoredImage = JsonConvert.SerializeObject(L95xxCurrentImage),

                Report = JsonConvert.SerializeObject(temp),
            });

            ClearRead(device);

            L95xxGetStored();
        }
    }
    [RelayCommand]
    private async Task ClearStored(string device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            if (device == "V275")
            {
                _ = SelectedDatabase.Delete_V275Result(ImageRollUID, SourceImageUID);
                V275GetStored();
            }
            else if (device == "V5")
            {
                _ = SelectedDatabase.Delete_V5Result(ImageRollUID, SourceImageUID);
                V5GetStored();
            }
            else if (device == "L95xx")
            {
                _ = SelectedDatabase.Delete_L95xxResult(ImageRollUID, SourceImageUID);
                L95xxGetStored();
            }
        }
    }
    [RelayCommand]
    private void ClearRead(string device)
    {
        if (device == "V275")
        {
            V275CurrentReport = null;
            V275CurrentTemplate = null;

            V275CurrentSectors.Clear();
            V275DiffSectors.Clear();
            V275CurrentImage = null;
            V275CurrentImageOverlay = null;
        }
        else if (device == "V5")
        {
            V5CurrentReport = null;
            V5CurrentTemplate = null;

            V5CurrentSectors.Clear();
            V5DiffSectors.Clear();
            V5CurrentImage = null;
            V5CurrentImageOverlay = null;
        }
        else if (device == "L95xx")
        {
            //No CurrentReport for L95xx
            //No template used for L95xx

            L95xxCurrentSectors.Remove(L95xxCurrentSectorSelected);
            //Diff sectors not used for L95xx, yet

            if(L95xxCurrentSectors.Count == 0)
            {
                L95xxCurrentImage = null;
                L95xxCurrentImageOverlay = null;
            }
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
            Filter = "Bitmap Image|*.bmp",//|Gif Image|*.gif|JPeg Image|*.jpg";
            Title = "Save an Image File"
        };
        _ = saveFileDialog1.ShowDialog();

        return saveFileDialog1.FileName;
    }
    private string SaveImageBytesToFile(string path, byte[] img)
    {
        File.WriteAllBytes(path, img);

        return "";
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

    private DrawingImage CreateSectorsImageOverlay(ImageEntry image, ObservableCollection<Sectors.Interfaces.ISector> sectors)
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
        foreach (Sectors.Interfaces.ISector newSec in sectors)
        {
            if (newSec.Report.SymbolType is "blemish" or "ocr" or "ocv")
                continue;

            GeometryDrawing sector = new()
            {
                Geometry = new RectangleGeometry(new Rect(newSec.Report.Left, newSec.Report.Top, newSec.Report.Width, newSec.Report.Height)),
                Pen = new Pen(GetGradeBrush(newSec.Report.OverallGradeLetter), 5)
            };
            drwGroup.Children.Add(sector);
            drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun(newSec.Template.Username, new Typeface("Arial"), 30.0, new Point(newSec.Report.Left - 8, newSec.Report.Top - 8))));

            GeometryDrawing sectorT = new()
            {
                Geometry = new RectangleGeometry(new Rect(newSec.Template.Left, newSec.Template.Top, newSec.Template.Width, newSec.Template.Height)),
                Pen = new Pen(Brushes.Black, 1)
            };
            drwGroup.Children.Add(sectorT);

            double y = newSec.Report.Top + (newSec.Report.Height / 2);
            double x = newSec.Report.Left + (newSec.Report.Width / 2);
            secCenter.Children.Add(new LineGeometry(new Point(x + 10, y), new Point(x + -10, y)));
            secCenter.Children.Add(new LineGeometry(new Point(x, y + 10), new Point(x, y + -10)));
        }

        GeometryDrawing sectorCenters = new()
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4)
        };
        drwGroup.Children.Add(sectorCenters);

        if(ShowExtendedData)
            drwGroup.Children.Add(GetModuleGrid(sectors));

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
    private static DrawingGroup GetModuleGrid(ObservableCollection<Sectors.Interfaces.ISector> sectors)
    {
        DrawingGroup drwGroup = new();

        foreach (Sectors.Interfaces.ISector sect in sectors)
        {

            if (sect == null)
                continue;

            if (sect.Report.SymbolType is "qrCode" or "dataMatrix")
            {
                Sectors.Interfaces.IReport res = sect.Report;

                if (res.ExtendedData == null)
                    continue;

                if (res.ExtendedData.ModuleReflectance == null)
                    continue;

                GeometryGroup moduleGrid = new();
                DrawingGroup textGrp = new();

                double qzX = (sect.Report.SymbolType == "dataMatrix") ? 0 : res.ExtendedData.QuietZone;
                double qzY = res.ExtendedData.QuietZone;

                double dX = (sect.Report.SymbolType == "dataMatrix") ? 0 : (res.ExtendedData.DeltaX / 2);
                double dY = (sect.Report.SymbolType == "dataMatrix") ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

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
                    double x = sect.Report.SymbolType == "dataMatrix"
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

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Databases.ImageResultsDatabase> message) => SelectedDatabase = message.NewValue;
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
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
