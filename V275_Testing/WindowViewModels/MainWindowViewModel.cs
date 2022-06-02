using V275_Testing.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using V275_Testing.V275;
using V275_Testing.V275.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using V275_Testing.Databases;
using V275_Testing.RunControllers;

namespace V275_Testing.WindowViewModels
{
    public class MainWindowViewModel : Core.BaseViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public class Repeat
        {
            public LabelControlViewModel Label { get; set; }
            public int RepeatNumber { get; set; } = -1;
            public Repeat(LabelControlViewModel label) => Label = label;
        }

        private V275_API_Commands V275 = new V275_API_Commands();
        private StandardsDatabase StandardsDatabase { get; }
        private V275_API_WebSocketEvents WebSocket { get; } = new V275_API_WebSocketEvents();
        private V275_API_WebSocketEvents SysWebSocket { get; } = new V275_API_WebSocketEvents();

        public string V275_Host { get => V275.Host = App.Settings.GetValue("V275_Host", "127.0.0.1"); set { App.Settings.SetValue("V275_Host", value); V275.Host = value; } }
        public string V275_SystemPort { get => V275.SystemPort = App.Settings.GetValue("V275_SystemPort", "8080"); set { App.Settings.SetValue("V275_SystemPort", value); ; V275.SystemPort = value; } }
        public string V275_NodeNumber { get => V275.NodeNumber = App.Settings.GetValue("V275_NodeNumber", "1"); set { App.Settings.SetValue("V275_NodeNumber", value); V275.NodeNumber = value; } }

        public ObservableCollection<V275_Devices.Node> Nodes { get; } = new ObservableCollection<V275_Devices.Node>();
        public V275_Devices.Node SelectedNode
        {
            get => selectedNode;
            set
            {
                SetProperty(ref selectedNode, value);
                if (value != null)
                {
                    V275_NodeNumber = value.enumeration.ToString();
                    IsDeviceSelected = true;
                }
            }
        }
        private V275_Devices.Node selectedNode;

        private ObservableCollection<LabelControlViewModel> labels = new ObservableCollection<LabelControlViewModel>();
        public ObservableCollection<LabelControlViewModel> Labels { get => labels; set => SetProperty(ref labels, value); }

        public string StoredStandard { get => App.Settings.GetValue("StoredStandard", "GS1 TABLE 1"); set { App.Settings.SetValue("StoredStandard", value); } }
        public ObservableCollection<string> Standards { get; } = new ObservableCollection<string>();
        public string SelectedStandard
        {
            get => selectedStandard;
            set
            {
                SetProperty(ref selectedStandard, value);

                if (!string.IsNullOrEmpty(value))
                {
                    StoredStandard = value;
                    LoadLabels();
                }
                else
                {
                    Labels.Clear();
                }
            }
        }
        private string selectedStandard;

        public string StoredPrinter { get => App.Settings.GetValue("StoredPrinter", ""); set { App.Settings.SetValue("StoredPrinter", value); } }
        public ObservableCollection<string> Printers { get; } = new ObservableCollection<string>();
        public string SelectedPrinter
        {
            get => selectedPrinter;
            set
            {
                SetProperty(ref selectedPrinter, value);

                if (!string.IsNullOrEmpty(value))
                {
                    StoredPrinter = value;

                    UpdatePrinters();
                }
            }
        }
        private string selectedPrinter;

        public string UserName { get => App.Settings.GetValue("UserName", "admin"); set => App.Settings.SetValue("UserName", value); }
        public string Password { get => App.Settings.GetValue("Password", "admin"); set => App.Settings.SetValue("Password", value); }
        private V275_Events_System.Data LoginData { get; } = new V275_Events_System.Data();

        public string UserMessage
        {
            get { return userMessage; }
            set { SetProperty(ref userMessage, value); }
        }
        private string userMessage;

        public ICommand GetDevices { get; }
        public bool IsGetDevices
        {
            get => isGetDevices;
            set { SetProperty(ref isGetDevices, value); OnPropertyChanged("IsNotGetDevices"); }
        }
        public bool IsNotGetDevices => !isGetDevices;
        private bool isGetDevices = false;

        public ICommand LoginMonitor { get; }
        public ICommand LoginControl { get; }
        public ICommand Logout { get; }

        public bool IsDeviceSelected
        {
            get => isDeviceSelected;
            set { SetProperty(ref isDeviceSelected, value); OnPropertyChanged("IsNotDeviceSelected"); }
        }
        public bool IsNotDeviceSelected => !isDeviceSelected;
        private bool isDeviceSelected = false;

        public bool IsLoggedIn
        {
            get => IsLoggedIn_Setup || IsLoggedIn_Control;
        }
        public bool IsNotLoggedIn => !(IsLoggedIn_Setup || IsLoggedIn_Control);

        public bool IsLoggedIn_Setup
        {
            get => isLoggedIn_Setup;
            set { SetProperty(ref isLoggedIn_Setup, value); OnPropertyChanged("IsNotLoggedIn_Setup"); OnPropertyChanged("IsNotLoggedIn"); OnPropertyChanged("IsLoggedIn"); }
        }
        public bool IsNotLoggedIn_Setup => !isLoggedIn_Setup;
        private bool isLoggedIn_Setup = false;

        public bool IsLoggedIn_Control
        {
            get => isLoggedIn_Control;
            set { SetProperty(ref isLoggedIn_Control, value); OnPropertyChanged("IsNotLoggedIn_Control"); OnPropertyChanged("IsNotLoggedIn"); OnPropertyChanged("IsLoggedIn"); }
        }
        public bool IsNotLoggedIn_Control => !isLoggedIn_Control;
        private bool isLoggedIn_Control = false;

        public string V275_State
        {
            get => v275_State;
            set => SetProperty(ref v275_State, value);
        }
        private string v275_State;
        public string V275_JobName
        {
            get => v275_JobName;
            set => SetProperty(ref v275_JobName, value);
        }
        private string v275_JobName;

        public bool V275_IsBackupVoid => V275.ConfigurationCamera.backupVoidMode == null ? false : V275.ConfigurationCamera.backupVoidMode.value == "ON";

        public ICommand Print { get; }

        public int RunLoopCount { get => App.Settings.GetValue("RunLoopCount", 1); set { App.Settings.SetValue("RunLoopCount", value); } }

        public ICommand StartRun { get; }
        public bool IsRunRunning
        {
            get => isRunRunning;
            set { SetProperty(ref isRunRunning, value); OnPropertyChanged("IsNotRunRunning"); }
        }
        public bool IsNotRunRunning => !isRunRunning;
        private bool isRunRunning = false;

        public ICommand PauseRun { get; }
        public bool IsRunPaused
        {
            get => isRunPaused;
            set { SetProperty(ref isRunPaused, value); OnPropertyChanged("IsNotRunPaused"); }
        }
        public bool IsNotRunPaused => !isRunPaused;
        private bool isRunPaused = false;

        public ICommand StopRun { get; }

        public RunController.RunStates RunState { get => runState; set => SetProperty(ref runState, value); }
        private RunController.RunStates runState;

        private Dictionary<int, Repeat> Repeats = new Dictionary<int, Repeat>();

        private int LabelCount { get; set; } = 0;
        private RunController CurrentRun { get; set; }

        public MainWindowViewModel()
        {
            GetDevices = new Core.RelayCommand(GetDevicesAction, c => true);

            LoginMonitor = new Core.RelayCommand(LoginMonitorAction, c => true);
            LoginControl = new Core.RelayCommand(LoginControlAction, c => true);
            Logout = new Core.RelayCommand(LogoutAction, c => true);

            StartRun = new Core.RelayCommand(StartRunAction, c => true);
            PauseRun = new Core.RelayCommand(PauseRunAction, c => true);
            StopRun = new Core.RelayCommand(StopRunAction, c => true);

            Print = new Core.RelayCommand(PrintAction, c => true);

            Logger.Info("Initializing standards database: {name}", $"{App.UserDataDirectory}\\{App.StandardsDatabaseName}");
            StandardsDatabase = new StandardsDatabase($"{App.UserDataDirectory}\\{App.StandardsDatabaseName}");

            SetupGradingStandards();
            LoadPrinters();
        }

        private void LoadPrinters()
        {
            Logger.Info("Loading printers.");

            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                Printers.Add(p);

                if (StoredPrinter == p)
                    SelectedPrinter = p;
            }

            Logger.Info("Processed {count} printers.", Printers.Count);

            if (string.IsNullOrEmpty(SelectedPrinter) && Printers.Count > 0)
                SelectedPrinter = Printers.First();
        }

        private void UpdatePrinters()
        {
            foreach (var r in Labels)
                r.PrinterName = StoredPrinter;

        }

        private void LoadLabels()
        {
            Logger.Info("Loading label images from standards directory: {name}", $"{App.StandardsRoot}\\{StoredStandard}\\600\\");

            Labels.Clear();

            List<string> Images = new List<string>();
            Images.Clear();
            foreach (var f in Directory.EnumerateFiles($"{App.StandardsRoot}\\{StoredStandard}\\600\\"))
                Images.Add(f);

            Images.Sort();

            Logger.Info("Processed {count} label images.", Images.Count);

            int i = 1;
            foreach (var img in Images)
            {
                var tmp = new LabelControlViewModel(i++, img, SelectedPrinter, SelectedStandard, StandardsDatabase, V275, MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance);

                tmp.Printing += Label_Printing;

                tmp.IsLoggedIn_Control = IsLoggedIn_Control;
                tmp.IsLoggedIn_Setup = IsLoggedIn_Setup;

                Labels.Add(tmp);
            }
        }



        private async Task ResetRepeats()
        {
            Repeats.Clear();

            await V275.GetRepeatsAvailable();

            if (V275.Available != null && V275.Available.Count > 0)
                LabelCount = V275.Available[0];
            else
                LabelCount = 0;
        }

        private void Reset()
        {
            UserMessage = "";
        }

        private async void GetDevicesAction(object parameter)
        {
            Logger.Info("Loading V275 devices.");

            Reset();

            IsDeviceSelected = false;
            Nodes.Clear();
            SelectedNode = null;

            if (await V275.GetDevices())
            {
                foreach (var node in V275.Devices.nodes)
                {
                    Logger.Debug("Device MAC: {dev}", node.cameraMAC);

                    Nodes.Add(node);

                    if (node.enumeration.ToString() == V275_NodeNumber)
                        SelectedNode = node;
                }

                Logger.Info("Processed {count} devices.", Nodes.Count);


                if (SelectedNode == null && Nodes.Count > 0)
                    SelectedNode = Nodes.First();

                IsGetDevices = true;

                await V275.GetProduct();
            }
            else
            {
                UserMessage = V275.Status;
                IsGetDevices = false;
            }

            
        }

        private void SetupGradingStandards()
        {
            Logger.Info("Loading grading standards from file system. {path}", App.StandardsRoot);

            Standards.Clear();
            SelectedStandard = null;

            foreach (var dir in Directory.EnumerateDirectories(App.StandardsRoot))
            {
                Logger.Debug("GS: {name}", dir.Substring(dir.LastIndexOf("\\") + 1));

                Standards.Add(dir.Substring(dir.LastIndexOf("\\") + 1));
            }
            Logger.Info("Processed {count} grading standards.", Standards.Count);

            Logger.Info("Updating standards database.");

            List<string> tables = StandardsDatabase.GetAllTables();

            foreach (var standard in Standards)
            {
                if (tables.Contains(standard))
                    continue;

                Logger.Debug("Creating table: {name}", standard);
                StandardsDatabase.CreateTable(standard);
            }

            if (Standards.Contains(StoredStandard))
                SelectedStandard = StoredStandard;
            else if (Standards.Count > 0)
                SelectedStandard = Standards.First();
        }

        private async void LoginMonitorAction(object parameter)
        {
            Reset();

            if (await V275.Login(UserName, Password, true))
            {
                _ = PostLogin(true);
            }
            else
            {
                UserMessage = V275.Status;
                IsLoggedIn_Setup = false;
            }
        }
        private async void LoginControlAction(object parameter)
        {
            Reset();

            if (await V275.Login(UserName, Password, false))
            {
                _ = PostLogin(false);
            }
            else
            {
                UserMessage = V275.Status;
                IsLoggedIn_Control = false;
            }
        }
        private async void LogoutAction(object parameter)
        {
            Reset();

            if (!await V275.Logout())
                UserMessage = V275.Status;

            LoginData.accessLevel = "";
            LoginData.token = "";
            LoginData.id = "";
            LoginData.state = "1";

            IsLoggedIn_Control = false;
            IsLoggedIn_Setup = false;

            foreach (var rep in Labels)
            {
                rep.IsLoggedIn_Control = IsLoggedIn_Control;
                rep.IsLoggedIn_Setup = IsLoggedIn_Setup;
            }

            try
            {
                WebSocket.SetupCapture -= WebSocket_SetupCapture;
                WebSocket.SessionStateChange -= WebSocket_SessionStateChange;
                WebSocket.Heartbeat -= WebSocket_Heartbeat;
                WebSocket.SetupDetect -= WebSocket_SetupDetect;

                await WebSocket.StopAsync();
                await SysWebSocket.StopAsync();

                V275_State = "";
                V275_JobName = "";
            }
            catch { }
        }
        private async Task PostLogin(bool isLoggedIn_Setup)
        {
            LoginData.accessLevel = isLoggedIn_Setup ? "monitor" : "control";
            LoginData.token = V275.Token;
            LoginData.id = UserName;
            LoginData.state = "0";

            IsLoggedIn_Setup = isLoggedIn_Setup;
            IsLoggedIn_Control = !isLoggedIn_Setup;

            foreach (var rep in Labels)
            {
                rep.IsLoggedIn_Setup = IsLoggedIn_Setup;
                rep.IsLoggedIn_Control = IsLoggedIn_Control;
            }

            await V275.GetCameraConfig();
            await V275.GetSymbologies();

            WebSocket.SetupCapture -= WebSocket_SetupCapture;
            WebSocket.SessionStateChange -= WebSocket_SessionStateChange;
            WebSocket.Heartbeat -= WebSocket_Heartbeat;
            WebSocket.SetupDetect -= WebSocket_SetupDetect;

            WebSocket.SetupCapture += WebSocket_SetupCapture;
            WebSocket.SessionStateChange += WebSocket_SessionStateChange;
            WebSocket.Heartbeat += WebSocket_Heartbeat;
            WebSocket.SetupDetect += WebSocket_SetupDetect;

            if (!await WebSocket.StartAsync(V275.URLs.WS_NodeEvents))
                return;

            if (!await SysWebSocket.StartAsync(V275.URLs.WS_SystemEvents))
                return;
        }

        private bool detectLock;
        private V275_Events_System detectEvent;
        private void WebSocket_SetupDetect(V275_Events_System ev, bool end)
        {
            detectEvent = ev;
            detectLock = end;
        }
        private async void WebSocket_Heartbeat(V275_Events_System ev)
        {
            string state = char.ToUpper(ev.data.state[0]) + ev.data.state.Substring(1);

            if (V275_State != state)
            {
                V275_State = state;

                if (V275_State == "Editing")
                {
                    await ResetRepeats();
                    new Task(async () =>
                        {
                            if (await V275.GetJob())
                            {
                                V275_JobName = V275.Job.name;
                            }
                            else
                            {
                                V275_JobName = "ERROR GETTING JOB NAME !";
                            }
                        }).Start();
                }
                else
                {
                    await ResetRepeats();
                    V275_JobName = "";
                }

            }

        }
        private void WebSocket_SessionStateChange(V275_Events_System ev)
        {
            //if (ev.data.id == LoginData.id)
            if (ev.data.state == "0")
                if (ev.data.accessLevel == "control")
                    if (LoginData.accessLevel == "control")
                        if (ev.data.token != LoginData.token)
                            LogoutAction(new object());
        }
        private void WebSocket_SetupCapture(V275_Events_System ev)
        {
            if (Repeats.ContainsKey(ev.data.repeat))
            {
                Repeats[ev.data.repeat].RepeatNumber = ev.data.repeat;

                if (IsLoggedIn_Control)
                    if (!Repeats.ContainsKey(ev.data.repeat + 1))
                        App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
            }
            else
            {
                LabelCount = ev.data.repeat;
                return;
            }
            
            if (IsLoggedIn_Control)
                PrintAction("1");
        }

        private void Label_Printing(LabelControlViewModel label)
        {
            if (V275_State == "Editing")
            {
                for (int i = 0; i < label.PrintCount; i++)
                    Repeats.Add(++LabelCount, new Repeat(label));

                if (IsLoggedIn_Control)
                    PrintAction("1");
            }

            //throw new NotImplementedException();
        }
        private async void ProcessRepeat(int repeat)
        {
            if (repeat > 0)
                if (!await V275.SetRepeat(repeat))
                {
                    return;
                }



            detectLock = false;
            int i = await Repeats[repeat].Label.Load();

            if (i == 2)
            {
                await Task.Run(() => { while (!detectLock) { } });

                await Repeats[repeat].Label.CreateSectors(detectEvent, StoredStandard);
            }

            await Repeats[repeat].Label.Read(repeat);

            Repeats[repeat].Label.IsWorking = false;
        }


        private async void PrintAction(object parameter)
        {
            if (V275_IsBackupVoid)
                await V275.Print(false);

            Thread.Sleep(200);

            await V275.Print((string)parameter == "1");
        }

        private void StartRunAction(object parameter)
        {
            if (CurrentRun != null)
            {
                CurrentRun.RunStateChange -= CurrentRun_RunStateChange;
                CurrentRun.Close();
                CurrentRun = null;
            }

            Logger.Info("Starting Run: {stand}; {count}", Labels[0].GradingStandard, RunLoopCount);

            CurrentRun = new RunController(Labels, RunLoopCount, StandardsDatabase, V275.Product.part, SelectedNode.cameraMAC).Init();
            CurrentRun.RunStateChange += CurrentRun_RunStateChange;

            if (CurrentRun == null)
                return;

            CurrentRun.StartAsync();
        }
        private void PauseRunAction(object parameter)
        {
            if (CurrentRun == null)
                return;

            if (CurrentRun.State != RunController.RunStates.PAUSED)
                CurrentRun.Pause();
            else
                CurrentRun.Resume();

        }
        private void StopRunAction(object parameter)
        {
            if (CurrentRun == null)
                return;

            CurrentRun.Stop();
        }

        private void CurrentRun_RunStateChange(RunController.RunStates state)
        {
            RunState = state;
            switch (state)
            {
                case RunController.RunStates.RUNNING:
                    IsRunRunning = true;
                    IsRunPaused = false;
                    break;
                case RunController.RunStates.PAUSED:
                    IsRunRunning = true;
                    IsRunPaused = true;
                    break;
                case RunController.RunStates.STOPPED:
                    IsRunRunning = false;
                    IsRunPaused = false;
                    break;
                default:
                    IsRunRunning = false;
                    IsRunPaused = false;
                    break;
            }
        }
    }
}
