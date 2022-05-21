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

namespace V275_Testing.WindowViewModels
{
    public class MainWindowViewModel : Core.BaseViewModel
    {
        private V275_API_Commands V275 = new V275_API_Commands();
        private StandardsDatabase StandardsDatabase { get; }
        private V275_API_WebSocketEvents WebSocket { get; } = new V275_API_WebSocketEvents();
        private V275_API_WebSocketEvents SysWebSocket { get; } = new V275_API_WebSocketEvents();

        public string V275_Host { get => V275.Host = App.Settings.GetValue("V275_Host", "127.0.0.1"); set { App.Settings.SetValue("V275_Host", value); V275.Host = value; } }
        public string V275_SystemPort { get => V275.SystemPort = App.Settings.GetValue("V275_SystemPort", "8080"); set { App.Settings.SetValue("V275_SystemPort", value); ; V275.SystemPort = value; } }
        public string V275_NodeNumber { get => V275.NodeNumber = App.Settings.GetValue("V275_NodeNumber", "1"); set { App.Settings.SetValue("V275_NodeNumber", value); V275.NodeNumber = value; } }

        public ObservableCollection<V275_Devices.Node> Nodes { get; } = new ObservableCollection<V275_Devices.Node>();
        public V275_Devices.Node SelectedNode { get => selectedNode; set { SetProperty(ref selectedNode, value); if (value != null) V275_NodeNumber = value.enumeration.ToString(); } }
        private V275_Devices.Node selectedNode;

        public ObservableCollection<LabelControlViewModel> Labels { get; } = new ObservableCollection<LabelControlViewModel>();

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
        public bool IsLoggedIn
        {
            get => IsSetup || IsRun;
        }
        public bool IsNotLoggedIn => !(IsSetup || IsRun);

        public bool IsSetup
        {
            get => isSetup;
            set { SetProperty(ref isSetup, value); OnPropertyChanged("IsNotSetup"); OnPropertyChanged("IsNotLoggedIn"); OnPropertyChanged("IsLoggedIn"); }
        }
        public bool IsNotSetup => !isSetup;
        private bool isSetup = false;

        public bool IsRun
        {
            get => isRun;
            set { SetProperty(ref isRun, value); OnPropertyChanged("IsNotRun"); OnPropertyChanged("IsNotLoggedIn"); OnPropertyChanged("IsLoggedIn"); }
        }
        public bool IsNotRun => !isRun;
        private bool isRun = false;

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

        public bool V275_IsBackupVoid => V275.ConfigurationCamera.backupVoidMode.value == "ON";

        public ICommand Start { get; }
        public bool IsStarted
        {
            get => isStarted;
            set { SetProperty(ref isStarted, value); OnPropertyChanged("IsNotStarted"); }
        }
        public bool IsNotStarted => !isStarted;
        private bool isStarted = false;


        public ICommand Stop { get; }

        public ICommand Print { get; }

        public MainWindowViewModel()
        {
            GetDevices = new Core.RelayCommand(GetDevicesAction, c => true);
            LoginMonitor = new Core.RelayCommand(LoginMonitorAction, c => true);
            LoginControl = new Core.RelayCommand(LoginControlAction, c => true);
            Logout = new Core.RelayCommand(LogoutAction, c => true);
            Start = new Core.RelayCommand(StartAction, c => true);
            Stop = new Core.RelayCommand(StopAction, c => true);

            Print = new Core.RelayCommand(PrintAction, c => true);

            StandardsDatabase = new StandardsDatabase($"{App.UserDataDirectory}\\{App.StandardsDatabaseName}");

            LoadPrinters();
            SetupGradingStandards();
            
        }

        private void LoadPrinters()
        {
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                Printers.Add(p);

                if (StoredPrinter == p)
                    SelectedPrinter = p;
            }

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
            Labels.Clear();

            List<string> Images = new List<string>();
            Images.Clear();
            foreach (var f in Directory.EnumerateFiles($"{App.StandardsRoot}\\{StoredStandard}\\600\\"))
                Images.Add(f);

            Images.Sort();

            int i = 1;
            foreach (var img in Images)
            {
                var tmp = new LabelControlViewModel(i++, img, SelectedPrinter, SelectedStandard, StandardsDatabase, V275, MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance);

                tmp.Printing += Label_Printing;

                tmp.IsRun = IsRun;
                tmp.IsSetup = IsSetup;

                Labels.Add(tmp);
            }
        }

        public class Repeat
        {
            public LabelControlViewModel Label { get; set; }

            public int RepeatNumber { get; set; } = -1;

            public Repeat(LabelControlViewModel label)
            {
                Label = label;
            }
        }

        private Dictionary<int, Repeat> Repeats = new Dictionary<int, Repeat>();

        private int LabelCount { get; set; } = 1;

        private async void ResetRepeats()
        {
            Repeats.Clear();

            await V275.GetRepeatsAvailable();

            if (V275.Available != null && V275.Available.Count > 0)
                LabelCount = V275.Available[0];
            else
                LabelCount = 1;
        }

        private void Reset()
        {
            UserMessage = "";
        }

        private async void GetDevicesAction(object parameter)
        {
            Reset();

            Nodes.Clear();
            SelectedNode = null;

            if (await V275.GetDevices())
            {
                foreach (var node in V275.Devices.nodes)
                {
                    Nodes.Add(node);
                    if (node.enumeration.ToString() == V275_NodeNumber)
                        SelectedNode = node;
                }

                if (SelectedNode == null && Nodes.Count > 0)
                    SelectedNode = Nodes.First();

                IsGetDevices = true;
            }
            else
            {
                UserMessage = V275.Status;
                IsGetDevices = false;
            }

        }

        private void SetupGradingStandards()
        {
            Standards.Clear();
            SelectedStandard = null;

            foreach (var dir in Directory.EnumerateDirectories(App.StandardsRoot))
            {
                Standards.Add(dir.Substring(dir.LastIndexOf("\\") + 1));

            }

            List<string> tables = StandardsDatabase.GetAllTables();

            foreach (var standard in Standards)
            {
                if (tables.Contains(standard))
                    continue;

                StandardsDatabase.CreateTable(standard);
            }

            if (Standards.Contains(StoredStandard))
                SelectedStandard = StoredStandard;
            else if (Standards.Count > 0)
                SelectedStandard = Standards.First();

            //if (await V275.GetGradingStandards())
            //{
            //    foreach (var standard in V275.GradingStandards.gradingStandards)
            //    {
            //        string s = $"{standard.standard} Table {standard.tableId}";

            //        if (!Standards.Contains(s))
            //        {
            //            if (Directory.Exists($"{App.StandardsRoot}\\{s}"))
            //                Standards.Add(s);
            //            else
            //                s = "";
            //        }
            //        else
            //            continue;

            //        if (StoredStandard == s)
            //            SelectedStandard = StoredStandard;
            //    }

            //    if (SelectedStandard == null && Standards.Count > 0)
            //        SelectedStandard = Standards.First();
            //}
            //else
            //{
            //    Status = V275.Status;
            //}
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
                IsSetup = false;
            }
        }

        private async Task PostLogin(bool isSetup)
        {
            LoginData.accessLevel = isSetup ? "monitor" : "control";
            LoginData.token = V275.Token;
            LoginData.id = UserName;
            LoginData.state = "0";

            IsSetup = isSetup;
            IsRun = !isSetup;

            foreach (var rep in Labels)
            {
                rep.IsSetup = IsSetup;
                rep.IsRun = IsRun;
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

        private void WebSocket_Heartbeat(V275_Events_System ev)
        {
            string state = char.ToUpper(ev.data.state[0]) + ev.data.state.Substring(1);

            if (V275_State != state)
            {
                V275_State = state;

                if (V275_State == "Editing")
                {
                    ResetRepeats();
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
                    ResetRepeats();
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

                if (IsRun)
                    if (!Repeats.ContainsKey(ev.data.repeat + 1))
                        App.Current.Dispatcher.Invoke(new Action(() => ProcessRepeat(ev.data.repeat)));
            }
            else
            {
                LabelCount = ev.data.repeat;
                return;
            }

            if (IsRun)
                PrintAction("1");
        }

        private void Label_Printing(LabelControlViewModel label)
        {
            if (V275_State == "Editing")
            {
                for (int i = 0; i < label.PrintCount; i++)
                    Repeats.Add(++LabelCount, new Repeat(label));

                if (IsRun)
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
                while (!detectLock) { };

                await Repeats[repeat].Label.CreateSectors(detectEvent, StoredStandard);
            }
                

            await Repeats[repeat].Label.Read(repeat);
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
                IsRun = false;
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

            IsRun = false;
            IsSetup = false;

            foreach (var rep in Labels)
            {
                rep.IsRun = IsRun;
                rep.IsSetup = IsSetup;
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

        private async void PrintAction(object parameter)
        {
            if (V275_IsBackupVoid)
                await V275.Print(false);

            Thread.Sleep(200);

            await V275.Print((string)parameter == "1");
        }

        private async void StartAction(object parameter)
        {
            Reset();
            if (!await V275.GetJob())
            {
                UserMessage = V275.Status;
                return;
            }
            if (!await V275.GetReport())
            {
                UserMessage = V275.Status;
                return;
            }

            //  Printer.PrintControl.Print(System.IO.Directory.GetCurrentDirectory() + "\\Assets\\GS1\\Table 1\\600\\", 1);

            //bool res = true;
            //string result = await V275_API_Connection.Put(V275_API_URLs.Print(), V275_API_URLs.Print_Body(true), Token);
            //Thread.Sleep(200);
            //result = await V275_API_Connection.Put(V275_API_URLs.Print(), V275_API_URLs.Print_Body(false), Token);
            //result = await V275_API_Connection.Put(V275_API_URLs.History("2"), "", Token);
            //string result1 = await V275_API_Connection.Put(V275_API_URLs.Inspect(), "", Token);

            //Thread.Sleep(1000);
            //string report = await V275_API_Connection.Get(V275_API_URLs.Report(), Token);

            //if (string.IsNullOrEmpty(report))
            //{
            //    res = false;
            //    Status = "No data returned!";
            //}
            //if (V275_API_Connection.IsException)
            //{
            //    res = false;
            //    Status = V275_API_Connection.Exception.Message;
            //}

            //if (res)
            //{
            //    V275_Report job = JsonConvert.DeserializeObject<V275_Report>(report);

            //}
        }



        private void StopAction(object parameter)
        {

        }

    }
}
