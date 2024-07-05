using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Logging;
using LabelVal.Messages;
using LabelVal.Run.ViewModels;
using LabelVal.Sectors.ViewModels;
using LabelVal.Utilities;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using NHibernate.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using V275_REST_lib.Models;

namespace LabelVal.Results.ViewModels;
public partial class ImageResults : ObservableRecipient,
    IRecipient<PropertyChangedMessage<ImageRollEntry>>,
    IRecipient<PropertyChangedMessage<Node>>,
    IRecipient<PropertyChangedMessage<Databases.ImageResultsDatabase>>,
    IRecipient<PropertyChangedMessage<Scanner>>,
    IRecipient<PropertyChangedMessage<PrinterSettings>>,
    IRecipient<SystemMessages.ControlMessage>
{
    private class V275Repeat
    {
        public ImageResultEntry ImageResult { get; set; }
        public int RepeatNumber { get; set; } = -1;
    }

    public RunControl RunControl { get; } = new RunControl();

    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));
    private int LoopCount => App.Settings.GetValue(nameof(LoopCount), 1);

    public ObservableCollection<ImageResultEntry> ImageResultsList { get; } = [];

    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private ImageRollEntry selectedImageRoll;

    [ObservableProperty] private PrinterSettings selectedPrinter;
    [ObservableProperty] private Databases.ImageResultsDatabase selectedDatabase;
    [ObservableProperty] private Scanner selectedScanner;
    partial void OnSelectedImageRollChanged(ImageRollEntry oldValue, ImageRollEntry newValue)
    {
        if (oldValue != null)
        {
            oldValue.Images.CollectionChanged -= SelectedImageRoll_Images_CollectionChanged;
            oldValue.Images.Clear();
        }

        if (newValue != null)
        {
            LoadImageResultsList();
        }
        else
            Application.Current.Dispatcher.Invoke(ClearImageResultsList);
    }

    private Dictionary<int, V275Repeat> TempV275Repeat { get; } = [];

    public ImageResults() => IsActive = true;

    public void ClearImageResultsList()
    {
        foreach (var lab in ImageResultsList)
        {
            lab.V275ProcessImage -= V275ProcessImage;
            //lab.DeleteImage -= DeleteImage;
        }

        ImageResultsList.Clear();
    }
    public async void LoadImageResultsList()
    {
        Application.Current.Dispatcher.Invoke(ClearImageResultsList);

        if (SelectedImageRoll == null)
            return;

        if (SelectedImageRoll.Images.Count == 0)
            await SelectedImageRoll.LoadImages();

        var taskList = new List<Task>();
        foreach (var img in SelectedImageRoll.Images)
        {
            var tsk = App.Current.Dispatcher.BeginInvoke(() => AddImageResultEntry(img)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);

        SelectedImageRoll.Images.CollectionChanged += SelectedImageRoll_Images_CollectionChanged;
    }
    private void SelectedImageRoll_Images_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            var itm = e.NewItems.Cast<ImageEntry>().FirstOrDefault();
            //var itm = e.NewItems.First();
            AddImageResultEntry((ImageEntry)itm);
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            var itm = e.OldItems.Cast<ImageEntry>().FirstOrDefault();
            //var itm = ((IList<ImageEntry>)e.OldItems).First();
            RemoveImageResultEntry((ImageEntry)itm);
        }
    }

    private void AddImageResultEntry(ImageEntry img)
    {
        var tmp = new ImageResultEntry(img, this);
        tmp.V275ProcessImage += V275ProcessImage;

        if(img.IsPlaceholder)
            tmp.V5ProcessCommand.Execute("sensor");

        ImageResultsList.Add(tmp);
    }
    private void RemoveImageResultEntry(ImageEntry img)
    {
        var itm = ImageResultsList.FirstOrDefault(ir => ir.SourceImage == img);
        if (itm != null)
        {
            itm.V275ProcessImage -= V275ProcessImage;
            //img.DeleteImage -= DeleteImage;
            ImageResultsList.Remove(itm);
        }

        // Reorder the remaining items in the list
        int order = 1;
        foreach (var item in ImageResultsList.OrderBy(item => item.SourceImage.Order))
        {
            item.SourceImage.Order = order++;
            SelectedImageRoll.SaveImage(item.SourceImage);
        }
    }

    [RelayCommand]
    public void DeleteImage(ImageResultEntry imageToDelete) =>
        // Remove the specified image from the list
        SelectedImageRoll.DeleteImage(imageToDelete.SourceImage);

    [RelayCommand] private void MoveImageTop(ImageResultEntry imageResult) => ChangeOrderTo(imageResult, 1);
    [RelayCommand] private void MoveImageUp(ImageResultEntry imageResult) => AdjustOrderForMove(imageResult, false);
    [RelayCommand] private void MoveImageDown(ImageResultEntry imageResult) => AdjustOrderForMove(imageResult, true);
    [RelayCommand] private void MoveImageBottom(ImageResultEntry imageResult) => ChangeOrderTo(imageResult, ImageResultsList.Count);
     
    [RelayCommand]
    private void AddV5Image()
    {
        var bees = BitmapImageUtilities.ImageToBytesPNG(BitmapImageUtilities.CreateRandomBitmapImage(50, 50));
        var imagEntry = SelectedImageRoll.GetNewImageEntry(bees);
        imagEntry.IsPlaceholder = true;

        SelectedImageRoll.AddImage(imagEntry);
    }
    [RelayCommand]
    private void AddImageTop()
    {
        var newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, 1);
    }
    [RelayCommand]
    private void AddImageAbove(ImageResultEntry imageResult)
    {
        var newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, imageResult.SourceImage.Order);
    }
    [RelayCommand]
    private void AddImageBelow(ImageResultEntry imageResult)
    {
        var newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, imageResult.SourceImage.Order + 1);
    }
    [RelayCommand]
    private void AddImageBottom()
    {
        var newImages = PromptForNewImages(); // Prompt the user to select multiple images
        if (newImages != null && newImages.Count != 0)
            InsertImageAtOrder(newImages, ImageResultsList.Count + 1);
    }

    private ImageResultEntry PromptForNewImage()
    {
        var settings = new Utilities.FileUtilities.LoadFileDialogSettings
        {
            Filters =
            [
                new Utilities.FileUtilities.FileDialogFilter("Image Files", ["png", "bmp"])
            ],
            Title = "Select image(s).",
        };

        if (Utilities.FileUtilities.LoadFileDialog(settings))
        {
            var newImage = SelectedImageRoll.GetNewImageEntry(settings.SelectedFile, 0); // Order will be set in InsertImageAtOrder
            return new ImageResultEntry(newImage, this);
        }

        return null;
    }
    private List<ImageResultEntry> PromptForNewImages()
    {
        var settings = new Utilities.FileUtilities.LoadFileDialogSettings
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
            var newImages = new List<ImageResultEntry>();
            foreach (var filePath in settings.SelectedFiles) // Iterate over selected files
            {
                var newImage = SelectedImageRoll.GetNewImageEntry(filePath, 0); // Order will be set in InsertImageAtOrder
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
        var currentItemOrder = itemToMove.SourceImage.Order;
        var nextItem = ImageResultsList.FirstOrDefault(item => item.SourceImage.Order == currentItemOrder + 1);
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
        var previousItem = ImageResultsList.FirstOrDefault(item => item.SourceImage.Order == currentItemOrder - 1);
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
            foreach (var item in ImageResultsList.Where(item => item.SourceImage.Order > originalOrder && item.SourceImage.Order <= newOrder))
            {
                item.SourceImage.Order--;
                SelectedImageRoll.SaveImage(item.SourceImage);
            }
        }
        else
        {
            // Moving up in the list
            foreach (var item in ImageResultsList.Where(item => item.SourceImage.Order < originalOrder && item.SourceImage.Order >= newOrder))
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
        var sortedNewImages = newImageResults.OrderBy(img => img.SourceImage.Order).ToList();

        // Adjust the orders of existing items to make space for the new items
        AdjustOrdersBeforeInsert(targetOrder, newImageResults.Count);

        // Insert each new image at the adjusted target order
        foreach (var newImageResult in sortedNewImages)
        {
            // Set the order of the new item
            newImageResult.SourceImage.Order = targetOrder++;
            SelectedImageRoll.AddImage(newImageResult.SourceImage);
        }
    }
    private void AdjustOrdersBeforeInsert(int targetOrder)
    {
        foreach (var item in ImageResultsList)
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
        foreach (var item in ImageResultsList)
        {
            if (item.SourceImage.Order >= targetOrder)
            {
                // Increment the order of existing items to accommodate the new items
                item.SourceImage.Order += numberOfNewItems;
                SelectedImageRoll.SaveImage(item.SourceImage);
            }
        }
    }

    #region V275 Image Results
    private ImageResultEntry PrintingImageResult { get; set; } = null;

    private bool WaitForRepeat;

    private async void V275ProcessImage(ImageResultEntry imageResults, string type)
    {
        if (SelectedNode == null)
        {
            LogWarning("No node selected.");

            _ = StartPrint(imageResults);

            imageResults.IsV275Working = false;

            return;
        }

        if (SelectedNode.IsLoggedIn_Control)
        {
            WaitForRepeat = true;
            PrintingImageResult = imageResults;
        }
        else
        {
            WaitForRepeat = false;
            PrintingImageResult = null;
        }

        if (SelectedNode.IsSimulator && !App.Settings.GetValue<string>(nameof(V275.ViewModels.V275.V275_Host)).Equals("127.0.0.1"))
            V275ProcessImage_API(imageResults, type);
        else if (SelectedNode.IsSimulator)
        {
            V275ProcessImage_FileSystem(imageResults, type);

            if (!SelectedNode.IsLoggedIn_Control)
            {
                imageResults.IsV275Working = false;

                if (!await SelectedNode.Connection.Commands.TriggerSimulator())
                {
                    LogError("Error triggering the simulator.");
                    imageResults.IsV275Working = false;
                    return;
                }
            }
        }
        else
        {
            //Do not wait for completion
            _ = StartPrint(imageResults);

            if (!SelectedNode.IsLoggedIn_Control)
                imageResults.IsV275Working = false;
        }

        if (imageResults.IsV275Working == false)
        {
            WaitForRepeat = false;
            PrintingImageResult = null;
            return;
        }

        //This will wait for the repeat to complete or turn off the working flag if it takes too long
        _ = Task.Run(() =>
        {
            var start = DateTime.Now;
            while (WaitForRepeat)
            {
                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                {
                    WaitForRepeat = false;

                    PrintingImageResult = null;

                    imageResults.IsV275Faulted = true;
                    imageResults.IsV275Working = false;
                    return;
                }
            }
        });

        if (SelectedNode.State != V275.ViewModels.NodeStates.Idle && SelectedNode.IsLoggedIn_Control)
            await SelectedNode.EnablePrint("1");
        else if (SelectedNode.IsSimulator)
            _ = await SelectedNode.Connection.SimulatorTogglePrint();
    }

    private Task StartPrint(ImageResultEntry imageResults) => Task.Run(() =>
    {
        var printer = new Printer.Controller();

        //if (RunViewModel.State != Run.Controller.RunStates.IDLE)
        //{
        //    var data = $"Loop {RunViewModel.RunController.CurrentLoopCount} : {RunViewModel.RunController.CurrentLabelCount}";
        //    printer.Print(imageResults.SourceImage, 1, SelectedPrinter.PrinterName, data);
        //}
        //else
            printer.Print(imageResults.SourceImage, PrintCount, SelectedPrinter.PrinterName, "");
    });

    private async void V275ProcessImage_API(ImageResultEntry imageResults, string type)
    {
        //V275ProcessSimulation_Old(imageResults, type);
        ImageEntry img = null;
        if (type == "source")
            img = imageResults.SourceImage;
        else if (type == "v275Stored")
            img = imageResults.V275ResultRow.Stored;
        else if (type == "v5Stored")
            img = imageResults.V5ResultRow.Stored;

        if (img == null)
        {
            LogError($"The image type is null: {type}");
            imageResults.IsV275Working = false;
            return;
        }

        if (!await SelectedNode.Connection.Commands.TriggerSimulator(new V275_REST_Lib.Models.SimulationTrigger() { image = img.GetPngBytes(), dpi = (int)img.Image.DpiX }))
        {
            LogError("Error triggering the simulator.");
            imageResults.IsV275Working = false;
            return;
        }
    }
    private void V275ProcessImage_FileSystem(ImageResultEntry imageResults, string type)
    {
        try
        {
            var verRes = 1;
            var prepend = "";

            var sim = new Simulator.SimulatorFileHandler();

            if (!sim.DeleteAllImages())
            {
                var verCur = SelectedNode.Product.part?[(SelectedNode.Product.part.LastIndexOf('-') + 1)..];

                if (verCur != null)
                {
                    var ver = System.Version.Parse(verCur);
                    var verMin = System.Version.Parse("1.1.0.3009");
                    verRes = ver.CompareTo(ver);
                }

                if (verRes > 0)
                {
                    LogError("Could not delete all simulator images.");
                    imageResults.IsV275Working = false;
                    return;
                }
                else
                {
                    sim.UpdateImageList();

                    prepend = "_";

                    foreach (var imgFile in sim.Images)
                    {
                        var name = Path.GetFileName(imgFile);

                        for (; ; )
                        {
                            if (name.StartsWith(prepend))
                                prepend += prepend;
                            else
                                break;
                        }
                    }
                }
            }

            if (type == "source")
            {
                if (!sim.SaveImage(prepend + Path.GetFileName(imageResults.SourceImage.Path), imageResults.SourceImage.GetBitmapBytes()))
                {
                    LogError("Could not copy the image to the simulator images directory.");
                    imageResults.IsV275Working = false;
                    return;
                }
            }
            else if (type == "v275Stored")
            {
                if (!sim.SaveImage(prepend + Path.GetFileName(imageResults.SourceImage.Path), imageResults.V275ResultRow.Stored.GetPngBytes()))
                {
                    LogError("Could not save the image to the simulator images directory.");
                    imageResults.IsV275Working = false;
                    return;
                }
            }
            else if (type == "v5Stored")
            {
                if (!sim.SaveImage(prepend + Path.GetFileName(imageResults.SourceImage.Path), imageResults.V5ResultRow.Stored.GetPngBytes()))
                {
                    LogError("Could not save the image to the simulator images directory.");
                    imageResults.IsV275Working = false;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            LogError(ex.Message);
            imageResults.IsV275Working = false;
        }
    }

    private async void ProcessRepeat(int repeat)
    {
        WaitForRepeat = false;

        if (TempV275Repeat[repeat].ImageResult.ImageResults.SelectedImageRoll.WriteSectorsBeforeProcess)
        {
            if (repeat > 0)
                if (!await SelectedNode.Connection.Commands.SetRepeat(repeat))
                {
                    ProcessRepeatFault(repeat);
                    return;
                }

            var i = await TempV275Repeat[repeat].ImageResult.V275LoadTask();

            if (i == 0)
            {
                ProcessRepeatFault(repeat);
                return;
            }

            if (i == 2)
            {
                var sectors = SelectedNode.Connection.CreateSectors(SelectedNode.Connection.SetupDetectEvent, V275GetTableID(SelectedImageRoll.SelectedGS1Table), SelectedNode.Symbologies);

                LogInfo("Creating sectors.");

                foreach (var sec in sectors)
                {
                    if (!await SelectedNode.Connection.AddSector(sec.name, JsonConvert.SerializeObject(sec)))
                    {
                        ProcessRepeatFault(repeat);
                        return;
                    }
                }
            }
        }

        LogInfo("Reading results and Image.");
        if (!await TempV275Repeat[repeat].ImageResult.V275ReadTask(repeat))
        {
            ProcessRepeatFault(repeat);
            return;
        }

        TempV275Repeat[repeat].ImageResult.IsV275Working = false;
        TempV275Repeat.Clear();
    }

    private string V275GetTableID(GS1TableNames gS1TableTypes)
        => gS1TableTypes switch
        {
            GS1TableNames._1 => "1",
            GS1TableNames._2 => "2",
            GS1TableNames._3 => "3",
            GS1TableNames._4 => "4",
            GS1TableNames._5 => "5",
            GS1TableNames._6 => "6",
            GS1TableNames._7_1 => "7.1",
            GS1TableNames._7_2 => "7.2",
            GS1TableNames._7_3 => "7.3",
            GS1TableNames._7_4 => "7.4",
            GS1TableNames._8 => "8",
            GS1TableNames._9 => "9",
            GS1TableNames._10 => "10",
            GS1TableNames._11 => "11",
            GS1TableNames._12_1 => "12.1",
            GS1TableNames._12_2 => "12.2",
            GS1TableNames._12_3 => "12.3",
            _ => "0",
        };

    private void ProcessRepeatFault(int repeat)
    {
        TempV275Repeat[repeat].ImageResult.IsV275Faulted = true;
        TempV275Repeat[repeat].ImageResult.IsV275Working = false;

        TempV275Repeat.Clear();
    }

    private void WebSocket_SetupCapture(Events_System ev)
    {
        if (PrintingImageResult == null)
            return;

        TempV275Repeat.Add(ev.data.repeat, new V275Repeat() { ImageResult = PrintingImageResult, RepeatNumber = ev.data.repeat });
        PrintingImageResult = null;

        if (SelectedNode.IsLoggedIn_Control)
            if (!TempV275Repeat.ContainsKey(ev.data.repeat + 1))
                App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
    }
    private void WebSocket_LabelEnd(Events_System ev)
    {
        if (SelectedNode.State == V275.ViewModels.NodeStates.Editing)
            return;
        if (PrintingImageResult == null)
            return;

        TempV275Repeat.Add(ev.data.repeat, new V275Repeat() { ImageResult = PrintingImageResult, RepeatNumber = ev.data.repeat });
        PrintingImageResult = null;

        if (SelectedNode.IsLoggedIn_Control)
            if (!TempV275Repeat.ContainsKey(ev.data.repeat + 1))
                App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
    }
    private void WebSocket_StateChange(Events_System ev)
    {
        if (ev != null)
            if (ev.data.toState == "editing" || (ev.data.toState == "running" && ev.data.fromState != "paused"))
                TempV275Repeat.Clear();
    }
    #endregion

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
        foreach (var lab in ImageResultsList)
            if (lab.V275StoredSectors.Count == 0)
                return false;
        return true;
    }

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Node> message)
    {
        if (SelectedNode != null)
        {
            SelectedNode.Connection.WebSocket.SetupCapture -= WebSocket_SetupCapture;
            SelectedNode.Connection.WebSocket.LabelEnd -= WebSocket_LabelEnd;
            SelectedNode.Connection.WebSocket.StateChange -= WebSocket_StateChange;

        }

        SelectedNode = message.NewValue;

        if (SelectedNode == null) return;

        SelectedNode.Connection.WebSocket.SetupCapture += WebSocket_SetupCapture;
        SelectedNode.Connection.WebSocket.LabelEnd += WebSocket_LabelEnd;
        SelectedNode.Connection.WebSocket.StateChange += WebSocket_StateChange;
    }
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
    public void Receive(SystemMessages.ControlMessage message)
    {
        if (message.Sender == RunControl)
        {
            if (message.Value == "StartRun")
                _ = StartRun();
        }
    }
    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    #endregion

    #region Logging
    private readonly Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion

}
