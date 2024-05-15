using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using LabelVal.Utilities;
using LabelVal.WindowViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using V275_REST_lib.Models;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageResults : ObservableRecipient,
    IRecipient<ImageRollMessages.SelectedImageRollChanged>,
    IRecipient<NodeMessages.SelectedNodeChanged>,
    IRecipient<PrinterMessages.SelectedPrinterChanged>,
    IRecipient<DatabaseMessages.SelectedDatabseChanged>,
    IRecipient<SystemMessages.StatusMessage>,
    IRecipient<ScannerMessages.SelectedScannerChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));

    [ObservableProperty] private ObservableCollection<ImageResultEntry> imageResultsList = []; 

    [ObservableProperty] private V275.ViewModels.Node selectedNode;
    [ObservableProperty] private ImageRollEntry selectedImageRoll;
    partial void OnSelectedImageRollChanged(ImageRollEntry value) => LoadImageResultsList();

    [ObservableProperty] private PrinterSettings selectedPrinter;
    [ObservableProperty] private Databases.ImageResults selectedDatabase;
    [ObservableProperty] private V5.ViewModels.Scanner selectedScanner;

    public RunViewModel RunViewModel { get; } = new RunViewModel();
    private int LoopCount => App.Settings.GetValue(nameof(RunViewModel.LoopCount), 1);

    private class V275Repeat
    {
        public ImageResultEntry ImageResult { get; set; }
        public int RepeatNumber { get; set; } = -1;
    }
    private Dictionary<int, V275Repeat> TempV275Repeat { get; } = [];

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public ImageResults() => IsActive = true;

    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

    public void Receive(NodeMessages.SelectedNodeChanged message)
    {
        if (SelectedNode != null)
        {
            SelectedNode.Connection.WebSocket.SetupCapture -= WebSocket_SetupCapture;
            SelectedNode.Connection.WebSocket.LabelEnd -= WebSocket_LabelEnd;
            SelectedNode.Connection.WebSocket.StateChange -= WebSocket_StateChange;

        }

        SelectedNode = message.Value;

        if (SelectedNode != null)
        {
            SelectedNode.Connection.WebSocket.SetupCapture += WebSocket_SetupCapture;
            SelectedNode.Connection.WebSocket.LabelEnd += WebSocket_LabelEnd;
            SelectedNode.Connection.WebSocket.StateChange += WebSocket_StateChange;
        }
    }
    public void Receive(ImageRollMessages.SelectedImageRollChanged message) => SelectedImageRoll = message.Value;
    public void Receive(PrinterMessages.SelectedPrinterChanged message) => SelectedPrinter = message.Value;
    public void Receive(DatabaseMessages.SelectedDatabseChanged message) => SelectedDatabase = message.Value;
    public void Receive(ScannerMessages.SelectedScannerChanged message)
    {
        if (SelectedScanner != null)
        {
            //SelectedScanner.ScannerController.ConfigUpdate -= ScannerController_ConfigUpdate;
        }

        SelectedScanner = message.Value;

        if (SelectedScanner != null)
        {
            //SelectedScanner.ScannerController.ConfigUpdate += ScannerController_ConfigUpdate;
        }
    }
    public void Receive(SystemMessages.StatusMessage message)
    {
        switch (message.Value)
        {
            case SystemMessages.StatusMessageType.Control:
                if (message.Sender == RunViewModel)
                {
                    if (message.Message == "StartRun")
                        _ = StartRun();

                }
                break;
        }
    }
    private void SendStatusMessage(string message, SystemMessages.StatusMessageType type) => WeakReferenceMessenger.Default.Send(new SystemMessages.StatusMessage(this, type, message));
    private void SendErrorMessage(string message) => WeakReferenceMessenger.Default.Send(new SystemMessages.StatusMessage(this, SystemMessages.StatusMessageType.Error, message));

    public void ClearImageResultsList()
    {
        foreach (var lab in ImageResultsList)
            lab.V275ProcessImage -= V275ProcessImage;
        
        ImageResultsList.Clear();
    }
    public void LoadImageResultsList()
    {
        Logger.Info("Loading label images from standards directory: {name}", $"{App.AssetsImageRollRoot}\\{SelectedImageRoll.Name}\\");

        ClearImageResultsList();

        List<string> images = [];
        foreach (var f in Directory.EnumerateFiles(SelectedImageRoll.Path))
            if (Path.GetExtension(f) == ".png")
                images.Add(f);

        images.Sort();

        Logger.Info("Found label images: {count}", images.Count);

        foreach (var img in images)
        {
            var comment = string.Empty;
            if (File.Exists(img.Replace(".png", ".txt")))
                comment = File.ReadAllText(img.Replace(".png", ".txt"));

            var tmp = new ImageResultEntry(img, comment, SelectedNode, SelectedImageRoll, SelectedDatabase, SelectedScanner);

            tmp.V275ProcessImage += V275ProcessImage;
            ImageResultsList.Add(tmp);
        }
    }



    #region V275 Image Results
    private ImageResultEntry PrintingImageResult { get; set; } = null;

    private bool WaitForRepeat;
    private async void V275ProcessImage(ImageResultEntry imageResults, string type)
    {
        if (SelectedNode == null)
        {
            SendStatusMessage("No node selected.", SystemMessages.StatusMessageType.Warning);

            var printer = new Printer.Controller();

            printer.Print(imageResults.SourceImagePath, PrintCount, SelectedPrinter.PrinterName, "");

            imageResults.IsV275Working = false;

            return;
        }

        if (SelectedNode.IsSimulator)
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
                        SendErrorMessage("Could not delete all simulator images.");
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
                    if (!sim.CopyImage(imageResults.SourceImagePath, prepend))
                    {
                        SendErrorMessage("Could not copy the image to the simulator images directory.");
                        imageResults.IsV275Working = false;
                        return;
                    }
                }
                else
                {
                    if (!sim.SaveImage(imageResults.SourceImagePath, imageResults.V275Image))
                    {
                        SendErrorMessage("Could not save the image to the simulator images directory.");
                        imageResults.IsV275Working = false;
                        return;
                    }
                }

                if (!SelectedNode.IsLoggedIn_Control)
                {
                    if (!await SelectedNode.Connection.Commands.TriggerSimulator())
                    {
                        SendErrorMessage("Error triggering the simulator.");
                        imageResults.IsV275Working = false;
                        return;
                    }
                }

                if (!SelectedNode.IsLoggedIn_Control)
                {
                    imageResults.IsV275Working = false;
                }
            }
            catch (Exception ex)
            {
                SendErrorMessage(ex.Message);
                imageResults.IsV275Working = false;
                Logger.Error(ex);
            }
        }
        else
        {
            _ = Task.Run(() =>
            {
                var printer = new Printer.Controller();

                if (RunViewModel.State != Run.Controller.RunStates.IDLE)
                {
                    var data = $"Loop {RunViewModel.RunController.CurrentLoopCount} : {RunViewModel.RunController.CurrentLabelCount}";
                    printer.Print(imageResults.SourceImagePath, 1, SelectedPrinter.PrinterName, data);
                }
                else
                    printer.Print(imageResults.SourceImagePath, PrintCount, SelectedPrinter.PrinterName, "");

                if (!SelectedNode.IsLoggedIn_Control)
                    imageResults.IsV275Working = false;
            });
        }

        if (SelectedNode.IsLoggedIn_Control)
        {
            PrintingImageResult = imageResults;

            _ = Task.Run(() =>
            {
                WaitForRepeat = true;

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
        }
        else
            PrintingImageResult = null;

        if (SelectedNode.State != V275.ViewModels.NodeStates.Idle && SelectedNode.IsLoggedIn_Control)
        {
            if (!await SelectedNode.EnablePrint("1"))
            {
                WaitForRepeat = false;

                PrintingImageResult = null;

                imageResults.IsV275Faulted = true;
                imageResults.IsV275Working = false;
            }
        }

        else if (SelectedNode.IsSimulator)
            _ = await SelectedNode.Connection.SimulatorTogglePrint();
    }
    private async void ProcessRepeat(int repeat)
    {
        WaitForRepeat = false;

        if (TempV275Repeat[repeat].ImageResult.SelectedImageRoll.WriteSectorsBeforeProcess)
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

                Logger.Info("Creating sectors.");

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

        Logger.Info("Reading results and Image.");
        if (!await TempV275Repeat[repeat].ImageResult.V275ReadTask(repeat))
        {
            ProcessRepeatFault(repeat);
            return;
        }

        TempV275Repeat[repeat].ImageResult.IsV275Working = false;
        TempV275Repeat.Clear();
    }

    private string V275GetTableID(GS1TableTypes gS1TableTypes)
        => gS1TableTypes switch
        {
            GS1TableTypes.Tabel_2 => "2",
            GS1TableTypes.Tabel_4 => "4",
            GS1TableTypes.Tabel_5 => "5",
            GS1TableTypes.Tabel_6 => "6",
            GS1TableTypes.Tabel_8 => "8",
            GS1TableTypes.Tabel_9 => "9",
            GS1TableTypes.Tabel_10 => "10",
            GS1TableTypes.Tabel_11 => "11",
            GS1TableTypes.Tabel_12_2 => "12.2",
            GS1TableTypes.Tabel_12_3 => "12.3",
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

        _ = RunViewModel.RunController.Init(ImageResultsList, LoopCount, SelectedDatabase, SelectedNode);

        RunViewModel.StartRunRequest();
    }
    private bool StartRunCheck()
    {
        foreach (var lab in ImageResultsList)
            if (lab.V275StoredSectors.Count == 0)
                return false;
        return true;
    }


}
