using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Logging;
using LabelVal.LVS_95xx.Sectors;
using LabelVal.LVS_95xx.ViewModels;
using LabelVal.Messages;
using LabelVal.Run.ViewModels;
using LabelVal.Sectors.Interfaces;
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
using System.Threading.Tasks;
using System.Windows;
using V275_REST_lib.Models;

namespace LabelVal.Results.ViewModels;
public partial class ImageResults : ObservableRecipient,
    IRecipient<PropertyChangedMessage<ImageRollEntry>>,
    IRecipient<PropertyChangedMessage<Node>>,
    IRecipient<PropertyChangedMessage<Databases.ImageResultsDatabase>>,
    IRecipient<PropertyChangedMessage<Scanner>>,
    IRecipient<PropertyChangedMessage<Verifier>>,
    IRecipient<PropertyChangedMessage<PrinterSettings>>,
    IRecipient<PropertyChangedMessage<LabelVal.LVS_95xx.Models.FullReport>>
{
    private class V275Repeat
    {
        public ImageResultEntry ImageResult { get; set; }
        public int RepeatNumber { get; set; } = -1;
    }

    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));

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
            LoadImageResultsList();
        }
        else
            Application.Current.Dispatcher.Invoke(ClearImageResultsList);
    }

    [ObservableProperty] bool isL95xxSelected;
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

    private Dictionary<int, V275Repeat> TempV275Repeat { get; } = [];

    private string V275Host => App.Settings.GetValue<string>($"{NodeManager.ClassName}{nameof(NodeManager.Host)}");

    public ImageResults()
    {
        IsActive = true;
        RecieveAll();
    }

    private void RecieveAll()
    {
        RequestMessage<Node> mes1 = new();
        WeakReferenceMessenger.Default.Send(mes1);
        SelectedNode = mes1.Response;

        RequestMessage<ImageRollEntry> mes3 = new();
        WeakReferenceMessenger.Default.Send(mes3);
        SelectedImageRoll = mes3.Response;

        RequestMessage<PrinterSettings> mes2 = new();
        WeakReferenceMessenger.Default.Send(mes2);
        SelectedPrinter = mes2.Response;

        RequestMessage<Databases.ImageResultsDatabase> mes4 = new();
        WeakReferenceMessenger.Default.Send(mes4);
        SelectedDatabase = mes4.Response;

        RequestMessage<Scanner> mes5 = new();
        WeakReferenceMessenger.Default.Send(mes5);
        SelectedScanner = mes5.Response;

        RequestMessage<Verifier> mes6 = new();
        WeakReferenceMessenger.Default.Send(mes6);
        SelectedVerifier = mes6.Response;
    }

    public void ClearImageResultsList()
    {
        foreach (ImageResultEntry lab in ImageResultsList)
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

        List<Task> taskList = new();
        foreach (ImageEntry img in SelectedImageRoll.Images)
        {
            Task tsk = App.Current.Dispatcher.BeginInvoke(() => AddImageResultEntry(img)).Task;
            taskList.Add(tsk);
        }

        await Task.WhenAll([.. taskList]);

        SelectedImageRoll.Images.CollectionChanged += SelectedImageRoll_Images_CollectionChanged;
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

    private void AddImageResultEntry(ImageEntry img)
    {
        ImageResultEntry tmp = new(img, this);
        tmp.V275ProcessImage += V275ProcessImage;

        if (img.NewData is V5_REST_Lib.Controller.TriggerResults v5)
            tmp.V5ProcessResults(v5);
        
        else if (img.NewData is LVS_95xx.Models.FullReport l95)
            tmp.L95xxProcessResults(l95);

        ImageResultsList.Add(tmp);
    }
    private void RemoveImageResultEntry(ImageEntry img)
    {
        ImageResultEntry itm = ImageResultsList.FirstOrDefault(ir => ir.SourceImage == img);
        if (itm != null)
        {
            itm.V275ProcessImage -= V275ProcessImage;
            //img.DeleteImage -= DeleteImage;
            ImageResultsList.Remove(itm);

            SelectedImageRoll.ImageRollsDatabase.DeleteImage(img.UID);
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
        var res = await ProcessV5();

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

    private async Task<V5_REST_Lib.Controller.TriggerResults> ProcessV5()
    {
        var res = await SelectedScanner.Controller.Trigger_Wait_Return(true);

        if (!res.OK)
        {
            LogError("Could not trigger the scanner.");
            return null;
        }

        return res;
    }

    private void L95xxProcess(LabelVal.LVS_95xx.Models.FullReport message)
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
            List<ImageResultEntry> newImages = new();
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
            WaitForRepeat = false;
            PrintingImageResult = null;

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

        if (SelectedNode.IsSimulator && !V275Host.Equals("127.0.0.1"))
            V275ProcessImage_API(imageResults, type);
        else if (SelectedNode.IsSimulator)
        {
            V275ProcessImage_FileSystem(imageResults, type);

            if (!SelectedNode.IsLoggedIn_Control)
            {
                imageResults.IsV275Working = false;

                if (!await SelectedNode.Controller.Commands.SimulationTrigger())
                    LogError("Error triggering the simulator.");
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
            DateTime start = DateTime.Now;
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


        if (!SelectedNode.IsSimulator && SelectedNode.State != V275.ViewModels.NodeStates.Idle && SelectedNode.IsLoggedIn_Control)
            await SelectedNode.EnablePrint("1");

        //Trigger the simulator if it is using the local file system
        if (SelectedNode.IsSimulator && V275Host.Equals("127.0.0.1"))
            _ = await SelectedNode.Controller.Commands.SimulationTrigger();
    }

    private Task StartPrint(ImageResultEntry imageResults) => Task.Run(() =>
    {
        Printer.Controller printer = new();

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

        if (!await SelectedNode.Controller.Commands.SimulationTriggerImage(
            new V275_REST_Lib.Models.SimulationTrigger()
            {
                image = img.ImageBytes,
                dpi = (uint)Math.Round(img.Image.DpiX, 0)
            }))
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
            int verRes = 1;
            string prepend = "";

            Simulator.SimulatorFileHandler sim = new();

            if (!sim.DeleteAllImages())
            {
                string verCur = SelectedNode.Product.part?[(SelectedNode.Product.part.LastIndexOf('-') + 1)..];

                if (verCur != null)
                {
                    Version ver = System.Version.Parse(verCur);
                    Version verMin = System.Version.Parse("1.1.0.3009");
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

                    foreach (string imgFile in sim.Images)
                    {
                        string name = Path.GetFileName(imgFile);

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
                if (!sim.SaveImage(prepend + Path.GetFileName(imageResults.SourceImage.Path), imageResults.SourceImage.ImageBytes))
                {
                    LogError("Could not copy the image to the simulator images directory.");
                    imageResults.IsV275Working = false;
                    return;
                }
            }
            else if (type == "v275Stored")
            {
                if (!sim.SaveImage(prepend + Path.GetFileName(imageResults.SourceImage.Path), imageResults.V275ResultRow.Stored.ImageBytes))
                {
                    LogError("Could not save the image to the simulator images directory.");
                    imageResults.IsV275Working = false;
                    return;
                }
            }
            else if (type == "v5Stored")
            {
                if (!sim.SaveImage(prepend + Path.GetFileName(imageResults.SourceImage.Path), imageResults.V5ResultRow.Stored.ImageBytes))
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
                if (!await SelectedNode.Controller.Commands.SetRepeat(repeat))
                {
                    ProcessRepeatFault(repeat);
                    return;
                }

            int i = await TempV275Repeat[repeat].ImageResult.V275LoadTask();

            if (i == 0)
            {
                ProcessRepeatFault(repeat);
                return;
            }

            if (i == 2)
            {
                List<Sector_New_Verify> sectors = SelectedNode.Controller.CreateSectors(SelectedNode.Controller.SetupDetectEvent, V275GetTableID(SelectedImageRoll.SelectedGS1Table), SelectedNode.Symbologies);

                LogInfo("Creating sectors.");

                foreach (Sector_New_Verify sec in sectors)
                {
                    if (!await SelectedNode.Controller.AddSector(sec.name, JsonConvert.SerializeObject(sec)))
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

    private string V275GetTableID(Sectors.Interfaces.GS1TableNames gS1TableTypes)
        => gS1TableTypes switch
        {
            Sectors.Interfaces.GS1TableNames._1 => "1",
            Sectors.Interfaces.GS1TableNames._2 => "2",
            Sectors.Interfaces.GS1TableNames._3 => "3",
            Sectors.Interfaces.GS1TableNames._4 => "4",
            Sectors.Interfaces.GS1TableNames._5 => "5",
            Sectors.Interfaces.GS1TableNames._6 => "6",
            Sectors.Interfaces.GS1TableNames._7_1 => "7.1",
            Sectors.Interfaces.GS1TableNames._7_2 => "7.2",
            Sectors.Interfaces.GS1TableNames._7_3 => "7.3",
            Sectors.Interfaces.GS1TableNames._7_4 => "7.4",
            Sectors.Interfaces.GS1TableNames._8 => "8",
            Sectors.Interfaces.GS1TableNames._9 => "9",
            Sectors.Interfaces.GS1TableNames._10 => "10",
            Sectors.Interfaces.GS1TableNames._11 => "11",
            Sectors.Interfaces.GS1TableNames._12_1 => "12.1",
            Sectors.Interfaces.GS1TableNames._12_2 => "12.2",
            Sectors.Interfaces.GS1TableNames._12_3 => "12.3",
            _ => "",
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
        foreach (ImageResultEntry lab in ImageResultsList)
            if (lab.V275StoredSectors.Count == 0)
                return false;
        return true;
    }

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<Node> message)
    {
        if (SelectedNode != null)
        {
            SelectedNode.Controller.WebSocket.SetupCapture -= WebSocket_SetupCapture;
            SelectedNode.Controller.WebSocket.LabelEnd -= WebSocket_LabelEnd;
            SelectedNode.Controller.WebSocket.StateChange -= WebSocket_StateChange;

        }

        SelectedNode = message.NewValue;

        if (SelectedNode == null) return;

        SelectedNode.Controller.WebSocket.SetupCapture += WebSocket_SetupCapture;
        SelectedNode.Controller.WebSocket.LabelEnd += WebSocket_LabelEnd;
        SelectedNode.Controller.WebSocket.StateChange += WebSocket_StateChange;
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
    public void Receive(PropertyChangedMessage<Verifier> message) => SelectedVerifier = message.NewValue;
    public void Receive(PropertyChangedMessage<LabelVal.LVS_95xx.Models.FullReport> message)
    {
        if (IsL95xxSelected)
            App.Current.Dispatcher.BeginInvoke(() => L95xxProcess(message.NewValue));
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
