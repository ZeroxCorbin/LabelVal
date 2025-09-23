using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ImageUtilities.lib.Wpf;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Main.ViewModels;
using LabelVal.Results.Databases;
using LabelVal.Results.Helpers;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Interfaces;
using LabelVal.Utilities;
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

#region Class
/// <summary>ViewModel combining an image source entry with result data from multiple devices and providing related commands.</summary>
public partial class ResultsEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<ResultsDatabase>>, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    #region Delegates
    /// <summary>Represents a handler that requests this entry be brought into view.</summary>
    public delegate void BringIntoViewDelegate();
    /// <summary>Represents a handler that requests deletion of a <see cref="ResultsEntry"/>.</summary>
    /// <param name="imageResults">The entry to delete.</param>
    public delegate void DeleteImageDelegate(ResultsEntry imageResults);
    #endregion

    #region Events
    /// <summary>Raised to request scrolling this entry into view.</summary>
    public event BringIntoViewDelegate BringIntoView;
    /// <summary>Raised to request deletion of this entry and its associated data.</summary>
    public event DeleteImageDelegate DeleteImage;
    #endregion

    #region Fields
    /// <summary>Tracks global settings changes to keep local cached values synchronized.</summary>
    private readonly PropertyChangedEventHandler _settingsPropertyChangedHandler;
    #endregion

    #region Properties

    #region Global Settings
    /// <summary>Gets the global application settings singleton.</summary>
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <summary>Gets or sets the maximum display height for images.</summary>
    [ObservableProperty] private int _imagesMaxHeight = App.Settings.GetValue<int>(nameof(ImagesMaxHeight));
    /// <seealso cref="ImagesMaxHeight"/>

    /// <summary>Gets or sets whether sectors are displayed using two columns.</summary>
    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue<bool>(nameof(DualSectorColumns));
    /// <seealso cref="DualSectorColumns"/>

    /// <summary>Gets or sets whether extended sector data (like module grids) is shown.</summary>
    [ObservableProperty] private bool showExtendedData = App.Settings.GetValue<bool>(nameof(ShowExtendedData));
    /// <seealso cref="ShowExtendedData"/>

    /// <summary>Handles updates when <see cref="ShowExtendedData"/> changes by refreshing device overlays.</summary>
    /// <param name="value">The new state.</param>
    partial void OnShowExtendedDataChanged(bool value)
    {
        foreach (var device in ResultsDeviceEntries)
            device.RefreshOverlays();
    }

    /// <summary>Gets or sets whether the Application Parameters section is visible.</summary>
    [ObservableProperty] private bool showApplicationParameters = App.Settings.GetValue(nameof(ShowApplicationParameters), true, true);
    /// <seealso cref="ShowApplicationParameters"/>

    /// <summary>Persists the new value of <see cref="ShowApplicationParameters"/> to settings.</summary>
    /// <param name="value">The new state.</param>
    partial void OnShowApplicationParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowApplicationParameters), value);

    /// <summary>Gets or sets whether the Grading Parameters section is visible.</summary>
    [ObservableProperty] private bool showGradingParameters = App.Settings.GetValue(nameof(ShowGradingParameters), true, true);
    /// <seealso cref="ShowGradingParameters"/>

    /// <summary>Persists the new value of <see cref="ShowGradingParameters"/> to settings.</summary>
    /// <param name="value">The new state.</param>
    partial void OnShowGradingParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowGradingParameters), value);

    /// <summary>Gets or sets whether the Symbology Parameters section is visible.</summary>
    [ObservableProperty] private bool showSymbologyParameters = App.Settings.GetValue(nameof(ShowSymbologyParameters), true, true);
    /// <seealso cref="ShowSymbologyParameters"/>

    /// <summary>Persists the new value of <see cref="ShowSymbologyParameters"/> to settings.</summary>
    /// <param name="value">The new state.</param>
    partial void OnShowSymbologyParametersChanged(bool value) => App.Settings.SetValue(nameof(ShowSymbologyParameters), value);
    #endregion

    #region UI State Properties
    /// <summary>Gets or sets whether image detail panels are expanded.</summary>
    [ObservableProperty] private bool showDetails;
    /// <seealso cref="ShowDetails"/>

    /// <summary>Gets or sets whether this entry is currently the topmost visible item.</summary>
    [ObservableProperty] private bool isTopmost;
    /// <seealso cref="IsTopmost"/>
    #endregion

    #region Data Properties
    /// <summary>Gets or sets the active results database used to load and store device results.</summary>
    [ObservableProperty] private ResultsDatabase selectedResultsDatabase;
    /// <seealso cref="SelectedResultsDatabase"/>

    /// <summary>Refreshes stored data for all devices when <see cref="SelectedResultsDatabase"/> changes.</summary>
    /// <param name="value">The new database.</param>
    partial void OnSelectedResultsDatabaseChanged(ResultsDatabase value)
    {
        foreach (var device in ResultsDeviceEntries)
            device.GetStored();
    }

    /// <summary>Gets the results manager providing contextual state such as the active image roll.</summary>
    public ResultsManagerViewModel ResultsManagerView { get; }

    /// <summary>Gets the UID of the active image roll.</summary>
    public string ImageRollUID => ResultsManagerView.ActiveImageRoll.UID;

    /// <summary>Gets the source image associated with this entry.</summary>
    public ImageEntry SourceImage { get; }

    /// <summary>Gets the UID of the source image.</summary>
    public string SourceImageUID => SourceImage.UID;

    /// <summary>Gets the collection of device-specific result entries.</summary>
    public ObservableCollection<IResultsDeviceEntry> ResultsDeviceEntries { get; }
    #endregion

    #region Printer Properties
    /// <summary>Gets the number of copies to print.</summary>
    public int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));

    /// <summary>Gets or sets whether the printer area overlay is drawn over the source image.</summary>
    [ObservableProperty] private bool showPrinterAreaOverSource;
    /// <seealso cref="ShowPrinterAreaOverSource"/>

    /// <summary>Gets or sets the current printer area overlay image.</summary>
    [ObservableProperty] private DrawingImage printerAreaOverlay;
    /// <seealso cref="PrinterAreaOverlay"/>

    /// <summary>Updates the printer overlay when <see cref="ShowPrinterAreaOverSource"/> changes.</summary>
    /// <param name="value">The new state.</param>
    partial void OnShowPrinterAreaOverSourceChanged(bool value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;

    /// <summary>Gets the controller used to query printer capabilities.</summary>
    private V275_REST_Lib.Printer.Controller PrinterController { get; } = new();

    /// <summary>Gets or sets the currently selected printer.</summary>
    [ObservableProperty] private PrinterSettings selectedPrinter;
    /// <seealso cref="SelectedPrinter"/>

    /// <summary>Updates the printer overlay when <see cref="SelectedPrinter"/> changes.</summary>
    /// <param name="value">The new printer.</param>
    partial void OnSelectedPrinterChanged(PrinterSettings value) => PrinterAreaOverlay = ShowPrinterAreaOverSource ? CreatePrinterAreaOverlay(true) : null;
    #endregion

    #endregion

    #region Constructor and Finalizer
    /// <summary>Initializes a new instance with the provided source image and manager context.</summary>
    /// <param name="sourceImage">The source image entry.</param>
    /// <param name="imageResults">The results manager.</param>
    public ResultsEntry(ImageEntry sourceImage, ResultsManagerViewModel imageResults)
    {
        ResultsManagerView = imageResults;
        SourceImage = sourceImage;

        ResultsDeviceEntries =
        [
            new ResultsDeviceEntryV275(this),
            new ResultsDeviceEntryV5(this),
            new ResultsDeviceEntryL95(this),
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

    /// <summary>Finalizer unsubscribing from global settings notifications.</summary>
    ~ResultsEntry()
    {
        App.Settings.PropertyChanged -= _settingsPropertyChangedHandler;
    }
    #endregion

    #region Commands
    /// <summary>Requests that this entry be scrolled into view.</summary>
    /// <seealso cref="BringIntoViewHandlerCommand"/>
    [RelayCommand]
    public void BringIntoViewHandler() => BringIntoView?.Invoke();

    /// <summary>Saves the specified image to disk honoring global format preservation settings.</summary>
    /// <seealso cref="SaveCommand"/>
    /// <param name="img">The image entry to save.</param>
    [RelayCommand]
    private void Save(ImageEntry img)
    {
        if (img == null || img.OriginalImage == null)
            return;

        var path = GetSaveFilePath();
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            bool preserve = AppSettings.PreseveImageFormat;
            int fallbackDpi = ResultsManagerView.ActiveImageRoll?.TargetDPI > 0
                ? ResultsManagerView.ActiveImageRoll.TargetDPI
                : 600;

            byte[] bytesToWrite;
            string extension;

            if (preserve)
            {
                bytesToWrite = img.OriginalImage;
                extension = img.ContainerFormat switch
                {
                    ImageContainerFormat.Png => ".png",
                    ImageContainerFormat.Jpeg => ".jpg",
                    ImageContainerFormat.Bmp => ".bmp",
                    ImageContainerFormat.Gif => ".gif",
                    ImageContainerFormat.Tiff => ".tiff",
                    _ => ".bin"
                };
            }
            else
            {
                bytesToWrite = ImageFormatHelpers.ConvertImageToBgr32PreserveDpi(img.OriginalImage, fallbackDpi, out _, out _);
                extension = ".bmp";
            }

            var directory = System.IO.Path.GetDirectoryName(path);
            var baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            var finalPath = System.IO.Path.Combine(directory ?? "", baseName + extension);

            File.WriteAllBytes(finalPath, bytesToWrite);
            Clipboard.SetText(finalPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    /// <summary>Stores current results for the specified device then brings this entry into view.</summary>
    /// <seealso cref="StoreCommand"/>
    /// <param name="device">The device to store results for.</param>
    [RelayCommand]
    private void Store(ResultsEntryDevices device)
    {
        StoreOnly(device);
        BringIntoViewHandler();
    }

    /// <summary>Processes the source image for the specified device and scrolls into view.</summary>
    /// <seealso cref="ProcessCommand"/>
    /// <param name="device">The target device.</param>
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

    /// <summary>Clears stored sectors for the specified device after confirmation.</summary>
    /// <seealso cref="ClearStoredCommand"/>
    /// <param name="device">The target device.</param>
    [RelayCommand]
    private async Task ClearStored(ResultsEntryDevices device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", "Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
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

    /// <summary>Clears currently processed (unstored) sectors for the specified device and scrolls into view.</summary>
    /// <seealso cref="ClearCurrentCommand"/>
    /// <param name="device">The target device.</param>
    [RelayCommand]
    private void ClearCurrent(ResultsEntryDevices device)
    {
        ClearOnly(device);
        BringIntoViewHandler();
    }

    /// <summary>Raises a deletion request for this image entry.</summary>
    /// <seealso cref="DeleteCommand"/>
    [RelayCommand]
    private void Delete() => DeleteImage?.Invoke(this);
    #endregion

    #region Public Methods
    /// <summary>Stores only the results for a specific device without changing view position.</summary>
    /// <param name="device">The device to store.</param>
    public void StoreOnly(ResultsEntryDevices device)
    {
        var dev = ResultsDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.Error($"Device not found: {device}");
            return;
        }
        _ = dev.Store();
    }

    /// <summary>Clears only the current (unstored) sectors for a specific device.</summary>
    /// <param name="device">The device whose current sectors are cleared.</param>
    public void ClearOnly(ResultsEntryDevices device)
    {
        var dev = ResultsDeviceEntries.FirstOrDefault(x => x.Device == device);
        if (dev == null)
        {
            Logger.Error($"Device not found: {device}");
            return;
        }
        dev.ClearCurrent();
    }

    /// <summary>Deletes all stored results for each device associated with this image.</summary>
    public void DeleteStored()
    {
        _ = SelectedResultsDatabase.Delete_Result(ResultsEntryDevices.L95, ImageRollUID, SourceImageUID, ImageRollUID);
        _ = SelectedResultsDatabase.Delete_Result(ResultsEntryDevices.V275, ImageRollUID, SourceImageUID, ImageRollUID);
        _ = SelectedResultsDatabase.Delete_Result(ResultsEntryDevices.V5, ImageRollUID, SourceImageUID, ImageRollUID);
    }

    /// <summary>Gets the sector name at a specified point if any sector contains that point.</summary>
    /// <param name="center">The point to test.</param>
    /// <returns>The sector name or null.</returns>
    public string? GetName(System.Drawing.Point center)
    {
        string name = null;
        foreach (var dev in ResultsDeviceEntries)
        {
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

    /// <summary>Triggers handler update logic for a specific device or all devices.</summary>
    /// <param name="device">The device or <see cref="ResultsEntryDevices.All"/>.</param>
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

    /// <summary>Creates a drawing overlay representing the printable area for the selected printer.</summary>
    /// <param name="useRatio">True to scale against the low-resolution proxy image.</param>
    /// <returns>The overlay image or null.</returns>
    public DrawingImage CreatePrinterAreaOverlay(bool useRatio)
    {
        if (SelectedPrinter == null) return null;

        double xRatio, yRatio;
        if (useRatio)
        {
            xRatio = (double)SourceImage.ImageLow.PixelWidth / SourceImage.ImageWidth;
            yRatio = (double)SourceImage.ImageLow.PixelHeight / SourceImage.ImageHeight;
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

    /// <summary>Sorts sectors top-to-bottom then left-to-right.</summary>
    /// <param name="list">The sectors list.</param>
    /// <returns>The sorted list.</returns>
    public List<Sectors.Interfaces.ISector> SortList3(List<Sectors.Interfaces.ISector> list) =>
        list.OrderBy(x => x.Report.Top).ThenBy(x => x.Report.Left).ToList();
    #endregion

    #region Private Methods
    /// <summary>Requests initial external state (printer and database) using messenger requests.</summary>
    private void ReceiveAll()
    {
        RequestMessage<PrinterSettings> mes2 = new();
        _ = WeakReferenceMessenger.Default.Send(mes2);
        SelectedPrinter = mes2.Response;

        RequestMessage<ResultsDatabase> mes4 = new();
        _ = WeakReferenceMessenger.Default.Send(mes4);
        SelectedResultsDatabase = mes4.Response;
    }

    /// <summary>Shows a save dialog and returns the chosen file path or null.</summary>
    /// <returns>The chosen file path or empty if cancelled.</returns>
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
    /// <summary>Updates the active results database when a property change message is received.</summary>
    /// <param name="message">The incoming message.</param>
    public void Receive(PropertyChangedMessage<ResultsDatabase> message) => SelectedResultsDatabase = message.NewValue;

    /// <summary>Updates the selected printer when a property change message is received.</summary>
    /// <param name="message">The incoming message.</param>
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    /// <summary>Gets the shared dialog coordinator instance.</summary>
    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    /// <summary>Displays an OK/Cancel style dialog and returns the user's selection.</summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Dialog message.</param>
    /// <returns>The dialog result.</returns>
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) =>
        await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion
}
#endregion