using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.Databases;
using LabelVal.Main.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
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

    [RelayCommand]
    public void BringIntoViewHandler() => BringIntoView?.Invoke();

    //Could be handled with dependency injection.
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <see cref="ImagesMaxHeight"/>>
    [ObservableProperty] private int _imagesMaxHeight = App.Settings.GetValue<int>(nameof(ImagesMaxHeight));
    /// <see cref="DualSectorColumns"/>>
    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue<bool>(nameof(DualSectorColumns));
    /// <see cref="ShowExtendedData"/>>
    [ObservableProperty] private bool showExtendedData = App.Settings.GetValue<bool>(nameof(ShowExtendedData));
    partial void OnShowExtendedDataChanged(bool value)
    {
        foreach (var device in ImageResultDeviceEntries)
            device.RefreshOverlays();
    }

    /// <summary>
    /// Show the Application parameters expander.
    /// </summary>
    [ObservableProperty] private bool showApplicationParameters = App.Settings.GetValue(nameof(ShowApplicationParameters), true, true);
    partial void OnShowApplicationParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowApplicationParameters), value);

    /// <summary>
    /// Show the Grading parameters expander.
    /// </summary>
    [ObservableProperty] private bool showGradingParameters = App.Settings.GetValue(nameof(ShowGradingParameters), true, true);
    partial void OnShowGradingParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowGradingParameters), value);

    /// <summary>
    /// Show the Symbology parameters expander.
    /// </summary>
    [ObservableProperty] private bool showSymbologyParameters = App.Settings.GetValue(nameof(ShowSymbologyParameters), true, true);
    partial void OnShowSymbologyParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowSymbologyParameters), value);

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
        foreach (var device in ImageResultDeviceEntries)
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
    private void Save(byte[] bmp)
    {
        var path = GetSaveFilePath();

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (bmp == null) return;
            using var img = new ImageMagick.MagickImage(bmp);
            File.WriteAllBytes(path,
                Path.GetExtension(path).Contains("png", StringComparison.InvariantCultureIgnoreCase)
                    ? img.ToByteArray(ImageMagick.MagickFormat.Png)
                    : img.ToByteArray(ImageMagick.MagickFormat.Bmp3));

            Clipboard.SetText(path);
        }
        catch (Exception ex)
        { Logger.LogError(ex);}
    }
    [RelayCommand]
    private void Store(ImageResultEntryDevices device)
    {
        var dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.LogError($"Device not found: {device}");
            return;
        }
        _ = dev.Store();
        BringIntoViewHandler();
    }
    [RelayCommand]
    private void Process(ImageResultEntryDevices device)
    {
        var dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.LogError($"Device not found: {device}");
            return;
        }
        dev.Process();
        BringIntoViewHandler();
    }
    [RelayCommand]
    private async Task ClearStored(ImageResultEntryDevices device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            _ = SelectedDatabase.Delete_Result(device, ImageRollUID, SourceImageUID, ImageRollUID);
            var dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
            if (dev == null)
            {
                Logger.LogError($"Device not found: {device}");
                return;
            }
            dev.GetStored();
            BringIntoViewHandler();
        }
    }
    public void DeleteStored()
    {
        _ = SelectedDatabase.Delete_Result(ImageResultEntryDevices.L95, ImageRollUID, SourceImageUID, ImageRollUID);
        _ = SelectedDatabase.Delete_Result(ImageResultEntryDevices.V275, ImageRollUID, SourceImageUID, ImageRollUID);
        _ = SelectedDatabase.Delete_Result(ImageResultEntryDevices.V5, ImageRollUID, SourceImageUID, ImageRollUID);
    }
    [RelayCommand]
    private void ClearCurrent(ImageResultEntryDevices device)
    {
        var dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.LogError($"Device not found: {device}");
            return;
        }
        dev.ClearCurrent();
        BringIntoViewHandler();

    }

    public string? GetName(System.Drawing.Point center)
    {
        string name = null;
        foreach (var dev in ImageResultDeviceEntries)
        {
            //Check the Report center points
            foreach (var sec in dev.StoredSectors)
            {
                if (sec.Report.CenterPoint.Contains(center))
                {
                    name = sec.Template.Name;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(name)) break;

            foreach (var sec in dev.CurrentSectors)
            {
                if (sec.Report.CenterPoint.Contains(center))
                {
                    name = sec.Template.Name;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(name)) break;

            //Check the Template center points
            foreach (var sec in dev.StoredSectors)
            {
                if (sec.Template.CenterPoint.Contains(center))
                {
                    name = sec.Template.Name;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(name)) break;

            foreach (var sec in dev.CurrentSectors)
            {
                if (sec.Template.CenterPoint.Contains(center))
                {
                    name = sec.Template.Name;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(name)) break;

        }
        return name;
    }

    public void HandlerUpdate(ImageResultEntryDevices device)
    {
        if (device == ImageResultEntryDevices.All)
        {
            foreach (var dev in ImageResultDeviceEntries)
                dev.HandlerUpdate();
        }
        else
        {
            var dev = ImageResultDeviceEntries.FirstOrDefault(x => x.Device == device);
            if (dev == null)
            {
                Logger.LogError($"Device not found: {device}");
                return;
            }
            dev.HandlerUpdate();
        }
    }

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