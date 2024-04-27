using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Databases;
using LabelVal.Messages;
using LabelVal.Models;
using LabelVal.Printer;
using LabelVal.Run;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using V275_REST_lib;
using V275_REST_lib.Models;
using static LabelVal.WindowViewModels.MainWindowViewModel;

namespace LabelVal.WindowViewModels;

public partial class MainWindowViewModel : ObservableRecipient, IRecipient<SystemMessages.StatusMessage>, IRecipient<NodeMessages.SelectedNodeChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


    public class Repeat
    {
        public LabelControlViewModel Label { get; set; }
        public int RepeatNumber { get; set; } = -1;
    }

    public ViewModel PrinterViewModel { get; } = new ViewModel();
    public RunViewModel RunViewModel { get; } = new RunViewModel();

    public StandardsDatabaseViewModel StandardsDatabaseViewModel { get; }

    public V275NodesViewModel V275NodesViewModel { get; } = new V275NodesViewModel();

    [ObservableProperty] private string userMessage = "";

    private int LoopCount => App.Settings.GetValue(nameof(RunViewModel.LoopCount), 1);

    //public class StandardEntryModel : ObservableObject
    //{
    //    private string name;
    //    public string Name
    //    {
    //        get => name;
    //        set
    //        {
    //            SetProperty(ref name, value);

    //            Is300 = Name.EndsWith("300");
    //            IsGS1 = Name.ToLower().StartsWith("gs1");
    //            StandardName = name.Replace(" 300", "");

    //            if (IsGS1)
    //            {
    //                var val = Regex.Match(Name, @"TABLE (\d*\.?\d+)");
    //                if (val.Groups.Count == 2)
    //                    TableID = val.Groups[1].Value;
    //            }
    //        }
    //    }

    //    private string standardPath;
    //    public string StandardPath { get => standardPath; set => SetProperty(ref standardPath, value); }

    //    private int numRows;
    //    public int NumRows { get => numRows; set => SetProperty(ref numRows, value); }

    //    public string StandardName { get; private set; }

    //    public string TableID { get; private set; }

    //    public bool Is300 { get; private set; }

    //    public bool IsGS1 { get; private set; }

    //}

    //public class StandardsDatabaseEntry : ObservableObject
    //{
    //    private string name;
    //    public string Name { get => name; set => SetProperty(ref name, value); }

    //    private string filePath;
    //    public string FilePath { get => filePath; set => SetProperty(ref filePath, value); }

    //}

    public static string Version => App.Version;

    [ObservableProperty] private ObservableCollection<LabelControlViewModel> labels = [];

    public Dictionary<int, Repeat> Repeats { get; } = [];

    private V275Node SelectedNode { get; set; }

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public MainWindowViewModel()
    {
        StandardsDatabaseViewModel = new StandardsDatabaseViewModel(this);

        IsActive = true;
    }

    public void Receive(NodeMessages.SelectedNodeChanged message)
    {
        if(SelectedNode != null)
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

    public void Receive(SystemMessages.StatusMessage message)
    {
        switch (message.Value)
        {
            case SystemMessages.StatusMessageType.Error:
                UserMessage = message.Message;
                break;
            case SystemMessages.StatusMessageType.Info:
                UserMessage = message.Message;
                break;
            case SystemMessages.StatusMessageType.Warning:
                UserMessage = message.Message;
                break;
            case SystemMessages.StatusMessageType.Control:
                if (message.Sender == RunViewModel)
                {
                    if(message.Message == "StartRun")
                        _ = StartRun();
                    
                }
                break;
        }    
    }

    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);

    //private void UpdatePrinters()
    //{
    //    foreach (var r in Labels)
    //        r.PrinterName = StoredPrinter;

    //}

    private void Label_StatusChanged(string status) => UserMessage = status;

    //private async Task ResetRepeats()
    //{
    //    Repeats.Clear();

    //    await V275.Commands.GetRepeatsAvailable();

    //    if (V275.Commands.Available != null && V275.Commands.Available.Count > 0)
    //    {
    //        LabelCount = V275.Commands.Available[0];
    //        if (LabelCount == -1)
    //            LabelCount = 0;
    //    }

    //    else
    //        LabelCount = 0;
    //}

    private void Reset() => Label_StatusChanged("");


    public void ClearLabels()
    {
        foreach (var lab in Labels)
        {
            //lab.Clear();
            lab.Printing -= Label_Printing;
            lab.StatusChanged -= Label_StatusChanged;
            //Labels.Remove(lab);
        }
        Labels.Clear();
    }
    public void LoadLabels()
    {
        Logger.Info("Loading label images from standards directory: {name}", $"{App.AssetsStandardsRoot}\\{StandardsDatabaseViewModel.SelectedStandard.StandardName}\\");

        ClearLabels();

        List<string> images = [];
        foreach (var f in Directory.EnumerateFiles(StandardsDatabaseViewModel.SelectedStandard.StandardPath))
            if (Path.GetExtension(f) == ".png")
                images.Add(f);

        images.Sort();

        Logger.Info("Found label images: {count}", images.Count);

        foreach (var img in images)
        {
            var comment = string.Empty;
            if (File.Exists(img.Replace(".png", ".txt")))
                comment = File.ReadAllText(img.Replace(".png", ".txt"));

            var tmp = new LabelControlViewModel(img, comment, this)
            {
                MainWindow = this,
            };

            tmp.Printing += Label_Printing;
            tmp.StatusChanged += Label_StatusChanged;

            Labels.Add(tmp);
        }
    }

    private async Task StartRun()
    {
        if (!StartRunCheck())
            if (await OkCancelDialog("Missing Label Sectors", "There are Labels that do not have stored sectors. Are you sure you want to continue?") == MessageDialogResult.Negative)
                return;

        _ = RunViewModel.RunController.Init(Labels, LoopCount, StandardsDatabaseViewModel.StandardsDatabase, SelectedNode);

        RunViewModel.StartRunRequest();
    }

    [RelayCommand] private void ClearUserMessage() => UserMessage = "";

    //private void WebSocket_Heartbeat(V275_Events_System ev)
    //{
    //    string state = char.ToUpper(ev.data.state[0]) + ev.data.state.Substring(1);

    //    if (v275_State != state)
    //    {
    //        v275_State = state;

    //        OnPropertyChanged("V275_State");
    //        OnPropertyChanged("V275_JobName");
    //    }

    //}
    private void WebSocket_SetupCapture(Events_System ev)
    {
        if (PrintingLabel == null)
            return;

        Repeats.Add(ev.data.repeat, new Repeat() { Label = PrintingLabel, RepeatNumber = ev.data.repeat });
        PrintingLabel = null;

        if (SelectedNode.IsLoggedIn_Control)
            if (!Repeats.ContainsKey(ev.data.repeat + 1))
                App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
    }
    private void WebSocket_LabelEnd(Events_System ev)
    {
        if (SelectedNode.State == NodeStates.Editing)
            return;
        if (PrintingLabel == null)
            return;

        Repeats.Add(ev.data.repeat, new Repeat() { Label = PrintingLabel, RepeatNumber = ev.data.repeat });
        PrintingLabel = null;

        if (SelectedNode.IsLoggedIn_Control)
            if (!Repeats.ContainsKey(ev.data.repeat + 1))
                App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
    }
    private void WebSocket_StateChange(Events_System ev)
    {
        if (ev != null)
            if (ev.data.toState == "editing" || (ev.data.toState == "running" && ev.data.fromState != "paused"))
                Repeats.Clear();
    }

    private LabelControlViewModel PrintingLabel { get; set; } = null;

    private bool WaitForRepeat;
    private async void Label_Printing(LabelControlViewModel label, string type)
    {
        if (SelectedNode.IsSimulator)
        {
            try
            {
                //int verRes = 1;
                var prepend = "";

                var sim = new Simulator.SimulatorFileHandler();

                if (!sim.DeleteAllImages())
                {
                    //string verCur = V275_Version != null ? V275_Version.Substring(V275_Version.LastIndexOf('-') + 1) : null;

                    //if (verCur != null)
                    //{
                    //    System.Version ver = System.Version.Parse(verCur);
                    //    System.Version verMin = System.Version.Parse("1.1.0.3009");
                    //    verRes = ver.CompareTo(ver);
                    //}

                    //if (verRes > 0)
                    //{
                    Label_StatusChanged("Could not delete all simulator images.");
                    label.IsWorking = false;
                    return;
                    //}
                    //else
                    //{
                    //    sim.UpdateImageList();

                    //    prepend = "_";

                    //    foreach(var imgFile in sim.Images)
                    //    {
                    //        string name = Path.GetFileName(imgFile);

                    //        for(; ; )
                    //        {
                    //            if (name.StartsWith(prepend))
                    //                prepend += prepend;
                    //            else
                    //                break;
                    //        }
                    //    }
                    //}
                }

                if (type == "label")
                {
                    if (!sim.CopyImage(label.LabelImagePath, prepend))
                    {
                        Label_StatusChanged("Could not copy the image to the simulator images directory.");
                        label.IsWorking = false;
                        return;
                    }
                }
                else
                {
                    if (!sim.SaveImage(label.LabelImagePath, label.RepeatImage))
                    {
                        Label_StatusChanged("Could not save the image to the simulator images directory.");
                        label.IsWorking = false;
                        return;
                    }
                }

                //if (!IsLoggedIn_Control )
                //{
                //    if(!await V275.Commands.TriggerSimulator())
                //    {
                //        UserMessage = "Error triggering the simulator.";
                //        label.IsWorking = false;
                //        return;
                //    }

                //}

                if (!SelectedNode.IsLoggedIn_Control)
                {
                    label.IsWorking = false;
                }
            }
            catch (Exception ex)
            {
                Label_StatusChanged(ex.Message);
                label.IsWorking = false;
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
                    printer.Print(label.LabelImagePath, 1, PrinterViewModel.SelectedPrinter, data);
                }
                else
                    printer.Print(label.LabelImagePath, label.PrintCount, PrinterViewModel.SelectedPrinter, "");

                if (!SelectedNode.IsLoggedIn_Control)
                    label.IsWorking = false;
            });
        }

        if (SelectedNode.IsLoggedIn_Control)
        {
            PrintingLabel = label;

            _ = Task.Run(() =>
            {
                WaitForRepeat = true;

                var start = DateTime.Now;
                while (WaitForRepeat)
                {
                    if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(10000))
                    {
                        WaitForRepeat = false;

                        PrintingLabel = null;

                        label.IsFaulted = true;
                        label.IsWorking = false;
                        return;
                    }
                }
            });
        }
        else
            PrintingLabel = null;

        if (SelectedNode.State != NodeStates.Idle && SelectedNode.IsLoggedIn_Control)
        {
            if (!await SelectedNode.EnablePrint("1"))
            {
                WaitForRepeat = false;

                PrintingLabel = null;

                label.IsFaulted = true;
                label.IsWorking = false;
            }
        }

        else
            _ = await SelectedNode.Connection.SimulatorTogglePrint();
    }
    private async void ProcessRepeat(int repeat)
    {
        WaitForRepeat = false;

        if (Repeats[repeat].Label.GradingStandard.IsGS1)
        {
            if (repeat > 0)
                if (!await SelectedNode.Connection.Commands.SetRepeat(repeat))
                {
                    ProcessRepeatFault(repeat);
                    return;
                }

            var i = await Repeats[repeat].Label.LoadTask();

            if (i == 0)
            {
                ProcessRepeatFault(repeat);
                return;
            }

            if (i == 2)
            {
                var sectors = SelectedNode.Connection.CreateSectors(SelectedNode.Connection.SetupDetectEvent, StandardsDatabaseViewModel.SelectedStandard.TableID);

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

        Logger.Info("Reading label results and Image.");
        if (!await Repeats[repeat].Label.ReadTask(repeat))
        {
            ProcessRepeatFault(repeat);
            return;
        }

        Repeats[repeat].Label.IsWorking = false;
        Repeats.Clear();
    }
    private void ProcessRepeatFault(int repeat)
    {
        Repeats[repeat].Label.IsFaulted = true;
        Repeats[repeat].Label.IsWorking = false;

        Repeats.Clear();
    }



    private bool StartRunCheck()
    {
        foreach (var lab in Labels)
            if (lab.LabelSectors.Count == 0)
                return false;
        return true;
    }


}
