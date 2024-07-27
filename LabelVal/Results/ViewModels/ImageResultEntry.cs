using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Utilities;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    public string ImageUID => SourceImage.UID;

    public ImageResults ImageResults { get; }
    public string RollUID => ImageResults.SelectedImageRoll.UID;

    public bool IsPlaceholder => SourceImage.IsPlaceholder;

    [ObservableProperty]private int imagesMaxHeight = App.Settings.GetValue(nameof(ImagesMaxHeight), 200, true);

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
        if(value)
        {
            SourceImage?.InitPrinterVariables(SelectedPrinter);

            V275CurrentImage?.InitPrinterVariables(SelectedPrinter);
            V275StoredImage?.InitPrinterVariables(SelectedPrinter);

            V5CurrentImage?.InitPrinterVariables(SelectedPrinter);
            V5StoredImage?.InitPrinterVariables(SelectedPrinter);
        }
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
                ImagesMaxHeight = App.Settings.GetValue(nameof(ImagesMaxHeight), 200, true);
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
        ImageRollUID = RollUID,
        SourceImageUID = ImageUID,
        V275Result = V275ResultRow,
        V5Result = V5ResultRow,
        L95xxResult = L95xxResultRow,
    };

    public CurrentImageResultGroup GetCurrentImageResultGroup(string runUID) => new()
    {
        RunUID = runUID,
        ImageRollUID = RollUID,
        SourceImageUID = ImageUID,
        V275Result = new Databases.V275Result
        {
            RunUID = runUID,
            SourceImageUID = ImageUID,
            ImageRollUID = RollUID,

            SourceImage = JsonConvert.SerializeObject(SourceImage),
            StoredImage = JsonConvert.SerializeObject(V275CurrentImage),

            Template = JsonConvert.SerializeObject(V275CurrentTemplate),
            Report = JsonConvert.SerializeObject(V275CurrentReport),
        },
        V5Result = new Databases.V5Result
        {
            RunUID = runUID,
            SourceImageUID = ImageUID,
            ImageRollUID = RollUID,

            SourceImage = JsonConvert.SerializeObject(SourceImage),
            StoredImage = JsonConvert.SerializeObject(V5CurrentImage),

            Template = JsonConvert.SerializeObject(V5CurrentTemplate),
            Report = JsonConvert.SerializeObject(V5CurrentReport),
        },
        L95xxResult = new Databases.L95xxResult
        {
            RunUID = runUID,
            ImageRollUID = RollUID,
            SourceImageUID = ImageUID,

            SourceImage = JsonConvert.SerializeObject(SourceImage),
            Report = JsonConvert.SerializeObject(L95xxStoredSectors.Select(x => new L95xxReport() { Report = ((LVS_95xx.Sectors.Sector)x).L95xxPacket, Template = x.Template }).ToList()),
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
                SourceImageUID = ImageUID,
                ImageRollUID = RollUID,

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
                SourceImageUID = ImageUID,
                ImageRollUID = RollUID,

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
                else //Remove the old sector from the stored list.
                    L95xxStoredSectors.Remove(old);
            }

            //Add the selected sector to the stored sectors list.
            L95xxStoredSectors.Add(L95xxCurrentSectorSelected);
            //Remove it from the current sectors list.
            L95xxCurrentSectors.Remove(L95xxCurrentSectorSelected);

            //Sort the stored list.
            List<Sectors.Interfaces.ISector> secs = L95xxStoredSectors.ToList();
            SortList(secs);
            SortObservableCollectionByList(secs, L95xxStoredSectors);

            //Save the list to the database.
            List<L95xxReport> temp = [];
            foreach (Sectors.Interfaces.ISector sec in L95xxStoredSectors)
                temp.Add(new L95xxReport() { Report = ((LVS_95xx.Sectors.Sector)sec).L95xxPacket, Template = sec.Template });

            _ = SelectedDatabase.InsertOrReplace_L95xxResult(new Databases.L95xxResult
            {
                ImageRollUID = RollUID,
                SourceImageUID = ImageUID,
                SourceImage = JsonConvert.SerializeObject(SourceImage),
                Report = JsonConvert.SerializeObject(temp),
            });
        }
    }
    [RelayCommand]
    private async Task ClearStored(string device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            if (device == "V275")
            {
                _ = SelectedDatabase.Delete_V275Result(RollUID, ImageUID);
                V275GetStored();
            }
            else if (device == "V5")
            {
                _ = SelectedDatabase.Delete_V5Result(RollUID, ImageUID);
                V5GetStored();
            }
            else if (device == "L95xx")
            {
                _ = SelectedDatabase.Delete_L95xxResult(RollUID, ImageUID);
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
            L95xxCurrentReport = null;

            L95xxCurrentSectors.Remove(L95xxCurrentSectorSelected);

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
