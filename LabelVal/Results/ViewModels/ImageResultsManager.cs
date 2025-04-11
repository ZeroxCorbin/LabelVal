using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Gma.System.MouseKeyHook;
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
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Results.ViewModels;

/// <summary>
/// This is the ViewModel for the Image Results Manager.
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
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    private IKeyboardMouseEvents _globalHook;
    private bool _shiftPressed = false;

    /// <see cref="ImagesMaxHeight"/>
    [ObservableProperty] private int imagesMaxHeight = App.Settings.GetValue(nameof(ImagesMaxHeight), 200, true);
    partial void OnImagesMaxHeightChanged(int value) => App.Settings.SetValue(nameof(ImagesMaxHeight), value);

    /// <see cref="ImagesMaxWidth"/>
    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue(nameof(DualSectorColumns), false, true);
    partial void OnDualSectorColumnsChanged(bool value) => App.Settings.SetValue(nameof(DualSectorColumns), value);

    /// <see cref="ShowExtendedData"/>
    [ObservableProperty] private bool showExtendedData = App.Settings.GetValue(nameof(ShowExtendedData), false, true);
    partial void OnShowExtendedDataChanged(bool value) => App.Settings.SetValue(nameof(ShowExtendedData), value);

    /// <see cref="HideErrorsWarnings"/>
    [ObservableProperty] private bool hideErrorsWarnings = App.Settings.GetValue(nameof(HideErrorsWarnings), false, true);
    partial void OnHideErrorsWarningsChanged(bool value) => App.Settings.SetValue(nameof(HideErrorsWarnings), value);

    /// <see cref="ImageAddPosition"/>
    [ObservableProperty] private ImageAddPositions imageAddPosition = App.Settings.GetValue(nameof(ImageAddPosition), ImageAddPositions.Top, true);
    partial void OnImageAddPositionChanged(ImageAddPositions value) => App.Settings.SetValue(nameof(ImageAddPosition), value);

    /// <see cref="SelectedImageRoll"/>
    [ObservableProperty] private ImageRoll selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRoll oldValue, ImageRoll newValue)
    {
        if (oldValue != null)
        {
            oldValue.ImageEntries.CollectionChanged -= SelectedImageRoll_Images_CollectionChanged;
            oldValue.ImageEntries.Clear();
        }

        if (newValue != null)
        {
            _ = LoadImageResultsEntries();
        }
        else
            Application.Current.Dispatcher.Invoke(() => ImageResultsEntries.Clear());
    }

    /// <see cref="SelectedDatabase"/>
    [ObservableProperty] private Databases.ImageResultsDatabase selectedDatabase;

    public ObservableCollection<ImageResultEntry> ImageResultsEntries { get; } = [];

    /// <see cref="FocusedTemplate"/>
    [ObservableProperty] private JObject focusedTemplate;
    /// <see cref="FocusedReport"/>
    [ObservableProperty] private JObject focusedReport;

    /// <see cref="SelectedV275Node"/>
    [ObservableProperty] private Node selectedV275Node;
    /// <see cref="SelectedV5"/>
    [ObservableProperty] private Scanner selectedV5;
    /// <see cref="SelectedL95"/>
    [ObservableProperty] private Verifier selectedL95;

    /// <see cref="SelectedPrinter"/>
    [ObservableProperty] private PrinterSettings selectedPrinter;

    /// <see cref="IsV275Working"/>
    [ObservableProperty] private bool isV275Working;
    /// <see cref="IsV275Selected"/>
    [ObservableProperty] private bool isV275Selected;
    partial void OnIsV275SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.V275); }
    /// <see cref="IsV5Faulted"/>
    [ObservableProperty] private bool isV275Faulted;
    public LabelHandlers V275Handler => SelectedV275Node?.Controller != null && SelectedV275Node.Controller.IsLoggedIn_Control
                ? SelectedV275Node.Controller.IsSimulator
                    ? _shiftPressed ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : _shiftPressed ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    /// <see cref="IsV5Working"/>
    [ObservableProperty] private bool isV5Working;
    /// <see cref="IsV5Selected"/>
    [ObservableProperty] private bool isV5Selected;
    partial void OnIsV5SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.V5); }
    /// <see cref="IsV5Faulted"/>
    [ObservableProperty] private bool isV5Faulted;
    public LabelHandlers V5Handler => SelectedV5?.Controller != null && SelectedV5.Controller.IsConnected
                ? SelectedV5.Controller.IsSimulator
                    ? _shiftPressed ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : _shiftPressed ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    /// <see cref="IsL95Working"/>
    [ObservableProperty] private bool isL95Working;
    /// <see cref="IsL95Selected"/>
    [ObservableProperty] private bool isL95Selected;
    partial void OnIsL95SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.L95); }
    /// <see cref="IsL95Faulted"/>
    [ObservableProperty] private bool isL95Faulted;
    public LabelHandlers L95Handler => SelectedL95?.Controller != null && SelectedL95.Controller.IsConnected && SelectedL95.Controller.ProcessState == Watchers.lib.Process.Win32_ProcessWatcherProcessState.Running
                ? SelectedL95.Controller.IsSimulator
                    ? _shiftPressed ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : _shiftPressed ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    public ImageResultsManager()
    {
        //WeakReferenceMessenger.Default.Register<RequestMessage<ImageRoll>>(
        //this,
        //(recipient, message) => message.Reply(SelectedImageRoll));

        _globalHook = Hook.GlobalEvents();
        _globalHook.KeyDown += _globalHook_KeyDown;
        _globalHook.KeyUp += _globalHook_KeyUp; ;

        IsActive = true;
        RecieveAll();
    }
    ~ImageResultsManager()
    {
        _globalHook.KeyDown -= _globalHook_KeyDown;
        _globalHook.KeyUp -= _globalHook_KeyUp;
        _globalHook.Dispose();
    }

    private void _globalHook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode is System.Windows.Forms.Keys.LShiftKey or System.Windows.Forms.Keys.RShiftKey)
        {
            _shiftPressed = false;
            App.Current.Dispatcher.Invoke(HandlerUpdate);
        }
    }
    private void _globalHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode is System.Windows.Forms.Keys.LShiftKey or System.Windows.Forms.Keys.RShiftKey)
        {
            _shiftPressed = true;
            App.Current.Dispatcher.Invoke(HandlerUpdate);
        }
    }

    private void RecieveAll()
    {
        //var ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<Node>());
        //if(ret1.HasReceivedResponse)
        //    SelectedV275Node = ret1.Response;

        RequestMessage<PrinterSettings> ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<PrinterSettings>());
        if (ret2.HasReceivedResponse)
            Receive(new PropertyChangedMessage<PrinterSettings>("", "", null, ret2.Response));

        //RequestMessage<ImageRoll> ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<ImageRoll>());
        //if (ret3.HasReceivedResponse)
        //    SelectedImageRoll = ret3.Response;

        RequestMessage<Databases.ImageResultsDatabase> ret4 = WeakReferenceMessenger.Default.Send(new RequestMessage<Databases.ImageResultsDatabase>());
        if (ret4.HasReceivedResponse)
            Receive(new PropertyChangedMessage<Databases.ImageResultsDatabase>("", "", null, ret4.Response));

        RequestMessage<Scanner> ret5 = WeakReferenceMessenger.Default.Send(new RequestMessage<Scanner>());
        if (ret5.HasReceivedResponse)
            Receive(new PropertyChangedMessage<Scanner>("", "", null, ret5.Response));

        RequestMessage<Verifier> ret6 = WeakReferenceMessenger.Default.Send(new RequestMessage<Verifier>());
        if (ret6.HasReceivedResponse)
            Receive(new PropertyChangedMessage<Verifier>("", "", null, ret6.Response));

    }

    private bool isLoadingImages = false;
    public async Task LoadImageResultsEntries()
    {
        if (SelectedImageRoll == null)
            return;
        try
        {
            System.Windows.Threading.DispatcherOperation clrTsk = Application.Current.Dispatcher.BeginInvoke(() => ImageResultsEntries.Clear());

            isLoadingImages = true;
            if (SelectedImageRoll.ImageEntries.Count == 0)
                await SelectedImageRoll.LoadImages();

            _ = clrTsk.Wait();

        }
        finally
        {
            isLoadingImages = false;
        }

        //foreach (ImageEntry img in SelectedImageRoll.ImageEntries)
        //    AddImageResultEntry(img);
    }
    private void SelectedImageRoll_Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {

        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (ImageEntry itm in e.NewItems.Cast<ImageEntry>())
                AddImageResultEntry(itm);
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            foreach (ImageEntry itm in e.OldItems.Cast<ImageEntry>())
                RemoveImageResultEntry(itm);
        }
    }

    private void AddImageResultEntry(ImageEntry img)
    {
        ImageResultEntry ire = ImageResultsEntries.FirstOrDefault(ir => ir.SourceImage.UID == img.UID);
        if (ire == null)
        {
            ire = new ImageResultEntry(img, this);
            ImageResultsEntries.Add(ire);
        }

        if (img.NewData is V275_REST_Lib.Controllers.FullReport v275)
        {
            if (ire.ImageResultDeviceEntries.FirstOrDefault((e) => e.Device == ImageResultEntryDevices.V275) is ImageResultDeviceEntry_V275 ird)
            {
                ird.ProcessFullReport(v275);
                WorkingUpdate(ird.Device, false);
            }
        }

        if (img.NewData is V5_REST_Lib.Controllers.FullReport v5)
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
    }
    private void RemoveImageResultEntry(ImageEntry img)
    {
        ImageResultEntry itm = ImageResultsEntries.FirstOrDefault(ir => ir.SourceImage == img);
        if (itm != null)
        {
            _ = ImageResultsEntries.Remove(itm);
        }

        //// Reorder the remaining items in the list
        //var order = 1;
        //foreach (ImageResultEntry item in ImageResultsEntries.OrderBy(item => item.SourceImage.Order))
        //{
        //    item.SourceImage.Order = order++;
        //    SelectedImageRoll.SaveImage(item.SourceImage);
        //}
    }

    public void HandlerUpdate()
    {
        OnPropertyChanged(nameof(V275Handler));
        OnPropertyChanged(nameof(V5Handler));
        OnPropertyChanged(nameof(L95Handler));
    }

    public void WorkingUpdate(ImageResultEntryDevices device, bool state)
    {
        if (device == ImageResultEntryDevices.V275)
            IsV275Working = state;
        else if (device == ImageResultEntryDevices.V5)
            IsV5Working = state;
        else if (device == ImageResultEntryDevices.L95)
            IsL95Working = state;
    }

    public void FaultedUpdate(ImageResultEntryDevices device, bool state)
    {
        if (device == ImageResultEntryDevices.V275)
            IsV275Faulted = state;
        else if (device == ImageResultEntryDevices.V5)
            IsV5Faulted = state;
        else if (device == ImageResultEntryDevices.L95)
            IsL95Faulted = state;
    }

    public void ResetSelected(ImageResultEntryDevices device)
    {
        foreach (ImageResultEntry lab in ImageResultsEntries)
        {
            IImageResultDeviceEntry dev = lab.ImageResultDeviceEntries.FirstOrDefault(d => d.Device == device);
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

    [RelayCommand]
    private void MoveImageTop(ImageResultEntry imageResult)
    {

    }
    [RelayCommand]
    private void MoveImageUp(ImageResultEntry imageResult)
    {

    }
    [RelayCommand]
    private void MoveImageDown(ImageResultEntry imageResult)
    {

    }
    [RelayCommand]
    private void MoveImageBottom(ImageResultEntry imageResult)
    {

    }

    [RelayCommand]
    private void AddImage() => AddImage(ImageAddPosition, null);
    private void AddImage(ImageAddPositions position, ImageResultEntry imageResult)
    {
        List<ImageEntry> newImages = PromptForNewImages(); // Prompt the user to select an image or multiple images

        SelectedImageRoll.AddImages(ImageAddPositions.Top, newImages);
    }

    [RelayCommand]
    private async Task AddDeviceImage(ImageResultEntryDevices device) => await App.Current.Dispatcher.Invoke(async () =>
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
                                                                                              V275_REST_Lib.Controllers.FullReport res = await SelectedV275Node.Controller.ReadTask(-1);
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
                                                                                              V5_REST_Lib.Controllers.FullReport res1 = await SelectedV5.Controller.Trigger_Wait_Return(true);
                                                                                              ProcessFullReport(res1);
                                                                                          }

                                                                                          break;
                                                                                      case ImageResultEntryDevices.L95:
                                                                                          WorkingUpdate(device, true);

                                                                                          FullReport res2 = SelectedL95.Controller.GetFullReport(-1);
                                                                                          ProcessFullReport(res2);
                                                                                          break;
                                                                                  }
                                                                              });

    public void Receive(PropertyChangedMessage<FullReport> message)
    {
        if (IsL95Selected)
            _ = App.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message.NewValue));
    }

    private void ProcessRepeat(V5_REST_Lib.Controllers.Repeat repeat) => ProcessFullReport(repeat.FullReport);
    public void ProcessFullReport(V5_REST_Lib.Controllers.FullReport res)
    {
        try
        {
            if (res == null)
                return;

            var ire = new ImageEntry(SelectedImageRoll.UID, res.Image, SelectedImageRoll.TargetDPI);

            if (SelectedImageRoll.IsLocked)
            {
                if (SelectedImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.LogWarning("The database is locked. Cannot add image.");
                    return;
                }

                //This assumes the image is already in the database and the results will be added to it.
            }

            (ImageEntry entry, var isNew) = SelectedImageRoll.GetImageEntry(res.Image);
            if (entry == null)
                return;

            entry.NewData = res;

            if (isNew)
                SelectedImageRoll.AddImage(ImageAddPosition, entry);
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
            if (res == null)
                return;

            var ire = new ImageEntry(SelectedImageRoll.UID, res.Image, SelectedImageRoll.TargetDPI);

            if (SelectedImageRoll.IsLocked)
            {
                if (SelectedImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.LogWarning("The database is locked. Cannot add image.");
                    return;
                }

                //This assumes the image is already in the database and the results will be added to it.
            }

            (ImageEntry entry, var isNew) = SelectedImageRoll.GetImageEntry(res.Image);
            if (entry == null)
                return;

            entry.NewData = res;

            if (isNew)
                SelectedImageRoll.AddImage(ImageAddPosition, entry);
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
            if (res == null || res.Report == null)
                return;

            var ire = new ImageEntry(SelectedImageRoll.UID, res.Template.GetParameter<byte[]>("Report.Thumbnail"), SelectedImageRoll.TargetDPI);

            if (SelectedImageRoll.IsLocked)
            {
                if (SelectedImageRoll.ImageEntries.FirstOrDefault(x => x.UID == ire.UID) == null)
                {
                    Logger.LogWarning("The database is locked. Cannot add image.");
                    return;
                }

                //This assumes the image is already in the database and the results will be added to it.
            }

            (ImageEntry entry, var isNew) = SelectedImageRoll.GetImageEntry(res.Template.GetParameter<byte[]>("Report.Thumbnail"));
            if (entry == null)
                return;

            entry.NewData = res;

            if (isNew)
                SelectedImageRoll.AddImage(ImageAddPosition, entry);
            else
                AddImageResultEntry(entry);
        }
        finally
        {
            WorkingUpdate(ImageResultEntryDevices.L95, false);
        }
    }

    [RelayCommand]
    public async Task DeleteImage(ImageResultEntry imageToDelete)
    {
        // Remove the image from the database
        if (SelectedImageRoll.IsLocked)
        {
            Logger.LogWarning("The database is locked. Cannot delete image.");
            return;
        }

        MessageDialogResult answer = await AllImageCancelDialog("Delete Image & Remove Results", $"Are you sure you want to delete this image from the Image Roll?\r\nThis can not be undone!");
        if (answer == MessageDialogResult.FirstAuxiliary)
            return;
        else
        if (answer == MessageDialogResult.Affirmative)
        {
            // Remove the image from the ImageRoll and remove all result entries
            foreach (ImageResultEntry img in ImageResultsEntries)
            {
                if (img.SourceImage.UID == imageToDelete.SourceImage.UID)
                {
                    img.DeleteStored();
                }
            }
            SelectedImageRoll.DeleteImage(imageToDelete.SourceImage);
        }
        else
        if (answer == MessageDialogResult.Negative)
        {
            // Remove the image from the ImageRoll
            SelectedImageRoll.DeleteImage(imageToDelete.SourceImage);
        }
    }

    [RelayCommand]
    private void StoreAllCurrentResults()
    {
        foreach (ImageResultEntry img in ImageResultsEntries)
        {
            img.StoreCommand.Execute(ImageResultEntryDevices.V275);
            img.StoreCommand.Execute(ImageResultEntryDevices.V5);
            img.StoreCommand.Execute(ImageResultEntryDevices.L95);
        }
    }

    [RelayCommand]
    private void ClearAllCurrentResults()
    {
        foreach (ImageResultEntry img in ImageResultsEntries)
        {
            img.ClearCurrentCommand.Execute(ImageResultEntryDevices.V275);
            img.ClearCurrentCommand.Execute(ImageResultEntryDevices.V5);
            img.ClearCurrentCommand.Execute(ImageResultEntryDevices.L95);
        }
    }

    [RelayCommand]
    private void CopyAllSectorsToClipboard()
    {
        var data = "";
        IOrderedEnumerable<ImageResultEntry> sorted = ImageResultsEntries.OrderBy(i => i.SourceImage.Order);
        foreach (ImageResultEntry img in sorted)
        {
            foreach (IImageResultDeviceEntry device in img.ImageResultDeviceEntries)
            {
                if (device.StoredSectors.Count != 0)
                    data += device.StoredSectors.GetSectorsReport($"{img.ImageResultsManager.SelectedImageRoll.Name}{(char)SectorOutputSettings.CurrentDelimiter}{img.SourceImage.Order}") + Environment.NewLine;
                if (device.CurrentSectors.Count != 0)
                    data += device.CurrentSectors.GetSectorsReport($"{img.ImageResultsManager.SelectedImageRoll.Name}{(char)SectorOutputSettings.CurrentDelimiter}{img.SourceImage.Order}") + Environment.NewLine;
            }
        }
        Clipboard.SetText(data);
    }

    private List<ImageEntry> PromptForNewImages()
    {
        FileUtilities.LoadFileDialogSettings settings = new()
        {
            Filters =
            [
                new Utilities.FileUtilities.FileDialogFilter("Image Files", ["png", "bmp"])
            ],
            Title = "Select image(s).",
            Multiselect = true, // Ensure this property is set to allow multiple file selections
        };

        if (Utilities.FileUtilities.LoadFileDialog(settings))
        {
            List<ImageEntry> newImages = [];
            foreach (var filePath in settings.SelectedFiles) // Iterate over selected files
            {
                (ImageEntry entry, var isNew) = SelectedImageRoll.GetImageEntry(filePath); // Order will be set in InsertImageAtOrder
                if (entry != null && isNew)
                {
                    newImages.Add(entry);
                }
            }
            return newImages;
        }

        return null;
    }

    #region Recieve Messages

    /// <summary>
    /// Recieve the selected ImageRoll.
    /// Attach to the PropertyChanged event of the new ImageRoll.
    /// </summary>
    /// <param name="message"></param>
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
    /// <summary>
    /// This will update the Handler for all ImageResultDeviceEntries.
    /// </summary>
    private void SelectedImageRoll_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImageRoll.SectorType))
            foreach (ImageResultEntry lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.All);
    }

    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    public void Receive(PropertyChangedMessage<Databases.ImageResultsDatabase> message) => SelectedDatabase = message.NewValue;

    /// <summary>
    /// Recieve the selected Node.
    /// Attach to the PropertyChanged event of the new Node.Controller.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(PropertyChangedMessage<Node> message)
    {
        if (SelectedV275Node != null && SelectedV275Node.Controller != null)
            SelectedV275Node.Controller.PropertyChanged -= V275Controller_PropertyChanged;

        SelectedV275Node = message.NewValue;

        if (SelectedV275Node != null && SelectedV275Node.Controller != null)
            SelectedV275Node.Controller.PropertyChanged += V275Controller_PropertyChanged;

        foreach (ImageResultEntry lab in ImageResultsEntries)
            lab.HandlerUpdate(ImageResultEntryDevices.V275);

        HandlerUpdate();
    }
    /// <summary>
    /// This will update the Handler for all ImageResultDeviceEntries.
    /// </summary>
    private void V275Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Node.Controller.IsSimulator) or nameof(Node.Controller.IsLoggedIn_Control))
        {
            foreach (ImageResultEntry lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.V275);

            HandlerUpdate();
        }
    }

    public void Receive(PropertyChangedMessage<Scanner> message)
    {
        if (SelectedV5 != null && SelectedV5.Controller != null)
            SelectedV5.Controller.PropertyChanged -= V5Controller_PropertyChanged;

        SelectedV5 = message.NewValue;

        if (SelectedV5 != null && SelectedV5.Controller != null)
            SelectedV5.Controller.PropertyChanged += V5Controller_PropertyChanged;

        foreach (ImageResultEntry lab in ImageResultsEntries)
            lab.HandlerUpdate(ImageResultEntryDevices.V5);

        HandlerUpdate();

    }
    private void V5Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(V5_REST_Lib.Controllers.Controller.IsSimulator) or nameof(V5_REST_Lib.Controllers.Controller.IsConnected))
        {
            foreach (ImageResultEntry lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.V5);

            HandlerUpdate();
        }
    }

    public void Receive(PropertyChangedMessage<Verifier> message)
    {
        if (SelectedL95 != null && SelectedL95.Controller != null)
            SelectedL95.Controller.PropertyChanged -= L95Controller_PropertyChanged;

        SelectedL95 = message.NewValue;

        if (SelectedL95 != null && SelectedL95.Controller != null)
            SelectedL95.Controller.PropertyChanged += L95Controller_PropertyChanged;

        foreach (ImageResultEntry lab in ImageResultsEntries)
            lab.HandlerUpdate(ImageResultEntryDevices.L95);

        HandlerUpdate();
    }
    private void L95Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Lvs95xx.lib.Core.Controllers.Controller.IsSimulator) or nameof(Lvs95xx.lib.Core.Controllers.Controller.IsConnected) or nameof(Lvs95xx.lib.Core.Controllers.Controller.ProcessState))
        {
            foreach (ImageResultEntry lab in ImageResultsEntries)
                lab.HandlerUpdate(ImageResultEntryDevices.L95);

            HandlerUpdate();
        }
    }

    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    public async Task<MessageDialogResult> AllImageCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings() { DefaultButtonFocus = MessageDialogResult.FirstAuxiliary, AffirmativeButtonText = "Image & Results", NegativeButtonText = "Image Only", FirstAuxiliaryButtonText = "Cancel" });
    #endregion

}
