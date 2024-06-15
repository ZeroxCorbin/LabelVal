using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using LabelVal.Utilities;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using V275_REST_lib.Models;

namespace LabelVal.ImageRolls.ViewModels;

public partial class ImageResultEntry : ObservableRecipient,
    IRecipient<PropertyChangedMessage<Node>>,
    IRecipient<PropertyChangedMessage<Databases.ImageResults>>,
    IRecipient<PropertyChangedMessage<Scanner>>,
    IRecipient<PropertyChangedMessage<PrinterSettings>>
{

    public delegate void BringIntoViewDelegate();
    public event BringIntoViewDelegate BringIntoView;


    public delegate void DeleteImageDelegate(ImageResultEntry imageResults);
    public event DeleteImageDelegate DeleteImage;

    //public delegate void StatusChange(string status);
    //public event StatusChange StatusChanged;

    //[ObservableProperty] private string status;
    //partial void OnStatusChanged(string value) => App.Current.Dispatcher.Invoke(() => StatusChanged?.Invoke(Status));

    public ImageEntry SourceImage { get; }
    [ObservableProperty] System.Windows.Media.DrawingImage printerAreaOverlay;

    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private ImageRollEntry selectedImageRoll;
    [ObservableProperty] private Scanner selectedScanner;
    [ObservableProperty] private PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value)
    {
        PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;
        OnShowDetailsChanged(ShowDetails);
    } 

    [ObservableProperty] private Databases.ImageResults selectedDatabase;
    partial void OnSelectedDatabaseChanged(Databases.ImageResults value) => GetStored();

    [ObservableProperty] private Sectors.ViewModels.Sector selectedSector;


    [ObservableProperty] private bool showPrinterAreaOverSource;
    partial void OnShowPrinterAreaOverSourceChanged(bool value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;


    [ObservableProperty] private bool showDetails;
    partial void OnShowDetailsChanged(bool value)
    {
        if (value)
        {
            SourceImage?.InitPrinterVariables(SelectedPrinter);
            V275Image?.InitPrinterVariables(SelectedPrinter);
            V5Image?.InitPrinterVariables(SelectedPrinter);
        }
    }

    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public ImageResultEntry(ImageEntry sourceImage, Node selectedNode, ImageRollEntry selectedImageRoll, Databases.ImageResults selectedDatabase, Scanner selectedScanner, PrinterSettings selectedPrinter)
    {
        SourceImage = sourceImage;

        // GetImage(imagePath);

        SelectedImageRoll = selectedImageRoll;
        SelectedNode = selectedNode;
        SelectedDatabase = selectedDatabase;
        SelectedScanner = selectedScanner;
        SelectedPrinter = selectedPrinter;

        IsActive = true;
    }


    private void SendStatusMessage(string message, SystemMessages.StatusMessageType type) => WeakReferenceMessenger.Default.Send(new SystemMessages.StatusMessage(this, type, message));
    private void SendErrorMessage(string message) => WeakReferenceMessenger.Default.Send(new SystemMessages.StatusMessage(this, SystemMessages.StatusMessageType.Error, message));

    public void Receive(PropertyChangedMessage<Node> message) => SelectedNode = message.NewValue;
    public void Receive(PropertyChangedMessage<Databases.ImageResults> message) => SelectedDatabase = message.NewValue;
    public void Receive(PropertyChangedMessage<Scanner> message) => SelectedScanner = message.NewValue;
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;


    public async Task<MessageDialogResult> OkCancelDialog(string title, string message)
    {

        var result = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

        return result;
    }

    //private void GetImage(string imagePath)
    //{
    //    SourceImage = File.ReadAllBytes(imagePath);
    //    SourceImageUID = ImageUtilities.ImageUID(SourceImage);
    //}
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

        var path = GetSaveFilePath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            byte[] bmp = null;
            if (type == "v275Stored")
                bmp = V275ResultRow.Stored.GetBitmapBytes();
            else if (type == "v275Current")
                bmp = V275Image.GetBitmapBytes();
            else if (type == "v5Stored")
                bmp = V5ResultRow.Stored.GetBitmapBytes();
            else if (type == "v5Current")
                bmp = V5Image.GetBitmapBytes();
            else
                bmp = SourceImage.GetBitmapBytes();

            if (bmp != null)
            {
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
        }
        catch (Exception)
        {

        }
    }
    [RelayCommand]
    private async Task Store(string device)
    {
        if (device == "V275")
        {
            if (V275StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V275Result(new Databases.ImageResults.V275Result
            {
                SourceImageUID = SourceImage.UID,
                ImageRollUID = SelectedImageRoll.UID,

                SourceImage = JsonConvert.SerializeObject(SourceImage),
                StoredImage = JsonConvert.SerializeObject(V275Image),

                Template = JsonConvert.SerializeObject(V275CurrentTemplate),
                Report = JsonConvert.SerializeObject(V275CurrentReport),
            });

            ClearRead(device, true);
        }
        else if (device == "V5")
        {
            if (V5StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V5Result(new Databases.ImageResults.V5Result
            {
                SourceImageUID = SourceImage.UID,
                ImageRollUID = SelectedImageRoll.UID,

                SourceImage = JsonConvert.SerializeObject(SourceImage),
                StoredImage = JsonConvert.SerializeObject(V5Image),

                Report = JsonConvert.SerializeObject(V5CurrentReport),
            });

            ClearRead(device, true);
        }
        else if (device == "L95xx")
        {
            if (L95xxStoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            var temp = new List<L95xxReport>();
            foreach (var sec in L95xxCurrentSectors)
                temp.Add(new L95xxReport() { Report = sec.L95xxPacket, Template = sec.Template });

            _ = SelectedDatabase.InsertOrReplace_L95xxResult(new Databases.ImageResults.L95xxResult
            {
                ImageRollUID = SelectedImageRoll.UID,
                SourceImageUID = SourceImage.UID,
                SourceImage = SourceImage.GetBitmapBytes(),
                Report = JsonConvert.SerializeObject(temp),
                //StoredImage = L95xxImage
            });

            ClearRead(device, true);
        }
    }
    [RelayCommand]
    private async Task ClearStored(string device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            if (device == "V275")
            {
                _ = SelectedDatabase.Delete_V275Result(SelectedImageRoll.UID, SourceImage.UID);
                V275GetStored();
            }
            else if (device == "V5")
            {
                _ = SelectedDatabase.Delete_V5Result(SelectedImageRoll.UID, SourceImage.UID);
                V5GetStored();
            }
            else if (device == "L95xx")
            {
                _ = SelectedDatabase.Delete_L95xxResult(SelectedImageRoll.UID, SourceImage.UID);
                L95xxGetStored();
            }
        }
    }
    [RelayCommand]
    private void ClearRead(string device) => ClearRead(device, false);
    private void ClearRead(string device, bool getStored)
    {
        if (device == "V275")
        {
            V275CurrentReport = null;
            V275CurrentTemplate = null;

            V275CurrentSectors.Clear();
            V275DiffSectors.Clear();

            V275ResultRow = SelectedDatabase.Select_V275Result(SelectedImageRoll.UID, SourceImage.UID);

            if (V275ResultRow == null)
            {
                V275Image = null;
                V275SectorsImageOverlay = null;
                IsV275ImageStored = false;
            }
            else
            {
                if (getStored)
                    V275GetStored();
                else
                {
                    V275Image = V275ResultRow.Stored;
                    V275SectorsImageOverlay = V275CreateSectorsImageOverlay(V275ResultRow._Job, false, V275ResultRow._Report);
                    IsV275ImageStored = true;
                }
            }
        }
        else if (device == "V5")
        {
            V5CurrentReport = null;

            V5CurrentSectors.Clear();
            V5DiffSectors.Clear();

            V5ResultRow = SelectedDatabase.Select_V5Result(SelectedImageRoll.UID, SourceImage.UID);

            if (V5ResultRow == null)
            {
                V5Image = null;
                V5SectorsImageOverlay = null;
                IsV5ImageStored = false;
            }
            else
            {
                if (getStored)
                    V5GetStored();
                else
                {
                    V5Image = V5ResultRow.Stored;
                    V5SectorsImageOverlay = V5CreateSectorsImageOverlay(V5ResultRow._Report);
                    IsV5ImageStored = true;
                }
            }
        }
        else if (device == "L95xx")
        {
            L95xxCurrentReport = null;

            L95xxCurrentSectors.Clear();
            L95xxDiffSectors.Clear();

            L95xxResultRow = SelectedDatabase.Select_L95xxResult(SelectedImageRoll.UID, SourceImage.UID);

            if (L95xxResultRow != null)
                if (getStored)
                    L95xxGetStored();
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
        var saveFileDialog1 = new SaveFileDialog();
        saveFileDialog1.Filter = "Bitmap Image|*.bmp";//|Gif Image|*.gif|JPeg Image|*.jpg";
        saveFileDialog1.Title = "Save an Image File";
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

        var lineWidth = 10 * xRatio;

        var printer = new System.Windows.Media.GeometryDrawing
        {
            Geometry = new System.Windows.Media.RectangleGeometry(new Rect(lineWidth / 2, lineWidth / 2,
            ((SelectedPrinter.DefaultPageSettings.PaperSize.Width / 100) * SelectedPrinter.DefaultPageSettings.PrinterResolution.X * xRatio) - lineWidth,
            ((SelectedPrinter.DefaultPageSettings.PaperSize.Height / 100) * SelectedPrinter.DefaultPageSettings.PrinterResolution.Y * yRatio) - lineWidth)),
            Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, lineWidth)
        };

        var drwGroup = new System.Windows.Media.DrawingGroup();
        drwGroup.Children.Add(printer);

        var geometryImage = new System.Windows.Media.DrawingImage(drwGroup);
        geometryImage.Freeze();
        return geometryImage;
    }
}
