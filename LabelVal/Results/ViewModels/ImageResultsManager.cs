using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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

    /// <see cref="ImagesMaxHeight"/>
    [ObservableProperty] private int imagesMaxHeight = App.Settings.GetValue(nameof(ImagesMaxHeight), 200, true);
    partial void OnImagesMaxHeightChanged(int value) => App.Settings.SetValue(nameof(ImagesMaxHeight), value);

    /// <see cref="ImagesMaxWidth"/>
    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue(nameof(DualSectorColumns), false, true);
    partial void OnDualSectorColumnsChanged(bool value) => App.Settings.SetValue(nameof(DualSectorColumns), value);

    /// <see cref="ShowExtendedData"/>
    [ObservableProperty] private bool showExtendedData = App.Settings.GetValue(nameof(ShowExtendedData), true, true);
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
            oldValue.Images.CollectionChanged -= SelectedImageRoll_Images_CollectionChanged;
            oldValue.Images.Clear();
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

    /// <see cref="IsV275Selected"/>
    [ObservableProperty] private bool isV275Selected;
    partial void OnIsV275SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.V275); }
    public LabelHandlers V275Handler => SelectedV275Node?.Controller != null && SelectedV275Node.Controller.IsLoggedIn_Control
                ? SelectedV275Node.Controller.IsSimulator
                    ? SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    /// <see cref="IsV5Selected"/>
    [ObservableProperty] private bool isV5Selected;
    partial void OnIsV5SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.V5); }
    public LabelHandlers V5Handler => SelectedV5?.Controller != null && SelectedV5.Controller.IsConnected
                ? SelectedV5.Controller.IsSimulator
                    ? SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    /// <see cref="IsL95Selected"/>
    [ObservableProperty] private bool isL95Selected;
    partial void OnIsL95SelectedChanging(bool value) { if (value) ResetSelected(ImageResultEntryDevices.L95); }

    public LabelHandlers L95Handler => SelectedL95?.Controller != null && SelectedL95.Controller.IsConnected && SelectedL95.Controller.ProcessState == Watchers.lib.Process.Win32_ProcessWatcherProcessState.Running
                ? SelectedL95.Controller.IsSimulator
                    ? SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic ? LabelHandlers.SimulatorDetect : LabelHandlers.SimulatorTrigger
                    : SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic ? LabelHandlers.CameraDetect : LabelHandlers.CameraTrigger
                : LabelHandlers.Offline;

    public ImageResultsManager()
    {
        WeakReferenceMessenger.Default.Register<RequestMessage<ImageRoll>>(
        this,
        (recipient, message) => message.Reply(SelectedImageRoll));

        IsActive = true;
        RecieveAll();
    }

    private void RecieveAll()
    {
        //var ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<Node>());
        //if(ret1.HasReceivedResponse)
        //    SelectedV275Node = ret1.Response;

        RequestMessage<PrinterSettings> ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<PrinterSettings>());
        if (ret2.HasReceivedResponse)
            Receive(new PropertyChangedMessage<PrinterSettings>("", "", null, ret2.Response));

        //RequestMessage<ImageRollEntry> ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<ImageRollEntry>());
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

    public async Task LoadImageResultsEntries()
    {
        if (SelectedImageRoll == null)
            return;

        System.Windows.Threading.DispatcherOperation clrTsk = Application.Current.Dispatcher.BeginInvoke(() => ImageResultsEntries.Clear());

        if (SelectedImageRoll.Images.Count == 0)
            await SelectedImageRoll.LoadImages();

        _ = clrTsk.Wait();

        foreach (ImageEntry img in SelectedImageRoll.Images)
            AddImageResultEntry(img);

        SelectedImageRoll.Images.CollectionChanged += SelectedImageRoll_Images_CollectionChanged;

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
            }
        }

        if (img.NewData is V5_REST_Lib.Controllers.FullReport v5)
        {
            if (ire.ImageResultDeviceEntries.FirstOrDefault((e) => e.Device == ImageResultEntryDevices.V5) is ImageResultDeviceEntry_V5 ird)
            {
                ird.ProcessFullReport(v5);
            }
        }

        else if (img.NewData is FullReport l95)
        {
            if (ire.ImageResultDeviceEntries.FirstOrDefault((e) => e.Device == ImageResultEntryDevices.L95) is ImageResultDeviceEntry_L95 ird)
            {
                ird.ProcessFullReport(l95, true);
            }
        }
    }
    private void RemoveImageResultEntry(ImageEntry img)
    {
        ImageResultEntry itm = ImageResultsEntries.FirstOrDefault(ir => ir.SourceImage == img);
        if (itm != null)
        {
            if (!SelectedImageRoll.ImageRollsDatabase.DeleteImage(SelectedImageRoll.UID, img.UID))
            {
                Logger.LogError("Could not delete image from database.");
                return;
            }
            _ = ImageResultsEntries.Remove(itm);
        }

        // Reorder the remaining items in the list
        var order = 1;
        foreach (ImageResultEntry item in ImageResultsEntries.OrderBy(item => item.SourceImage.Order))
        {
            item.SourceImage.Order = order++;
            SelectedImageRoll.SaveImage(item.SourceImage);
        }
    }

    public void HandlerUpdate()
    {
        OnPropertyChanged(nameof(V275Handler));
        OnPropertyChanged(nameof(V5Handler));
        OnPropertyChanged(nameof(L95Handler));
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

    [RelayCommand] private void MoveImageTop(ImageResultEntry imageResult) => ChangeOrderTo(imageResult, 1);
    [RelayCommand] private void MoveImageUp(ImageResultEntry imageResult) => AdjustOrderForMove(imageResult, false);
    [RelayCommand] private void MoveImageDown(ImageResultEntry imageResult) => AdjustOrderForMove(imageResult, true);
    [RelayCommand] private void MoveImageBottom(ImageResultEntry imageResult) => ChangeOrderTo(imageResult, ImageResultsEntries.Count);

    [RelayCommand]
    private void AddImage() => AddImage(ImageAddPosition, null);
    private void AddImage(ImageAddPositions position, ImageResultEntry imageResult)
    {
        List<ImageEntry> newImages = PromptForNewImages(); // Prompt the user to select an image or multiple images

        if (newImages != null && newImages.Count != 0)
        {
            SelectedImageRoll.AddImages(position, newImages, imageResult?.SourceImage);
        }
    }

    [RelayCommand]
    private async Task AddDeviceImage(ImageResultEntryDevices device)
    {
        switch (device)
        {
            case ImageResultEntryDevices.V275:

                V275_REST_Lib.Controllers.FullReport res = await SelectedV275Node.Controller.ReadTask(-1);
                ProcessFullReport(res);
                break;
            case ImageResultEntryDevices.V5:
                V5_REST_Lib.Controllers.FullReport res1 = await SelectedV5.Controller.Trigger_Wait_Return(true);
                ProcessFullReport(res1);
                break;
            case ImageResultEntryDevices.L95:
                FullReport res2 = SelectedL95.Controller.GetFullReport(-1);
                ProcessFullReport(res2);
                break;
        }
    }

    public void Receive(PropertyChangedMessage<FullReport> message)
    {
        if (IsL95Selected)
            _ = App.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message.NewValue));
    }

    public void ProcessFullReport(V5_REST_Lib.Controllers.FullReport res)
    {
        if (res == null)
            return;
        ImageEntry imagEntry = SelectedImageRoll.GetNewImageEntry(res.Image, ImageAddPosition);
        if (imagEntry == null)
            return;

        imagEntry.NewData = res;

        SelectedImageRoll.AddImage(ImageAddPosition, imagEntry);
    }
    public void ProcessFullReport(V275_REST_Lib.Controllers.FullReport res)
    {
        if (res == null)
            return;
        ImageEntry imagEntry = SelectedImageRoll.GetNewImageEntry(res.Image, ImageAddPosition);
        if (imagEntry == null)
            return;

        imagEntry.NewData = res;
        SelectedImageRoll.AddImage(ImageAddPosition, imagEntry);
    }
    public void ProcessFullReport(FullReport res)
    {
        if (res == null || res.Report == null)
            return;

        //TODO Find a good name
        _ = res.Template.SetParameter<string>("Name", "Verify_1");

        ImageEntry imagEntry = SelectedImageRoll.GetNewImageEntry(res.Template.GetParameter<byte[]>("Report.Thumbnail"), ImageAddPosition);
        if (imagEntry == null)
            return;

        imagEntry.NewData = res;

        SelectedImageRoll.AddImage(ImageAddPositions.Top, imagEntry);
    }

    [RelayCommand]
    public void DeleteImage(ImageResultEntry imageToDelete) =>
    // Remove the specified image from the list
    SelectedImageRoll.Images.Remove(imageToDelete.SourceImage);

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
                ImageEntry newImage = SelectedImageRoll.GetNewImageEntry(filePath, 0); // Order will be set in InsertImageAtOrder
                if (newImage != null)
                {
                    newImages.Add(newImage);
                }
            }
            return newImages;
        }

        return null;
    }

    private void AdjustOrderForMove(ImageResultEntry itemToMove, bool increase)
    {
        // This method can be used to generalize the adjustment logic if needed
        if (increase)
        {
            IncreaseOrder(itemToMove);
        }
        else
        {
            DecreaseOrder(itemToMove);
        }
    }
    public void IncreaseOrder(ImageResultEntry itemToMove)
    {
        var currentItemOrder = itemToMove.SourceImage.Order;
        ImageResultEntry nextItem = ImageResultsEntries.FirstOrDefault(item => item.SourceImage.Order == currentItemOrder + 1);
        if (nextItem != null)
        {
            // Swap the order values
            itemToMove.SourceImage.Order++;
            nextItem.SourceImage.Order--;

            SelectedImageRoll.SaveImage(itemToMove.SourceImage);
            SelectedImageRoll.SaveImage(nextItem.SourceImage);
        }
    }
    public void DecreaseOrder(ImageResultEntry itemToMove)
    {
        var currentItemOrder = itemToMove.SourceImage.Order;
        ImageResultEntry previousItem = ImageResultsEntries.FirstOrDefault(item => item.SourceImage.Order == currentItemOrder - 1);
        if (previousItem != null)
        {
            // Swap the order values
            itemToMove.SourceImage.Order--;
            previousItem.SourceImage.Order++;

            SelectedImageRoll.SaveImage(itemToMove.SourceImage);
            SelectedImageRoll.SaveImage(previousItem.SourceImage);
        }
    }

    public void ChangeOrderTo(ImageResultEntry itemToMove, int newOrder)
    {
        var originalOrder = itemToMove.SourceImage.Order;

        if (newOrder == originalOrder) return; // No change needed

        // Temporarily assign a placeholder order to avoid conflicts during adjustment
        itemToMove.SourceImage.Order = int.MinValue;

        if (newOrder > originalOrder)
        {
            // Moving down in the list
            foreach (ImageResultEntry item in ImageResultsEntries.Where(item => item.SourceImage.Order > originalOrder && item.SourceImage.Order <= newOrder))
            {
                item.SourceImage.Order--;
                SelectedImageRoll.SaveImage(item.SourceImage);
            }
        }
        else
        {
            // Moving up in the list
            foreach (ImageResultEntry item in ImageResultsEntries.Where(item => item.SourceImage.Order < originalOrder && item.SourceImage.Order >= newOrder))
            {
                item.SourceImage.Order++;
                SelectedImageRoll.SaveImage(item.SourceImage);
            }
        }

        // Set the item to its new order
        itemToMove.SourceImage.Order = newOrder;
        SelectedImageRoll.SaveImage(itemToMove.SourceImage);

        // Sort or notify UI if necessary
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
            SelectedImageRoll.PropertyChanged -= SelectedImageRoll_PropertyChanged;

        SelectedImageRoll = message.NewValue;

        if (SelectedImageRoll != null)
            SelectedImageRoll.PropertyChanged += SelectedImageRoll_PropertyChanged;
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
    #endregion

}
