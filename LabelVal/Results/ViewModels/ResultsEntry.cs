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
using System.ComponentModel;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

/// <summary>
/// This is a viewmodel to support both the Image Roll and the Results database information.
/// </summary>
public partial class ResultsEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<ResultsDatabase>>, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    #region Delegates
    /// <summary>
    /// Delegate for the BringIntoView event.
    /// </summary>
    public delegate void BringIntoViewDelegate();
    /// <summary>
    /// Delegate for the DeleteImage event.
    /// </summary>
    /// <param name="imageResults">The image result entry to delete.</param>
    public delegate void DeleteImageDelegate(ResultsEntry imageResults);
    #endregion

    #region Events
    /// <summary>
    /// Occurs when a request is made to bring this entry into view.
    /// </summary>
    public event BringIntoViewDelegate BringIntoView;
    /// <summary>
    /// Occurs when a request is made to delete this image entry.
    /// </summary>
    public event DeleteImageDelegate DeleteImage;
    #endregion

    #region Fields
    private readonly PropertyChangedEventHandler _settingsPropertyChangedHandler;
    #endregion

    #region Properties

    #region Global Settings
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
        foreach (var device in ResultsDeviceEntries)
            device.RefreshOverlays();
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the Application parameters expander.
    /// </summary>
    [ObservableProperty] private bool showApplicationParameters = App.Settings.GetValue(nameof(ShowApplicationParameters), true, true);
    partial void OnShowApplicationParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowApplicationParameters), value);

    /// <summary>
    /// Gets or sets a value indicating whether to show the Grading parameters expander.
    /// </summary>
    [ObservableProperty] private bool showGradingParameters = App.Settings.GetValue(nameof(ShowGradingParameters), true, true);
    partial void OnShowGradingParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowGradingParameters), value);

    /// <summary>
    /// Gets or sets a value indicating whether to show the Symbology parameters expander.
    /// </summary>
    [ObservableProperty] private bool showSymbologyParameters = App.Settings.GetValue(nameof(ShowSymbologyParameters), true, true);
    partial void OnShowSymbologyParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowSymbologyParameters), value);

    #endregion

    #region UI State Properties
    /// <summary>
    /// Gets or sets a value indicating whether to show the image details for the Source and Device image entries.
    /// </summary>
    [ObservableProperty] private bool showDetails;

    /// <summary>
    /// Gets or sets a value indicating whether this is the topmost visible item in the scroll viewer.
    /// </summary>
    [ObservableProperty] private bool isTopmost;

    #endregion

    #region Data Properties
    /// <summary>
    /// This is the database where all of the Results are stored.
    /// It can be changed by sending a <see cref="PropertyChangedMessage{ResultssDatabase}"/>
    /// When it changes, the results rows for each device are updated.
    /// <see cref="SelectedResultsDatabase"/>
    /// </summary>
    [ObservableProperty] private ResultsDatabase selectedResultsDatabase;
    partial void OnSelectedResultsDatabaseChanged(ResultsDatabase value)
    {
        foreach (var device in ResultsDeviceEntries)
            device.GetStored();
    }

    /// <summary>
    /// Gets the manager for Image Rolls and Devices.
    /// </summary>
    public ResultssManager ResultssManager { get; }

    /// <summary>
    /// Gets the currently selected Image Roll UID.
    /// </summary>
    public string ImageRollUID => ResultssManager.SelectedImageRoll.UID;

    /// <summary>
    /// Gets the Source image for this entry. This is the same Source image as the Image Roll Entry.
    /// </summary>
    public ImageEntry SourceImage { get; }
    /// <summary>
    /// Gets the UID of the source image.
    /// </summary>
    public string SourceImageUID => SourceImage.UID;

    /// <summary>
    /// Gets the collection of device-specific image result entries.
    /// </summary>
    public ObservableCollection<IResultsDeviceEntry> ResultsDeviceEntries { get; }
    #endregion

    #region Printer Properties
    /// <summary>
    /// Gets how many times to print the image.
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
    #endregion

    #endregion

    #region Constructor and Finalizer
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsEntry"/> class.
    /// </summary>
    /// <param name="sourceImage">The source image entry.</param>
    /// <param name="imageResults">The image results manager.</param>
    public ResultsEntry(ImageEntry sourceImage, ResultssManager imageResults)
    {
        ResultssManager = imageResults;
        SourceImage = sourceImage;

        ResultsDeviceEntries =
        [
            new ResultsDeviceEntryV275(this),
            new ResultsDeviceEntry_V5(this),
            new ResultsDeviceEntry_L95(this),
        ];

        IsActive = true;
        ReceiveAll();

        _settingsPropertyChangedHandler = (s, e) =>
        {
            if (e.PropertyName == nameof(ImagesMaxHeight))
                ImagesMaxHeight = App.Settings.GetValue<int>(nameof(ImagesMaxHeight));
            else if (e.PropertyName == nameof(DualSectorColumns))
                DualSectorColumns = App.Settings.GetValue<bool>(nameof(DualSectorColumns));
            else if (e.PropertyName == nameof(ShowExtendedData))
                ShowExtendedData = App.Settings.GetValue<bool>(nameof(ShowExtendedData));
        };
        App.Settings.PropertyChanged += _settingsPropertyChangedHandler;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ResultsEntry"/> class.
    /// </summary>
    ~ResultsEntry()
    {
        App.Settings.PropertyChanged -= _settingsPropertyChangedHandler;
    }
    #endregion

    #region Commands
    /// <summary>
    /// Invokes the <see cref="BringIntoView"/> event to scroll this item into the visible area.
    /// </summary>
    [RelayCommand]
    public void BringIntoViewHandler() => BringIntoView?.Invoke();

    /// <summary>
    /// Saves the provided image data to a file.
    /// </summary>
    /// <param name="bmp">The image data in BMP format.</param>
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
        { Logger.Error(ex); }
    }

    /// <summary>
    /// Stores the current results for a specific device to the database.
    /// </summary>
    /// <param name="device">The device to store results for.</param>
    [RelayCommand]
    private void Store(ResultsEntryDevices device)
    {
        var dev = ResultsDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.Error($"Device not found: {device}");
            return;
        }
        _ = dev.Store();
        BringIntoViewHandler();
    }

    /// <summary>
    /// Processes the image for a specific device.
    /// </summary>
    /// <param name="device">The device to process the image with.</param>
    [RelayCommand]
    private void Process(ResultsEntryDevices device)
    {
        var dev = ResultsDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.Error($"Device not found: {device}");
            return;
        }
        dev.Process();
        BringIntoViewHandler();
    }

    /// <summary>
    /// Clears the stored sectors for a specific device after user confirmation.
    /// </summary>
    /// <param name="device">The device whose stored sectors will be cleared.</param>
    [RelayCommand]
    private async Task ClearStored(ResultsEntryDevices device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            _ = SelectedResultsDatabase.Delete_Result(device, ImageRollUID, SourceImageUID, ImageRollUID);
            var dev = ResultsDeviceEntries.FirstOrDefault(x => x.Device == device);
            if (dev == null)
            {
                Logger.Error($"Device not found: {device}");
                return;
            }
            dev.GetStored();
            BringIntoViewHandler();
        }
    }

    /// <summary>
    /// Clears the currently processed (but not stored) sectors for a specific device.
    /// </summary>
    /// <param name="device">The device whose current sectors will be cleared.</param>
    [RelayCommand]
    private void ClearCurrent(ResultsEntryDevices device)
    {
        var dev = ResultsDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.Error($"Device not found: {device}");
            return;
        }
        dev.ClearCurrent();
        BringIntoViewHandler();

    }

    /// <summary>
    /// Invokes the <see cref="DeleteImage"/> event to delete this entry.
    /// </summary>
    [RelayCommand]
    private void Delete() => DeleteImage?.Invoke(this);
    #endregion

    #region Public Methods
    /// <summary>
    /// Deletes all stored results associated with this image entry from the database.
    /// </summary>
    public void DeleteStored()
    {
        _ = SelectedResultsDatabase.Delete_Result(ResultsEntryDevices.L95, ImageRollUID, SourceImageUID, ImageRollUID);
        _ = SelectedResultsDatabase.Delete_Result(ResultsEntryDevices.V275, ImageRollUID, SourceImageUID, ImageRollUID);
        _ = SelectedResultsDatabase.Delete_Result(ResultsEntryDevices.V5, ImageRollUID, SourceImageUID, ImageRollUID);
    }

    /// <summary>
    /// Gets the name of a sector at a specific point on the image.
    /// </summary>
    /// <param name="center">The point to check.</param>
    /// <returns>The name of the sector if found; otherwise, null.</returns>
    public string? GetName(System.Drawing.Point center)
    {
        string name = null;
        foreach (var dev in ResultsDeviceEntries)
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

    /// <summary>
    /// Triggers a handler update for a specific device or all devices.
    /// </summary>
    /// <param name="device">The device to update. Use 'All' to update all devices.</param>
    public void HandlerUpdate(ResultsEntryDevices device)
    {
        if (device == ResultsEntryDevices.All)
        {
            foreach (var dev in ResultsDeviceEntries)
                dev.HandlerUpdate();
        }
        else
        {
            var dev = ResultsDeviceEntries.FirstOrDefault(x => x.Device == device);
            if (dev == null)
            {
                Logger.Error($"Device not found: {device}");
                return;
            }
            dev.HandlerUpdate();
        }
    }

    /// <summary>
    /// Creates an overlay representing the printable area of the selected printer.
    /// </summary>
    /// <param name="useRatio">If true, scales the overlay to match the low-resolution image dimensions.</param>
    /// <returns>A <see cref="DrawingImage"/> of the printer area overlay.</returns>
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

    /// <summary>
    /// Sorts a list of sectors from top to bottom, then left to right.
    /// </summary>
    /// <param name="list">The list of sectors to sort.</param>
    /// <returns>The sorted list of sectors.</returns>
    public List<Sectors.Interfaces.ISector> SortList3(List<Sectors.Interfaces.ISector> list) =>
        //Sort the list from top to bottom, left to right given x,y coordinates
        list.OrderBy(x => x.Report.Top).ThenBy(x => x.Report.Left).ToList();
    #endregion

    #region Private Methods
    /// <summary>
    /// Requests initial state from other view models via MVVM messaging.
    /// </summary>
    private void ReceiveAll()
    {
        RequestMessage<PrinterSettings> mes2 = new();
        _ = WeakReferenceMessenger.Default.Send(mes2);
        SelectedPrinter = mes2.Response;

        RequestMessage<ResultsDatabase> mes4 = new();
        _ = WeakReferenceMessenger.Default.Send(mes4);
        SelectedResultsDatabase = mes4.Response;
    }

    /// <summary>
    /// Shows a save file dialog to get a path for saving an image.
    /// </summary>
    /// <returns>The selected file path, or null if canceled.</returns>
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
    #endregion

    #region Message Handlers
    /// <summary>
    /// Receives property changed messages for the ResultssDatabase.
    /// </summary>
    public void Receive(PropertyChangedMessage<ResultsDatabase> message) => SelectedResultsDatabase = message.NewValue;
    /// <summary>
    /// Receives property changed messages for the PrinterSettings.
    /// </summary>
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    /// <summary>
    /// Shows a dialog with "OK" and "Cancel" buttons.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>The result of the user's interaction with the dialog.</returns>
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion
}