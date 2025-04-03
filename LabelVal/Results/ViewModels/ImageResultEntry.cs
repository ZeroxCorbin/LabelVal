using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Main.ViewModels;
using LabelVal.Results.Databases;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

/// <summary>
/// This is a viewmodel to support both the Image Roll and the Results database information.
/// 
/// </summary>
public partial class ImageResultEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<Databases.ImageResultsDatabase>>, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    public delegate void BringIntoViewDelegate();
    public event BringIntoViewDelegate BringIntoView;

    public delegate void DeleteImageDelegate(ImageResultEntry imageResults);
    public event DeleteImageDelegate DeleteImage;

    //Could be handled with dependency injection.
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <see cref="ImagesMaxHeight"/>>
    [ObservableProperty] private int imagesMaxHeight = App.Settings.GetValue<int>(nameof(ImagesMaxHeight));
    /// <see cref="DualSectorColumns"/>>
    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue<bool>(nameof(DualSectorColumns));
    /// <see cref="ShowExtendedData"/>>
    [ObservableProperty] private bool showExtendedData = App.Settings.GetValue<bool>(nameof(ShowExtendedData));
    partial void OnShowExtendedDataChanged(bool value)
    {
        foreach (IImageResultDeviceEntry device in ImageResultDeviceEntries)
            device.RefreshOverlays();
    }

    /// <summary>
    /// Show the image details for the Source and Device image entries.
    /// </summary>
    [ObservableProperty] private bool showDetails;

    /// <summary>
    /// This is the database where all of the Results are stored.
    /// It can be changed by sending a <see cref="PropertyChangedMessage{ImageResultsDatabase}"/>
    /// When it changes, the results rows for each device are updated.
    /// <see cref="SelectedDatabase"/>
    /// </summary>
    [ObservableProperty] private ImageResultsDatabase selectedDatabase;
    partial void OnSelectedDatabaseChanged(Databases.ImageResultsDatabase value)
    {
        foreach (IImageResultDeviceEntry device in ImageResultDeviceEntries)
            device.GetStored();
    }

    /// <summary>
    /// This manages the Image Roll and Devices.
    /// </summary>
    public ImageResultsManager ImageResultsManager { get; }

    /// <summary>
    /// The currently selected Image Roll UID.
    /// </summary>
    public string ImageRollUID => ImageResultsManager.SelectedImageRoll.UID;

    /// <summary>
    /// The Source image for this entry
    /// This is the same Source image as the Image Roll Entry.
    /// </summary>
    public ImageEntry SourceImage { get; }
    public string SourceImageUID => SourceImage.UID;

    public ObservableCollection<IImageResultDeviceEntry> ImageResultDeviceEntries { get; }
    /// <summary>
    /// How many times to print the image
    /// </summary>
    public int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));

    /// <see cref="ShowPrinterAreaOverSource"/>>
    [ObservableProperty] private bool showPrinterAreaOverSource;
    /// <see cref="PrinterAreaOverlay"/>>
    [ObservableProperty] private DrawingImage printerAreaOverlay;
    partial void OnShowPrinterAreaOverSourceChanged(bool value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;

    private V275_REST_Lib.Printer.Controller PrinterController { get; } = new();
    [ObservableProperty] private PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;

    public ImageResultEntry(ImageEntry sourceImage, ImageResultsManager imageResults)
    {
        ImageResultsManager = imageResults;
        SourceImage = sourceImage;

        ImageResultDeviceEntries = [
            new ImageResultDeviceEntry_V275(this),
            new ImageResultDeviceEntry_V5(this) ,
            new ImageResultDeviceEntry_L95(this) ,
        ];

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
    ~ImageResultEntry()
    {
        App.Settings.PropertyChanged -= (s, e) =>
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

    [RelayCommand]
    private void Save(ImageResultEntryImageTypes type)
    {
        var path = GetSaveFilePath();

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            //var bmp = type == ImageResultEntryImageTypes.V275Stored
            //        ? V275StoredImage.ImageBytes
            //        : type == ImageResultEntryImageTypes.V275Current
            //        ? V275CurrentImage.ImageBytes
            //        : type == ImageResultEntryImageTypes.V5Stored
            //        ? V5StoredImage.ImageBytes
            //        : type == ImageResultEntryImageTypes.V5Current
            //        ? V5CurrentImage.ImageBytes
            //        : type == ImageResultEntryImageTypes.L95Stored
            //        ? L95StoredImage.ImageBytes
            //        : type == ImageResultEntryImageTypes.Source
            //        ? SourceImage.ImageBytes : null;

            //if (bmp != null)
            //{
            //    using var img = new ImageMagick.MagickImage(bmp);
            //    if (Path.GetExtension(path).Contains("png", StringComparison.InvariantCultureIgnoreCase))
            //        File.WriteAllBytes(path, img.ToByteArray(ImageMagick.MagickFormat.Png));
            //    else
            //        File.WriteAllBytes(path, img.ToByteArray(ImageMagick.MagickFormat.Bmp3));

            //    Clipboard.SetText(path);
            //}
        }
        catch { }
    }
    [RelayCommand]
    private void Store(ImageResultEntryDevices device)
    {
        IImageResultDeviceEntry dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.LogError($"Device not found: {device}");
            return;
        }
        _ = dev.Store();
    }
    [RelayCommand]
    private void Process(ImageResultEntryDevices device)
    {
        IImageResultDeviceEntry dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.LogError($"Device not found: {device}");
            return;
        }
        dev.Process();
    }
    [RelayCommand]
    private async Task ClearStored(ImageResultEntryDevices device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            _ = SelectedDatabase.Delete_Result(device, ImageRollUID, SourceImageUID, ImageRollUID);
            IImageResultDeviceEntry dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
            if (dev == null)
            {
                Logger.LogError($"Device not found: {device}");
                return;
            }
            dev.GetStored();
        }
    }
    [RelayCommand]
    private void ClearCurrent(ImageResultEntryDevices device)
    {
        IImageResultDeviceEntry dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.LogError($"Device not found: {device}");
            return;
        }
        dev.ClearCurrent();

        //else if (device == ImageResultEntryDevices.L95)
        //{
        //    //No CurrentReport for L95
        //    //No template used for L95

        //    _ = L95CurrentSectors.Remove(L95CurrentSectorSelected);
        //    L95DiffSectors.Clear();

        //    if (L95CurrentSectors.Count == 0)
        //    {
        //        L95CurrentImage = null;
        //        L95CurrentImageOverlay = null;
        //    }
        //}
    }

    public void HandlerUpdate(ImageResultEntryDevices device)
    {
        if (device == ImageResultEntryDevices.All)
        {
            foreach (IImageResultDeviceEntry dev in ImageResultDeviceEntries)
                dev.HandlerUpdate();
        }
        else
        {
            IImageResultDeviceEntry dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
            if (dev == null)
            {
                Logger.LogError($"Device not found: {device}");
                return;
            }
            dev.HandlerUpdate();
        }
        
        //ImageResultsManager.HandlerUpdate();
    }

    //[RelayCommand] private void RedoFiducial() => ImageUtilities.lib.Core.ImageUtilities.RedrawFiducial(SourceImage.Path, false);

    [RelayCommand] private void Delete() => DeleteImage?.Invoke(this);

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

    public List<Sectors.Interfaces.ISector> SortList3(List<Sectors.Interfaces.ISector> list) =>
        //Sort the list from top to bottom, left to right given x,y coordinates
        list.OrderBy(x => x.Report.Top).ThenBy(x => x.Report.Left).ToList();

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Databases.ImageResultsDatabase> message) => SelectedDatabase = message.NewValue;
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

}
