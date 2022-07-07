using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LabelVal.V275;
using LabelVal.V275.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using LabelVal.Databases;
using LabelVal.RunControllers;
using LabelVal.Printer;
using MahApps.Metro.Controls.Dialogs;
using System.Text.RegularExpressions;

namespace LabelVal.WindowViewModels
{
    public class MainWindowViewModel : Core.BaseViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public enum V275_States
        {
            editing,
            running,
            paused,
        }
        public class Repeat
        {
            public LabelControlViewModel Label { get; set; }
            public int RepeatNumber { get; set; } = -1;
        }

        public class StandardEntry : Core.BaseViewModel
        {
            private string name;
            public string Name
            {
                get => name;
                set
                {
                    SetProperty(ref name, value);

                    Is300 = Name.EndsWith("300");
                    IsGS1 = Name.ToLower().StartsWith("gs1");
                    StandardName = name.Replace(" 300", "");

                    if (IsGS1)
                    {
                        var val = Regex.Match(Name, @"TABLE (\d*\.?\d+)");
                        if (val.Groups.Count == 2)
                            TableID = val.Groups[1].Value;
                    }
                }
            }

            private string standardPath;
            public string StandardPath { get => standardPath; set => SetProperty(ref standardPath, value); }

            private int numRows;
            public int NumRows { get => numRows; set => SetProperty(ref numRows, value); }

            public string StandardName { get; private set; }

            public string TableID { get; private set; }

            public bool Is300 { get; private set; }

            public bool IsGS1 { get; private set; }

        }

        public class StandardsDatabaseEntry : Core.BaseViewModel
        {
            private string name;
            public string Name { get => name; set => SetProperty(ref name, value); }

            private string filePath;
            public string FilePath { get => filePath; set => SetProperty(ref filePath, value); }

        }

        public string Version => App.Version;

        public V275_API_Controller V275 { get; } = new V275_API_Controller();
        private StandardsDatabase StandardsDatabase { get; set; }
        //private V275_API_WebSocketEvents WebSocket { get; } = new V275_API_WebSocketEvents();
        //private V275_API_WebSocketEvents SysWebSocket { get; } = new V275_API_WebSocketEvents();

        public string V275_Host { get => V275.Host = App.Settings.GetValue("V275_Host", "127.0.0.1"); set { App.Settings.SetValue("V275_Host", value); V275.Host = value; } }
        public uint V275_SystemPort { get => V275.SystemPort = App.Settings.GetValue<uint>("V275_SystemPort", 8080); set { App.Settings.SetValue("V275_SystemPort", value); ; V275.SystemPort = value; } }
        public uint V275_NodeNumber { get => V275.NodeNumber = App.Settings.GetValue<uint>("V275_NodeNumber", 1); set { App.Settings.SetValue("V275_NodeNumber", value); V275.NodeNumber = value; } }
        public string V275_MAC { get; set; }


        private ObservableCollection<V275_Devices.Node> nodes = new ObservableCollection<V275_Devices.Node>();
        public ObservableCollection<V275_Devices.Node> Nodes { get => nodes; set => SetProperty(ref nodes, value); }

        private V275_Devices.Node selectedNode;
        public V275_Devices.Node SelectedNode
        {
            get => selectedNode;
            set
            {
                SetProperty(ref selectedNode, value);
                if (value != null)
                {
                    V275_NodeNumber = (uint)value.enumeration;
                    V275_MAC = value.cameraMAC;

                    IsDeviceSelected = true;
                }
            }
        }

        private ObservableCollection<LabelControlViewModel> labels = new ObservableCollection<LabelControlViewModel>();
        public ObservableCollection<LabelControlViewModel> Labels { get => labels; set => SetProperty(ref labels, value); }

        private bool isGS1Standard;
        public bool IsGS1Standard { get => isGS1Standard; set => SetProperty(ref isGS1Standard, value); }

        public string StoredStandardsDatabase { get => App.Settings.GetValue("StoredStandardsDatabase", App.StandardsDatabaseDefaultName); set { App.Settings.SetValue("StoredStandardsDatabase", value); } }
        public ObservableCollection<string> StandardsDatabases { get; } = new ObservableCollection<string>();
        public string SelectedStandardsDatabase
        {
            get => selectedStandardsDatabase;
            set
            {
                SetProperty(ref selectedStandardsDatabase, value);

                SelectedStandard = null;

                if (!string.IsNullOrEmpty(value))
                {
                    StoredStandardsDatabase = value;

                    LoadStandardsDatabase(StoredStandardsDatabase);
                    SelectStandard();
                }
            }
        }
        private string selectedStandardsDatabase;
        public bool IsDatabaseLocked
        {
            get => isDatabaseLocked;
            set { SetProperty(ref isDatabaseLocked, value); OnPropertyChanged("IsNotDatabaseLocked"); }
        }
        public bool IsNotDatabaseLocked => !isDatabaseLocked;
        private bool isDatabaseLocked = false;
        public bool IsDatabasePermLocked
        {
            get => isDatabasePermLocked;
            set { SetProperty(ref isDatabasePermLocked, value); OnPropertyChanged("IsNotDatabasePermLocked"); }
        }
        public bool IsNotDatabasePermLocked => !isDatabasePermLocked;
        private bool isDatabasePermLocked = false;
        public ICommand CreateStandardsDatabase { get; }
        public ICommand LockStandardsDatabase { get; }

        public StandardEntry StoredStandard { get => App.Settings.GetValue<StandardEntry>("StoredStandard", null); set { App.Settings.SetValue("StoredStandard", value); } }
        public ObservableCollection<StandardEntry> Standards { get; } = new ObservableCollection<StandardEntry>();
        public StandardEntry SelectedStandard
        {
            get => selectedStandard;
            set
            {
                SetProperty(ref selectedStandard, value);

                if (value != null)
                {
                    StoredStandard = value;
                    LoadLabels();

                    CheckTemplateName();
                }
                else
                {
                    ClearLabels();
                }
            }
        }
        private StandardEntry selectedStandard;
        public bool IsWrongTemplateName
        {
            get => isWrongTemplateName;
            set { SetProperty(ref isWrongTemplateName, value); OnPropertyChanged("IsNotWrongTemplateName"); }
        }
        public bool IsNotWrongTemplateName => !isWrongTemplateName;
        private bool isWrongTemplateName = false;

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

        public bool IsDeviceSimulator => V275_MAC == null ? false : V275_MAC.Equals("00:00:00:00:00:00");
        public string SimulatorImageDirectory
        {
            get => App.Settings.GetValue("Simulator_ImageDirectory", @"C:\Program Files\V275\data\images\simulation");
            set { App.Settings.SetValue("Simulator_ImageDirectory", value); OnPropertyChanged("SimulatorImageDirectory"); }
        }

        public bool IsLoggedIn
        {
            get => IsLoggedIn_Monitor || IsLoggedIn_Control;
        }
        public bool IsNotLoggedIn => !(IsLoggedIn_Monitor || IsLoggedIn_Control);

        public bool IsLoggedIn_Monitor
        {
            get => isLoggedIn_Monitor;
            set { SetProperty(ref isLoggedIn_Monitor, value); OnPropertyChanged("IsNotLoggedIn_Monitor"); OnPropertyChanged("IsNotLoggedIn"); OnPropertyChanged("IsLoggedIn"); }
        }
        public bool IsNotLoggedIn_Monitor => !isLoggedIn_Monitor;
        private bool isLoggedIn_Monitor = false;

        public bool IsLoggedIn_Control
        {
            get => isLoggedIn_Control;
            set { SetProperty(ref isLoggedIn_Control, value); OnPropertyChanged("IsNotLoggedIn_Control"); OnPropertyChanged("IsNotLoggedIn"); OnPropertyChanged("IsLoggedIn"); }
        }
        public bool IsNotLoggedIn_Control => !isLoggedIn_Control;
        private bool isLoggedIn_Control = false;

        public bool V275_IsBackupVoid => V275.Commands.ConfigurationCamera.backupVoidMode == null ? false : V275.Commands.ConfigurationCamera.backupVoidMode.value == "ON";

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

        public ICommand V275_SwitchRun { get; }
        public ICommand V275_SwitchEdit { get; }
        public ICommand V275_RemoveRepeat { get; }


        public RunController.RunStates RunState { get => runState; set => SetProperty(ref runState, value); }
        private RunController.RunStates runState = RunController.RunStates.IDLE;

        private Dictionary<int, Repeat> Repeats = new Dictionary<int, Repeat>();

        private RunController CurrentRun { get; set; }

        public ICommand TriggerSim { get; }

        public MainWindowViewModel()
        {
            GetDevices = new Core.RelayCommand(GetDevicesAction, c => true);

            LoginMonitor = new Core.RelayCommand(LoginMonitorAction, c => true);
            LoginControl = new Core.RelayCommand(LoginControlAction, c => true);
            Logout = new Core.RelayCommand(LogoutAction, c => true);

            StartRun = new Core.RelayCommand(StartRunAction, c => true);
            PauseRun = new Core.RelayCommand(PauseRunAction, c => true);
            StopRun = new Core.RelayCommand(StopRunAction, c => true);

            Print = new Core.RelayCommand(EnablePrintAction, c => true);

            V275_SwitchRun = new Core.RelayCommand(V275_SwitchRunAction, c => true);
            V275_SwitchEdit = new Core.RelayCommand(V275_SwitchEditAction, c => true);
            V275_RemoveRepeat = new Core.RelayCommand(V275_RemoveRepeatAction, c => true);

            TriggerSim = new Core.RelayCommand(TriggerSimAction, c => true);

            CreateStandardsDatabase = new Core.RelayCommand(CreateStandardsDatabaseAction, c => true);
            LockStandardsDatabase = new Core.RelayCommand(LockStandardsDatabaseAction, c => true);
            V275.PropertyChanged += V275_PropertyChanged;

            V275.WebSocket.SetupCapture += WebSocket_SetupCapture;
            V275.WebSocket.SessionStateChange += WebSocket_SessionStateChange;
            //V275.WebSocket.Heartbeat += WebSocket_Heartbeat;
            V275.WebSocket.LabelEnd += WebSocket_LabelEnd;
            V275.WebSocket.StateChange += WebSocket_StateChange;


            LoadStandardsList();
            LoadStandardsDatabasesList();

            SelectStandardsDatabase();

            LoadPrinters();
        }

        private void V275_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "V275_JobName")
            {
                CheckTemplateName();
            }
            if (e.PropertyName == "V275_State")
            {
                if(V275.V275_State == "Idle")
                    CheckTemplateName();
            }
        }

        private void CheckTemplateName()
        {
            IsWrongTemplateName = false;
            if (!IsLoggedIn)
            {
                return;
            }

            if (V275.V275_JobName == "")
            {
                IsWrongTemplateName = true;
                _ = OkDialog("Template Not Loaded!", "There is no template loaded in the V275 software.");
                return;
            }

            if (!SelectedStandard.IsGS1)
            {
                if (V275.V275_JobName.ToLower().Equals(SelectedStandard.Name.ToLower()))
                {
                    return;
                }
            }
            else
            {
                if (V275.V275_JobName.ToLower().StartsWith("gs1"))
                {
                    return;
                }
            }

            IsWrongTemplateName = true;
            _ = OkDialog("Template Name Mismatch!", "The template name loaded in the V275 software does not match the selected standard.");
        }

        public async Task<MessageDialogResult> OkCancelDialog(string title, string message)
        {
            MessageDialogResult result = await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

            return result;
        }
        public async Task<MessageDialogResult> OkDialog(string title, string message)
        {
            MessageDialogResult result = await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);

            return result;
        }

        public async Task<string> GetStringDialog(string title, string message)
        {
            string result = await DialogCoordinator.Instance.ShowInputAsync(this, title, message);

            return result;
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

        private void ClearLabels()
        {
            foreach (var lab in Labels)
            {
                //lab.Clear();
                lab.Printing -= Label_Printing;
                //Labels.Remove(lab);
            }
            Labels.Clear();
        }

        private void LoadLabels()
        {
            //IsGS1Standard = SelectedStandard.IsGS1;

            Logger.Info("Loading label images from standards directory: {name}", $"{App.StandardsRoot}\\{SelectedStandard.StandardName}\\");

            ClearLabels();

            List<string> images = new List<string>();
            foreach (var f in Directory.EnumerateFiles(SelectedStandard.StandardPath))
                if (Path.GetExtension(f) == ".png")
                    images.Add(f);

            images.Sort();

            Logger.Info("Found label images: {count}", images.Count);

            foreach (var img in images)
            {
                string comment = string.Empty;
                if (File.Exists(img.Replace(".png", ".txt")))
                    comment = File.ReadAllText(img.Replace(".png", ".txt"));

                var tmp = new LabelControlViewModel(img, comment, SelectedPrinter, SelectedStandard.Name, StandardsDatabase, V275, MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance);

                tmp.Printing += Label_Printing;

                tmp.IsDatabaseLocked = IsDatabaseLocked || IsDatabasePermLocked;
                tmp.IsSimulation = IsDeviceSimulator;
                tmp.IsLoggedIn_Control = IsLoggedIn_Control;
                tmp.IsLoggedIn_Monitor = IsLoggedIn_Monitor;

                Labels.Add(tmp);
            }
        }

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

        private void Reset()
        {
            UserMessage = "";
        }

        private async void GetDevicesAction(object parameter)
        {
            Logger.Info("Loading V275 devices.");

            Reset();

            IsDeviceSelected = false;
            V275_MAC = "";
            Nodes.Clear();
            SelectedNode = null;

            if (await V275.Commands.GetDevices())
            {
                foreach (var node in V275.Commands.Devices.nodes)
                {
                    Logger.Debug("Device MAC: {dev}", node.cameraMAC);

                    Nodes.Add(node);

                    if (node.enumeration == V275_NodeNumber)
                        SelectedNode = node;
                }

                Logger.Info("Processed {count} devices.", Nodes.Count);


                if (SelectedNode == null && Nodes.Count > 0)
                    SelectedNode = Nodes.First();

                IsGetDevices = true;

                await V275.Commands.GetProduct();
            }
            else
            {
                UserMessage = V275.Status;
                IsGetDevices = false;
            }


        }

        private ObservableCollection<string> OrphandStandards { get; } = new ObservableCollection<string>();

        private void LoadStandardsList()
        {
            Logger.Info("Loading grading standards from file system. {path}", App.StandardsRoot);

            Standards.Clear();
            SelectedStandard = null;

            foreach (var dir in Directory.EnumerateDirectories(App.StandardsRoot))
            {
                Logger.Debug("Found: {name}", dir.Substring(dir.LastIndexOf("\\") + 1));

                foreach (var subdir in Directory.EnumerateDirectories(dir))
                {
                    if (subdir.EndsWith("600"))
                        Standards.Add(new StandardEntry()
                        {
                            Name = dir.Substring(dir.LastIndexOf("\\") + 1),
                            StandardPath = subdir,
                        });
                    else if (subdir.EndsWith("300"))
                        Standards.Add(new StandardEntry()
                        {
                            Name = $"{dir.Substring(dir.LastIndexOf("\\") + 1)} 300",
                            StandardPath = subdir,
                        });
                }
            }

            Logger.Info("Processed {count} grading standards.", Standards.Count);
        }
        private void SelectStandard()
        {
            StandardEntry std;
            if (StoredStandard != null && (std = Standards.FirstOrDefault((e) => e.Name.Equals(StoredStandard.Name))) != null)
                SelectedStandard = std;
            else if (Standards.Count > 0)
                SelectedStandard = Standards.First();
        }

        private void LoadStandardsDatabasesList()
        {
            Logger.Info("Loading grading standards databases from file system. {path}", App.StandardsDatabaseRoot);

            StandardsDatabases.Clear();
            SelectedStandardsDatabase = null;

            foreach (var file in Directory.EnumerateFiles(App.StandardsDatabaseRoot))
            {
                Logger.Debug("Found: {name}", Path.GetFileName(file));

                if (file.EndsWith(App.DatabaseExtension))
                    StandardsDatabases.Add(Path.GetFileName(file).Replace(App.DatabaseExtension, ""));
            }

            if (StandardsDatabases.Count == 0)
                StandardsDatabases.Add(App.StandardsDatabaseDefaultName);
        }
        private void SelectStandardsDatabase()
        {
            if (StandardsDatabases.Contains(StoredStandardsDatabase))
                SelectedStandardsDatabase = StoredStandardsDatabase;
            else if (StandardsDatabases.Count > 0)
                SelectedStandardsDatabase = StandardsDatabases.First();
        }
        private void LoadStandardsDatabase(string fileName)
        {
            OrphandStandards.Clear();

            string file = Path.Combine(App.StandardsDatabaseRoot, fileName + App.DatabaseExtension);

            StandardsDatabase?.Close();

            Logger.Info("Initializing standards database: {name}", file);
            StandardsDatabase = new StandardsDatabase(file);

            List<string> tables = StandardsDatabase.GetAllTables();

            IsDatabasePermLocked = tables.Contains("LOCKPERM");
            IsDatabaseLocked = tables.Contains("LOCK");

            foreach (var tbl in tables)
            {
                StandardEntry std;
                if ((std = Standards.FirstOrDefault((e) => e.Name.Equals(tbl))) == null)
                {
                    if (tbl.StartsWith("LOCK"))
                        continue;
                    else
                        OrphandStandards.Add(tbl);
                }
                else
                    std.NumRows = StandardsDatabase.GetAllRowsCount(tbl);
            }

        }

        private async void CreateStandardsDatabaseAction(object parameter)
        {
            string res = await GetStringDialog("New Standards Database", "What is the name of the new database?");
            if (res == null) return;

            if (string.IsNullOrEmpty(res) || res.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            {
                _ = OkDialog("Invalid Name", $"The name '{res}' contains invalid characters.");
                return;
            }

            string file = Path.Combine(App.StandardsDatabaseRoot, res + App.DatabaseExtension);

            _ = new StandardsDatabase(file);

            SelectedStandardsDatabase = null;

            LoadStandardsDatabasesList();

            StoredStandardsDatabase = res;
            SelectStandardsDatabase();

        }
        private void LockStandardsDatabaseAction(object parameter)
        {
            if (IsDatabasePermLocked) return;

            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                StandardsDatabase.DeleteLockTable(false);
                StandardsDatabase.CreateLockTable(true);
            }
            else
            {
                if (IsDatabaseLocked)
                    StandardsDatabase.DeleteLockTable(false);
                else
                    StandardsDatabase.CreateLockTable(false);
            }

            SelectedStandardsDatabase = null;
            SelectStandardsDatabase();
        }

        private async void LoginMonitorAction(object parameter)
        {
            Reset();

            if (!PreLogin()) return;

            if (await V275.Commands.Login(UserName, Password, true))
            {
                _ = PostLogin(true);
            }
            else
            {
                UserMessage = V275.Status;
                IsLoggedIn_Monitor = false;
            }
        }
        private async void LoginControlAction(object parameter)
        {
            Reset();

            if (!PreLogin()) return;

            if (await V275.Commands.Login(UserName, Password, false))
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

            if (!await V275.Commands.Logout())
                UserMessage = V275.Status;

            LoginData.accessLevel = "";
            LoginData.token = "";
            LoginData.id = "";
            LoginData.state = "1";

            IsLoggedIn_Control = false;
            IsLoggedIn_Monitor = false;

            foreach (var rep in Labels)
            {
                rep.IsSimulation = false;
                rep.IsLoggedIn_Control = IsLoggedIn_Control;
                rep.IsLoggedIn_Monitor = IsLoggedIn_Monitor;
            }

            try
            {
                await V275.WebSocket.StopAsync();
                //await SysWebSocket.StopAsync();

                V275.V275_State = "";
                V275.V275_JobName = "";
            }
            catch { }
        }
        private bool PreLogin()
        {
            if (IsDeviceSimulator)
            {
                if (Directory.Exists(SimulatorImageDirectory))
                {
                    try
                    {
                        File.Create(Path.Combine(SimulatorImageDirectory, "file")).Close();
                        File.Delete(Path.Combine(SimulatorImageDirectory, "file"));
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    _ = OkDialog("Invalid Simulation Images Directory", $"Please select a valid simulator images directory.\r\n'{SimulatorImageDirectory}'");
                    return false;
                }

            }
            return true;
        }
        private async Task PostLogin(bool isLoggedIn_Monitor)
        {
            LoginData.accessLevel = isLoggedIn_Monitor ? "monitor" : "control";
            LoginData.token = V275.Commands.Token;
            LoginData.id = UserName;
            LoginData.state = "0";

            IsLoggedIn_Monitor = isLoggedIn_Monitor;
            IsLoggedIn_Control = !isLoggedIn_Monitor;

            foreach (var rep in Labels)
            {
                rep.IsDatabaseLocked = IsDatabaseLocked || IsDatabasePermLocked;
                rep.IsSimulation = IsDeviceSimulator;
                rep.IsLoggedIn_Monitor = IsLoggedIn_Monitor;
                rep.IsLoggedIn_Control = IsLoggedIn_Control;
            }

            await V275.Commands.GetCameraConfig();
            await V275.Commands.GetSymbologies();

            if (!await V275.WebSocket.StartAsync(V275.Commands.URLs.WS_NodeEvents))
                return;

            Repeats.Clear();
        }


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
            if (PrintingLabel == null)
                return;

            Repeats.Add(ev.data.repeat, new Repeat() { Label = PrintingLabel, RepeatNumber = ev.data.repeat });
            PrintingLabel = null;

            if (IsLoggedIn_Control)
                if (!Repeats.ContainsKey(ev.data.repeat + 1))
                    App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
        }
        private void WebSocket_LabelEnd(V275_Events_System ev)
        {
            if (V275.V275_State == "Editing")
                return;

            if (PrintingLabel == null)
                return;

            Repeats.Add(ev.data.repeat, new Repeat() { Label = PrintingLabel, RepeatNumber = ev.data.repeat });
            PrintingLabel = null;

            if (IsLoggedIn_Control)
                if (!Repeats.ContainsKey(ev.data.repeat + 1))
                    App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
        }
        private void WebSocket_StateChange(V275_Events_System ev)
        {
            if (ev.data.toState == "editing" || (ev.data.toState == "running" && ev.data.fromState != "paused"))
                Repeats.Clear();
        }

        private LabelControlViewModel PrintingLabel { get; set; } = null;

        private bool WaitForRepeat;
        private void Label_Printing(LabelControlViewModel label, string type)
        {
            if (label.IsSimulation)
            {
                try
                {
                    var sim = new Simulator.SimulatorFileHandler();
                    if (!sim.DeleteAllImages())
                    {
                        label.IsWorking = false;
                        return;
                    }

                    if (type == "label")
                    {
                        if (!sim.CopyImage(label.LabelImagePath))
                        {
                            label.IsWorking = false;
                            return;
                        }
                    }
                    else
                    {
                        if (!sim.SaveImage(label.LabelImagePath, label.RepeatImage))
                        {
                            label.IsWorking = false;
                            return;
                        }
                    }

                    _ = V275.Commands.TriggerSimulator();

                    if (!IsLoggedIn_Control)
                    {
                        label.IsWorking = false;
                    }
                }
                catch (Exception ex)
                {
                    label.IsWorking = false;
                    Logger.Error(ex);
                }


            }
            else
            {
                Task.Run(() =>
                {
                    PrintControl printer = new PrintControl();

                    string data = String.Empty;
                    if (RunState != RunController.RunStates.IDLE)
                        data = $"Loop {CurrentRun.CurrentLoopCount} : {CurrentRun.CurrentLabelCount}";

                    printer.Print(label.LabelImagePath, label.PrintCount, SelectedPrinter, data);

                    if (!IsLoggedIn_Control)
                        label.IsWorking = false;
                });
            }

            if (IsLoggedIn_Control)
            {
                PrintingLabel = label;

                Task.Run(() =>
                {
                    WaitForRepeat = true;

                    DateTime start = DateTime.Now;
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

            if (V275.V275_State != "Idle" && IsLoggedIn_Control)
                EnablePrintAction("1");
        }
        private async void ProcessRepeat(int repeat)
        {
            WaitForRepeat = false;

            if (Repeats[repeat].Label.IsGS1Standard)
            {
                if (repeat > 0)
                    if (!await V275.Commands.SetRepeat(repeat))
                    {
                        ProcessRepeatFault(repeat);
                        return;
                    }

                int i = await Repeats[repeat].Label.Load();

                if (i == 0)
                {
                    ProcessRepeatFault(repeat);
                    return;
                }

                if (i == 2)
                {
                    var sectors = V275.CreateSectors(V275.SetupDetectEvent, StoredStandard.TableID);

                    Logger.Info("Creating sectors.");

                    foreach (var sec in sectors)
                        if (!await V275.AddSector(sec.name, JsonConvert.SerializeObject(sec)))
                        {
                            ProcessRepeatFault(repeat);
                            return;
                        }
                }
            }

            Logger.Info("Reading label results and Image.");
            if (!await Repeats[repeat].Label.Read(repeat))
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

        private async void EnablePrintAction(object parameter)
        {
            if (V275_IsBackupVoid)
            {
                await V275.Commands.Print(false);
                Thread.Sleep(50);
            }

            await V275.Commands.Print((string)parameter == "1");
        }

        private void TriggerSimAction(object parameter)
        {
            _ = V275.Commands.TriggerSimulator();
        }
        private async void V275_SwitchRunAction(object parameter)
        {
            await V275.SwitchToRun();
        }
        private async void V275_SwitchEditAction(object parameter)
        {
            await V275.SwitchToEdit();
        }
        private async void V275_RemoveRepeatAction(object parameter)
        {
            int repeat;

            repeat = await V275.GetLatestRepeat();
            if (repeat == -9999)
                return;

            if (!await V275.Commands.RemoveRepeat(repeat))
            {
                return;
            }

            if (!await V275.Commands.ResumeJob())
            {
                return;
            }
        }

        private async void StartRunAction(object parameter)
        {
            if (!StartRunCheck())
                if (await OkCancelDialog("Missing Label Sectors", "There are Labels that do not have stored sectors. Are you sure you want to continue?") == MessageDialogResult.Negative)
                    return;

            if (CurrentRun != null)
            {
                CurrentRun.RunStateChange -= CurrentRun_RunStateChange;
                CurrentRun.Close();
                CurrentRun = null;
            }

            Logger.Info("Starting Run: {stand}; {count}", Labels[0].GradingStandard, RunLoopCount);

            CurrentRun = new RunController(Labels, RunLoopCount, StandardsDatabase, V275.Commands.Product.part, SelectedNode.cameraMAC).Init();
            CurrentRun.RunStateChange += CurrentRun_RunStateChange;

            if (CurrentRun == null)
                return;

            CurrentRun.StartAsync();
        }
        private bool StartRunCheck()
        {
            foreach (var lab in Labels)
                if (lab.LabelSectors.Count == 0)
                    return false;
            return true;
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

            switch (state)
            {
                case RunController.RunStates.RUNNING:
                    IsRunRunning = true;
                    IsRunPaused = false;
                    RunState = state;
                    break;
                case RunController.RunStates.PAUSED:
                    IsRunRunning = true;
                    IsRunPaused = true;
                    RunState = state;
                    break;
                case RunController.RunStates.STOPPED:
                    IsRunRunning = false;
                    IsRunPaused = false;
                    RunState = RunController.RunStates.IDLE;
                    break;
                default:
                    IsRunRunning = false;
                    IsRunPaused = false;
                    RunState = RunController.RunStates.IDLE;
                    break;
            }
        }
    }
}
