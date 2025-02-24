using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.LVS_95xx.ViewModels;
using LabelVal.Utilities;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using NHibernate.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Results.ViewModels;
public partial class ImageResults : ObservableRecipient,
    IRecipient<PropertyChangedMessage<ImageRollEntry>>,
    IRecipient<PropertyChangedMessage<Node>>,
    IRecipient<PropertyChangedMessage<Databases.ImageResultsDatabase>>,
    IRecipient<PropertyChangedMessage<Scanner>>,
    IRecipient<PropertyChangedMessage<Verifier>>,
    IRecipient<PropertyChangedMessage<PrinterSettings>>,
    IRecipient<PropertyChangedMessage<Lvs95xx.lib.Core.Models.FullReport>>
{
    private class V275Repeat
    {
        public ImageResultEntry ImageResult { get; set; }
        public int RepeatNumber { get; set; } = -1;
    }

    [ObservableProperty] private int imagesMaxHeight = App.Settings.GetValue(nameof(ImagesMaxHeight), 200, true);
    partial void OnImagesMaxHeightChanged(int value) => App.Settings.SetValue(nameof(ImagesMaxHeight), value);

    [ObservableProperty] private bool dualSectorColumns = App.Settings.GetValue(nameof(DualSectorColumns), false, true);
    partial void OnDualSectorColumnsChanged(bool value) => App.Settings.SetValue(nameof(DualSectorColumns), value);

    [ObservableProperty] private bool showExtendedData = App.Settings.GetValue(nameof(ShowExtendedData), true, true);
    partial void OnShowExtendedDataChanged(bool value) => App.Settings.SetValue(nameof(ShowExtendedData), value);

    [ObservableProperty] private bool hideErrorsWarnings = App.Settings.GetValue(nameof(HideErrorsWarnings), false, true);
    partial void OnHideErrorsWarningsChanged(bool value) => App.Settings.SetValue(nameof(HideErrorsWarnings), value);

    public ObservableCollection<ImageResultEntry> ImageResultsList { get; } = [];

    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private ImageRollEntry selectedImageRoll;
    [ObservableProperty] private PrinterSettings selectedPrinter;
    [ObservableProperty] private Databases.ImageResultsDatabase selectedDatabase;
    [ObservableProperty] private Scanner selectedScanner;
    [ObservableProperty] private Verifier selectedVerifier;
    partial void OnSelectedImageRollChanged(ImageRollEntry oldValue, ImageRollEntry newValue)
    {
        if (oldValue != null)
        {
            oldValue.Images.CollectionChanged -= SelectedImageRoll_Images_CollectionChanged;
            oldValue.Images.Clear();
        }

        if (newValue != null)
        {
            _ = LoadImageResultsList();
        }
        else
            Application.Current.Dispatcher.Invoke(() => ImageResultsList.Clear());
    }

    [ObservableProperty] private bool isL95xxSelected;
    partial void OnIsL95xxSelectedChanging(bool value) => ResetL95xxSelected();

    private bool reseting;
    public bool ResetL95xxSelected()
    {
        if (reseting) return false;

        reseting = true;
        foreach (ImageResultEntry lab in ImageResultsList)
            lab.IsL95xxSelected = false;

        return reseting = false;
    }

    public ImageResults()
    {
        IsActive = true;
        RecieveAll();
    }

    private void RecieveAll()
    {
        //var ret1 = WeakReferenceMessenger.Default.Send(new RequestMessage<Node>());
        //if(ret1.HasReceivedResponse)
        //    SelectedNode = ret1.Response;

        RequestMessage<PrinterSettings> ret2 = WeakReferenceMessenger.Default.Send(new RequestMessage<PrinterSettings>());
        if (ret2.HasReceivedResponse)
            SelectedPrinter = ret2.Response;

        RequestMessage<ImageRollEntry> ret3 = WeakReferenceMessenger.Default.Send(new RequestMessage<ImageRollEntry>());
        if (ret3.HasReceivedResponse)
            SelectedImageRoll = ret3.Response;

        RequestMessage<Databases.ImageResultsDatabase> ret4 = WeakReferenceMessenger.Default.Send(new RequestMessage<Databases.ImageResultsDatabase>());
        if (ret4.HasReceivedResponse)
            SelectedDatabase = ret4.Response;

        RequestMessage<Scanner> ret5 = WeakReferenceMessenger.Default.Send(new RequestMessage<Scanner>());
        if (ret5.HasReceivedResponse)
            SelectedScanner = ret5.Response;

        RequestMessage<Verifier> ret6 = WeakReferenceMessenger.Default.Send(new RequestMessage<Verifier>());
        if (ret6.HasReceivedResponse)
            SelectedVerifier = ret6.Response;
    }

    public async Task LoadImageResultsList()
    {
        if (SelectedImageRoll == null)
            return;

        System.Windows.Threading.DispatcherOperation clrTsk = Application.Current.Dispatcher.BeginInvoke(() => ImageResultsList.Clear());

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
        ImageResultEntry tmp = new(img, this);

        if (img.NewData is V5_REST_Lib.Controller.FullReport v5)
            tmp.V5ProcessResults(v5);

        else if (img.NewData is Lvs95xx.lib.Core.Models.FullReport l95)
            tmp.L95xxProcessResults(l95);

        ImageResultsList.Add(tmp);
    }
    private void RemoveImageResultEntry(ImageEntry img)
    {
        ImageResultEntry itm = ImageResultsList.FirstOrDefault(ir => ir.SourceImage == img);
        if (itm != null)
        {
            _ = ImageResultsList.Remove(itm);
            _ = SelectedImageRoll.ImageRollsDatabase.DeleteImage(img.UID);
        }

        // Reorder the remaining items in the list
        int order = 1;
        foreach (ImageResultEntry item in ImageResultsList.OrderBy(item => item.SourceImage.Order))
        {
            item.SourceImage.Order = order++;
            SelectedImageRoll.SaveImage(item.SourceImage);
        }
    }

    [RelayCommand]
    public void DeleteImage(ImageResultEntry imageToDelete) =>
        // Remove the specified image from the list
        SelectedImageRoll.Images.Remove(imageToDelete.SourceImage);

    [RelayCommand] private void MoveImageTop(ImageResultEntry imageResult) => ChangeOrderTo(imageResult, 1);
    [RelayCommand] private void MoveImageUp(ImageResultEntry imageResult) => AdjustOrderForMove(imageResult, false);
    [RelayCommand] private void MoveImageDown(ImageResultEntry imageResult) => AdjustOrderForMove(imageResult, true);
    [RelayCommand] private void MoveImageBottom(ImageResultEntry imageResult) => ChangeOrderTo(imageResult, ImageResultsList.Count);

    [RelayCommand]
    private async Task AddV5Image()
    {
        V5_REST_Lib.Controller.FullReport res = await ProcessV5();

        if (res == null)
            return;

        ImageEntry imagEntry = SelectedImageRoll.GetNewImageEntry(res.FullImage);
        if (imagEntry == null)
            return;

        imagEntry.NewData = res;

        SelectedImageRoll.AddImage(imagEntry);
    }

    [RelayCommand]
    private void AddImageTop()
    {
        List<ImageResultEntry> newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, 1);
    }
    [RelayCommand]
    private void AddImageAbove(ImageResultEntry imageResult)
    {
        List<ImageResultEntry> newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, imageResult.SourceImage.Order);
    }
    [RelayCommand]
    private void AddImageBelow(ImageResultEntry imageResult)
    {
        List<ImageResultEntry> newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, imageResult.SourceImage.Order + 1);
    }
    [RelayCommand]
    private void AddImageBottom()
    {
        List<ImageResultEntry> newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, ImageResultsList.Count + 1);
    }

    [RelayCommand]
    private void StoreAllCurrentResults()
    {
        foreach (ImageResultEntry img in ImageResultsList)
        {
            if (img.L95xxCurrentSectors.Count != 0)
                img.StoreCommand.Execute("L95xx-All");



        }
    }

    private async Task<V5_REST_Lib.Controller.FullReport> ProcessV5()
    {
        V5_REST_Lib.Controller.FullReport res = await SelectedScanner.Controller.Trigger_Wait_Return(true);

        if (!res.OK)
        {
            LogError("Could not trigger the scanner.");
            return null;
        }

        return res;
    }

    private void L95xxProcess(Lvs95xx.lib.Core.Models.FullReport message)
    {
        if (message == null || message.Report == null)
            return;

        if (message.Report.OverallGrade.StartsWith("Bar"))
            return;

        message.Name = "Verify_1";

        // byte[] bees = BitmapImageUtilities.ImageToBytes(BitmapImageUtilities.CreateRandomBitmapImage(50, 50));
        ImageEntry imagEntry = SelectedImageRoll.GetNewImageEntry(message.Report.Thumbnail);
        imagEntry.NewData = message;

        SelectedImageRoll.AddImage(imagEntry);

        //L95xxCurrentSectors.Add(new Sector(message, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));
        //List<ISector> secs = L95xxCurrentSectors.ToList();
        //SortList(secs);
        //SortObservableCollectionByList(secs, L95xxCurrentSectors);

        //L95xxCurrentImage = new ImageEntry(ImageRollUID, message.Report.Thumbnail, 600);
        //UpdateL95xxCurrentImageOverlay();
        //V5CurrentTemplate = config;
        //V5CurrentReport = JsonConvert.DeserializeObject<V5_REST_Lib.Models.ResultsAlt>(triggerResults.ReportJSON);

        //V5CurrentSectors.Clear();

        //List<Sectors.Interfaces.ISector> tempSectors = [];
        //foreach (ResultsAlt.Decodedata rSec in V5CurrentReport._event.data.decodeData)
        //    tempSectors.Add(new V5.Sectors.Sector(rSec, V5CurrentTemplate.response.data.job.toolList[rSec.toolSlot - 1], $"DecodeTool{rSec.toolSlot.ToString()}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

        //if (tempSectors.Count > 0)
        //{
        //    SortList(tempSectors);

        //    foreach (Sectors.Interfaces.ISector sec in tempSectors)
        //        V5CurrentSectors.Add(sec);
        //}

        //V5GetSectorDiff();

        //V5CurrentImageOverlay = CreateSectorsImageOverlay(V5CurrentImage, V5CurrentSectors);

        //return true;
    }

    private ImageResultEntry PromptForNewImage()
    {
        FileUtilities.LoadFileDialogSettings settings = new()
        {
            Filters =
            [
                new Utilities.FileUtilities.FileDialogFilter("Image Files", ["png", "bmp"])
            ],
            Title = "Select image(s).",
        };

        if (Utilities.FileUtilities.LoadFileDialog(settings))
        {
            ImageEntry newImage = SelectedImageRoll.GetNewImageEntry(settings.SelectedFile, 0); // Order will be set in InsertImageAtOrder
            return new ImageResultEntry(newImage, this);
        }

        return null;
    }
    private List<ImageResultEntry> PromptForNewImages()
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
            List<ImageResultEntry> newImages = [];
            foreach (string filePath in settings.SelectedFiles) // Iterate over selected files
            {
                ImageEntry newImage = SelectedImageRoll.GetNewImageEntry(filePath, 0); // Order will be set in InsertImageAtOrder
                if (newImage != null)
                {
                    newImages.Add(new ImageResultEntry(newImage, this));
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
        int currentItemOrder = itemToMove.SourceImage.Order;
        ImageResultEntry nextItem = ImageResultsList.FirstOrDefault(item => item.SourceImage.Order == currentItemOrder + 1);
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
        int currentItemOrder = itemToMove.SourceImage.Order;
        ImageResultEntry previousItem = ImageResultsList.FirstOrDefault(item => item.SourceImage.Order == currentItemOrder - 1);
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
        int originalOrder = itemToMove.SourceImage.Order;

        if (newOrder == originalOrder) return; // No change needed

        // Temporarily assign a placeholder order to avoid conflicts during adjustment
        itemToMove.SourceImage.Order = int.MinValue;

        if (newOrder > originalOrder)
        {
            // Moving down in the list
            foreach (ImageResultEntry item in ImageResultsList.Where(item => item.SourceImage.Order > originalOrder && item.SourceImage.Order <= newOrder))
            {
                item.SourceImage.Order--;
                SelectedImageRoll.SaveImage(item.SourceImage);
            }
        }
        else
        {
            // Moving up in the list
            foreach (ImageResultEntry item in ImageResultsList.Where(item => item.SourceImage.Order < originalOrder && item.SourceImage.Order >= newOrder))
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

    public void InsertImageAtOrder(ImageResultEntry newImageResult, int targetOrder)
    {
        // Adjust the order of existing items to make space for the new item
        AdjustOrdersBeforeInsert(targetOrder);

        // Set the order of the new item
        newImageResult.SourceImage.Order = targetOrder;
        SelectedImageRoll.AddImage(newImageResult.SourceImage);
    }
    public void InsertImageAtOrder(List<ImageResultEntry> newImageResults, int targetOrder)
    {
        if (newImageResults == null || !newImageResults.Any()) return;

        // Sort the new images by their current order (if any) to maintain consistency in their addition
        List<ImageResultEntry> sortedNewImages = newImageResults.OrderBy(img => img.SourceImage.Order).ToList();

        // Adjust the orders of existing items to make space for the new items
        AdjustOrdersBeforeInsert(targetOrder, newImageResults.Count);

        // Insert each new image at the adjusted target order
        foreach (ImageResultEntry newImageResult in sortedNewImages)
        {
            // Set the order of the new item
            newImageResult.SourceImage.Order = targetOrder++;
            SelectedImageRoll.AddImage(newImageResult.SourceImage);
        }
    }
    private void AdjustOrdersBeforeInsert(int targetOrder)
    {
        foreach (ImageResultEntry item in ImageResultsList)
        {
            if (item.SourceImage.Order >= targetOrder)
            {
                // Increment the order of existing items that come after the target order
                item.SourceImage.Order++;
                SelectedImageRoll.SaveImage(item.SourceImage);
            }
        }
    }
    private void AdjustOrdersBeforeInsert(int targetOrder, int numberOfNewItems)
    {
        foreach (ImageResultEntry item in ImageResultsList)
        {
            if (item.SourceImage.Order >= targetOrder)
            {
                // Increment the order of existing items to accommodate the new items
                item.SourceImage.Order += numberOfNewItems;
                SelectedImageRoll.SaveImage(item.SourceImage);
            }
        }
    }

    private async Task StartRun()
    {
        if (!StartRunCheck())
            if (await OkCancelDialog("Missing Stored Sectors", "There are images that do not have stored sectors. Are you sure you want to continue?") == MessageDialogResult.Negative)
                return;

        //_ = RunViewModel.RunController.Init(ImageResultsList, LoopCount, SelectedDatabase, SelectedNode);

        //RunViewModel.StartRunRequest();
    }
    private bool StartRunCheck()
    {
        foreach (ImageResultEntry lab in ImageResultsList)
            if (lab.V275StoredSectors.Count == 0)
                return false;
        return true;
    }

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Node> message) => SelectedNode = message.NewValue;
    public void Receive(PropertyChangedMessage<ImageRollEntry> message) => SelectedImageRoll = message.NewValue;
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    public void Receive(PropertyChangedMessage<Databases.ImageResultsDatabase> message) => SelectedDatabase = message.NewValue;
    public void Receive(PropertyChangedMessage<Scanner> message)
    {
        if (SelectedScanner != null)
        {
            //SelectedScanner.ScannerController.ConfigUpdate -= ScannerController_ConfigUpdate;
        }

        SelectedScanner = message.NewValue;

        if (SelectedScanner != null)
        {
            //SelectedScanner.ScannerController.ConfigUpdate += ScannerController_ConfigUpdate;
        }
    }
    public void Receive(PropertyChangedMessage<Verifier> message) => SelectedVerifier = message.NewValue;
    public void Receive(PropertyChangedMessage<Lvs95xx.lib.Core.Models.FullReport> message)
    {
        if (IsL95xxSelected)
            _ = App.Current.Dispatcher.BeginInvoke(() => L95xxProcess(message.NewValue));
    }
    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

    #region Logging
    private void LogInfo(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
#if DEBUG
    private void LogDebug(string message) => Logging.lib.Logger.LogDebug(GetType(), message);
#else
    private void LogDebug(string message) { }
#endif
    private void LogWarning(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
    private void LogError(string message) => Logging.lib.Logger.LogError(GetType(), message);
    private void LogError(Exception ex) => Logging.lib.Logger.LogError(GetType(), ex);
    private void LogError(string message, Exception ex) => Logging.lib.Logger.LogError(GetType(), ex, message);

    #endregion

}
