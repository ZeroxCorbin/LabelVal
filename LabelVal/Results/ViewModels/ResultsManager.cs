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
using LabelVal.Main.Messages;
using LabelVal.Main.ViewModels;
using LabelVal.Results.Databases;
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
using V275_REST_Lib.Models;

namespace LabelVal.Results.ViewModels;

/// <summary>
/// This is the ViewModel for the Image Results Manager.
/// It manages the display and interaction with image results from various devices.
/// </summary>
public partial class ResultsManager : ObservableRecipient,
    IRecipient<PropertyChangedMessage<ImageRoll>>,
    IRecipient<PropertyChangedMessage<Node>>,
    IRecipient<PropertyChangedMessage<ResultsDatabase>>,
    IRecipient<PropertyChangedMessage<Scanner>>,
    IRecipient<PropertyChangedMessage<Verifier>>,
    IRecipient<PropertyChangedMessage<PrinterSettings>>,
    IRecipient<PropertyChangedMessage<FullReport>>,
    IRecipient<DeleteResultsForRollMessage>
{
    #region Fields

    private readonly IKeyboardMouseEvents _globalHook;
    private bool _shiftPressed;
    private bool _isLoadingImages;
    private readonly List<Window> _openSectorsWindows = new();

    #endregion

    #region Properties

    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

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
    [ObservableProperty] private ImageRoll _activeImageRoll;
    partial void OnActiveImageRollChanged(ImageRoll oldValue, ImageRoll newValue)
    {
        CloseAllSectorsDetailsWindows();

        if (oldValue != null)
        {
            oldValue.ImageEntries.CollectionChanged -= ActiveImageRoll_Images_CollectionChanged;
            oldValue.ImageMoved -= ActiveImageRoll_ImageMoved;
            oldValue.ImageEntries.Clear();
        }

        if (newValue != null)
        {
            newValue.ImageEntries.CollectionChanged += ActiveImageRoll_Images_CollectionChanged;
            newValue.ImageMoved += ActiveImageRoll_ImageMoved;
            _ = LoadResultssEntries();
        }
        else
            Application.Current.Dispatcher.Invoke(() => ResultssEntries.Clear());
    }

    /// <summary>
    /// Gets or sets the currently selected image results database.
    /// </summary>
    [ObservableProperty] private ResultsDatabase _selectedResultsDatabase;

    /// <summary>
    /// Gets or sets the topmost image result entry in the view.
    /// </summary>
    [ObservableProperty]
    private ResultsEntry _topmostItem;
    partial void OnTopmostItemChanged(ResultsEntry oldValue, ResultsEntry newValue)
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
    partial void OnIsV275SelectedChanging(bool value) { if (value) ResetSelected(ResultsEntryDevices.V275); }

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
    partial void OnIsV5SelectedChanging(bool value) { if (value) ResetSelected(ResultsEntryDevices.V5); }

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
    partial void OnIsL95SelectedChanging(bool value) { if (value) ResetSelected(ResultsEntryDevices.L95); }

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
    public ObservableCollection<ResultsEntry> ResultssEntries { get; } = [];

    #endregion

    #endregion

    #region Constructor and Destructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsManager"/> class.
    /// </summary>
    public ResultsManager()
    {
        _globalHook = Hook.GlobalEvents();
        _globalHook.KeyDown += _globalHook_KeyDown;
        _globalHook.KeyUp += _globalHook_KeyUp;

        IsActive = true;
        ReceiveAll();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ResultsManager"/> class.
    /// </summary>
    ~ResultsManager()
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

        var ret4 = WeakReferenceMessenger.Default.Send(new RequestMessage<ResultsDatabase>());
        if (ret4.HasReceivedResponse)
            Receive(new PropertyChangedMessage<ResultsDatabase>("", "", null, ret4.Response));

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
    public async Task LoadResultssEntries()
    {
        if (ActiveImageRoll == null)
            return;

        _isLoadingImages = true;
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() => ResultssEntries.Clear());

            if (ActiveImageRoll.ImageEntries.Count == 0)
            {
                await ActiveImageRoll.LoadImages(); // This will trigger CollectionChanged and populate ResultssEntries
            }
            else
            {
                // Manually populate if the collection is already filled
                var existingEntries = ActiveImageRoll.ImageEntries.OrderBy(i => i.Order).ToList();
                foreach (var entry in existingEntries)
                {
                    AddResultsEntry(entry);
                }
            }
        }
        finally
        {
            _isLoadingImages = false;
        }

        // After loading is complete, bring the relevant item into view.
        if (ResultssEntries.Any())
        {
            ResultsEntry entryToView = null;
            var sortedEntries = ResultssEntries.OrderBy(e => e.SourceImage.Order).ToList();
            switch (ActiveImageRoll.ImageAddPosition)
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

    private void ActiveImageRoll_Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (var itm in e.NewItems.Cast<ImageEntry>())
                AddResultsEntry(itm);
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            foreach (var itm in e.OldItems.Cast<ImageEntry>())
                RemoveResultsEntry(itm);
        }
    }

    private void ActiveImageRoll_ImageMoved(object sender, ImageEntry imageEntry)
    {
        var ire = ResultssEntries.FirstOrDefault(ir => ir.SourceImage.UID == imageEntry.UID);
        if (ire != null)
        {
            ire.BringIntoViewHandler();
        }
    }

    private void AddResultsEntry(ImageEntry img)
    {
        var ire = ResultssEntries.FirstOrDefault(ir => ir.SourceImage.UID == img.UID);
        if (ire == null)
        {
            ire = new ResultsEntry(img, this);
            ResultssEntries.Add(ire);
        }

        if (img.NewData is V275_REST_Lib.Controllers.FullReport v275)
        {
            if (ire.ResultsDeviceEntries.FirstOrDefault((e) => e.Device == ResultsEntryDevices.V275) is ResultsDeviceEntryV275 ird)
            {
                ird.ProcessFullReport(v275);
                WorkingUpdate(ird.Device, false);
            }
        }
        else if (img.NewData is V5_REST_Lib.Controllers.FullReport v5)
        {
            if (ire.ResultsDeviceEntries.FirstOrDefault((e) => e.Device == ResultsEntryDevices.V5) is ResultsDeviceEntry_V5 ird)
            {
                ird.ProcessFullReport(v5);
                WorkingUpdate(ird.Device, false);
            }
        }
        else if (img.NewData is FullReport l95)
        {
            System.Drawing.Point center = new(l95.Template.GetParameter<int>("Report.X1") + (l95.Template.GetParameter<int>("Report.SizeX") / 2), l95.Template.GetParameter<int>("Report.Y1") + (l95.Template.GetParameter<int>("Report.SizeY") / 2));
            if (ire.ResultsDeviceEntries.FirstOrDefault((e) => e.Device == ResultsEntryDevices.L95) is ResultsDeviceEntry_L95 ird)
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

    private void RemoveResultsEntry(ImageEntry img)
    {
        var itm = ResultssEntries.FirstOrDefault(ir => ir.SourceImage == img);
        if (itm != null)
        {
            _ = ResultssEntries.Remove(itm);
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
    public void WorkingUpdate(ResultsEntryDevices device, bool state)
    {
        if (device == ResultsEntryDevices.V275)
            IsV275Working = state;
        else if (device == ResultsEntryDevices.V5)
            IsV5Working = state;
        else if (device == ResultsEntryDevices.L95)
            IsL95Working = state;
    }

    /// <summary>
    /// Updates the faulted state for a specific device.
    /// </summary>
    /// <param name="device">The device to update.</param>
    /// <param name="state">The new faulted state.</param>
    public void FaultedUpdate(ResultsEntryDevices device, bool state)
    {
        if (device == ResultsEntryDevices.V275)
            IsV275Faulted = state;
        else if (device == ResultsEntryDevices.V5)
            IsV5Faulted = state;
        else if (device == ResultsEntryDevices.L95)
            IsL95Faulted = state;
    }

    /// <summary>
    /// Resets the selection state for a given device across all image entries.
    /// </summary>
    /// <param name="device">The device whose selection to reset.</param>
    public void ResetSelected(ResultsEntryDevices device)
    {
        foreach (var lab in ResultssEntries)
        {
            var dev = lab.ResultsDeviceEntries.FirstOrDefault(d => d.Device == device);
            if (dev != null)
                dev.IsSelected = false; ;
        }
        switch (device)
        {
            case ResultsEntryDevices.V275:
                IsV275Selected = false;
                break;
            case ResultsEntryDevices.V5:
                IsV5Selected = false;
                break;
            case ResultsEntryDevices.L95:
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
    private void AddImage() => AddImage(ActiveImageRoll.ImageAddPosition, null);

    /// <summary>
    /// Adds an image acquired from a specified device.
    /// </summary>
    [RelayCommand]
    private async Task AddDeviceImage(ResultsEntryDevices device) => await Application.Current.Dispatcher.Invoke(async () =>
    {
        switch (device)
        {
            case ResultsEntryDevices.V275:
                WorkingUpdate(device, true);
                if (V275Handler is LabelHandlers.CameraDetect or LabelHandlers.SimulatorDetect)
                {
                    var label = new V275_REST_Lib.Controllers.Label(ProcessRepeat, null, V275Handler, ActiveImageRoll.SelectedGS1Table);
                    await SelectedV275Node.Controller.ProcessLabel(0, label);
                }
                else
                {
                    var res = await SelectedV275Node.Controller.ReadTask(-1);
                    ProcessFullReport(res);
                }
                break;

            case ResultsEntryDevices.V5:
                WorkingUpdate(device, true);
                if (V5Handler is LabelHandlers.CameraDetect or LabelHandlers.SimulatorDetect)
                {
                    var label = new V5_REST_Lib.Controllers.Label(ProcessRepeat, null, V5Handler, ActiveImageRoll.SelectedGS1Table);
                    _ = await SelectedV5.Controller.ProcessLabel(label);
                }
                else
                {
                    var res1 = await SelectedV5.Controller.Trigger_Wait_Return(true);
                    ProcessFullReport(res1);
                }
                break;

            case ResultsEntryDevices.L95:
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
    public async Task DeleteImage(ResultsEntry imageToDelete)
    {
        if (ActiveImageRoll.IsLocked)
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
            foreach (var img in ResultssEntries)
            {
                if (img.SourceImage.UID == imageToDelete.SourceImage.UID)
                {
                    img.DeleteStored();
                }
            }
            ActiveImageRoll.DeleteImage(imageToDelete.SourceImage);
        }
        else if (answer == MessageDialogResult.Negative)
        {
            // Remove the image from the ImageRoll
            ActiveImageRoll.DeleteImage(imageToDelete.SourceImage);
        }
    }

    /// <summary>
    /// Stores the current results for all images and devices.
    /// </summary>
    [RelayCommand]
    private void StoreAllCurrentResults()
    {
        foreach (var img in ResultssEntries)
        {
            img.StoreCommand.Execute(ResultsEntryDevices.V275);
            img.StoreCommand.Execute(ResultsEntryDevices.V5);
            img.StoreCommand.Execute(ResultsEntryDevices.L95);
        }
    }

    /// <summary>
    /// Clears the current results for all images and devices.
    /// </summary>
    [RelayCommand]
    private void ClearAllCurrentResults()
    {
        foreach (var img in ResultssEntries)
        {
            img.ClearCurrentCommand.Execute(ResultsEntryDevices.V275);
            img.ClearCurrentCommand.Execute(ResultsEntryDevices.V5);
            img.ClearCurrentCommand.Execute(ResultsEntryDevices.L95);
        }
    }

    /// <summary>
    /// Copies all sector data to the clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyAllSectorsToClipboard()
    {
        var data = "";
        var sorted = ResultssEntries.OrderBy(i => i.SourceImage.Order);
        foreach (var img in sorted)
        {
            foreach (var device in img.ResultsDeviceEntries)
            {
                if (device.StoredSectors.Count != 0)
                    data += device.StoredSectors.GetSectorsReport($"{img.ResultssManager.ActiveImageRoll.Name}{(char)SectorOutputSettings.CurrentDelimiter}{img.SourceImage.Order}") + Environment.NewLine;
                if (device.CurrentSectors.Count != 0)
                    data += device.CurrentSectors.GetSectorsReport($"{img.ResultssManager.ActiveImageRoll.Name}{(char)SectorOutputSettings.CurrentDelimiter}{img.SourceImage.Order}") + Environment.NewLine;
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

            if (ActiveImageRoll.IsLocked)
            {
                var ire = new ImageEntry(ActiveImageRoll.UID, res.Image, ActiveImageRoll.TargetDPI);
                if (ActiveImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.Warning("The database is locked. Cannot add image.");
                    return;
                }
            }

            (var entry, var isNew) = ActiveImageRoll.GetImageEntry(res.Image);
            if (entry == null) return;

            entry.NewData = res;

            if (isNew)
                ActiveImageRoll.AddImage(ActiveImageRoll.ImageAddPosition, entry);
            else
                AddResultsEntry(entry);
        }
        finally
        {
            WorkingUpdate(ResultsEntryDevices.V5, false);
        }
    }

    private void ProcessRepeat(V275_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat.FullReport);
    public void ProcessFullReport(V275_REST_Lib.Controllers.FullReport res)
    {
        try
        {
            if (res == null) return;

            if (ActiveImageRoll.IsLocked)
            {
                var ire = new ImageEntry(ActiveImageRoll.UID, res.Image, ActiveImageRoll.TargetDPI);
                if (ActiveImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.Warning("The database is locked. Cannot add image.");
                    return;
                }
            }

            (var entry, var isNew) = ActiveImageRoll.GetImageEntry(res.Image);
            if (entry == null) return;

            entry.NewData = res;

            if (isNew)
                ActiveImageRoll.AddImage(ActiveImageRoll.ImageAddPosition, entry);
            else
                AddResultsEntry(entry);
        }
        finally
        {
            WorkingUpdate(ResultsEntryDevices.V275, false);
        }
    }

    public void ProcessFullReport(FullReport res)
    {
        try
        {
            if (res == null || res.Report == null) return;

            if (res.Report.GetParameter<string>(Parameters.OverallGrade.GetPath(BarcodeVerification.lib.Common.Devices.L95, BarcodeVerification.lib.Common.Symbologies.DataMatrix)) == "Bar Code Not Detected"
                && GlobalAppSettings.Instance.LvsIgnoreNoResults)
                return; // Ignore reports where no barcode was detected

            var thumbnail = res.Template.GetParameter<byte[]>("Report.Thumbnail");
            if (ActiveImageRoll.IsLocked)
            {
                var ire = new ImageEntry(ActiveImageRoll.UID, thumbnail, ActiveImageRoll.TargetDPI);
                if (ActiveImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.Warning("The database is locked. Cannot add image.");
                    return;
                }
            }

            (var entry, var isNew) = ActiveImageRoll.GetImageEntry(thumbnail);
            if (entry == null) return;

            entry.NewData = res;

            if (isNew)
                ActiveImageRoll.AddImage(ActiveImageRoll.ImageAddPosition, entry);
            else
                AddResultsEntry(entry);
        }
        finally
        {
            WorkingUpdate(ResultsEntryDevices.L95, false);
        }
    }

    #endregion

    #region Private Helper Methods

    private void AddImage(ImageAddPositions position, ResultsEntry imageResult)
    {
        var newImages = PromptForNewImages();
        if (newImages != null && newImages.Count > 0)
        {
            ActiveImageRoll.AddImages(position, newImages);
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
                (var entry, var isNew) = ActiveImageRoll.GetImageEntry(filePath);
                if (entry != null && isNew)
                {
                    newImages.Add(entry);
                }
            }
            return newImages;
        }

        return null;
    }

    private void DeleteAllResultsForDatabase(string rollUid)
    {
        if (SelectedResultsDatabase == null || string.IsNullOrEmpty(rollUid))
            return;

        // Remove all ResultsEntry objects matching the roll UID
        var entriesToRemove = ResultssEntries.Where(e => e.ImageRollUID == rollUid).ToList();
        foreach (var entry in entriesToRemove)
            ResultssEntries.Remove(entry); // Remove from UI

        SelectedResultsDatabase.DeleteAllResultsByRollUid(rollUid);
    }

    #endregion

    #region Message Handlers (Receive)

    /// <summary>
    /// Receives property changes for <see cref="ImageRoll"/> and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<ImageRoll> message)
    {
        if (ActiveImageRoll != null)
        {
            ActiveImageRoll.PropertyChanged -= ActiveImageRoll_PropertyChanged;
            ActiveImageRoll.ImageEntries.CollectionChanged -= ActiveImageRoll_Images_CollectionChanged;
        }

        ActiveImageRoll = message.NewValue;

        if (ActiveImageRoll != null)
        {
            ActiveImageRoll.PropertyChanged += ActiveImageRoll_PropertyChanged;
            ActiveImageRoll.ImageEntries.CollectionChanged += ActiveImageRoll_Images_CollectionChanged;
        }
    }

    private void ActiveImageRoll_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImageRoll.SectorType))
            foreach (var lab in ResultssEntries)
                lab.HandlerUpdate(ResultsEntryDevices.All);
    }

    /// <summary>
    /// Receives property changes for <see cref="PrinterSettings"/> and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;

    /// <summary>
    /// Receives property changes for <see cref="Databases.ResultsDatabase"/> and updates the selection.
    /// </summary>
    public void Receive(PropertyChangedMessage<ResultsDatabase> message) => SelectedResultsDatabase = message.NewValue;

    public void Receive(DeleteResultsForRollMessage message) => DeleteAllResultsForDatabase(message.Value);

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

        foreach (var lab in ResultssEntries)
            lab.HandlerUpdate(ResultsEntryDevices.V275);

        HandlerUpdate();
    }

    private void V275Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Node.Controller.IsSimulator) or nameof(Node.Controller.IsLoggedIn_Control))
        {
            foreach (var lab in ResultssEntries)
                lab.HandlerUpdate(ResultsEntryDevices.V275);

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

        foreach (var lab in ResultssEntries)
            lab.HandlerUpdate(ResultsEntryDevices.V5);

        HandlerUpdate();
    }

    private void V5Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(V5_REST_Lib.Controllers.Controller.IsSimulator) or nameof(V5_REST_Lib.Controllers.Controller.IsConnected))
        {
            foreach (var lab in ResultssEntries)
                lab.HandlerUpdate(ResultsEntryDevices.V5);

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

        foreach (var lab in ResultssEntries)
            lab.HandlerUpdate(ResultsEntryDevices.L95);

        HandlerUpdate();
    }

    private void L95Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Lvs95xx.lib.Core.Controllers.Controller.IsSimulator) or nameof(Lvs95xx.lib.Core.Controllers.Controller.IsConnected) or nameof(Lvs95xx.lib.Core.Controllers.Controller.ProcessState))
        {
            foreach (var lab in ResultssEntries)
                lab.HandlerUpdate(ResultsEntryDevices.L95);

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

    #region Sectors Windows Management

    /// <summary>
    /// Displays a new sectors details window.
    /// </summary>
    /// <param name="sectors">The sectors data to display.</param>
    public void ShowSectorsDetailsWindow(object sectors)
    {
        var win = new Views.SectorsDetailsWindow
        {
            DataContext = sectors,
            Owner = Application.Current.MainWindow
        };
        win.Closed += (s, e) => _openSectorsWindows.Remove(win);
        _openSectorsWindows.Add(win);
        win.Show();
    }

    public void ShowSectorsDetailsWindow(JObject templates, JObject reports)
    {
        var win = new Views.SectorsJsonWindow
        {
            Templates = templates,
            Reports = reports,
            Title = "Sectors JSON Data",
            Owner = Application.Current.MainWindow
        };
        win.Closed += (s, e) => _openSectorsWindows.Remove(win);
        _openSectorsWindows.Add(win);
        win.Show();
    }

    /// <summary>
    /// Closes all open sectors details windows.
    /// </summary>
    public void CloseAllSectorsDetailsWindows()
    {
        foreach (var win in _openSectorsWindows.ToArray())
        {
            win.Close();
        }
        _openSectorsWindows.Clear();
    }

    #endregion
}