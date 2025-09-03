using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Gma.System.MouseKeyHook;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.L95.ViewModels;
using LabelVal.Main.ViewModels;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Extensions;
using LabelVal.Utilities;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using Lvs95xx.lib.Core.Controllers;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Results.ViewModels;

/// <summary>
/// This is the ViewModel for the Image Results Manager.
/// It manages the display and interaction with image results from various devices.
/// </summary>
public partial class ImageResultsManager : ObservableRecipient,
    IRecipient<PropertyChangedMessage<ImageRoll>>,
    IRecipient<PropertyChangedMessage<Node>>,
    IRecipient<PropertyChangedMessage<Databases.ImageResultsDatabase>>,
    IRecipient<PropertyChangedMessage<Scanner>>,
    IRecipient<PropertyChangedMessage<Verifier>>,
    IRecipient<PropertyChangedMessage<PrinterSettings>>,
    IRecipient<PropertyChangedMessage<FullReport>>
{
    #region Fields

    private readonly IKeyboardMouseEvents _globalHook;
    private bool _shiftPressed;
    private bool _isLoadingImages;

    #endregion

    #region Properties

    #region UI State Properties

    /// <summary>
    /// Gets or sets the maximum height for images in the results view.
    /// </summary>
    [ObservableProperty] private int _imagesMaxHeight = App.Settings.GetValue(nameof(ImagesMaxHeight), 200, true);
    partial void OnImagesMaxHeightChanged(int value) => App.Settings.SetValue(nameof(ImagesMaxHeight), value);

    /// <summary>
    /// Gets or sets a value indicating whether to display sectors in two columns.
    /// </summary>
    [ObservableProperty] private bool _dualSectorColumns = App.Settings.GetValue(nameof(DualSectorColumns), false, true);
    partial void OnDualSectorColumnsChanged(bool value) => App.Settings.SetValue(nameof(DualSectorColumns), value);

    /// <summary>
    /// Gets or sets a value indicating whether to show extended data for results.
    /// </summary>
    [ObservableProperty] private bool _showExtendedData = App.Settings.GetValue(nameof(ShowExtendedData), false, true);
    partial void OnShowExtendedDataChanged(bool value) => App.Settings.SetValue(nameof(ShowExtendedData), value);

    /// <summary>
    /// Gets or sets a value indicating whether to hide entries with errors or warnings.
    /// </summary>
    [ObservableProperty] private bool _hideErrorsWarnings = App.Settings.GetValue(nameof(HideErrorsWarnings), false, true);
    partial void OnHideErrorsWarningsChanged(bool value) => App.Settings.SetValue(nameof(HideErrorsWarnings), value);

    #endregion

    #region Selected Item Properties

    /// <summary>
    /// Gets or sets the currently selected image roll.
    /// </summary>
    [ObservableProperty] private ImageRoll _selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRoll oldValue, ImageRoll newValue)
    {
        if (oldValue != null)
        {
            oldValue.ImageEntries.CollectionChanged -= SelectedImageRoll_Images_CollectionChanged;
            oldValue.ImageMoved -= SelectedImageRoll_ImageMoved;
            oldValue.ImageEntries.Clear();
        }

        if (newValue != null)
        {
            newValue.ImageEntries.CollectionChanged += SelectedImageRoll_Images_CollectionChanged;
            newValue.ImageMoved += SelectedImageRoll_ImageMoved;
            _ = LoadImageResultsEntries();
        }
        else
            Application.Current.Dispatcher.Invoke(() => ImageResultsEntries.Clear());
    }

    /// <summary>
    /// Gets or sets the currently selected image results database.
    /// </summary>
    [ObservableProperty] private Databases.ImageResultsDatabase _selectedDatabase;

    /// <summary>
    /// Gets or sets the topmost image result entry in the view.
    /// </summary>
    [ObservableProperty]
    private ImageResultEntry _topmostItem;
    partial void OnTopmostItemChanged(ImageResultEntry oldValue, ImageResultEntry newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsTopmost = false;
        }
        if (newValue is not null)
        {
            newValue.IsTopmost = true;
        }
    }

    /// <summary>
    /// Gets or sets the focused template JSON object.
    /// </summary>
    [ObservableProperty] private JObject _focusedTemplate;

    /// <summary>
    /// Gets or sets the focused report JSON object.
    /// </summary>
    [ObservableProperty] private JObject _focusedReport;

    /// <summary>
    /// Gets or sets the selected V275 device node.
    /// </summary>
    [ObservableProperty] private Node _selectedV275Node;

    /// <summary>
    /// Gets or sets the selected V5 scanner device.
    /// </summary>
    [ObservableProperty] private Scanner _selectedV5;

    /// <summary>
    /// Gets or sets the selected L95 verifier device.
    /// </summary>
    [ObservableProperty] private Verifier _selectedL95;

    /// <summary>
    /// Gets or sets the selected printer.
    /// </summary>
    [ObservableProperty] private PrinterSettings _selectedPrinter;

    #endregion

    #region Device State Properties

    /// <summary>
    /// Gets or sets a value indicating whether the V275 device is currently working.
    /// </summary>
    [ObservableProperty] private bool _isV275Working;

    /// <summary>
    /// Gets or sets a value indicating whether the V275 device is selected for operations.
    /// </summary>
    [ObservableProperty] private bool _isV275Selected;
    partial void OnIsV275SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.V275); }

    /// <summary>
    /// Gets or sets a value indicating whether the V275 device is in a faulted state.
    /// </summary>
    [ObservableProperty] private bool _isV275Faulted;

    /// <summary>
    /// Gets the current label handler for the V275 device based on its state.
    /// </summary>
    public LabelHandlers V275Handler => SelectedV275Node?.Controller != null && SelectedV275Node.Controller.IsLoggedIn_Control
                ? SelectedV275Node.Controller.IsSimulator
                    ? _shiftPressed ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : _shiftPressed ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    /// <summary>
    /// Gets or sets a value indicating whether the V5 device is currently working.
    /// </summary>
    [ObservableProperty] private bool _isV5Working;

    /// <summary>
    /// Gets or sets a value indicating whether the V5 device is selected for operations.
    /// </summary>
    [ObservableProperty] private bool _isV5Selected;
    partial void OnIsV5SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.V5); }

    /// <summary>
    /// Gets or sets a value indicating whether the V5 device is in a faulted state.
    /// </summary>
    [ObservableProperty] private bool _isV5Faulted;

    /// <summary>
    /// Gets the current label handler for the V5 device based on its state.
    /// </summary>
    public LabelHandlers V5Handler => SelectedV5?.Controller != null && SelectedV5.Controller.IsConnected
                ? SelectedV5.Controller.IsSimulator
                    ? _shiftPressed ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : _shiftPressed ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    /// <summary>
    /// Gets or sets a value indicating whether the L95 device is currently working.
    /// </summary>
    [ObservableProperty] private bool _isL95Working;

    /// <summary>
    /// Gets or sets a value indicating whether the L95 device is selected for operations.
    /// </summary>
    [ObservableProperty] private bool _isL95Selected;
    partial void OnIsL95SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.L95); }

    /// <summary>
    /// Gets or sets a value indicating whether the L95 device is in a faulted state.
    /// </summary>
    [ObservableProperty] private bool _isL95Faulted;

    /// <summary>
    /// Gets the current label handler for the L95 device based on its state.
    /// </summary>
    public LabelHandlers L95Handler => SelectedL95?.Controller != null && SelectedL95?.Controller.IsConnected == true && SelectedL95.Controller.ProcessState == Watchers.lib.Process.Win32_ProcessWatcherProcessState.Running
                ? SelectedL95.Controller.IsSimulator
                    ? _shiftPressed ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : _shiftPressed ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of image result entries displayed in the UI.
    /// </summary>
    public ObservableCollection<ImageResultEntry> ImageResultsEntries { get; } = [];

    #endregion

    #endregion

    #region Constructor and Destructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageResultsManager"/> class.
    /// </summary>
    public ImageResultsManager()
    {
        _globalHook = Hook.GlobalEvents();
        _globalHook.KeyDown += _globalHook_KeyDown;
        _globalHook.KeyUp += _globalHook_KeyUp;

        IsActive = true;
        ReceiveAll();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ImageResultsManager"/> class.
    /// </summary>
    ~ImageResultsManager()
    {
        _globalHook.KeyDown -= _globalHook_KeyDown;
        _globalHook.KeyUp -= _globalHook_KeyUp;
        _globalHook.Dispose();
    }

    #endregion

    #region Keyboard Hook Handlers

    private void _globalHook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode is System.Windows.Forms.Keys.LShiftKey or System.Windows.Forms.Keys.RShiftKey)
        {
            _shiftPressed = false;
            Application.Current.Dispatcher.Invoke(HandlerUpdate);
        }
    }

    private void _globalHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode is System.Windows.Forms.Keys.LShiftKey or System.Windows.Forms.Keys.RShiftKey)
        {
            _shiftPressed = true;
            Application.Current.Dispatcher.Invoke(HandlerUpdate);
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Requests initial state from other view models upon activation.
    /// </summary>
    private void ReceiveAll()
    {
        var ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<PrinterSettings>());
        if (ret2.HasReceivedResponse)
            Receive(new PropertyChangedMessage<PrinterSettings>("", "", null, ret2.Response));

        var ret4 = WeakReferenceMessenger.Default.Send(new RequestMessage<Databases.ImageResultsDatabase>());
        if (ret4.HasReceivedResponse)
            Receive(new PropertyChangedMessage<Databases.ImageResultsDatabase>("", "", null, ret4.Response));

        var ret5 = WeakReferenceMessenger.Default.Send(new RequestMessage<Scanner>());
        if (ret5.HasReceivedResponse)
            Receive(new PropertyChangedMessage<Scanner>("", "", null, ret5.Response));

        var ret6 = WeakReferenceMessenger.Default.Send(new RequestMessage<Verifier>());
        if (ret6.HasReceivedResponse)
            Receive(new PropertyChangedMessage<Verifier>("", "", null, ret6.Response));
    }

    #endregion

    #region Image Entry Management

    /// <summary>
    /// Asynchronously loads image result entries from the currently selected image roll.
    /// </summary>
    public async Task LoadImageResultsEntries()
    {
        if (SelectedImageRoll == null)
            return;

        _isLoadingImages = true;
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() => ImageResultsEntries.Clear());

            if (SelectedImageRoll.ImageEntries.Count == 0)
            {
                await SelectedImageRoll.LoadImages(); // This will trigger CollectionChanged and populate ImageResultsEntries
            }
            else
            {
                // Manually populate if the collection is already filled
                var existingEntries = SelectedImageRoll.ImageEntries.OrderBy(i => i.Order).ToList();
                foreach (var entry in existingEntries)
                {
                    AddImageResultEntry(entry);
                }
            }
        }
        finally
        {
            _isLoadingImages = false;
        }

        // After loading is complete, bring the relevant item into view.
        if (ImageResultsEntries.Any())
        {
            ImageResultEntry entryToView = null;
            var sortedEntries = ImageResultsEntries.OrderBy(e => e.SourceImage.Order).ToList();
            switch (SelectedImageRoll.ImageAddPosition)
            {
                case ImageAddPositions.Top:
                case ImageAddPositions.Above:
                    entryToView = sortedEntries.FirstOrDefault();
                    break;
                case ImageAddPositions.Bottom:
                case ImageAddPositions.Below:
                default:
                    entryToView = sortedEntries.LastOrDefault();
                    break;
            }

            if (entryToView != null)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    entryToView.BringIntoViewHandler();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }
    }

    private void SelectedImageRoll_Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (var itm in e.NewItems.Cast<ImageEntry>())
                AddImageResultEntry(itm);
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            foreach (var itm in e.OldItems.Cast<ImageEntry>())
                RemoveImageResultEntry(itm);
        }
    }

    private void SelectedImageRoll_ImageMoved(object sender, ImageEntry imageEntry)
    {
        var ire = ImageResultsEntries.FirstOrDefault(ir => ir.SourceImage.UID == imageEntry.UID);
        if (ire != null)
        {
            ire.BringIntoViewHandler();
        }
    }

    private void AddImageResultEntry(ImageEntry img)
    {
        var ire = ImageResultsEntries.FirstOrDefault(ir => ir.SourceImage.UID == img.UID);
        if (ire == null)
        {
            ire = new ImageResultEntry(img, this);
            ImageResultsEntries.Add(ire);
        }

        if (img.NewData is V275_REST_Lib.Controllers.FullReport v275)
        {
            if (ire.ImageResultDeviceEntries.FirstOrDefault((e) => e.Device == ImageResultEntryDevices.V275) is ImageResultDeviceEntryV275 ird)
            {
                ird.ProcessFullReport(v275);
                WorkingUpdate(ird.Device, false);
            }
        }
        else if (img.NewData is V5_REST_Lib.Controllers.FullReport v5)
        {
            if (ire.ImageResultDeviceEntries.FirstOrDefault((e) => e.Device == ImageResultEntryDevices.V5) is ImageResultDeviceEntry_V5 ird)
            {
                ird.ProcessFullReport(v5);
                WorkingUpdate(ird.Device, false);
            }
        }
        else if (img.NewData is FullReport l95)
        {
            System.Drawing.Point center = new(l95.Template.GetParameter<int>("Report.X1") + (l95.Template.GetParameter<int>("Report.SizeX") / 2), l95.Template.GetParameter<int>("Report.Y1") + (l95.Template.GetParameter<int>("Report.SizeY") / 2));
            if (ire.ImageResultDeviceEntries.FirstOrDefault((e) => e.Device == ImageResultEntryDevices.L95) is ImageResultDeviceEntry_L95 ird)
            {
                string name = null;
                if ((name = ire.GetName(center)) == null)
                    name ??= $"Verify_{ird.CurrentSectors.Count + 1}";

                _ = l95.Template.SetParameter<string>("Name", name);

                ird.ProcessFullReport(l95, true);
                WorkingUpdate(ird.Device, false);
            }
        }

        img.NewData = null;

        if (!_isLoadingImages)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ire.BringIntoViewHandler();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
    }

    private void RemoveImageResultEntry(ImageEntry img)
    {
        var itm = ImageResultsEntries.FirstOrDefault(ir => ir.SourceImage == img);
        if (itm != null)
        {
            _ = ImageResultsEntries.Remove(itm);
        }
    }

    #endregion

    #region Device and UI State Updates

    /// <summary>
    /// Notifies the UI that device handler properties have changed.
    /// </summary>
    public void HandlerUpdate()
    {
        OnPropertyChanged(nameof(V275Handler));
        OnPropertyChanged(nameof(V5Handler));
        OnPropertyChanged(nameof(L95Handler));
    }

    /// <summary>
    /// Updates the working state for a specific device.
    /// </summary>
    /// <param name="device">The device to update.</param>
    /// <param name="state">The new working state.</param>
    public void WorkingUpdate(ImageResultEntryDevices device, bool state)
    {
        if (device == ImageResultEntryDevices.V275)
            IsV275Working = state;
        else if (device == ImageResultEntryDevices.V5)
            IsV5Working = state;
        else if (device == ImageResultEntryDevices.L95)
            IsL95Working = state;
    }

    /// <summary>
    /// Updates the faulted state for a specific device.
    /// </summary>
    /// <param name="device">The device to update.</param>
    /// <param name="state">The new faulted state.</param>
    public void FaultedUpdate(ImageResultEntryDevices device, bool state)
    {
        if (device == ImageResultEntryDevices.V275)
            IsV275Faulted = state;
        else if (device == ImageResultEntryDevices.V5)
            IsV5Faulted = state;
        else if (device == ImageResultEntryDevices.L95)
            IsL95Faulted = state;
    }

    /// <summary>
    /// Resets the selection state for a given device across all image entries.
    /// </summary>
    /// <param name="device">The device whose selection to reset.</param>
    public void ResetSelected(ImageResultEntryDevices device)
    {
        foreach (var lab in ImageResultsEntries)
        {
            var dev = lab.ImageResultDeviceEntries.FirstOrDefault(d => d.Device == device);
            if (dev != null)
                dev.IsSelected = false; ;
        }
        switch (device)
        {
            case ImageResultEntryDevices.V275:
                IsV275Selected = false;
                break;
            case ImageResultEntryDevices.V5:
                IsV5Selected = false;
                break;
            case ImageResultEntryDevices.L95:
                IsL95Selected = false;
                break;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Adds a new image from a file.
    /// </summary>
    [RelayCommand]
    private void AddImage() => AddImage(SelectedImageRoll.ImageAddPosition, null);

    /// <summary>
    /// Adds an image acquired from a specified device.
    /// </summary>
    [RelayCommand]
    private async Task AddDeviceImage(ImageResultEntryDevices device) => await Application.Current.Dispatcher.Invoke(async () =>
    {
        switch (device)
        {
            case ImageResultEntryDevices.V275:
                WorkingUpdate(device, true);
                if (V275Handler is LabelHandlers.CameraDetect or LabelHandlers.SimulatorDetect)
                {
                    var label = new V275_REST_Lib.Controllers.Label(ProcessRepeat, null, V275Handler, SelectedImageRoll.SelectedGS1Table);
                    await SelectedV275Node.Controller.ProcessLabel(0, label);
                }
                else
                {
                    var res = await SelectedV275Node.Controller.ReadTask(-1);
                    ProcessFullReport(res);
                }
                break;

            case ImageResultEntryDevices.V5:
                WorkingUpdate(device, true);
                if (V5Handler is LabelHandlers.CameraDetect or LabelHandlers.SimulatorDetect)
                {
                    var label = new V5_REST_Lib.Controllers.Label(ProcessRepeat, null, V5Handler, SelectedImageRoll.SelectedGS1Table);
                    _ = await SelectedV5.Controller.ProcessLabel(label);
                }
                else
                {
                    var res1 = await SelectedV5.Controller.Trigger_Wait_Return(true);
                    ProcessFullReport(res1);
                }
                break;

            case ImageResultEntryDevices.L95:
                WorkingUpdate(device, true);
                var res2 = SelectedL95.Controller.GetFullReport(-1);
                ProcessFullReport(res2);
                break;
        }
    });

    /// <summary>
    /// Deletes an image and its associated results.
    /// </summary>
    [RelayCommand]
    public async Task DeleteImage(ImageResultEntry imageToDelete)
    {
        if (SelectedImageRoll.IsLocked)
        {
            Logger.Warning("The database is locked. Cannot delete image.");
            return;
        }

        var answer = await AllImageCancelDialog("Delete Image & Remove Results", $"Are you sure you want to delete this image from the Image Roll?\r\nThis can not be undone!");
        if (answer == MessageDialogResult.FirstAuxiliary)
            return;
        else if (answer == MessageDialogResult.Affirmative)
        {
            // Remove the image from the ImageRoll and remove all result entries
            foreach (var img in ImageResultsEntries)
            {
                if (img.SourceImage.UID == imageToDelete.SourceImage.UID)
                {
                    img.DeleteStored();
                }
            }
            SelectedImageRoll.DeleteImage(imageToDelete.SourceImage);
        }
        else if (answer == MessageDialogResult.Negative)
        {
            // Remove the image from the ImageRoll
            SelectedImageRoll.DeleteImage(imageToDelete.SourceImage);
        }
    }

    /// <summary>
    /// Stores the current results for all images and devices.
    /// </summary>
    [RelayCommand]
    private void StoreAllCurrentResults()
    {
        foreach (var img in ImageResultsEntries)
        {
            img.StoreCommand.Execute(ImageResultEntryDevices.V275);
            img.StoreCommand.Execute(ImageResultEntryDevices.V5);
            img.StoreCommand.Execute(ImageResultEntryDevices.L95);
        }
    }

    /// <summary>
    /// Clears the current results for all images and devices.
    /// </summary>
    [RelayCommand]
    private void ClearAllCurrentResults()
    {
        foreach (var img in ImageResultsEntries)
        {
            img.ClearCurrentCommand.Execute(ImageResultEntryDevices.V275);
            img.ClearCurrentCommand.Execute(ImageResultEntryDevices.V5);
            img.ClearCurrentCommand.Execute(ImageResultEntryDevices.L95);
        }
    }

    /// <summary>
    /// Copies all sector data to the clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyAllSectorsToClipboard()
    {
        var data = "";
        var sorted = ImageResultsEntries.OrderBy(i => i.SourceImage.Order);
        foreach (var img in sorted)
        {
            foreach (var device in img.ImageResultDeviceEntries)
            {
                if (device.StoredSectors.Count != 0)
                    data += device.StoredSectors.GetSectorsReport($"{img.ImageResultsManager.SelectedImageRoll.Name}{(char)SectorOutputSettings.CurrentDelimiter}{img.SourceImage.Order}") + Environment.NewLine;
                if (device.CurrentSectors.Count != 0)
                    data += device.CurrentSectors.GetSectorsReport($"{img.ImageResultsManager.SelectedImageRoll.Name}{(char)SectorOutputSettings.CurrentDelimiter}{img.SourceImage.Order}") + Environment.NewLine;
            }
        }
        Clipboard.SetText(data);
    }

    #endregion

    #region Report Processing

    public void Receive(PropertyChangedMessage<FullReport> message)
    {
        if (IsL95Selected)
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message.NewValue));
    }

    private void ProcessRepeat(V5_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat.FullReport);
    public void ProcessFullReport(V5_REST_Lib.Controllers.FullReport res)
    {
        try
        {
            if (res == null) return;

            if (SelectedImageRoll.IsLocked)
            {
                var ire = new ImageEntry(SelectedImageRoll.UID, res.Image, SelectedImageRoll.TargetDPI);
                if (SelectedImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.Warning("The database is locked. Cannot add image.");
                    return;
                }
            }

            (var entry, var isNew) = SelectedImageRoll.GetImageEntry(res.Image);
            if (entry == null) return;

            entry.NewData = res;

            if (isNew)
                SelectedImageRoll.AddImage(SelectedImageRoll.ImageAddPosition, entry);
            else
                AddImageResultEntry(entry);
        }
        finally
        {
            WorkingUpdate(ImageResultEntryDevices.V5, false);
        }
    }

    private void ProcessRepeat(V275_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat.FullReport);
    public void ProcessFullReport(V275_REST_Lib.Controllers.FullReport res)
    {
        try
        {
            if (res == null) return;

            if (SelectedImageRoll.IsLocked)
            {
                var ire = new ImageEntry(SelectedImageRoll.UID, res.Image, SelectedImageRoll.TargetDPI);
                if (SelectedImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.Warning("The database is locked. Cannot add image.");
                    return;
                }
            }

            (var entry, var isNew) = SelectedImageRoll.GetImageEntry(res.Image);
            if (entry == null) return;

            entry.NewData = res;

            if (isNew)
                SelectedImageRoll.AddImage(SelectedImageRoll.ImageAddPosition, entry);
            else
                AddImageResultEntry(entry);
        }
        finally
        {
            WorkingUpdate(ImageResultEntryDevices.V275, false);
        }
    }

    public void ProcessFullReport(FullReport res)
    {
        try
        {
            if (res == null || res.Report == null) return;

            var thumbnail = res.Template.GetParameter<byte[]>("Report.Thumbnail");
            if (SelectedImageRoll.IsLocked)
            {
                var ire = new ImageEntry(SelectedImageRoll.UID, thumbnail, SelectedImageRoll.TargetDPI);
                if (SelectedImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.Warning("The database is locked. Cannot add image.");
                    return;
                }
            }

            (var entry, var isNew) = SelectedImageRoll.GetImageEntry(thumbnail);
            if (entry == null) return;

            entry.NewData = res;

            if (isNew)
                SelectedImageRoll.AddImage(SelectedImageRoll.ImageAddPosition, entry);
            else
                AddImageResultEntry(entry);
        }
        finally
        {
            WorkingUpdate(ImageResultEntryDevices.L95, false);
        }
    }

    #endregion

    #region Private Helper Methods

    private void AddImage(ImageAddPositions position, ImageResultEntry imageResult)
    {
        var newImages = PromptForNewImages();
        if (newImages != null && newImages.Count > 0)
        {
            SelectedImageRoll.AddImages(position, newImages);
        }
    }

    private List<ImageEntry> PromptForNewImages()
    {
        FileUtilities.LoadFileDialogSettings settings = new()
        {
            Filters = [new Utilities.FileUtilities.FileDialogFilter("Image Files", ["png", "bmp"])],
            Title = "Select image(s).",
            Multiselect = true,
        };

        if (Utilities.FileUtilities.LoadFileDialog(settings))
        {
            List<ImageEntry> newImages = [];
            foreach (var filePath in settings.SelectedFiles)
            {
                (var entry, var isNew) = SelectedImageRoll.GetImageEntry(filePath);
                if (entry != null && isNew)
                {
                    newImages.Add(entry);
                }
            }
            return newImages;
        }

        return null;
    }

    #endregion

    #region Message Handlers (Receive)

    /// <summary>
    /// Receives property changes for <see cref="ImageRoll"/> and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<ImageRoll> message)
    {
        if (SelectedImageRoll != null)
        {
            SelectedImageRoll.PropertyChanged -= SelectedImageRoll_PropertyChanged;
            SelectedImageRoll.ImageEntries.CollectionChanged -= SelectedImageRoll_Images_CollectionChanged;
        }

        SelectedImageRoll = message.NewValue;

        if (SelectedImageRoll != null)
        {
            SelectedImageRoll.PropertyChanged += SelectedImageRoll_PropertyChanged;
            SelectedImageRoll.ImageEntries.CollectionChanged += SelectedImageRoll_Images_CollectionChanged;
        }
    }

    private void SelectedImageRoll_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImageRoll.SectorType))
            foreach (var lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.All);
    }

    /// <summary>
    /// Receives property changes for <see cref="PrinterSettings"/> and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;

    /// <summary>
    /// Receives property changes for <see cref="Databases.ImageResultsDatabase"/> and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<Databases.ImageResultsDatabase> message) => SelectedDatabase = message.NewValue;

    /// <summary>
    /// Receives property changes for <see cref="Node"/> (V275) and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<Node> message)
    {
        if (SelectedV275Node?.Controller != null)
            SelectedV275Node.Controller.PropertyChanged -= V275Controller_PropertyChanged;

        SelectedV275Node = message.NewValue;

        if (SelectedV275Node?.Controller != null)
            SelectedV275Node.Controller.PropertyChanged += V275Controller_PropertyChanged;

        foreach (var lab in ImageResultsEntries)
            lab.HandlerUpdate(ImageResultEntryDevices.V275);

        HandlerUpdate();
    }

    private void V275Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Node.Controller.IsSimulator) or nameof(Node.Controller.IsLoggedIn_Control))
        {
            foreach (var lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.V275);

            HandlerUpdate();
        }
    }

    /// <summary>
    /// Receives property changes for <see cref="Scanner"/> (V5) and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<Scanner> message)
    {
        if (SelectedV5?.Controller != null)
            SelectedV5.Controller.PropertyChanged -= V5Controller_PropertyChanged;

        SelectedV5 = message.NewValue;

        if (SelectedV5?.Controller != null)
            SelectedV5.Controller.PropertyChanged += V5Controller_PropertyChanged;

        foreach (var lab in ImageResultsEntries)
            lab.HandlerUpdate(ImageResultEntryDevices.V5);

        HandlerUpdate();
    }

    private void V5Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(V5_REST_Lib.Controllers.Controller.IsSimulator) or nameof(V5_REST_Lib.Controllers.Controller.IsConnected))
        {
            foreach (var lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.V5);

            HandlerUpdate();
        }
    }

    /// <summary>
    /// Receives property changes for <see cref="Verifier"/> (L95) and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<Verifier> message)
    {
        if (SelectedL95?.Controller != null)
            SelectedL95.Controller.PropertyChanged -= L95Controller_PropertyChanged;

        SelectedL95 = message.NewValue;

        if (SelectedL95?.Controller != null)
            SelectedL95.Controller.PropertyChanged += L95Controller_PropertyChanged;

        foreach (var lab in ImageResultsEntries)
            lab.HandlerUpdate(ImageResultEntryDevices.L95);

        HandlerUpdate();
    }

    private void L95Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Lvs95xx.lib.Core.Controllers.Controller.IsSimulator) or nameof(Lvs95xx.lib.Core.Controllers.Controller.IsConnected) or nameof(Lvs95xx.lib.Core.Controllers.Controller.ProcessState))
        {
            foreach (var lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.L95);

            HandlerUpdate();
        }
    }

    #endregion

    #region Dialogs

    /// <summary>
    /// Gets the dialog coordinator for showing dialogs.
    /// </summary>
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    /// <summary>
    /// Shows a dialog with OK and Cancel buttons.
    /// </summary>
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

    /// <summary>
    /// Shows a dialog for deleting an image with multiple options.
    /// </summary>
    public async Task<MessageDialogResult> AllImageCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings() { DefaultButtonFocus = MessageDialogResult.FirstAuxiliary, AffirmativeButtonText = "Image & Results", NegativeButtonText = "Image Only", FirstAuxiliaryButtonText = "Cancel" });

    #endregion
}